using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Response.AISummary;
using Service.RequestAndResponse.Response.Submission;

namespace Service.Mapping
{
    public class SubmissionProfile : Profile
    {
        public SubmissionProfile()
        {
            CreateMap<Submission, SubmissionResponse>();
            CreateMap<Assignment, AssignmentInfoResponse>();
            CreateMap<User, UserInfoResponse>();
            CreateMap<ReviewAssignment, SubmissionReviewAssignmentResponse>();
            CreateMap<AISummary, AISummaryResponse>();
            CreateMap<RegradeRequest, RegradeRequestResponse>();
        }
    }
}