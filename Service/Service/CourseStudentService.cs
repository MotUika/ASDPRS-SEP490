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
            UserManager<User> userManager)
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
        }

        // Method import Excel: Đọc file, thêm sinh viên, kiểm tra đơn giản
        public async Task<BaseResponse<List<CourseStudentResponse>>> ImportStudentsFromExcelAsync(int courseInstanceId, Stream fileStream, int? changedByUserId)
        {
            try
            {
                var courseInstance = await _context.CourseInstances
                    .Include(ci => ci.Course)
                    .FirstOrDefaultAsync(ci => ci.CourseInstanceId == courseInstanceId);

                if (courseInstance == null)
                {
                    return new BaseResponse<List<CourseStudentResponse>>("Không tìm thấy lớp học", StatusCodeEnum.NotFound_404, null);
                }

                var results = new List<CourseStudentResponse>();
                var createdUsers = new List<User>();

                using (var package = new ExcelPackage(fileStream))
                {
                    var worksheet = package.Workbook.Worksheets[0]; // Sheet đầu tiên
                    var rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++) // Bắt đầu từ dòng 2 (bỏ header)
                    {
                        var code = worksheet.Cells[row, 2].Value?.ToString().Trim(); // Cột B: Code
                        var surname = worksheet.Cells[row, 3].Value?.ToString().Trim(); // Cột C: Surname
                        var middleName = worksheet.Cells[row, 4].Value?.ToString().Trim(); // Cột D: Middle name
                        var givenName = worksheet.Cells[row, 5].Value?.ToString().Trim(); // Cột E: Given name
                        var email = worksheet.Cells[row, 6].Value?.ToString().Trim(); // Cột F: Email

                        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(email))
                            continue;

                        // Tìm hoặc tạo user
                        var user = await _userManager.FindByEmailAsync(email);
                        if (user == null)
                        {
                            user = await CreateStudentUserAsync(code, surname, middleName, givenName, email, courseInstance.CampusId);
                            if (user != null)
                            {
                                createdUsers.Add(user);
                            }
                        }

                        if (user != null)
                        {
                            // Kiểm tra xem sinh viên đã trong lớp chưa
                            var existingCourseStudent = await _context.CourseStudents
                                .FirstOrDefaultAsync(cs => cs.CourseInstanceId == courseInstanceId && cs.UserId == user.Id);

                            if (existingCourseStudent == null)
                            {
                                var courseStudent = new CourseStudent
                                {
                                    CourseInstanceId = courseInstanceId,
                                    UserId = user.Id,
                                    EnrolledAt = DateTime.UtcNow,
                                    Status = "Enrolled",
                                    ChangedByUserId = changedByUserId
                                };

                                var created = await _courseStudentRepository.AddAsync(courseStudent);
                                var response = _mapper.Map<CourseStudentResponse>(created);
                                results.Add(response);
                            }
                        }
                    }
                }

                return new BaseResponse<List<CourseStudentResponse>>($"Import thành công {results.Count} sinh viên", StatusCodeEnum.Created_201, results);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<CourseStudentResponse>>($"Lỗi khi import: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
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

                using (var package = new ExcelPackage(fileStream))
                {
                    foreach (var worksheet in package.Workbook.Worksheets)
                    {
                        var sheetResult = new SheetImportResult
                        {
                            SheetName = worksheet.Name,
                            ImportedStudents = new List<CourseStudentResponse>()
                        };

                        // Tìm CourseInstance dựa trên sheet name (section code) và campus
                        var courseInstance = await _context.CourseInstances
                            .Include(ci => ci.Course)
                            .FirstOrDefaultAsync(ci => ci.SectionCode == worksheet.Name && ci.CampusId == campusId);

                        if (courseInstance == null)
                        {
                            sheetResult.Message = $"Không tìm thấy lớp học với mã section: {worksheet.Name} trong campus {campusId}";
                            results.SheetResults.Add(sheetResult);
                            continue;
                        }

                        sheetResult.CourseInstanceId = courseInstance.CourseInstanceId;
                        sheetResult.CourseName = courseInstance.Course?.CourseName;

                        var rowCount = worksheet.Dimension?.Rows ?? 0;
                        int successCount = 0;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var code = worksheet.Cells[row, 2].Value?.ToString().Trim();
                            var surname = worksheet.Cells[row, 3].Value?.ToString().Trim();
                            var middleName = worksheet.Cells[row, 4].Value?.ToString().Trim();
                            var givenName = worksheet.Cells[row, 5].Value?.ToString().Trim();
                            var email = worksheet.Cells[row, 6].Value?.ToString().Trim();

                            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(email))
                                continue;

                            try
                            {
                                // Tìm hoặc tạo user
                                var user = await _userManager.FindByEmailAsync(email);
                                if (user == null)
                                {
                                    user = await CreateStudentUserAsync(code, surname, middleName, givenName, email, campusId);
                                }

                                if (user != null)
                                {
                                    // Kiểm tra xem sinh viên đã trong lớp chưa
                                    var existingCourseStudent = await _context.CourseStudents
                                        .FirstOrDefaultAsync(cs => cs.CourseInstanceId == courseInstance.CourseInstanceId && cs.UserId == user.Id);

                                    if (existingCourseStudent == null)
                                    {
                                        var courseStudent = new CourseStudent
                                        {
                                            CourseInstanceId = courseInstance.CourseInstanceId,
                                            UserId = user.Id,
                                            EnrolledAt = DateTime.UtcNow,
                                            Status = "Enrolled",
                                            ChangedByUserId = changedByUserId
                                        };

                                        var created = await _courseStudentRepository.AddAsync(courseStudent);
                                        var response = _mapper.Map<CourseStudentResponse>(created);
                                        sheetResult.ImportedStudents.Add(response);
                                        successCount++;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // Ghi log lỗi nhưng tiếp tục với các dòng khác
                                Console.WriteLine($"Lỗi import sinh viên {code}: {ex.Message}");
                            }
                        }

                        sheetResult.SuccessCount = successCount;
                        sheetResult.Message = $"Import thành công {successCount} sinh viên";
                        results.SheetResults.Add(sheetResult);
                        results.TotalSuccessCount += successCount;
                    }
                }

                return new BaseResponse<MultipleCourseImportResponse>("Import hoàn tất", StatusCodeEnum.OK_200, results);
            }
            catch (Exception ex)
            {
                return new BaseResponse<MultipleCourseImportResponse>($"Lỗi khi import nhiều lớp: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        private async Task<User> CreateStudentUserAsync(string studentCode, string surname, string middleName, string givenName, string email, int campusId)
        {
            try
            {
                // Tạo LastName từ Surname + Middle name
                var lastName = $"{surname} {middleName}".Trim();

                // Tạo username từ email (phần trước @)
                var username = email.Split('@')[0];

                // Kiểm tra xem username đã tồn tại chưa
                var existingUser = await _userManager.FindByNameAsync(username);
                if (existingUser != null)
                {
                    // Nếu đã tồn tại, thêm số ngẫu nhiên
                    username = $"{username}_{new Random().Next(1000, 9999)}";
                }

                var user = new User
                {
                    UserName = username,
                    Email = email,
                    FirstName = givenName,
                    LastName = lastName,
                    StudentCode = studentCode,
                    CampusId = campusId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // Tạo mật khẩu ngẫu nhiên
                var password = GenerateRandomPassword();
                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Gán role Student
                    await _userManager.AddToRoleAsync(user, "Student");
                    return user;
                }
                else
                {
                    Console.WriteLine($"Lỗi tạo user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi tạo user {email}: {ex.Message}");
                return null;
            }
        }

        private string GenerateRandomPassword(int length = 12)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_-+=[{]};:>|./?";
            var random = new Random();
            var chars = new char[length];

            for (int i = 0; i < length; i++)
            {
                chars[i] = validChars[random.Next(validChars.Length)];
            }

            return new string(chars);
        }
        // Method enroll: Sinh viên nhập key để kích hoạt
        public async Task<BaseResponse<CourseStudentResponse>> EnrollStudentAsync(int courseInstanceId, int studentUserId, string enrollKey)
        {
            try
            {
                var courseInstance = await _courseInstanceRepository.GetByIdAsync(courseInstanceId);
                if (courseInstance == null || courseInstance.EnrollmentPassword != enrollKey) return new BaseResponse<CourseStudentResponse>("Key sai hoặc lớp không tồn tại", StatusCodeEnum.BadRequest_400, null);

                var courseStudent = (await _courseStudentRepository.GetByCourseInstanceIdAsync(courseInstanceId)).FirstOrDefault(cs => cs.UserId == studentUserId && cs.Status == "Pending");

                if (courseStudent == null) return new BaseResponse<CourseStudentResponse>("Bạn chưa được import vào lớp", StatusCodeEnum.NotFound_404, null);

                courseStudent.Status = "Enrolled";
                await _courseStudentRepository.UpdateAsync(courseStudent);

                return new BaseResponse<CourseStudentResponse>("Enroll thành công", StatusCodeEnum.OK_200, new CourseStudentResponse { /* Map */ });
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseStudentResponse>("Lỗi enroll: " + ex.Message, StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<CourseStudentResponse>> CreateCourseStudentAsync(CreateCourseStudentRequest request)
        {
            try
            {
                if (!string.IsNullOrEmpty(request.StudentCode) && request.UserId == 0)
                {
                    var userByCode = await _userRepository.GetByStudentCodeAsync(request.StudentCode);

                    if (userByCode == null)
                    {
                        return new BaseResponse<CourseStudentResponse>(
                            "Student not found with the provided student code",
                            StatusCodeEnum.NotFound_404,
                            null);
                    }

                    request.UserId = userByCode.Id;
                }

                // Validate course instance exists
                var courseInstance = await _courseInstanceRepository.GetByIdAsync(request.CourseInstanceId);
                if (courseInstance == null)
                {
                    return new BaseResponse<CourseStudentResponse>(
                        "Course instance not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Validate user exists
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return new BaseResponse<CourseStudentResponse>(
                        "User not found",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                // Check if student is already enrolled
                var existing = (await _courseStudentRepository.GetByCourseInstanceIdAsync(request.CourseInstanceId))
                    .FirstOrDefault(cs => cs.UserId == request.UserId);

                if (existing != null)
                {
                    return new BaseResponse<CourseStudentResponse>(
                        "Student is already enrolled in this course instance",
                        StatusCodeEnum.Conflict_409,
                        null);
                }

                var courseStudent = new CourseStudent
                {
                    CourseInstanceId = request.CourseInstanceId,
                    UserId = request.UserId,
                    EnrolledAt = DateTime.UtcNow,
                    Status = request.Status,
                    FinalGrade = request.FinalGrade,
                    IsPassed = request.IsPassed,
                    StatusChangedAt = DateTime.UtcNow,
                    ChangedByUserId = request.ChangedByUserId
                };

                await _courseStudentRepository.AddAsync(courseStudent);

                User changedByUser = null;
                if (request.ChangedByUserId.HasValue)
                {
                    changedByUser = await _userRepository.GetByIdAsync(request.ChangedByUserId.Value);
                }

                var response = MapToResponse(courseStudent, courseInstance, user, changedByUser);
                return new BaseResponse<CourseStudentResponse>(
                    "Course student enrolled successfully",
                    StatusCodeEnum.Created_201,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseStudentResponse>(
                    $"Error creating course student: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteCourseStudentAsync(int courseStudentId, int courseInstanceId, int userId)
        {
            try
            {
                // 1. Kiểm tra lớp có tồn tại không
                var courseInstance = await _courseInstanceRepository.GetByIdAsync(courseInstanceId);
                if (courseInstance == null)
                {
                    return new BaseResponse<bool>(
                        "Course instance not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                // 2. Kiểm tra user có tồn tại không
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<bool>(
                        "User not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                // 3. Tìm courseStudent theo id
                var courseStudent = await _courseStudentRepository.GetByIdAsync(courseStudentId);
                if (courseStudent == null)
                {
                    return new BaseResponse<bool>(
                        "Course student not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                // 4. Kiểm tra xem có khớp cả 3 field không
                if (courseStudent.UserId != userId || courseStudent.CourseInstanceId != courseInstanceId)
                {
                    return new BaseResponse<bool>(
                        "Mismatch detected: student does not belong to this course instance",
                        StatusCodeEnum.BadRequest_400,
                        false);
                }

                // 5. Tiến hành xóa
                await _courseStudentRepository.DeleteAsync(courseStudent);

                return new BaseResponse<bool>(
                    "Student removed from course successfully",
                    StatusCodeEnum.OK_200,
                    true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>(
                    $"Error removing student from course: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false);
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
                courseStudent.StatusChangedAt = DateTime.UtcNow;
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
                courseStudent.StatusChangedAt = DateTime.UtcNow;
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
                        EnrolledAt = DateTime.UtcNow,
                        Status = request.Status,
                        StatusChangedAt = DateTime.UtcNow,
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
        public async Task<BaseResponse<List<CourseInstanceResponse>>> GetStudentCoursesAsync(int studentId)
        {
            try
            {
                var courseStudents = await _courseStudentRepository.GetByUserIdAsync(studentId);
                var responses = new List<CourseInstanceResponse>();

                foreach (var cs in courseStudents.Where(cs => cs.Status == "Enrolled"))
                {
                    var courseInstance = await _courseInstanceRepository.GetByIdAsync(cs.CourseInstanceId);
                    if (courseInstance != null)
                    {
                        var response = new CourseInstanceResponse
                        {
                            CourseInstanceId = courseInstance.CourseInstanceId,
                            CourseId = courseInstance.CourseId,
                            SectionCode = courseInstance.SectionCode,
                            CourseName = courseInstance.Course?.CourseName ?? string.Empty,
                            CourseCode = courseInstance.Course?.CourseCode ?? string.Empty,
                            SemesterName = courseInstance.Semester?.Name ?? string.Empty,
                            CampusName = courseInstance.Campus?.CampusName ?? string.Empty,
                            EnrollmentPassword = courseInstance.EnrollmentPassword
                        };
                        responses.Add(response);
                    }
                }

                return new BaseResponse<List<CourseInstanceResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<CourseInstanceResponse>>(
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
                CourseInstanceName = courseInstance?.SectionCode ?? string.Empty,
                CourseCode = courseInstance?.Course?.CourseCode ?? string.Empty,
                CourseName = courseInstance?.Course?.CourseName ?? string.Empty,
                UserId = courseStudent.UserId,
                StudentName = user?.FirstName ?? string.Empty,
                StudentEmail = user?.Email ?? string.Empty,
                StudentCode = user?.StudentCode ?? string.Empty,
                EnrolledAt = courseStudent.EnrolledAt,
                Status = courseStudent.Status,
                FinalGrade = courseStudent.FinalGrade,
                IsPassed = courseStudent.IsPassed,
                StatusChangedAt = courseStudent.StatusChangedAt,
                ChangedByUserId = courseStudent.ChangedByUserId,
                ChangedByUserName = changedByUser?.UserName ?? string.Empty
            };
        }
    }
}