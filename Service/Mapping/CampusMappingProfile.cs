using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Request.Campus;
using Service.RequestAndResponse.Response.Campus;

namespace Service.Mapping
{
    public class CampusMappingProfile : Profile
    {
        public CampusMappingProfile()
        {
            CreateMap<CreateCampusRequest, Campus>();
            CreateMap<UpdateCampusRequest, Campus>();
            CreateMap<Campus, CampusResponse>();
        }
    }
}