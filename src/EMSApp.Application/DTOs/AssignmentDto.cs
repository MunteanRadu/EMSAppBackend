namespace EMSApp.Application;

public sealed record AssignmentDto(
     string Id,
     string Title,
     string Description,
     DateTime DueDate,
     string DepartmentId,
     string AssignedToId,
     string Status
);
