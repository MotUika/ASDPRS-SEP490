using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Response.RegradeRequest;

namespace Service.Mapping
{
    public class RegradeRequestProfile : Profile
    {
        public RegradeRequestProfile()
        {
            // RegradeRequest -> RegradeRequestResponse
            CreateMap<RegradeRequest, RegradeRequestResponse>()
                .ForMember(dest => dest.RequestedByStudent, opt => opt.MapFrom(src => new UserInfoRegradeResponse
                {
                    UserId = src.Submission.UserId,
                    FullName = src.Submission.User != null ? $"{src.Submission.User.FirstName} {src.Submission.User.LastName}".Trim() : null,
                    Email = src.Submission.User.Email
                }))
                .ForMember(dest => dest.ReviewedByInstructor, opt => opt.MapFrom(src => new UserInfoRegradeResponse
                {
                    UserId = src.ReviewedByInstructorId ?? 0,
                    FullName = src.ReviewedByInstructor != null ? $"{src.ReviewedByInstructor.FirstName} {src.ReviewedByInstructor.LastName}".Trim() : null,
                    Email = src.ReviewedByInstructor.Email
                    
                }))
                .ForMember(dest => dest.Assignment, opt => opt.MapFrom(src => src.Submission.Assignment));

            // Submission -> SubmissionInfoResponse
            CreateMap<Submission, SubmissionInfoResponse>();

            // User -> UserInfoRegradeResponse (dùng cho mapping khác nếu cần)
            CreateMap<User, UserInfoRegradeResponse>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}".Trim()));

            // Assignment -> AssignmentInfoRegradeResponse
            CreateMap<Assignment, AssignmentInfoRegradeResponse>()
                .ForMember(
                    dest => dest.CourseName,
                    opt => opt.MapFrom(src => src.CourseInstance != null && src.CourseInstance.Course != null
                                               ? src.CourseInstance.Course.CourseName
                                               : null)
                );
            CreateMap<Submission, SubmissionInfoResponse>()
                .ForMember(dest => dest.InstructorScore, opt => opt.MapFrom(src => src.InstructorScore))
                .ForMember(dest => dest.PeerAverageScore, opt => opt.MapFrom(src => src.PeerAverageScore))
                .ForMember(dest => dest.FinalScore, opt => opt.MapFrom(src => src.FinalScore))
                .ForMember(dest => dest.Feedback, opt => opt.MapFrom(src => src.Feedback))
                .ForMember(dest => dest.GradedAt, opt => opt.MapFrom(src => src.GradedAt));
        }
    }
}
