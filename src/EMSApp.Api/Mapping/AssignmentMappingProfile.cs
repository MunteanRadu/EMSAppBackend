using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain.Entities;

namespace EMSApp.Api;

public class AssignmentMappingProfile : Profile
{
    public AssignmentMappingProfile()
    {
        CreateMap<Assignment, AssignmentDto>();
    }
}
