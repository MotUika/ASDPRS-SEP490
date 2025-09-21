using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Request.User;
using Service.RequestAndResponse.Response.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            // Request to Entity
            CreateMap<CreateUserRequest, User>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<UpdateUserRequest, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()); // Don't map password hash from update request

            // Entity to Response
            CreateMap<User, UserResponse>()
                .ForMember(dest => dest.CampusName, opt => opt.MapFrom(src => src.Campus != null ? src.Campus.CampusName : string.Empty));
        }
    }
}
