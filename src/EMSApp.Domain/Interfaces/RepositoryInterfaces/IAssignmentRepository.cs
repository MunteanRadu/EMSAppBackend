using EMSApp.Domain.Entities;
using System.Linq.Expressions;

namespace EMSApp.Domain;

public interface IAssignmentRepository
{
    Task<Assignment?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Assignment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Assignment>> ListByAssigneeAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Assignment>> ListByStatusAsync(AssignmentStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Assignment>> ListOverdueAsync(DateTime asOf, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Assignment>> ListAsync(Expression<Func<Assignment, bool>>? predicate, CancellationToken cancellationToken = default);
    Task CreateAsync(Assignment task, CancellationToken cancellationToken = default);
    Task UpdateAsync(Assignment task, bool isUpsert, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
