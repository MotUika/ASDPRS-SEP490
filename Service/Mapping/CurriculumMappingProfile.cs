using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Request.Curriculum;
using Service.RequestAndResponse.Response.Curriculum;

namespace Service.Mapping
{
    public class CurriculumMappingProfile : Profile
    {
        public CurriculumMappingProfile()
        {
            CreateMap<CreateCurriculumRequest, Curriculum>();
            CreateMap<UpdateCurriculumRequest, Curriculum>();
            CreateMap<Curriculum, CurriculumResponse>();
        }
    }
}