using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Request.Semester;
using Service.RequestAndResponse.Response.Semester;

namespace Service.Mapping
{
    public class SemesterMappingProfile : Profile
    {
        public SemesterMappingProfile()
        {
            CreateMap<CreateSemesterRequest, Semester>();
            CreateMap<UpdateSemesterRequest, Semester>();
            CreateMap<Semester, SemesterResponse>();
        }
    }
}