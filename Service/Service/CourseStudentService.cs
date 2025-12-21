using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Repository.IRepository;
using Repository.Repository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.CourseStudent;
using Service.RequestAndResponse.Response.CourseInstance;
using Service.RequestAndResponse.Response.CourseStudent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class CourseStudentService : ICourseStudentService
    {
        private readonly ICourseStudentRepository _courseStudentRepository;
        private readonly ICourseInstanceRepository _courseInstanceRepository;
        private readonly IUserRepository _userRepository;
        private readonly ASDPRSContext _context;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IReviewAssignmentRepository _reviewAssignmentRepository;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;
        private readonly ICourseInstructorRepository _courseInstructorRepository;


        public CourseStudentService(
            ICourseStudentRepository courseStudentRepository,
            ICourseInstanceRepository courseInstanceRepository,
            IUserRepository userRepository,
            ASDPRSContext context,
            IAssignmentRepository assignmentRepository,
            ISubmissionRepository submissionRepository,
            IReviewRepository reviewRepository,
            IReviewAssignmentRepository reviewAssignmentRepository,
            IMapper mapper,
            UserManager<User> userManager,
            IEmailService emailService,
            ICourseInstructorRepository courseInstructorRepository)
        {
            _courseStudentRepository = courseStudentRepository;
            _courseInstanceRepository = courseInstanceRepository;
            _userRepository = userRepository;
            _context = context;
            _assignmentRepository = assignmentRepository;
            _submissionRepository = submissionRepository;
            _reviewRepository = reviewRepository;
            _reviewAssignmentRepository = reviewAssignmentRepository;
            _mapper = mapper;
            _userManager = userManager;
            _emailService = emailService;
            _courseInstructorRepository = courseInstructorRepository;
        }

        public async Task<BaseResponse<List<CourseStudentResponse>>> ImportStudentsFromExcelAsync(int courseInstanceId, Stream fileStream, int? changedByUserId)
        {
            try
            {
                var courseInstance = await _context.CourseInstances
                    .Include(ci => ci.Course)
                    .FirstOrDefaultAsync(ci => ci.CourseInstanceId == courseInstanceId);

                if (courseInstance == null)
                    return new BaseResponse<List<CourseStudentResponse>>("Course instance not found", StatusCodeEnum.NotFound_404, null);
                int currentCourseId = courseInstance.CourseId;
                int currentSemesterId = courseInstance.SemesterId;

                var deadline = courseInstance.StartDate.AddDays(14);
                bool isPastDeadline = DateTime.UtcNow.AddHours(7) > deadline;

                var results = new List<CourseStudentResponse>();
                var errors = new List<string>();
                int instructorCount = 0;

                User changedBy = null;
                if (changedByUserId.HasValue) changedBy = await _userManager.FindByIdAsync(changedByUserId.Value.ToString());

                using (var package = new ExcelPackage(fileStream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension?.Rows ?? 0;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var code = worksheet.Cells[row, 2].Value?.ToString().Trim();
                        var surname = worksheet.Cells[row, 3].Value?.ToString().Trim();
                        var middle = worksheet.Cells[row, 4].Value?.ToString().Trim();
                        var given = worksheet.Cells[row, 5].Value?.ToString().Trim();
                        var email = worksheet.Cells[row, 6].Value?.ToString().Trim();
                        var majorCode = worksheet.Cells[row, 7].Value?.ToString().Trim();
                        var excelRole = worksheet.Cells[row, 8].Value?.ToString().Trim();
                        var campusVal = worksheet.Cells[row, 9].Value?.ToString()?.Trim();

                        if (string.IsNullOrEmpty(email)) continue;

                        if (int.TryParse(campusVal, out int excelCampusId))
                        {
                            if (excelCampusId != courseInstance.CampusId)
                            {
                                errors.Add($"{email}: Campus mismatch (File {excelCampusId} vs Class {courseInstance.CampusId})");
                                continue;
                            }
                        }

                        int? majorId = null;
                        if (!string.IsNullOrEmpty(majorCode))
                        {
                            var major = await _context.Majors.FirstOrDefaultAsync(m => m.MajorCode == majorCode);
                            if (major != null) majorId = major.MajorId;
                        }

                        var user = await _userManager.FindByEmailAsync(email);
                        bool isNewUser = false;
                        string generatedPassword = null;

                        if (user != null)
                        {

                            if (!string.IsNullOrEmpty(code) && user.StudentCode != code)
                            {
                                var duplicate = await _context.Users.FirstOrDefaultAsync(u => u.StudentCode == code);
                                if (duplicate != null)
                                {
                                    errors.Add($"{email}: Cannot update code. StudentCode '{code}' belongs to another user.");
                                    continue;
                                }
                                user.StudentCode = code;
                            }

                            user.FirstName = given;
                            user.LastName = $"{surname} {middle}".Trim();
                            user.CampusId = courseInstance.CampusId;

                            var isSystemInstructor = await _userManager.IsInRoleAsync(user, "Instructor");
                            if (isSystemInstructor)
                            {

                            }
                            else
                            {
                                if (majorId.HasValue) user.MajorId = majorId.Value;
                            }

                            await _userManager.UpdateAsync(user);
                        }
                        else
                        {

                            if (!string.IsNullOrEmpty(code) && user.StudentCode != code)
                            {
                                errors.Add($"{email}: StudentCode mismatch. Cannot change from '{user.StudentCode}' to '{code}'.");
                                continue;
                            }


                            string roleToAssign = string.Equals(excelRole, "Instructor", StringComparison.OrdinalIgnoreCase) ? "Instructor" : "Student";

                            if (roleToAssign == "Instructor")
                            {
                                majorId = null;
                            }
                            else
                            {
                                if (!majorId.HasValue)
                                {
                                    errors.Add($"{email}: Major is required for new Students.");
                                    continue;
                                }
                            }

                            user = new User
                            {
                                UserName = email.Split('@')[0],
                                Email = email,
                                StudentCode = code,
                                FirstName = given,
                                LastName = $"{surname} {middle}".Trim(),
                                CampusId = courseInstance.CampusId,
                                MajorId = majorId,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow.AddHours(7)
                            };

                            generatedPassword = GenerateRandomPassword();
                            var result = await _userManager.CreateAsync(user, generatedPassword);

                            if (!result.Succeeded)
                            {
                                errors.Add($"{email}: Failed to create user - {result.Errors.First().Description}");
                                continue;
                            }

                            await _userManager.AddToRoleAsync(user, roleToAssign);
                            isNewUser = true;
                        }

                        if (isNewUser && !string.IsNullOrEmpty(generatedPassword))
                        {
                            var roleForEmail = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Student";
                            await SendWelcomeEmailAsync(user, generatedPassword, roleForEmail);
                        }

                        var currentRoles = await _userManager.GetRolesAsync(user);
                        bool isInstructor = currentRoles.Contains("Instructor");

                        if (isInstructor)
                        {
                            var existingInstructor = await _context.CourseInstructors
                                .FirstOrDefaultAsync(ci => ci.CourseInstanceId == courseInstanceId && ci.UserId == user.Id);

                            if (existingInstructor == null)
                            {
                                var ci = new CourseInstructor
                                {
                                    CourseInstanceId = courseInstanceId,
                                    UserId = user.Id
                                };
                                await _courseInstructorRepository.AddAsync(ci);
                                instructorCount++;
                            }
                        }
                        else
                        {
                            if (isPastDeadline)
                            {
                                errors.Add($"{email}: Past enrollment deadline.");
                                continue;
                            }

                            var existingStudent = await _context.CourseStudents
                                .FirstOrDefaultAsync(cs => cs.CourseInstanceId == courseInstanceId && cs.UserId == user.Id);

                            if (existingStudent == null)
                            {
                                bool isTimeOverlapping = await IsStudentInOverlappingCourseAsync(
                                    user.Id,
                                    courseInstance.CourseId,
                                    courseInstance.StartDate,
                                    courseInstance.EndDate
                                );

                                if (isTimeOverlapping)
                                {
                                    errors.Add($"{email}: Student is already enrolled in this course during this time range.");
                                    continue;
                                }
                                var cs = new CourseStudent
                                {
                                    CourseInstanceId = courseInstanceId,
                                    UserId = user.Id,
                                    EnrolledAt = DateTime.UtcNow.AddHours(7),
                                    Status = "Enrolled",
                                    ChangedByUserId = changedByUserId
                                };
                                cs.Status = "Pending";

                                var created = await _courseStudentRepository.AddAsync(cs);
                                var response = MapToResponse(created, courseInstance, user, changedBy);
                                response.Role = "Student";
                                results.Add(response);
                            }
                        }
                    }
                }

                string message = $"Processed. Added {results.Count} students, {instructorCount} instructors.";
                if (errors.Any()) message += $" Skipped: {string.Join("; ", errors.Take(3))}";

                return new BaseResponse<List<CourseStudentResponse>>(message, StatusCodeEnum.Created_201, results);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<CourseStudentResponse>>($"Error importing: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
        public async Task<BaseResponse<MultipleCourseImportResponse>> ImportStudentsFromMultipleSheetsAsync(int campusId, Stream fileStream, int? changedByUserId)
        {
            try
            {
                var results = new MultipleCourseImportResponse
                {
                    SheetResults = new List<SheetImportResult>()
                };

                User changedBy = null;
                if (changedByUserId.HasValue) changedBy = await _userManager.FindByIdAsync(changedByUserId.Value.ToString());

                using (var package = new ExcelPackage(fileStream))
                {
                    foreach (var worksheet in package.Workbook.Worksheets)
                    {
                        var sheetResult = new SheetImportResult
                        {
                            SheetName = worksheet.Name,
                            ImportedStudents = new List<CourseStudentResponse>()
                        };

                        var courseInstance = await _context.CourseInstances
                            .Include(ci => ci.Course)
                            .FirstOrDefaultAsync(ci => ci.SectionCode == worksheet.Name && ci.CampusId == campusId);

                        if (courseInstance == null)
                        {
                            sheetResult.Message = $"Course instance not found for section: {worksheet.Name} in campus {campusId}";
                            results.SheetResults.Add(sheetResult);
                            continue;
                        }

                        sheetResult.CourseInstanceId = courseInstance.CourseInstanceId;
                        sheetResult.CourseName = courseInstance.Course?.CourseName;

                        var rowCount = worksheet.Dimension?.Rows ?? 0;
                        int successCount = 0;
                        int instructorCount = 0;
                        var skippedStudents = new List<string>();

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var code = worksheet.Cells[row, 2].Value?.ToString().Trim();
                            var surname = worksheet.Cells[row, 3].Value?.ToString().Trim();
                            var middle = worksheet.Cells[row, 4].Value?.ToString().Trim();
                            var given = worksheet.Cells[row, 5].Value?.ToString().Trim();
                            var email = worksheet.Cells[row, 6].Value?.ToString().Trim();
                            var majorCode = worksheet.Cells[row, 7].Value?.ToString().Trim();
                            var role = worksheet.Cells[row, 8].Value?.ToString().Trim();
                            var campusVal = worksheet.Cells[row, 9].Value?.ToString()?.Trim();

                            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(email)) continue;

                            if (int.TryParse(campusVal, out int excelCampusId))
                            {
                                if (excelCampusId != courseInstance.CampusId)
                                {
                                    skippedStudents.Add($"{email} (Campus mismatch)");
                                    continue;
                                }
                            }

                            try
                            {
                                var user = await _userManager.FindByEmailAsync(email);
                                if (user == null)
                                {
                                    // SỬ DỤNG HÀM MỚI (đã có tham số role)
                                    user = await CreateUserFromImportAsync(code, surname, middle, given, email, campusId, role, majorCode);
                                }

                                if (user != null)
                                {
                                    if (user.CampusId != courseInstance.CampusId)
                                    {
                                        skippedStudents.Add($"{email} (User Campus mismatch)");
                                        continue;
                                    }

                                    // Logic Instructor vs Student
                                    if (string.Equals(role, "Instructor", StringComparison.OrdinalIgnoreCase))
                                    {
                                        var existingInstructor = await _context.CourseInstructors
                                            .FirstOrDefaultAsync(ci => ci.CourseInstanceId == courseInstance.CourseInstanceId && ci.UserId == user.Id);
                                        if (existingInstructor == null)
                                        {
                                            var ci = new CourseInstructor
                                            {
                                                CourseInstanceId = courseInstance.CourseInstanceId,
                                                UserId = user.Id
                                            };
                                            await _courseInstructorRepository.AddAsync(ci);
                                            instructorCount++;
                                        }
                                    }
                                    else // Student
                                    {
                                        var existingCourseStudent = await _context.CourseStudents
                                            .FirstOrDefaultAsync(cs => cs.CourseInstanceId == courseInstance.CourseInstanceId && cs.UserId == user.Id);

                                        if (existingCourseStudent == null)
                                        {
                                            var courseStudent = new CourseStudent
                                            {
                                                CourseInstanceId = courseInstance.CourseInstanceId,
                                                UserId = user.Id,
                                                EnrolledAt = DateTime.UtcNow.AddHours(7),
                                                Status = "Enrolled",
                                                ChangedByUserId = changedByUserId
                                            };
                                            var created = await _courseStudentRepository.AddAsync(courseStudent);
                                            var response = MapToResponse(created, courseInstance, user, changedBy);
                                            sheetResult.ImportedStudents.Add(response);
                                            successCount++;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error importing student {code}: {ex.Message}");
                            }
                        }

                        sheetResult.SuccessCount = successCount;
                        sheetResult.Message = $"Imported {successCount} students and {instructorCount} instructors.";
                        if (skippedStudents.Any())
                        {
                            sheetResult.Message += $" Skipped {skippedStudents.Count} due to campus mismatch.";
                        }
                        results.SheetResults.Add(sheetResult);
                        results.TotalSuccessCount += successCount;
                    }
                }
                return new BaseResponse<MultipleCourseImportResponse>("Import completed with details in sheet results", StatusCodeEnum.OK_200, results);
            }
            catch (Exception ex)
            {
                return new BaseResponse<MultipleCourseImportResponse>($"Error during multiple sheet import: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
        private async Task<User> CreateUserFromImportAsync(string code, string surname, string middleName, string givenName, string email, int campusId, string role, string majorCode = null)
        {
            try
            {
                var user = new User
                {
                    UserName = email.Split('@')[0],
                    Email = email,
                    StudentCode = code,
                    FirstName = givenName,
                    LastName = $"{surname} {middleName}".Trim(),
                    CampusId = campusId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddHours(7)
                };

                if (!string.IsNullOrEmpty(majorCode))
                {
                    var major = await _context.Majors.FirstOrDefaultAsync(m => m.MajorCode == majorCode);
                    if (major != null) user.MajorId = major.MajorId;
                }

                var password = GenerateRandomPassword();
                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    string roleToAssign = string.Equals(role, "Instructor", StringComparison.OrdinalIgnoreCase) ? "Instructor" : "Student";
                    await _userManager.AddToRoleAsync(user, roleToAssign);
                    // Gọi hàm gửi mail mới với đủ 3 tham số
                    await SendWelcomeEmailAsync(user, password, roleToAssign);
                    return user;
                }
                return null;
            }
            catch { return null; }
        }
        private string GenerateRandomPassword(int length = 12)
        {
            const string lowers = "abcdefghijklmnopqrstuvwxyz";
            const string uppers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string specials = "!@#$%^&*()_-+=[{]};:>|./?";
            const string allChars = lowers + uppers + digits + specials;

            var random = new Random();
            var passwordChars = new List<char>();

            passwordChars.Add(lowers[random.Next(lowers.Length)]);
            passwordChars.Add(uppers[random.Next(uppers.Length)]);
            passwordChars.Add(digits[random.Next(digits.Length)]);
            passwordChars.Add(specials[random.Next(specials.Length)]);

            for (int i = passwordChars.Count; i < length; i++)
            {
                passwordChars.Add(allChars[random.Next(allChars.Length)]);
            }

            return new string(passwordChars.OrderBy(x => random.Next()).ToArray());
        }
        public async Task<BaseResponse<CourseStudentResponse>> EnrollStudentAsync(int courseInstanceId, int studentUserId, string enrollKey)
        {
            try
            {
                var courseInstance = await _courseInstanceRepository.GetByIdAsync(courseInstanceId);

                if (courseInstance == null || courseInstance.EnrollmentPassword != enrollKey)
                    return new BaseResponse<CourseStudentResponse>("Invalid enrollment key or course instance not found", StatusCodeEnum.BadRequest_400, null);
                if (DateTime.UtcNow.AddHours(7) < courseInstance.StartDate)
                {
                    return new BaseResponse<CourseStudentResponse>(
                        $"The course has not started yet. You can only enroll starting from {courseInstance.StartDate:dd/MM/yyyy HH:mm}",
                        StatusCodeEnum.Forbidden_403,
                        null);
                }

                var courseStudent = (await _courseStudentRepository.GetByCourseInstanceIdAsync(courseInstanceId))
                    .FirstOrDefault(cs => cs.UserId == studentUserId && cs.Status == "Pending");

                if (courseStudent == null)
                    return new BaseResponse<CourseStudentResponse>("You have not been added to the pending list or are already enrolled", StatusCodeEnum.NotFound_404, null);

                courseStudent.Status = "Enrolled";
                courseStudent.EnrolledAt = DateTime.UtcNow.AddHours(7);

                await _courseStudentRepository.UpdateAsync(courseStudent);

                var user = await _userRepository.GetByIdAsync(studentUserId);
                var response = MapToResponse(courseStudent, courseInstance, user, null);

                return new BaseResponse<CourseStudentResponse>("Enrollment successful", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseStudentResponse>("Enrollment error: " + ex.Message, StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<CourseStudentResponse>> CreateCourseStudentAsync(CreateCourseStudentRequest request)
        {
            try
            {
                var courseInstance = await _courseInstanceRepository.GetByIdAsync(request.CourseInstanceId);
                if (courseInstance == null)
                    return new BaseResponse<CourseStudentResponse>("Course instance not found", StatusCodeEnum.NotFound_404, null);

                var deadline = courseInstance.StartDate.AddDays(14);
                if (DateTime.UtcNow.AddHours(7) > deadline)
                {
                    return new BaseResponse<CourseStudentResponse>("Cannot add students after 14 days from the course start date.", StatusCodeEnum.Forbidden_403, null);
                }

                if (!string.IsNullOrEmpty(request.StudentCode) && request.UserId == 0)
                {
                    var userByCode = await _userRepository.GetByStudentCodeAsync(request.StudentCode);
                    if (userByCode == null)
                        return new BaseResponse<CourseStudentResponse>("Student not found with the provided student code", StatusCodeEnum.NotFound_404, null);
                    request.UserId = userByCode.Id;
                }
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                    return new BaseResponse<CourseStudentResponse>("User not found", StatusCodeEnum.BadRequest_400, null);

                if (!await _userManager.IsInRoleAsync(user, "Student"))
                    return new BaseResponse<CourseStudentResponse>("User does not have the Student role", StatusCodeEnum.BadRequest_400, null);

                if (user.CampusId != courseInstance.CampusId)
                {
                    return new BaseResponse<CourseStudentResponse>("Student belongs to a different campus than the course.", StatusCodeEnum.Conflict_409, null);
                }
                // Check if student is already enrolled
                var existing = (await _courseStudentRepository.GetByCourseInstanceIdAsync(request.CourseInstanceId))
                    .FirstOrDefault(cs => cs.UserId == request.UserId);
                if (existing != null)
                    return new BaseResponse<CourseStudentResponse>("Student is already enrolled in this course instance", StatusCodeEnum.Conflict_409, null);

                bool isTimeOverlapping = await IsStudentInOverlappingCourseAsync(
                    request.UserId,
                    courseInstance.CourseId,
                    courseInstance.StartDate,
                    courseInstance.EndDate
                );

                if (isTimeOverlapping)
                {
                    return new BaseResponse<CourseStudentResponse>("Student is already taking this course in an overlapping time range.", StatusCodeEnum.Conflict_409, null);
                }
                var courseStudent = new CourseStudent
                {
                    CourseInstanceId = request.CourseInstanceId,
                    UserId = request.UserId,
                    EnrolledAt = DateTime.UtcNow.AddHours(7),
                    Status = request.Status ?? "Enrolled",
                    ChangedByUserId = request.ChangedByUserId,
                    StatusChangedAt = DateTime.UtcNow.AddHours(7)
                };
                await _courseStudentRepository.AddAsync(courseStudent);
                User changedByUser = null;
                if (request.ChangedByUserId.HasValue) changedByUser = await _userRepository.GetByIdAsync(request.ChangedByUserId.Value);

                var response = MapToResponse(courseStudent, courseInstance, user, changedByUser);
                return new BaseResponse<CourseStudentResponse>("Student added successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseStudentResponse>($"Error creating course student: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
        public async Task<BaseResponse<bool>> DeleteCourseStudentAsync(int courseStudentId, int courseInstanceId, int userId)
        {
            try
            {
                var courseInstance = await _courseInstanceRepository.GetByIdAsync(courseInstanceId);
                if (courseInstance == null)
                    return new BaseResponse<bool>("Course instance not found", StatusCodeEnum.NotFound_404, false);

                var deadline = courseInstance.StartDate.AddDays(14);
                if (DateTime.UtcNow.AddHours(7) > deadline)
                {
                    return new BaseResponse<bool>("Cannot remove students after 14 days from the course start date.", StatusCodeEnum.Forbidden_403, false);
                }

                var courseStudent = await _courseStudentRepository.GetByIdAsync(courseStudentId);
                if (courseStudent == null)
                    return new BaseResponse<bool>("Record not found", StatusCodeEnum.NotFound_404, false);

                if (courseStudent.UserId != userId || courseStudent.CourseInstanceId != courseInstanceId)
                    return new BaseResponse<bool>("Mismatch information", StatusCodeEnum.BadRequest_400, false);

                await _courseStudentRepository.DeleteAsync(courseStudent);
                return new BaseResponse<bool>("Student removed successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error removing student: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }
        public async Task<BaseResponse<CourseStudentResponse>> GetCourseStudentByIdAsync(int id)
        {
            try
            {
                var courseStudent = await _courseStudentRepository.GetByIdAsync(id);
                if (courseStudent == null)
                {
                    return new BaseResponse<CourseStudentResponse>(
                        "Course student not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var courseInstance = await _courseInstanceRepository.GetByIdWithRelationsAsync(courseStudent.CourseInstanceId);
                var user = await _userRepository.GetByIdAsync(courseStudent.UserId);
                User changedByUser = null;
                if (courseStudent.ChangedByUserId.HasValue)
                {
                    changedByUser = await _userRepository.GetByIdAsync(courseStudent.ChangedByUserId.Value);
                }

                var response = MapToResponse(courseStudent, courseInstance, user, changedByUser);
                return new BaseResponse<CourseStudentResponse>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseStudentResponse>(
                    $"Error retrieving course student: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<CourseStudentResponse>>> GetCourseStudentsByCourseInstanceAsync(int courseInstanceId)
        {
            try
            {
                var courseStudents = await _courseStudentRepository.GetByCourseInstanceIdAsync(courseInstanceId);
                var responses = new List<CourseStudentResponse>();

                foreach (var cs in courseStudents)
                {
                    var courseInstance = await _courseInstanceRepository.GetByIdAsync(cs.CourseInstanceId);
                    var user = await _userRepository.GetByIdAsync(cs.UserId);
                    User changedByUser = null;
                    if (cs.ChangedByUserId.HasValue)
                    {
                        changedByUser = await _userRepository.GetByIdAsync(cs.ChangedByUserId.Value);
                    }
                    responses.Add(MapToResponse(cs, courseInstance, user, changedByUser));
                }

                return new BaseResponse<List<CourseStudentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<CourseStudentResponse>>(
                    $"Error retrieving course students: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<CourseStudentResponse>>> GetCourseStudentsByStudentAsync(int studentId)
        {
            try
            {
                var courseStudents = await _courseStudentRepository.GetByUserIdAsync(studentId);
                var responses = new List<CourseStudentResponse>();

                var courseInstanceIds = courseStudents.Select(cs => cs.CourseInstanceId).Distinct().ToList();

                var studentCounts = await _context.CourseStudents
                    .Where(cs => courseInstanceIds.Contains(cs.CourseInstanceId) && cs.Status == "Enrolled")
                    .GroupBy(cs => cs.CourseInstanceId)
                    .Select(g => new { CourseInstanceId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.CourseInstanceId, x => x.Count);

                var instructors = await _context.CourseInstructors
                    .Where(ci => courseInstanceIds.Contains(ci.CourseInstanceId))
                    .Include(ci => ci.User)
                    .GroupBy(ci => ci.CourseInstanceId)
                    .Select(g => new
                    {
                        CourseInstanceId = g.Key,
                        InstructorNames = g.Select(ci => $"{ci.User.FirstName} {ci.User.LastName}".Trim()).ToList()
                    })
                    .ToDictionaryAsync(x => x.CourseInstanceId, x => x.InstructorNames);

                foreach (var cs in courseStudents)
                {
                    var courseInstance = await _courseInstanceRepository.GetByIdWithRelationsAsync(cs.CourseInstanceId);
                    var user = await _userRepository.GetByIdAsync(cs.UserId);
                    User changedByUser = null;
                    if (cs.ChangedByUserId.HasValue)
                    {
                        changedByUser = await _userRepository.GetByIdAsync(cs.ChangedByUserId.Value);
                    }

                    var response = MapToResponse(cs, courseInstance, user, changedByUser);

                    response.StudentCount = studentCounts.GetValueOrDefault(cs.CourseInstanceId, 0);
                    response.InstructorNames = instructors.GetValueOrDefault(cs.CourseInstanceId, new List<string>());

                    responses.Add(response);
                }

                return new BaseResponse<List<CourseStudentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<CourseStudentResponse>>(
                    $"Error retrieving course students: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }
        public async Task<BaseResponse<CourseStudentResponse>> UpdateCourseStudentStatusAsync(int courseStudentId, string status, int changedByUserId)
        {
            try
            {
                var courseStudent = await _courseStudentRepository.GetByIdAsync(courseStudentId);
                if (courseStudent == null)
                {
                    return new BaseResponse<CourseStudentResponse>(
                        "Course student not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                courseStudent.Status = status;
                courseStudent.StatusChangedAt = DateTime.UtcNow.AddHours(7);
                courseStudent.ChangedByUserId = changedByUserId;

                await _courseStudentRepository.UpdateAsync(courseStudent);

                var courseInstance = await _courseInstanceRepository.GetByIdAsync(courseStudent.CourseInstanceId);
                var user = await _userRepository.GetByIdAsync(courseStudent.UserId);
                User changedByUser = await _userRepository.GetByIdAsync(changedByUserId);

                var response = MapToResponse(courseStudent, courseInstance, user, changedByUser);
                return new BaseResponse<CourseStudentResponse>(
                    "Course student status updated successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseStudentResponse>(
                    $"Error updating course student status: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<CourseStudentResponse>> UpdateCourseStudentGradeAsync(int courseStudentId, decimal? finalGrade, bool isPassed, int changedByUserId)
        {
            try
            {
                var courseStudent = await _courseStudentRepository.GetByIdAsync(courseStudentId);
                if (courseStudent == null)
                {
                    return new BaseResponse<CourseStudentResponse>(
                        "Course student not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                courseStudent.FinalGrade = finalGrade;
                courseStudent.IsPassed = isPassed;
                courseStudent.StatusChangedAt = DateTime.UtcNow.AddHours(7);
                courseStudent.ChangedByUserId = changedByUserId;

                await _courseStudentRepository.UpdateAsync(courseStudent);

                var courseInstance = await _courseInstanceRepository.GetByIdAsync(courseStudent.CourseInstanceId);
                var user = await _userRepository.GetByIdAsync(courseStudent.UserId);
                User changedByUser = await _userRepository.GetByIdAsync(changedByUserId);

                var response = MapToResponse(courseStudent, courseInstance, user, changedByUser);
                return new BaseResponse<CourseStudentResponse>(
                    "Course student grade updated successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseStudentResponse>(
                    $"Error updating course student grade: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<CourseStudentResponse>>> BulkAssignStudentsAsync(BulkAssignStudentsRequest request)
        {
            try
            {
                var courseInstance = await _courseInstanceRepository.GetByIdAsync(request.CourseInstanceId);
                if (courseInstance == null)
                {
                    return new BaseResponse<List<CourseStudentResponse>>(
                        "Course instance not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var responses = new List<CourseStudentResponse>();
                var existingStudents = (await _courseStudentRepository.GetByCourseInstanceIdAsync(request.CourseInstanceId))
                    .Select(cs => cs.UserId)
                    .ToHashSet();

                foreach (var studentId in request.StudentIds)
                {
                    if (existingStudents.Contains(studentId))
                        continue;

                    var user = await _userRepository.GetByIdAsync(studentId);
                    if (user == null)
                        continue;

                    var courseStudent = new CourseStudent
                    {
                        CourseInstanceId = request.CourseInstanceId,
                        UserId = studentId,
                        EnrolledAt = DateTime.UtcNow.AddHours(7),
                        Status = request.Status,
                        StatusChangedAt = DateTime.UtcNow.AddHours(7),
                        ChangedByUserId = request.ChangedByUserId
                    };

                    await _courseStudentRepository.AddAsync(courseStudent);

                    User changedByUser = null;
                    if (request.ChangedByUserId.HasValue)
                    {
                        changedByUser = await _userRepository.GetByIdAsync(request.ChangedByUserId.Value);
                    }

                    responses.Add(MapToResponse(courseStudent, courseInstance, user, changedByUser));
                }

                return new BaseResponse<List<CourseStudentResponse>>(
                    "Bulk assign students completed successfully",
                    StatusCodeEnum.Created_201,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<CourseStudentResponse>>(
                    $"Error in bulk assign students: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<CourseStudentResponse>>> GetStudentsByCourseAndCampusAsync(int courseId, int semesterId, int campusId)
        {
            try
            {
                // Implementation sẽ cần thêm method trong repository để query phức tạp
                // Tạm thời trả về empty list
                return new BaseResponse<List<CourseStudentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    new List<CourseStudentResponse>());
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<CourseStudentResponse>>(
                    $"Error retrieving students: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }
        public async Task<BaseResponse<List<MyCourseResponse>>> GetStudentCoursesAsync(int studentId)
        {
            try
            {
                var courseStudents = await _courseStudentRepository.GetByUserIdAsync(studentId);
                var responses = new List<MyCourseResponse>();

                foreach (var cs in courseStudents.Where(cs => cs.Status == "Enrolled"))
                {

                    var courseInstance = await _courseInstanceRepository.GetByIdAsync(cs.CourseInstanceId);

                    var instructors = await _context.CourseInstructors
                        .Where(ci => ci.CourseInstanceId == courseInstance.CourseInstanceId)
                        .Include(ci => ci.User)
                        .Select(ci => $"{ci.User.FirstName} {ci.User.LastName}".Trim())
                        .ToListAsync();

                    var studentCount = await _context.CourseStudents
                        .CountAsync(cst => cst.CourseInstanceId == courseInstance.CourseInstanceId && cst.Status == "Enrolled");
                    if (courseInstance != null)
                    {
                        var response = new MyCourseResponse
                        {
                            CourseInstanceId = courseInstance.CourseInstanceId,
                            CourseId = courseInstance.CourseId,
                            SectionCode = courseInstance.SectionCode,
                            CourseName = courseInstance.Course?.CourseName ?? string.Empty,
                            CourseCode = courseInstance.Course?.CourseCode ?? string.Empty,
                            SemesterName = courseInstance.Semester?.Name ?? string.Empty,
                            CampusName = courseInstance.Campus?.CampusName ?? string.Empty,
                            EnrollmentPassword = courseInstance.EnrollmentPassword,
                            InstructorNames = instructors,
                            StudentCount = studentCount
                        };
                        responses.Add(response);
                    }
                }

                return new BaseResponse<List<MyCourseResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<MyCourseResponse>>(
                    $"Error retrieving student courses: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        // Trong CourseStudentService
        public async Task<BaseResponse<bool>> IsStudentEnrolledAsync(int courseInstanceId, int studentId)
        {
            try
            {
                var courseStudent = await _context.CourseStudents
                    .FirstOrDefaultAsync(cs => cs.CourseInstanceId == courseInstanceId && cs.UserId == studentId);

                if (courseStudent == null)
                {
                    return new BaseResponse<bool>("Student is not enrolled in this course", StatusCodeEnum.Forbidden_403, false);
                }

                if (courseStudent.Status != "Enrolled")
                {
                    return new BaseResponse<bool>($"Student enrollment status is {courseStudent.Status}, not Enrolled", StatusCodeEnum.Forbidden_403, false);
                }

                return new BaseResponse<bool>("Student is enrolled", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error checking enrollment: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<CourseStudentResponse>> GetEnrollmentStatusAsync(int courseInstanceId, int studentId)
        {
            try
            {
                var courseStudent = await _context.CourseStudents
                    .Include(cs => cs.CourseInstance)
                    .Include(cs => cs.User)
                    .FirstOrDefaultAsync(cs => cs.CourseInstanceId == courseInstanceId && cs.UserId == studentId);

                if (courseStudent == null)
                {
                    return new BaseResponse<CourseStudentResponse>("Student is not enrolled in this course", StatusCodeEnum.NotFound_404, null);
                }

                var response = MapToResponse(courseStudent, courseStudent.CourseInstance, courseStudent.User, null);
                return new BaseResponse<CourseStudentResponse>("Enrollment status retrieved", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseStudentResponse>($"Error retrieving enrollment status: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
/*        // Cập nhật method tính điểm assignment
        public async Task<BaseResponse<decimal>> CalculateTotalAssignmentGradeAsync(int courseInstanceId, int studentId)
        {
            try
            {
                var assignments = await _assignmentRepository.GetByCourseInstanceIdAsync(courseInstanceId);
                decimal totalGrade = 0;
                decimal totalWeight = assignments.Where(a => a.Weight > 0).Sum(a => a.Weight);

                if (totalWeight == 0)
                    return new BaseResponse<decimal>("No weights set", StatusCodeEnum.BadRequest_400, 0);

                foreach (var assignment in assignments.Where(a => a.Weight > 0))
                {
                    var assignmentGrade = await CalculateAssignmentGradeForStudentAsync(assignment.AssignmentId, studentId);
                    totalGrade += assignmentGrade * (assignment.Weight / totalWeight);
                }

                return new BaseResponse<decimal>("Success", StatusCodeEnum.OK_200, Math.Round(totalGrade, 2));
            }
            catch (Exception ex)
            {
                return new BaseResponse<decimal>(ex.Message, StatusCodeEnum.InternalServerError_500, 0);
            }
        }*/

        // Method tính điểm cho từng assignment
        private async Task<decimal> CalculateAssignmentGradeForStudentAsync(int assignmentId, int studentId)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            // Bổ sung: Nếu assignment Cancelled -> 0 điểm
            if (assignment.Status == "Cancelled")
            {
                return 0;
            }
            var submission = (await _submissionRepository.GetByAssignmentIdAsync(assignmentId))
                .FirstOrDefault(s => s.UserId == studentId);

            if (submission == null)
                return 0;

            // Lấy tất cả reviews (LOẠI BỎ AI REVIEWS)
            var reviews = (await _reviewRepository.GetBySubmissionIdAsync(submission.SubmissionId))
                .Where(r => r.FeedbackSource != "AI" && r.OverallScore.HasValue)
                .ToList();

            if (!reviews.Any())
                return 0;

            // Phân loại reviews
            var peerReviews = reviews.Where(r => r.ReviewType == "Peer").ToList();
            var instructorReviews = reviews.Where(r => r.ReviewType == "Instructor").ToList();

            decimal peerScore = peerReviews.Any() ? peerReviews.Average(r => r.OverallScore.Value) : 0;
            decimal instructorScore = instructorReviews.Any() ? instructorReviews.Average(r => r.OverallScore.Value) : peerScore;

            // Tính điểm tổng hợp theo trọng số
            decimal finalScore = (assignment.InstructorWeight * instructorScore + assignment.PeerWeight * peerScore) / 100;

            // Áp dụng penalty
            finalScore = await ApplyPenaltiesAsync(assignment, submission, studentId, finalScore);

            return finalScore;
        }
        
        // Method áp dụng penalties
        private async Task<decimal> ApplyPenaltiesAsync(Assignment assignment, Submission submission, int studentId, decimal currentScore)
        {
            decimal penalty = 0;

            // Late submission penalty
            if (submission.SubmittedAt > assignment.Deadline)
            {
                var latePenaltyStr = await GetAssignmentConfig(assignment.AssignmentId, "LateSubmissionPenalty");
                if (decimal.TryParse(latePenaltyStr, out decimal latePenaltyPercent))
                {
                    penalty += currentScore * (latePenaltyPercent / 100);
                }
            }

            // Missing review penalty
            var completedReviews = await _reviewAssignmentRepository.GetByReviewerIdAsync(studentId);
            var requiredReviews = assignment.NumPeerReviewsRequired;
            var actualCompletedReviews = completedReviews.Count(ra => ra.Status == "Completed");

            if (actualCompletedReviews < requiredReviews)
            {
                var missingReviewPenaltyStr = await GetAssignmentConfig(assignment.AssignmentId, "MissingReviewPenalty");
                if (decimal.TryParse(missingReviewPenaltyStr, out decimal missingReviewPenaltyPercent))
                {
                    int missingCount = requiredReviews - actualCompletedReviews;
                    penalty += currentScore * (missingReviewPenaltyPercent / 100) * missingCount;
                }
            }

            return Math.Max(0, currentScore - penalty);
        }
        private async Task<string> GetAssignmentConfig(int assignmentId, string key)
        {
            var configKey = $"{key}_{assignmentId}";
            var config = await _context.SystemConfigs.FirstOrDefaultAsync(sc => sc.ConfigKey == configKey);
            return config?.ConfigValue;
        }

        private CourseStudentResponse MapToResponse(CourseStudent courseStudent, CourseInstance courseInstance, User user, User changedByUser)
        {
            return new CourseStudentResponse
            {
                CourseStudentId = courseStudent.CourseStudentId,
                CourseInstanceId = courseStudent.CourseInstanceId,
                CourseInstanceName = courseInstance?.SectionCode ?? "",
                CourseCode = courseInstance?.Course?.CourseCode ?? "",
                CourseName = courseInstance?.Course?.CourseName ?? "",
                SemesterId = courseInstance?.SemesterId ?? 0,
                Semester = courseInstance?.Semester?.Name ?? "",
                UserId = courseStudent.UserId,
                StudentName = user?.FirstName + " " + user?.LastName,
                StudentEmail = user?.Email,
                StudentCode = user?.StudentCode,
                Role = "Student",
                EnrolledAt = courseStudent.EnrolledAt,
                Status = courseStudent.Status,
                FinalGrade = courseStudent.FinalGrade,
                IsPassed = courseStudent.IsPassed,
                StatusChangedAt = courseStudent.StatusChangedAt,
                ChangedByUserId = courseStudent.ChangedByUserId,
                ChangedByUserName = changedByUser?.UserName
            };
        }

        private async Task SendWelcomeEmailAsync(User user, string password, string role)
        {
            try
            {
                string subject;
                string htmlContent;

                if (role == "Instructor")
                {
                    subject = "Welcome to ASDPRS System - Instructor Account Created";
                    htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Welcome to ASDPRS</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333333;
            margin: 0;
            padding: 0;
            background-color: #f4f4f7;
        }}
        .container {{
            max-width: 600px;
            margin: 20px auto;
            background: #ffffff;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 4px 10px rgba(0,0,0,0.05);
        }}
        .header {{
            background-color: #ffffff;
            padding: 30px 20px;
            text-align: center;
            border-bottom: 1px solid #eeeeee;
        }}
        .header img {{
            max-width: 180px;
            height: auto;
        }}
        .body-content {{
            padding: 40px 30px;
        }}
        .welcome-text {{
            font-size: 22px;
            color: #2c3e50;
            margin-bottom: 20px;
            font-weight: bold;
        }}
        .highlight-box {{
            background-color: #f8f9fa;
            border-left: 4px solid #007bff;
            padding: 15px;
            margin: 20px 0;
        }}
        .button {{
            display: inline-block;
            padding: 12px 25px;
            background-color: #007bff;
            color: #ffffff !important;
            text-decoration: none;
            border-radius: 5px;
            font-weight: bold;
            margin-top: 10px;
        }}
        .footer {{
            background-color: #f4f4f7;
            padding: 20px;
            text-align: center;
            font-size: 12px;
            color: #777777;
        }}
        .info-text {{
            font-size: 14px;
            color: #555555;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <img src=""https://res.cloudinary.com/daf34jpxn/image/upload/v1766342084/FASM_ss094j.png"" alt=""ASDPRS Logo"">
        </div>

        <div class=""body-content"">
            <div class=""welcome-text"">Welcome to ASDPRS System</div>
            
            <p class=""info-text"">Dear <strong>{user.FirstName} {user.LastName}</strong>,</p>
     
            <p class=""info-text"">Your instructor account has been successfully created in the <strong>ASDPRS system</strong>.
            We are excited to have you on board!</p>
            
            <div class=""highlight-box"">
                <p class=""info-text"" style=""margin: 0;"">
                    <strong>Login Email:</strong> {user.Email}<br>
                    <strong>Method:</strong> Google Authentication
                </p>
            </div>

            <p class=""info-text"">Simply click on the Google login button on the login page and use your Google account associated with this email.</p>
            
            <p style=""text-align: center;"">
                <a href=""https://fasm-fpt.site/login"" class=""button"">Go to Login Page</a>
            </p>

            <p class=""info-text"" style=""color: #e67e22; font-weight: bold;"">
                * No password is required for Google login.
            </p>

            <p class=""info-text"" style=""margin-top: 30px;"">
                If you have any issues, please contact the system administrator.<br><br>
                Best regards,<br>
                <strong>ASDPRS Team</strong>
            </p>
        </div>

        <div class=""footer"">
            &copy; 2025 ASDPRS System. All rights reserved.<br>
            This is an automated email, please do not reply.
        </div>
    </div>
</body>
</html>";
                }
                else
                {
                    subject = "Welcome to ASDPRS System - Your Account Credentials";
                    htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Account Created - ASDPRS</title>
    <style>
        body {{
            font-family: 'Segoe UI', Arial, sans-serif;
            line-height: 1.6;
            color: #333333;
            margin: 0;
            padding: 0;
            background-color: #f9f9f9;
        }}
        .container {{
            max-width: 600px;
            margin: 20px auto;
            background: #ffffff;
            border: 1px solid #eeeeee;
            border-radius: 12px;
            overflow: hidden;
        }}
        .header {{
            background-color: #ffffff;
            padding: 25px;
            text-align: center;
            border-bottom: 2px solid #f0f0f0;
        }}
        .header img {{
            max-width: 150px;
            height: auto;
        }}
        .content {{
            padding: 35px;
        }}
        .title {{
            color: #1a73e8;
            font-size: 22px;
            font-weight: bold;
            margin-bottom: 20px;
        }}
        .credentials-card {{
            background-color: #f0f7ff;
            border-radius: 8px;
            padding: 20px;
            margin: 25px 0;
            border: 1px dashed #1a73e8;
        }}
        .credentials-item {{
            margin: 5px 0;
            font-size: 15px;
        }}
        .button {{
            display: inline-block;
            padding: 14px 30px;
            background-color: #1a73e8;
            color: #ffffff !important;
            text-decoration: none;
            border-radius: 6px;
            font-weight: 600;
            margin: 10px 0;
        }}
        .warning {{
            color: #d93025;
            font-size: 13px;
            font-style: italic;
            margin-top: 15px;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 20px;
            text-align: center;
            font-size: 12px;
            color: #70757a;
            border-top: 1px solid #eeeeee;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <img src=""https://res.cloudinary.com/daf34jpxn/image/upload/v1766342084/FASM_ss094j.png"" alt=""ASDPRS Logo"">
        </div>

        <div class=""content"">
            <div class=""title"">Welcome to ASDPRS System</div>
            
            <p>Dear <strong>{user.FirstName} {user.LastName}</strong>,</p>
            
            <p>Your student account has been automatically created following your successful course enrollment.
            Below are your login credentials:</p>
            
            <div class=""credentials-card"">
                <div class=""credentials-item""><strong>Username:</strong> <code>{user.UserName}</code></div>
                <div class=""credentials-item""><strong>Password:</strong> <code>{password}</code></div>
            </div>

            <p style=""text-align: center;"">
                <a href=""https://fasm-fpt.site/login"" class=""button"">Login to Your Account</a>
            </p>

            <p class=""warning"">
                ⚠️ For security reasons, please log in and change your password immediately after your first access.
            </p>

            <p style=""margin-top: 30px;"">
                Best regards,<br>
                <strong>ASDPRS Team</strong>
            </p>
        </div>

        <div class=""footer"">
            © 2025 ASDPRS System. All rights reserved.<br>
            If you did not expect this email, please ignore it or contact support.
        </div>
    </div>
</body>
</html>";
                }

                await _emailService.SendEmail(user.Email, subject, htmlContent);
            }
            catch (Exception ex)
            {
                // Log error (Console/Logger) but ensure the import process doesn't crash
                Console.WriteLine($"Failed to send email to {user.Email}: {ex.Message}");
            }
        }

        private async Task<bool> IsStudentInOverlappingCourseAsync(int userId, int courseId, DateTime newStart, DateTime newEnd, int? currentInstanceId = null)
        {
            return await _context.CourseStudents
                .Include(cs => cs.CourseInstance)
                .AnyAsync(cs =>
                    cs.UserId == userId &&
                    cs.CourseInstance.CourseId == courseId &&
                    (!currentInstanceId.HasValue || cs.CourseInstanceId != currentInstanceId.Value) &&
                    (cs.Status == "Enrolled" || cs.Status == "Pending") &&
                    cs.CourseInstance.StartDate < newEnd &&
                    cs.CourseInstance.EndDate > newStart
                );
        }
    }
}