using AutoMapper;
using BussinessObject.Models;
using Repository.IRepository;
using Repository.Repository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Notification;
using Service.RequestAndResponse.Response.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public NotificationService(
            INotificationRepository notificationRepository,
            IMapper mapper,
            IAssignmentRepository assignmentRepository,
            ICourseStudentRepository courseStudentRepository,
            IReviewAssignmentRepository reviewAssignmentRepository,
            ICourseInstructorRepository courseInstructorRepository,
            IUserRepository userRepository,
            ISubmissionRepository submissionRepository)
        {
            _notificationRepository = notificationRepository;
            _mapper = mapper;
            _assignmentRepository = assignmentRepository;
            _courseStudentRepository = courseStudentRepository;
            _reviewAssignmentRepository = reviewAssignmentRepository;
            _courseInstructorRepository = courseInstructorRepository;
            _userRepository = userRepository;
            _submissionRepository = submissionRepository;
        }

        public async Task<BaseResponse<NotificationResponse>> CreateNotificationAsync(CreateNotificationRequest request)
        {
            try
            {
                var notification = _mapper.Map<Notification>(request);
                notification.CreatedAt = DateTime.UtcNow;
                notification.IsRead = false;

                var created = await _notificationRepository.AddAsync(notification);
                var response = _mapper.Map<NotificationResponse>(created);

                return new BaseResponse<NotificationResponse>("Notification created successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<NotificationResponse>($"Error creating notification: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
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

                var response = _mapper.Map<IEnumerable<NotificationResponse>>(notifications);

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

        // Specific notification methods
        public async Task SendNewAssignmentNotificationAsync(int assignmentId, int courseInstanceId)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null) return;

            var students = await _courseStudentRepository.GetByCourseInstanceIdAsync(courseInstanceId);

            foreach (var student in students)
            {
                var request = new CreateNotificationRequest
                {
                    UserId = student.UserId,
                    Title = "New Assignment Available",
                    Message = $"A new assignment '{assignment.Title}' has been created. Deadline: {assignment.Deadline}",
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
    }
}
