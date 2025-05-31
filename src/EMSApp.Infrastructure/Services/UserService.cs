using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Entities;

namespace EMSApp.Infrastructure;

public class UserService : IUserService
{
    private readonly IUserRepository _repo;
    public UserService(IUserRepository repo) => _repo = repo;
    public async Task<User> CreateAsync(string email, string username, string password, string departmentId, CancellationToken ct)
    {
        var user = new User(email, username, password, departmentId);
        await _repo.CreateAsync(user, ct);
        return user;
    }

    public Task<User?> GetByIdAsync(string id, CancellationToken ct)
    {
        return _repo.GetByIdAsync(id, ct);
    }

    public Task<IReadOnlyList<User>> ListByDepartmentAsync(string departmentId, CancellationToken ct)
    {
        return _repo.ListByDepartmentAsync(departmentId, ct);
    }

    public Task UpdateAsync(User user, CancellationToken ct)
    {
        return _repo.UpdateAsync(user, false, ct);
    }

    public Task DeleteAsync(string id, CancellationToken ct)
    {
        return _repo.DeleteAsync(id, ct);
    }

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct)
    {
        return _repo.GetAllAsync(ct);
    }

    public Task<IReadOnlyList<User>> ListByRoleAsync(UserRole? role, CancellationToken ct)
    {
        return _repo.ListByRoleAsync(role, ct);
    }
}
