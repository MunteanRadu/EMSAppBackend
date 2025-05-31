using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain;

namespace EMSApp.Api;

public class LeaveRequestMappingProfile : Profile
{
    public LeaveRequestMappingProfile()
    {
        CreateMap<LeaveRequest, LeaveRequestDto>();
    }
}
