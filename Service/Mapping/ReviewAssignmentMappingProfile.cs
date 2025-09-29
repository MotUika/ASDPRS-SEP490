using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Response.ReviewAssignment;

namespace Service.Mapping
{
    public class ReviewAssignmentMappingProfile : Profile
    {
        public ReviewAssignmentMappingProfile()
        {
            CreateMap<ReviewAssignment, ReviewAssignmentResponse>()
                .ForMember(dest => dest.ReviewerName, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewerEmail, opt => opt.Ignore())
                .ForMember(dest => dest.AssignmentTitle, opt => opt.Ignore())
                .ForMember(dest => dest.StudentName, opt => opt.Ignore())
                .ForMember(dest => dest.StudentCode, opt => opt.Ignore())
                .ForMember(dest => dest.CourseName, opt => opt.Ignore())
                .ForMember(dest => dest.Reviews, opt => opt.Ignore())
                .ForMember(dest => dest.IsOverdue, opt => opt.Ignore())
                .ForMember(dest => dest.DaysUntilDeadline, opt => opt.Ignore());
        }
    }
}