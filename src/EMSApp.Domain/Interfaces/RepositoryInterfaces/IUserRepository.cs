using EMSApp.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace EMSApp.Domain;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> ListByDepartmentAsync(string departmentId, CancellationToken cancellationToken = default);
    Task CreateAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, bool isUpsert, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> ListByRoleAsync(UserRole? role, CancellationToken ct);
}
