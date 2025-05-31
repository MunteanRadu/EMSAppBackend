using EMSApp.Application;
using EMSApp.Domain;

namespace EMSApp.Infrastructure;

public class ScheduleService : IScheduleService
{
    private readonly IScheduleRepository _repo;
    public ScheduleService(IScheduleRepository repo)
    {
        _repo = repo;
    }

    public async Task<Schedule> CreateAsync(string departmentId, string managerId, DayOfWeek day, TimeOnly startTime, TimeOnly endTime, bool isWorkingDay, CancellationToken ct)
    {
        var schedule = new Schedule(departmentId, managerId, day, startTime, endTime, isWorkingDay);
        await _repo.CreateAsync(schedule, ct);
        return schedule;
    }

    public Task<Schedule?> GetByIdAsync(string id, CancellationToken ct)
    {
        return _repo.GetByIdAsync(id, ct);
    }

    public Task<IReadOnlyList<Schedule>> ListByDepartmentAsync(string departmentId, CancellationToken ct)
    {
        return _repo.ListByDepartmentAsync(departmentId, ct);
    }

    public Task<IReadOnlyList<Schedule>> ListByManagerAsync(string managerId, CancellationToken ct)
    {
        return _repo.ListByManagerAsync(managerId, ct);
    }

    public Task UpdateAsync(Schedule schedule, CancellationToken ct)
    {
        return _repo.UpdateAsync(schedule, false, ct);
    }

    public Task DeleteAsync(string id, CancellationToken ct)
    {
        return _repo.DeleteAsync(id, ct);
    }

    public Task<IReadOnlyList<Schedule>> GetAllAsync(CancellationToken ct)
    {
        return _repo.GetAllAsync();
    }

    public async Task RemoveExceptionAsync(string id, DateOnly exceptionDate, CancellationToken ct)
    {
        var schedule = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Schedule {id} not found");
        schedule.RemoveException(exceptionDate);
        await _repo.UpdateAsync(schedule, true, ct);
    }

    public async Task AddExceptionAsync(string id, DateOnly exceptionDate, CancellationToken ct)
    {
        var schedule = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Schedule {id} not found");
        schedule.AddException(exceptionDate);
        await _repo.UpdateAsync(schedule, true, ct);
    }
}
