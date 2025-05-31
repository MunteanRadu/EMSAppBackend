using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain.Entities;

namespace EMSApp.Api;

public class UserProfileMappingProfile : Profile
{
    public UserProfileMappingProfile()
    {
        CreateMap<UserProfile, UserProfileDto>();
    }
}
