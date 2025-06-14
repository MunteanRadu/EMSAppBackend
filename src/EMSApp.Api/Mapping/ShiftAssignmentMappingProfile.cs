using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain;

namespace EMSApp.Api;

public class ShiftAssignmentMappingProfile : Profile
{
    public ShiftAssignmentMappingProfile()
    {
        CreateMap<ShiftAssignment, ShiftFromAiDto>();
    }
}
