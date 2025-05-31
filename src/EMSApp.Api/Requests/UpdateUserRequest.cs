using EMSApp.Application;
using EMSApp.Domain.Entities;

namespace EMSApp.Api;

public sealed record UpdateUserRequest(
    string? Email,
    string? Username,
    string? PasswordHash,
    string? DepartmentId,
    decimal? Salary,
    string? JobTitle,
    UserProfileDto? Profile,
    UserRole? Role
);
