using EMSApp.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace EMSApp.Domain;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(Department department, CancellationToken cancellationToken = default);
    Task UpdateAsync(Department department, bool isUpsert, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
