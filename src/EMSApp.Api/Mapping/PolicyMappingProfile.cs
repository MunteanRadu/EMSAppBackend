using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain;

namespace EMSApp.Api;

public class PolicyMappingProfile : Profile
{
    public PolicyMappingProfile()
    {
        CreateMap<Policy, PolicyDto>();
    }
}
