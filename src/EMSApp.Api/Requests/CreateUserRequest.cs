namespace EMSApp.Api;

public record CreateUserRequest(
    string Email,
    string Username,
    string PasswordHash,
    string DepartmentId
);
