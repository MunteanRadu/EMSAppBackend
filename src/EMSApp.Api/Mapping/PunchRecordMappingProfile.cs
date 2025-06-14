using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain;

namespace EMSApp.Api;

public class PunchRecordMappingProfile : Profile
{
    public PunchRecordMappingProfile()
    {
        CreateMap<BreakSession, BreakSessionDto>();
        CreateMap<PunchRecord, PunchRecordDto>();
    }
}
