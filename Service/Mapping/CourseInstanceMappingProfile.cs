using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Request.CourseInstance;
using Service.RequestAndResponse.Response.CourseInstance;

namespace Service.Mapping
{
    public class CourseInstanceMappingProfile : Profile
    {
        public CourseInstanceMappingProfile()
        {
            CreateMap<CreateCourseInstanceRequest, CourseInstance>();
            CreateMap<UpdateCourseInstanceRequest, CourseInstance>();
            CreateMap<CourseInstance, CourseInstanceResponse>();
        }
    }
}