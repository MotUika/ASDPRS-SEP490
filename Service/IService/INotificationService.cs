using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.Notification;
using Service.RequestAndResponse.Response.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface INotificationService
    {
        Task<BaseResponse<NotificationResponse>> CreateNotificationAsync(CreateNotificationRequest request);       
        Task<BaseResponse<IEnumerable<NotificationResponse>>> GetNotificationsByUserAsync(int userId, bool unreadOnly = false);
        Task<BaseResponse<bool>> MarkAsReadAsync(int notificationId);
        Task<BaseResponse<bool>> MarkAllAsReadAsync(int userId);
        // Methods for specific notifications
        Task SendNewAssignmentNotificationAsync(int assignmentId, int courseInstanceId);
        Task SendDeadlineReminderAsync(int assignmentId);
        Task SendMissingSubmissionNotificationAsync(int assignmentId, int instructorId);
        Task SendMissingReviewNotificationAsync(int reviewAssignmentId);
        Task<BaseResponse<bool>> SendAnnouncementToAllAsync(SendAnnouncementRequest request);
        Task<BaseResponse<bool>> SendAnnouncementToUsersAsync(SendAnnouncementRequest request, List<int> userIds);
        Task<BaseResponse<bool>> SendAnnouncementToCourseAsync(SendAnnouncementRequest request, int courseInstanceId);
        Task SendGradesPublishedNotificationToStudents(int assignmentId);
        Task SendInstructorAssignedNotificationAsync(int userId, int courseInstanceId);
        Task SendDeadlineExtendedNotificationAsync(int assignmentId, DateTime newDeadline);
    }
}
