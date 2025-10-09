using BussinessObject.Models;
using DataAccessLayer;
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

        public CourseStudentService(
            ICourseStudentRepository courseStudentRepository,
            ICourseInstanceRepository courseInstanceRepository,
            IUserRepository userRepository,
            ASDPRSContext context,
            IAssignmentRepository assignmentRepository,
            ISubmissionRepository submissionRepository,
            IReviewRepository reviewRepository,
            IReviewAssignmentRepository reviewAssignmentRepository)
        {
            _courseStudentRepository = courseStudentRepository;
            _courseInstanceRepository = courseInstanceRepository;
            _userRepository = userRepository;
            _context = context;
            _assignmentRepository = assignmentRepository;
            _submissionRepository = submissionRepository;
            _reviewRepository = reviewRepository;
            _reviewAssignmentRepository = reviewAssignmentRepository;
        }

        // Method import Excel: Đọc file, thêm sinh viên, kiểm tra đơn giản
        public async Task<BaseResponse<List<CourseStudentResponse>>> ImportStudentsFromExcelAsync(int courseInstanceId, Stream excelStream, int? changedByUserId)
        {
            try
            {
                var courseInstance = await _courseInstanceRepository.GetByIdAsync(courseInstanceId);
                if (courseInstance == null) return new BaseResponse<List<CourseStudentResponse>>("Không tìm thấy lớp", StatusCodeEnum.NotFound_404, null);

                var existingStudents = (await _courseStudentRepository.GetByCourseInstanceIdAsync(courseInstanceId)).Select(cs => cs.UserId).ToHashSet();

                var responses = new List<CourseStudentResponse>();
                using var package = new ExcelPackage(excelStream);
                var worksheet = package.Workbook.Worksheets[0];

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    var studentCode = worksheet.Cells[row, 4].Value?.ToString();  // Cột Code (ví dụ: SE161544)
                    bool isRetaking = worksheet.Cells[row, 8].Value?.ToString()?.ToLower() == "true";  // Giả định cột 8 là IsRetaking, mặc định false nếu không có

                    if (string.IsNullOrEmpty(studentCode)) continue;

                    var user = await _context.Users.FirstOrDefaultAsync(u => u.StudentCode == studentCode);
                    if (user == null || existingStudents.Contains(user.Id)) continue;

                    // Kiểm tra: Campus khớp, Semester khớp, Curriculum có môn
                    if (user.CampusId != courseInstance.CampusId) continue;
                    if (courseInstance.SemesterId == null) continue;
                    var curriculum = await _context.Curriculums.Include(c => c.Courses).FirstOrDefaultAsync(c => c.CampusId == user.CampusId);
                    if (curriculum == null || !curriculum.Courses.Any(c => c.CourseId == courseInstance.CourseId)) continue;

                    var courseStudent = new CourseStudent
                    {
                        CourseInstanceId = courseInstanceId,
                        UserId = user.Id,
                        EnrolledAt = DateTime.UtcNow,
                        Status = isRetaking ? "Retaking" : "Pending",
                        StatusChangedAt = DateTime.UtcNow,
                        ChangedByUserId = changedByUserId
                    };
                    await _courseStudentRepository.AddAsync(courseStudent);
                    responses.Add(new CourseStudentResponse { /* Map đơn giản, như code cũ */ });
                }

                return new BaseResponse<List<CourseStudentResponse>>("Import thành công", StatusCodeEnum.Created_201, responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<CourseStudentResponse>>("Lỗi import: " + ex.Message, StatusCodeEnum.InternalServerError_500, null);
            }
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

                var courseInstance = await _courseInstanceRepository.GetByIdAsync(courseStudent.CourseInstanceId);
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
        public async Task<BaseResponse<decimal>> CalculateTotalAssignmentGradeAsync(int courseInstanceId, int studentId)
        {
            try
            {
                var assignments = await _assignmentRepository.GetByCourseInstanceIdAsync(courseInstanceId);
                decimal totalGrade = 0;
                decimal totalWeight = assignments.Sum(a => a.Weight);

                if (totalWeight == 0) return new BaseResponse<decimal>("No weights set", StatusCodeEnum.BadRequest_400, 0);

                // Get student's review assignments across all assignments (to check missing reviews)
                decimal totalMissingReviewPenalty = 0;
                int totalRequiredReviews = 0;
                int completedReviews = 0;

                foreach (var assignment in assignments)
                {
                    var submission = (await _submissionRepository.GetByAssignmentIdAsync(assignment.AssignmentId))
                        .FirstOrDefault(s => s.UserId == studentId);

                    decimal assignmentGrade = 0;

                    if (submission == null)
                    {
                        // No submission: 0
                        assignmentGrade = 0;
                    }
                    else
                    {
                        // Calculate base grade from reviews
                        var reviews = await _reviewRepository.GetBySubmissionIdAsync(submission.SubmissionId);
                        var avgScore = reviews.Where(r => r.OverallScore.HasValue).Average(r => r.OverallScore.Value);
                        assignmentGrade = avgScore;

                        // Apply late penalty if submitted late
                        if (submission.SubmittedAt > assignment.Deadline && submission.SubmittedAt <= (assignment.FinalDeadline ?? DateTime.MaxValue))
                        {
                            var latePenaltyStr = await GetAssignmentConfig(assignment.AssignmentId, "LateSubmissionPenalty");
                            if (decimal.TryParse(latePenaltyStr, out decimal latePenalty))
                            {
                                assignmentGrade -= assignmentGrade * (latePenalty / 100);
                                assignmentGrade = Math.Max(0, assignmentGrade);  // Not negative
                            }
                        }
                    }

                    totalGrade += assignmentGrade * (assignment.Weight / totalWeight);

                    // Count required reviews for this assignment
                    var studentReviews = await _reviewAssignmentRepository.GetByReviewerIdAsync(studentId);
                    var assignmentReviews = studentReviews.Where(ra =>
                    {
                        var sub = _submissionRepository.GetByIdAsync(ra.SubmissionId).Result;
                        return sub?.AssignmentId == assignment.AssignmentId;
                    });

                    totalRequiredReviews += assignmentReviews.Count();
                    completedReviews += assignmentReviews.Count(ra => ra.Status == "Completed");
                }

                // Apply missing review penalty to total grade
                int missingReviews = totalRequiredReviews - completedReviews;
                if (missingReviews > 0)
                {
                    // Assume global or per course penalty; here use first assignment's for simplicity
                    var sampleAssignment = assignments.FirstOrDefault();
                    if (sampleAssignment != null)
                    {
                        var missPenaltyStr = await GetAssignmentConfig(sampleAssignment.AssignmentId, "MissingReviewPenalty");
                        if (decimal.TryParse(missPenaltyStr, out decimal missPenalty))
                        {
                            totalGrade -= totalGrade * (missPenalty / 100) * missingReviews;  // Per missing
                            totalGrade = Math.Max(0, totalGrade);
                        }
                    }
                }

                return new BaseResponse<decimal>("Success", StatusCodeEnum.OK_200, totalGrade);
            }
            catch (Exception ex)
            {
                return new BaseResponse<decimal>(ex.Message, StatusCodeEnum.InternalServerError_500, 0);
            }
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