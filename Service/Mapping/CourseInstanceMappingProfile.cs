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
            // Request to Entity
            CreateMap<CreateCourseInstanceRequest, CourseInstance>();
            CreateMap<UpdateCourseInstanceRequest, CourseInstance>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) =>
                    srcMember != null && !(srcMember is int intValue && intValue == 0)));

            // Entity to Response
            CreateMap<CourseInstance, CourseInstanceResponse>()
                .ForMember(dest => dest.CourseCode, opt => opt.MapFrom(src => src.Course.CourseCode))
                .ForMember(dest => dest.CourseName, opt => opt.MapFrom(src => src.Course.CourseName))
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Semester.Name))
                .ForMember(dest => dest.CampusName, opt => opt.MapFrom(src => src.Campus.CampusName))
                .ForMember(dest => dest.InstructorCount, opt => opt.MapFrom(src => src.CourseInstructors.Count))
                .ForMember(dest => dest.StudentCount, opt => opt.MapFrom(src => src.CourseStudents.Count))
                .ForMember(dest => dest.AssignmentCount, opt => opt.MapFrom(src => src.Assignments.Count));
        }
    }
}