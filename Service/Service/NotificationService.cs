using AutoMapper;
using BussinessObject.Models;
using Microsoft.AspNetCore.SignalR;
using Repository.IRepository;
using Service.Hubs;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Notification;
using Service.RequestAndResponse.Response.Notification;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IMapper _mapper;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ICourseStudentRepository _courseStudentRepository;
        private readonly IReviewAssignmentRepository _reviewAssignmentRepository;
        private readonly ICourseInstructorRepository _courseInstructorRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly ICourseInstanceRepository _courseInstanceRepository;
        private readonly IEmailService _emailService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(
            INotificationRepository notificationRepository,
            IMapper mapper,
            IAssignmentRepository assignmentRepository,
            ICourseStudentRepository courseStudentRepository,
            IReviewAssignmentRepository reviewAssignmentRepository,
            ICourseInstructorRepository courseInstructorRepository,
            IUserRepository userRepository,
            ISubmissionRepository submissionRepository,
            ICourseInstanceRepository courseInstanceRepository,
            IEmailService emailService,
            IHubContext<NotificationHub> hubContext)
        {
            _notificationRepository = notificationRepository;
            _mapper = mapper;
            _assignmentRepository = assignmentRepository;
            _courseStudentRepository = courseStudentRepository;
            _reviewAssignmentRepository = reviewAssignmentRepository;
            _courseInstructorRepository = courseInstructorRepository;
            _userRepository = userRepository;
            _submissionRepository = submissionRepository;
            _courseInstanceRepository = courseInstanceRepository;
            _emailService = emailService;
            _hubContext = hubContext;
        }

        public async Task<BaseResponse<NotificationResponse>> CreateNotificationAsync(CreateNotificationRequest request)
        {
            try
            {
                var notification = _mapper.Map<Notification>(request);
                notification.CreatedAt = DateTime.UtcNow.AddHours(7);
                notification.IsRead = false;
                var validationContext = new ValidationContext(notification);
                Validator.ValidateObject(notification, validationContext, true);

                var created = await _notificationRepository.AddAsync(notification);

                // Map sang response
                var response = _mapper.Map<NotificationResponse>(created);

                await PopulateCourseInfoAsync(response);

                // GỬI REAL-TIME NOTIFICATION
                await SendRealTimeNotification(created, response.CourseId, response.CourseInstanceId);

                // CHỈ GỬI EMAIL CHO CÁC TRƯỜNG HỢP QUAN TRỌNG
                if (ShouldSendEmail(created))
                {
                    await SendNotificationEmail(created);
                }

                return new BaseResponse<NotificationResponse>("Notification created successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<NotificationResponse>($"Error creating notification: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        private async Task PopulateCourseInfoAsync(NotificationResponse response)
        {
            try
            {
                // Logic tìm AssignmentId trước
                int? foundAssignmentId = response.AssignmentId;

                if (!foundAssignmentId.HasValue)
                {
                    if (response.SubmissionId.HasValue)
                    {
                        var sub = await _submissionRepository.GetByIdAsync(response.SubmissionId.Value);
                        foundAssignmentId = sub?.AssignmentId;
                    }
                    else if (response.ReviewAssignmentId.HasValue)
                    {
                        var review = await _reviewAssignmentRepository.GetByIdAsync(response.ReviewAssignmentId.Value);
                        if (review != null)
                        {
                            var sub = await _submissionRepository.GetByIdAsync(review.SubmissionId);
                            foundAssignmentId = sub?.AssignmentId;
                        }
                    }
                }

                // Nếu tìm thấy Assignment, truy xuất CourseInstance
                if (foundAssignmentId.HasValue)
                {
                    var assign = await _assignmentRepository.GetByIdAsync(foundAssignmentId.Value);
                    if (assign != null)
                    {
                        // Gán CourseInstanceId trực tiếp từ Assignment
                        response.CourseInstanceId = assign.CourseInstanceId;

                        // Lấy thêm CourseId từ bảng CourseInstance
                        var ci = await _courseInstanceRepository.GetByIdAsync(assign.CourseInstanceId);
                        if (ci != null)
                        {
                            response.CourseId = ci.CourseId;
                        }
                    }
                }

            }
            catch
            {
                // Ignore errors during extra info population to keep main flow alive
            }
        }

        private bool ShouldSendEmail(Notification notification)
        {
            if (notification.Type.StartsWith("DeadlineReminder"))
            {
                return true;
            }
            return notification.Type switch
            {
                "GradesPublished" => true,
                "AssignmentActive" => true,
                "InstructorAssigned" => true,
                "RegradeRequest" => true,
                "RegradeStatusUpdate" => true,
                _ => false
            };
        }

        // Cập nhật để gửi cả CourseId và CourseInstanceId qua SignalR
        private async Task SendRealTimeNotification(Notification notification, int? courseId, int? courseInstanceId)
        {
            try
            {
                await _hubContext.Clients.Group($"user_{notification.UserId}")
                    .SendAsync("ReceiveNotification", new
                    {
                        notification.NotificationId,
                        notification.Title,
                        notification.Message,
                        notification.Type,
                        notification.CreatedAt,
                        notification.IsRead,
                        notification.AssignmentId,
                        notification.SubmissionId,
                        CourseId = courseId,
                        CourseInstanceId = courseInstanceId
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending real-time notification: {ex.Message}");
            }
        }
        private async Task SendNotificationEmail(Notification notification)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(notification.UserId);
                if (user == null || string.IsNullOrEmpty(user.Email)) return;

                var emailSubject = $"[ASDPRS] {notification.Title}";
                var emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2563eb;'>{notification.Title}</h2>
                    <div style='background: #f8fafc; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        {notification.Message}
                    </div>
                    <p style='color: #64748b; font-size: 14px;'>
                        Sent: {notification.CreatedAt:dd/MM/yyyy HH:mm}
                    </p>
                    <p style='color: #64748b; font-size: 12px;'>
                        This is an automated notification from FASM System.
                    </p>
                </div>
            ";

                await _emailService.SendEmail(user.Email, emailSubject, emailBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending notification email: {ex.Message}");
            }
        }

        public async Task<BaseResponse<IEnumerable<NotificationResponse>>> GetNotificationsByUserAsync(int userId, bool unreadOnly = false)
        {
            try
            {
                var notifications = await _notificationRepository.GetByUserIdAsync(userId);
                if (unreadOnly)
                {
                    notifications = notifications.Where(n => !n.IsRead);
                }

                notifications = notifications.OrderByDescending(n => n.CreatedAt);

                var response = _mapper.Map<IEnumerable<NotificationResponse>>(notifications).ToList();

                // LOOP để điền CourseId và CourseInstanceId
                foreach (var item in response)
                {
                    await PopulateCourseInfoAsync(item);
                }

                return new BaseResponse<IEnumerable<NotificationResponse>>("Notifications retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<NotificationResponse>>($"Error retrieving notifications: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<bool>> MarkAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _notificationRepository.GetByIdAsync(notificationId);
                if (notification == null)
                {
                    return new BaseResponse<bool>("Notification not found", StatusCodeEnum.NotFound_404, false);
                }

                notification.IsRead = true;
                await _notificationRepository.UpdateAsync(notification);

                return new BaseResponse<bool>("Notification marked as read", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error marking notification as read: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<bool>> MarkAllAsReadAsync(int userId)
        {
            try
            {
                var notifications = await _notificationRepository.GetByUserIdAsync(userId);
                var unread = notifications.Where(n => !n.IsRead).ToList();

                foreach (var notif in unread)
                {
                    notif.IsRead = true;
                    await _notificationRepository.UpdateAsync(notif);
                }

                return new BaseResponse<bool>("All notifications marked as read", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error marking all as read: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<bool>> DeleteAllReadNotificationsAsync(int userId)
        {
            try
            {
                var result = await _notificationRepository.DeleteAllReadByUserIdAsync(userId);

                if (result)
                {
                    return new BaseResponse<bool>("All read notifications deleted successfully", StatusCodeEnum.OK_200, true);
                }

                return new BaseResponse<bool>("No read notifications found to delete", StatusCodeEnum.OK_200, false);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deleting read notifications: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<bool>> DeleteAllNotificationsAsync(int userId)
        {
            try
            {
                var result = await _notificationRepository.DeleteAllByUserIdAsync(userId);

                if (result)
                {
                    return new BaseResponse<bool>("All notifications deleted successfully", StatusCodeEnum.OK_200, true);
                }

                return new BaseResponse<bool>("No notifications found to delete", StatusCodeEnum.OK_200, false);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deleting all notifications: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }
        public async Task<BaseResponse<bool>> DeleteNotificationAsync(int notificationId, int userId)
        {
            try
            {
                // 1. Tìm thông báo
                var notification = await _notificationRepository.GetByIdAsync(notificationId);

                // 2. Kiểm tra tồn tại
                if (notification == null)
                {
                    return new BaseResponse<bool>("Notification not found", StatusCodeEnum.NotFound_404, false);
                }

                // 3. Kiểm tra quyền sở hữu (User chỉ được xóa thông báo của chính mình)
                if (notification.UserId != userId)
                {
                    return new BaseResponse<bool>("You do not have permission to delete this notification", StatusCodeEnum.Forbidden_403, false);
                }

                // 4. Thực hiện xóa
                var result = await _notificationRepository.DeleteAsync(notificationId);

                if (result)
                {
                    return new BaseResponse<bool>("Notification deleted successfully", StatusCodeEnum.OK_200, true);
                }

                return new BaseResponse<bool>("Failed to delete notification", StatusCodeEnum.InternalServerError_500, false);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deleting notification: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        // Specific notification methods
        public async Task SendNewAssignmentNotificationAsync(int assignmentId, int courseInstanceId)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null) return;

            if (assignment.Status != AssignmentStatusEnum.Upcoming.ToString() &&
                assignment.Status != AssignmentStatusEnum.Active.ToString())
            {
                return;
            }

            var students = await _courseStudentRepository.GetByCourseInstanceIdAsync(courseInstanceId);

            foreach (var student in students)
            {
                var request = new CreateNotificationRequest
                {
                    UserId = student.UserId,
                    Title = assignment.Status == AssignmentStatusEnum.Upcoming.ToString()
                ? "Upcoming Assignment"
                : "New Assignment Available",
                    Message = assignment.Status == AssignmentStatusEnum.Upcoming.ToString()
                ? $"A new assignment '{assignment.Title}' is upcoming. Start date: {assignment.StartDate}"
                : $"A new assignment '{assignment.Title}' is now active. Deadline: {assignment.Deadline}",
                    Type = "AssignmentNew",
                    AssignmentId = assignmentId
                };
                await CreateNotificationAsync(request);
            }
        }

        public async Task SendDeadlineReminderAsync(int assignmentId)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null) return;

            var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);
            var submittedUserIds = submissions.Select(s => s.UserId).ToHashSet();

            var students = await _courseStudentRepository.GetByCourseInstanceIdAsync(assignment.CourseInstanceId);
            var missingStudents = students.Where(s => !submittedUserIds.Contains(s.UserId));

            foreach (var student in missingStudents)
            {
                var request = new CreateNotificationRequest
                {
                    UserId = student.UserId,
                    Title = "Assignment Deadline Reminder",
                    Message = $"Reminder: Assignment '{assignment.Title}' is due soon on {assignment.Deadline}. Please submit your work.",
                    Type = "DeadlineReminder",
                    AssignmentId = assignmentId
                };
                await CreateNotificationAsync(request);
            }
        }

        public async Task SendMissingSubmissionNotificationAsync(int assignmentId, int instructorId)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null) return;

            var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);
            var submittedUserIds = submissions.Select(s => s.UserId).ToHashSet();

            var students = await _courseStudentRepository.GetByCourseInstanceIdAsync(assignment.CourseInstanceId);
            var missingStudents = students.Where(s => !submittedUserIds.Contains(s.UserId)).ToList();

            if (!missingStudents.Any()) return;

            var missingNames = new List<string>();
            foreach (var student in missingStudents)
            {
                var user = await _userRepository.GetByIdAsync(student.UserId);
                missingNames.Add(user?.FirstName + " " + user?.LastName ?? "Unknown");
            }

            var request = new CreateNotificationRequest
            {
                UserId = instructorId,
                Title = "Missing Submissions Report",
                Message = $"The following students have not submitted for '{assignment.Title}': {string.Join(", ", missingNames)}",
                Type = "MissingSubmission",
                AssignmentId = assignmentId
            };
            await CreateNotificationAsync(request);
        }

        public async Task SendMissingReviewNotificationAsync(int reviewAssignmentId)
        {
            var reviewAssignment = await _reviewAssignmentRepository.GetByIdAsync(reviewAssignmentId);
            if (reviewAssignment == null || reviewAssignment.Status == "Completed") return;

            var submission = await _submissionRepository.GetByIdAsync(reviewAssignment.SubmissionId);
            var assignment = submission != null ? await _assignmentRepository.GetByIdAsync(submission.AssignmentId) : null;

            var instructors = await _courseInstructorRepository.GetByCourseInstanceIdAsync(assignment?.CourseInstanceId ?? 0);

            foreach (var instructor in instructors)
            {
                var reviewer = await _userRepository.GetByIdAsync(reviewAssignment.ReviewerUserId);
                var request = new CreateNotificationRequest
                {
                    UserId = instructor.UserId,
                    Title = "Missing Review Report",
                    Message = $"Student {reviewer?.FirstName} {reviewer?.LastName} has not completed their assigned review for assignment '{assignment?.Title}'",
                    Type = "MissingReview",
                    ReviewAssignmentId = reviewAssignmentId
                };
                await CreateNotificationAsync(request);
            }
        }

        public async Task<BaseResponse<bool>> SendAnnouncementToAllAsync(SendAnnouncementRequest request)
        {
            try
            {
                var allUsers = await _userRepository.GetAllAsync();
                var userIds = allUsers.Select(u => u.Id).ToList();

                return await SendBulkNotifications(userIds, request);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error sending announcement to all users: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<bool>> SendAnnouncementToUsersAsync(SendAnnouncementRequest request, List<int> userIds)
        {
            return await SendBulkNotifications(userIds, request);
        }

        public async Task<BaseResponse<bool>> SendAnnouncementToCourseAsync(SendAnnouncementRequest request, int courseInstanceId)
        {
            try
            {
                var students = await _courseStudentRepository.GetByCourseInstanceIdAsync(courseInstanceId);
                var studentIds = students.Select(s => s.UserId).ToList();

                var instructors = await _courseInstructorRepository.GetByCourseInstanceIdAsync(courseInstanceId);
                var instructorIds = instructors.Select(i => i.UserId).ToList();

                var allUserIds = studentIds.Union(instructorIds).ToList();

                return await SendBulkNotifications(allUserIds, request);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error sending announcement to course: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500, false);
            }
        }

        private async Task<BaseResponse<bool>> SendBulkNotifications(List<int> userIds, SendAnnouncementRequest request)
        {
            var successCount = 0;
            var errorCount = 0;

            foreach (var userId in userIds)
            {
                try
                {
                    var notificationRequest = new CreateNotificationRequest
                    {
                        UserId = userId,
                        Title = request.Title,
                        Message = request.Message,
                        Type = "Announcement",
                        SenderUserId = request.SenderUserId
                    };

                    var result = await CreateNotificationAsync(notificationRequest);
                    if (result.StatusCode == StatusCodeEnum.Created_201)
                        successCount++;
                    else
                        errorCount++;
                }
                catch (Exception)
                {
                    errorCount++;
                }
            }

            var message = $"Announcement sent successfully to {successCount} users.";
            if (errorCount > 0)
                message += $" Failed for {errorCount} users.";

            return new BaseResponse<bool>(message, StatusCodeEnum.OK_200, true);
        }

        public async Task SendGradesPublishedNotificationToStudents(int assignmentId)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null) return;

            var students = await _courseStudentRepository.GetByCourseInstanceIdAsync(assignment.CourseInstanceId);

            foreach (var student in students)
            {
                var request = new CreateNotificationRequest
                {
                    UserId = student.UserId,
                    Title = "Grades Published",
                    Message = $"The grades for assignment '{assignment.Title}' have been published.",
                    Type = "GradesPublished",
                    AssignmentId = assignment.AssignmentId
                };

                await CreateNotificationAsync(request);
            }
        }

        public async Task SendInstructorAssignedNotificationAsync(int userId, int courseInstanceId)
        {
            var courseInstance = await _courseInstanceRepository.GetByIdWithRelationsAsync(courseInstanceId);

            if (courseInstance == null)
                return;

            var courseName = courseInstance.Course?.CourseName ?? "Unknown Course";
            var courseCode = courseInstance.Course?.CourseCode ?? "";
            var section = courseInstance.SectionCode ?? "";

            var request = new CreateNotificationRequest
            {
                UserId = userId,
                Type = "InstructorAssigned",
                Title = "New Teaching Assignment",
                Message = $"You have been assigned to teach **{courseCode} - {courseName} ({section})**.",
                CourseInstanceId = courseInstanceId
            };

            await CreateNotificationAsync(request);
        }

        public async Task SendDeadlineExtendedNotificationAsync(int assignmentId, DateTime newDeadline)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null) return;

            // Lấy danh sách sinh viên của CourseInstance
            var students = await _courseStudentRepository.GetByCourseInstanceIdAsync(assignment.CourseInstanceId);

            foreach (var student in students)
            {
                var request = new CreateNotificationRequest
                {
                    UserId = student.UserId,
                    Title = "Assignment Deadline Extended",
                    Message = $"The deadline for assignment '{assignment.Title}' has been extended to {newDeadline}.",
                    Type = "DeadlineExtended",
                    AssignmentId = assignment.AssignmentId
                };

                await CreateNotificationAsync(request);
            }
        }


    }
}