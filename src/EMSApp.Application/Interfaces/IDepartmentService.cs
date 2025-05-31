using EMSApp.Domain.Entities;

namespace EMSApp.Application;

public interface IDepartmentService
{
    Task<Department> CreateAsync(string name, CancellationToken ct);
    Task<Department?> GetByIdAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken ct);
    Task UpdateAsync(Department department, CancellationToken ct);
    Task DeleteAsync(string id, CancellationToken ct);
    Task AddEmployeeAsync(string id, string userId, CancellationToken ct);
    Task RemoveEmployeeAsync(string id, string userId, CancellationToken ct);
}
