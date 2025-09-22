using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Request.CriteriaTemplate;
using Service.RequestAndResponse.Response.CriteriaTemplate;

namespace Service.Mapping
{
    public class CriteriaTemplateMappingProfile : Profile
    {
        public CriteriaTemplateMappingProfile()
        {
            CreateMap<CreateCriteriaTemplateRequest, CriteriaTemplate>();
            CreateMap<UpdateCriteriaTemplateRequest, CriteriaTemplate>();
            CreateMap<CriteriaTemplate, CriteriaTemplateResponse>();
        }
    }
}