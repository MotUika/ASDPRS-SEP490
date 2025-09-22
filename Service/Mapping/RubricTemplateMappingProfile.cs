using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Request.RubricTemplate;
using Service.RequestAndResponse.Response.RubricTemplate;

namespace Service.Mapping
{
    public class RubricTemplateMappingProfile : Profile
    {
        public RubricTemplateMappingProfile()
        {
            CreateMap<CreateRubricTemplateRequest, RubricTemplate>();
            CreateMap<UpdateRubricTemplateRequest, RubricTemplate>();
            CreateMap<RubricTemplate, RubricTemplateResponse>();
        }
    }
}