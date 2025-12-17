using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Request.RubricTemplate;
using Service.RequestAndResponse.Response.RubricTemplate;

namespace Service.Mapping
{
    public class RubricTemplateMappingProfile : Profile
    {
        public RubricTemplateMappingProfile()
        {
            // Request to Entity
            CreateMap<CreateRubricTemplateRequest, RubricTemplate>();
            CreateMap<UpdateRubricTemplateRequest, RubricTemplate>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Entity to Response
            CreateMap<RubricTemplate, RubricTemplateResponse>()
                .ForMember(dest => dest.CreatedByUserName, opt => opt.MapFrom(src =>
                    src.CreatedByUser != null ?
                    $"{src.CreatedByUser.FirstName} {src.CreatedByUser.LastName}" :
                    string.Empty))
                .ForMember(dest => dest.RubricCount, opt => opt.MapFrom(src => src.Rubrics.Count))
                .ForMember(dest => dest.CriteriaTemplateCount, opt => opt.MapFrom(src => src.CriteriaTemplates.Count))
                .ForMember(dest => dest.MajorId, opt => opt.MapFrom(src => src.MajorId))
                .ForMember(dest => dest.MajorName, opt => opt.MapFrom(src => src.Major != null ? src.Major.MajorName : null))
                .ForMember(dest => dest.CourseId, opt => opt.MapFrom(src => src.CourseId))
                .ForMember(dest => dest.CourseName, opt => opt.MapFrom(src => src.Course != null ? src.Course.CourseName : null));
        }
    }
}