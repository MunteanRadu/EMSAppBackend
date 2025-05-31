using EMSApp.Domain;

namespace EMSApp.Application;

public interface IScheduleService
{
    Task<Schedule> CreateAsync(string departmentId, string managerId, DayOfWeek day, TimeOnly startTime, TimeOnly endTime, bool isWorkingDay, CancellationToken ct);
    Task<Schedule?> GetByIdAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<Schedule>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<Schedule>> ListByDepartmentAsync(string departmentId, CancellationToken ct);
    Task<IReadOnlyList<Schedule>> ListByManagerAsync(string managerId, CancellationToken ct);
    Task UpdateAsync(Schedule schedule, CancellationToken ct);
    Task DeleteAsync(string id, CancellationToken ct);
    Task RemoveExceptionAsync(string id, DateOnly exceptionDate, CancellationToken ct);
    Task AddExceptionAsync(string id, DateOnly exceptionDate, CancellationToken ct);
}
