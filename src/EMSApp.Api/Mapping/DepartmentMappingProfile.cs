using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain.Entities;

namespace EMSApp.Api;

public class DepartmentMappingProfile : Profile
{
    public DepartmentMappingProfile()
    {
        CreateMap<Department, DepartmentDto>();
    }
}
