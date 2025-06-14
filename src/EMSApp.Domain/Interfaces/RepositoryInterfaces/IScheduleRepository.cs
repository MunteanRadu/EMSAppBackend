namespace EMSApp.Domain;

public interface IScheduleRepository
{
    Task<Schedule?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Schedule>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Schedule>> GetByDepartmentAsync(string departmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Schedule>> GetByManagerIdAsync(string managerId, CancellationToken cancellationToken = default);
    Task CreateAsync(Schedule schedule, CancellationToken cancellationToken = default);
    Task UpdateAsync(Schedule schedule, bool isUpsert, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
