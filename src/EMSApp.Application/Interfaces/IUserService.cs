using EMSApp.Domain.Entities;

namespace EMSApp.Application;

public interface IUserService
{
    Task<User> CreateAsync(string email, string username, string password, string departmentId, CancellationToken ct);
    Task<User?> GetByIdAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<User>> ListByDepartmentAsync(string departmentId, CancellationToken ct);
    Task UpdateAsync(User user, CancellationToken ct);
    Task DeleteAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<User>> ListByRoleAsync(UserRole? role, CancellationToken ct);
}
