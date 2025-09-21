using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Request.AcademicYear;
using Service.RequestAndResponse.Response.AcademicYear;

namespace Service.Mapping
{
    public class AcademicYearMappingProfile : Profile
    {
        public AcademicYearMappingProfile()
        {
            CreateMap<CreateAcademicYearRequest, AcademicYear>();
            CreateMap<UpdateAcademicYearRequest, AcademicYear>();
            CreateMap<AcademicYear, AcademicYearResponse>();
        }
    }
}