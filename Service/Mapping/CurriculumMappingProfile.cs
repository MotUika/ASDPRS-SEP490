using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Request.Curriculum;
using Service.RequestAndResponse.Response.Curriculum;

namespace Service.Mapping
{
    public class CurriculumProfile : Profile
    {
        public CurriculumProfile()
        {
            // Request to Entity
            CreateMap<CreateCurriculumRequest, Curriculum>();
            CreateMap<UpdateCurriculumRequest, Curriculum>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Entity to Response
            CreateMap<Curriculum, CurriculumResponse>()
                .ForMember(dest => dest.CampusName, opt => opt.MapFrom(src => src.Campus.CampusName))
                .ForMember(dest => dest.MajorName, opt => opt.MapFrom(src => src.Major.MajorName))
                .ForMember(dest => dest.CourseCount, opt => opt.MapFrom(src => src.Courses.Count));
        }
    }
}