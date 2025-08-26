namespace EMSApp.Api;

public sealed record CreateUserRequest(
    string Email,
    string Username,
    string PasswordHash,
    string DepartmentId
);
