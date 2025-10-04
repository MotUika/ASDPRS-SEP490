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
            // Request to Entity
            CreateMap<CreateCourseRequest, Course>();
            CreateMap<UpdateCourseRequest, Course>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) =>
                    srcMember != null && !(srcMember is int intValue && intValue == 0)));

            // Entity to Response
            CreateMap<Course, CourseResponse>()
                .ForMember(dest => dest.CurriculumName, opt => opt.MapFrom(src => src.Curriculum.CurriculumName))
                .ForMember(dest => dest.MajorName, opt => opt.MapFrom(src => src.Curriculum.Major.MajorName))
                .ForMember(dest => dest.CourseInstanceCount, opt => opt.MapFrom(src => src.CourseInstances.Count));
        }
    }
}