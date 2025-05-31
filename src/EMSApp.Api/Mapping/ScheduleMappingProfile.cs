using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain;

namespace EMSApp.Api;

public class ScheduleMappingProfile : Profile
{
    public ScheduleMappingProfile()
    {
        CreateMap<Schedule, ScheduleDto>();
    }
}
