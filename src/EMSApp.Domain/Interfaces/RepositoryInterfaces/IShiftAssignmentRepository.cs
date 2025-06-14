namespace EMSApp.Domain;

public interface IShiftAssignmentRepository
{
    Task<IEnumerable<ShiftAssignment>> GetByDepartmentAndWeekAsync(string departmentId, DateOnly weekStart, CancellationToken ct);
    Task<IEnumerable<ShiftAssignment>> GetByUserAndWeekAsync(string userId, DateOnly weekStart, CancellationToken ct);
    Task<IReadOnlyList<ShiftAssignment>> GetAllAsync(CancellationToken ct);
    Task AddAsync(ShiftAssignment assignment, CancellationToken ct);
    Task AddManyAsync(IEnumerable<ShiftAssignment> assignments, CancellationToken ct);
    Task DeleteByDepartmentAndWeekAsync(string departmentId, DateOnly weekStart, CancellationToken ct);
    Task<ShiftAssignment?> GetForUserOnDateAsync(string userId, DateOnly date, CancellationToken ct);
}

