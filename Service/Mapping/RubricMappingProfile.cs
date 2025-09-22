using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Request.Rubric;
using Service.RequestAndResponse.Response.Rubric;

namespace Service.Mapping
{
    public class RubricMappingProfile : Profile
    {
        public RubricMappingProfile()
        {
            CreateMap<CreateRubricRequest, Rubric>();
            CreateMap<UpdateRubricRequest, Rubric>();
            CreateMap<Rubric, RubricResponse>();
        }
    }
}