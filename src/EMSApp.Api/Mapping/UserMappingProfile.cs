using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain.Entities;

namespace EMSApp.Api;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>();
    }
}
