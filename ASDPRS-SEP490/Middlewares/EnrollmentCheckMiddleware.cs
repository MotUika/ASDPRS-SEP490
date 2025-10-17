using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Middlewares
{
    public class EnrollmentCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public EnrollmentCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Chỉ kiểm tra các route cần enrollment
            var path = context.Request.Path.ToString().ToLower();

            // Danh sách các endpoint cần kiểm tra enrollment
            var protectedPaths = new[]
            {
                "/api/assignment/",
                "/api/submission/",
                "/api/review/",
                "/api/studentreview/",
                "/api/coursestudent/" // trừ endpoint enroll
            };

            bool needsEnrollmentCheck = protectedPaths.Any(p => path.Contains(p)) &&
                                       !path.Contains("/enroll") &&
                                       !path.Contains("/import");

            if (needsEnrollmentCheck && context.User.Identity.IsAuthenticated)
            {
                // Lấy courseInstanceId từ route hoặc query
                var (courseInstanceId, studentId) = ExtractIdsFromRequest(context);

                if (courseInstanceId.HasValue && studentId.HasValue)
                {
                    var courseStudentService = context.RequestServices.GetService<ICourseStudentService>();
                    var enrollmentCheck = await courseStudentService.IsStudentEnrolledAsync(courseInstanceId.Value, studentId.Value);

                    if (!enrollmentCheck.Data)
                    {
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsJsonAsync(new BaseResponse<object>(
                            $"Access denied: {enrollmentCheck.Message}. Please enroll in the course first.",
                            StatusCodeEnum.Forbidden_403,
                            null
                        ));
                        return;
                    }
                }
            }

            await _next(context);
        }

        private (int? courseInstanceId, int? studentId) ExtractIdsFromRequest(HttpContext context)
        {
            int? courseInstanceId = null;
            int? studentId = null;

            // Lấy từ route values
            if (context.Request.RouteValues.TryGetValue("courseInstanceId", out var courseInstanceIdObj))
            {
                int.TryParse(courseInstanceIdObj?.ToString(), out int tempCourseInstanceId);
                courseInstanceId = tempCourseInstanceId;
            }

            // Lấy từ query string
            if (context.Request.Query.TryGetValue("courseInstanceId", out var courseInstanceIdQuery))
            {
                int.TryParse(courseInstanceIdQuery, out int tempCourseInstanceId);
                courseInstanceId = tempCourseInstanceId;
            }

            // Lấy studentId từ claim
            var studentIdClaim = context.User.FindFirst("userId") ?? context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (studentIdClaim != null && int.TryParse(studentIdClaim.Value, out int tempStudentId))
            {
                studentId = tempStudentId;
            }

            return (courseInstanceId, studentId);
        }
    }
}