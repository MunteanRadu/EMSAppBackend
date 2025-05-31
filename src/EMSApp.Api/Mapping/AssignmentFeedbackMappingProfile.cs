using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain;

namespace EMSApp.Api;

public class AssignmentFeedbackMappingProfile : Profile
{
    public AssignmentFeedbackMappingProfile()
    {
        CreateMap<AssignmentFeedback, AssignmentFeedbackDto>();
    }
}
