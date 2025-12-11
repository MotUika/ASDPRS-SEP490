using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Request.Major;
using Service.RequestAndResponse.Response.Major;

namespace Service.Mapping
{
    public class MajorMappingProfile : Profile
    {
        public MajorMappingProfile()
        {
            CreateMap<CreateMajorRequest, Major>();
            CreateMap<UpdateMajorRequest, Major>();
            CreateMap<Major, MajorResponse>();
        }
    }
}
