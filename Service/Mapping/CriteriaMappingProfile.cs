using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Request.Criteria;
using Service.RequestAndResponse.Response.Criteria;

namespace Service.Mapping
{
    public class CriteriaMappingProfile : Profile
    {
        public CriteriaMappingProfile()
        {
            CreateMap<CreateCriteriaRequest, Criteria>();
            CreateMap<UpdateCriteriaRequest, Criteria>();
            CreateMap<Criteria, CriteriaResponse>();
        }
    }
}