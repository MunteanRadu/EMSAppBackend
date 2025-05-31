using EMSApp.Domain;
using EMSApp.Domain.Entities;
using System.Linq.Expressions;

namespace EMSApp.Application;

public interface IAssignmentService
{
    Task<Assignment> CreateAsync(string title, string description, DateTime dueDate, string departmentId, string managerId, CancellationToken ct);
    Task<Assignment?> GetByIdAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<Assignment>> ListAsync(Expression<Func<Assignment, bool>>? predicate, CancellationToken ct);
    Task UpdateAsync(Assignment assignment, CancellationToken ct);
    Task DeleteAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<Assignment>> GetAllAsync(CancellationToken ct);
}
