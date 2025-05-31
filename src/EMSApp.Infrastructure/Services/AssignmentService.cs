using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Entities;
using System.Linq.Expressions;

namespace EMSApp.Infrastructure;

public class AssignmentService : IAssignmentService
{
    private readonly IAssignmentRepository _repo;
    public AssignmentService(IAssignmentRepository repo)
    {
        _repo = repo;
    }

    public async Task<Assignment> CreateAsync(string title, string description, DateTime dueDate, string departmentId, string managerId, CancellationToken ct)
    {
        var assignment = new Assignment(title, description, dueDate, departmentId, managerId);
        await _repo.CreateAsync(assignment, ct);
        return assignment;
    }

    public Task<Assignment?> GetByIdAsync(string id, CancellationToken ct)
    {
        return _repo.GetByIdAsync(id, ct);
    }

    public Task<IReadOnlyList<Assignment>> ListAsync(Expression<Func<Assignment, bool>>? predicate, CancellationToken ct)
    {
        return _repo.ListAsync(predicate, ct);
    }

    public Task UpdateAsync(Assignment assignment, CancellationToken ct)
    {
        return _repo.UpdateAsync(assignment, false, ct);
    }

    public Task DeleteAsync(string id, CancellationToken ct)
    {
        return _repo.DeleteAsync(id, ct);
    }

    public Task<IReadOnlyList<Assignment>> GetAllAsync(CancellationToken ct)
    {
        return _repo.GetAllAsync(ct);
    }
}
