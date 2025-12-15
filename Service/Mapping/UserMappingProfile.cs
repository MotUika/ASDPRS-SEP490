using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Request.User;
using Service.RequestAndResponse.Response.User;
using System.Linq;

namespace Service.Mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            // Request to Entity
            CreateMap<CreateUserRequest, User>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow.AddHours(7)));

            CreateMap<UpdateUserRequest, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
            // Entity to Response
            CreateMap<User, UserResponse>()
            .ForMember(dest => dest.CampusName,
            opt => opt.MapFrom(src => src.Campus != null ? src.Campus.CampusName : null))
            .ForMember(dest => dest.MajorName,
            opt => opt.MapFrom(src => src.Major != null ? src.Major.MajorName : null));
            CreateMap<User, UserDetailResponse>()
            .IncludeBase<User, UserResponse>();
        }
    }
}