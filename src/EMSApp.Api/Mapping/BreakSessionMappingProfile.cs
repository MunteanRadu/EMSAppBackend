using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain;

namespace EMSApp.Api;

public class BreakSessionMappingProfile : Profile
{
    public BreakSessionMappingProfile()
    {
        CreateMap<BreakSession, BreakSessionDto>();
    }
}
