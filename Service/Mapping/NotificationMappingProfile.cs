using AutoMapper;
using BussinessObject.Models;
using Service.RequestAndResponse.Request.Notification;
using Service.RequestAndResponse.Response.Notification;

namespace Service.Mapping
{
    public class NotificationMappingProfile : Profile
    {
        public NotificationMappingProfile()
        {
            CreateMap<CreateNotificationRequest, Notification>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsRead, opt => opt.Ignore())
                .ForMember(dest => dest.NotificationId, opt => opt.Ignore());

            CreateMap<Notification, NotificationResponse>();
        }
    }
}