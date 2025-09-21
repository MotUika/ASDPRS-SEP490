using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Request.Course;
using Service.RequestAndResponse.Response.Course;

namespace Service.Mapping
{
    public class CourseMappingProfile : Profile
    {
        public CourseMappingProfile()
        {
            CreateMap<CreateCourseRequest, Course>();
            CreateMap<UpdateCourseRequest, Course>();
            CreateMap<Course, CourseResponse>();
        }
    }
}