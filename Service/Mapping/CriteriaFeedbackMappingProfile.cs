using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Request.CriteriaFeedback;
using Service.RequestAndResponse.Response.CriteriaFeedback;

namespace Service.Mapping
{
    public class CriteriaFeedbackMappingProfile : Profile
    {
        public CriteriaFeedbackMappingProfile()
        {
            CreateMap<CreateCriteriaFeedbackRequest, CriteriaFeedback>();
            CreateMap<UpdateCriteriaFeedbackRequest, CriteriaFeedback>();
            CreateMap<CriteriaFeedback, CriteriaFeedbackResponse>();
        }
    }
}