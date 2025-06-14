namespace EMSApp.Domain;

public interface IShiftRuleRepository
{
    Task<ShiftRule?> GetByDepartmentAsync(string departmentId, CancellationToken ct);

    Task UpsertAsync(ShiftRule rule, CancellationToken ct);

    Task DeleteByDepartmentAsync(string departmentId, CancellationToken ct);
}
