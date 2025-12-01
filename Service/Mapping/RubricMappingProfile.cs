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
            // Request to Entity
            CreateMap<CreateRubricRequest, Rubric>();
            CreateMap<UpdateRubricRequest, Rubric>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) =>
                    srcMember != null && !(srcMember is int intValue && intValue == 0)));

            // Entity to Response
            CreateMap<Rubric, RubricResponse>()
                .ForMember(dest => dest.TemplateTitle, opt => opt.MapFrom(src => src.Template.Title))
                .ForMember(dest => dest.AssignmentTitle, opt => opt.MapFrom(src => src.Assignment.Title))
                .ForMember(dest => dest.CriteriaCount, opt => opt.MapFrom(src => src.Criteria.Count))
                .ForMember(dest => dest.CourseName,
                             opt => opt.MapFrom(src => src.Assignment.CourseInstance.Course.CourseName))
                .ForMember(dest => dest.ClassName,
                            opt => opt.MapFrom(src =>
                        $"{src.Assignment.CourseInstance.Course.CourseName} - {src.Assignment.CourseInstance.SectionCode}"));
        }
    }
}