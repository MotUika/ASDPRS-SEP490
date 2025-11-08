using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Response.RegradeRequest;

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
            CreateMap<User, UserInfoRegradeResponse>();
            CreateMap<Assignment, AssignmentInfoRegradeResponse>();
        }
    }
}