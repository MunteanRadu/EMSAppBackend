using EMSApp.Domain.Entities;

namespace EMSApp.Application;

public sealed record UserDto(
     string Id,
     string Email,
     string Username,
     string DepartmentId,
     UserRole? Role,
     UserProfileDto? Profile,
     decimal Salary,
     string JobTitle
);
