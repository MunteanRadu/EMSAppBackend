namespace EMSApp.Api;

public sealed record CreateAssignmentRequest(
     string Title, 
     string Description, 
     DateTime DueDate,
     string DepartmentId,
     string ManagerId
);
