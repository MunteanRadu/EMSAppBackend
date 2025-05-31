using EMSApp.Domain.Entities;

namespace EMSApp.Api;

public sealed record UpdateAssignmentRequest(
     string? Title ,
     string? Description,
     DateTime? DueDate,
     string? AssignedToId,
     AssignmentStatus? Status
);
