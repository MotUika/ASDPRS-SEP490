using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Response.RegradeRequest;
using System.Linq;

namespace Service.Mapping
{
    public class RegradeRequestProfile : Profile
    {
        public RegradeRequestProfile()
        {
            CreateMap<RegradeRequest, RegradeRequestResponse>()
                .ForMember(dest => dest.RequestedByStudent, opt => opt.MapFrom(src => src.Submission.User))
                .ForMember(dest => dest.Assignment, opt => opt.MapFrom(src => src.Submission.Assignment))
                .ForMember(dest => dest.ReviewedByInstructor, opt => opt.MapFrom(src => src.ReviewedByInstructor));

            CreateMap<Submission, SubmissionInfoResponse>();

            CreateMap<User, UserInfoRegradeResponse>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}".Trim()));

            CreateMap<Assignment, AssignmentInfoRegradeResponse>()
                .ForMember(
                    dest => dest.CourseName,
                    opt => opt.MapFrom(src => src.CourseInstance != null 
                                               && src.CourseInstance.Course != null
                                               ? src.CourseInstance.Course.CourseName 
                                               : null)
                      );

        }
    }
}
