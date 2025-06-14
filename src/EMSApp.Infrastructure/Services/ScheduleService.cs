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

    public async Task<Schedule> CreateAsync(string departmentId, string managerId, ShiftType shiftType, DayOfWeek day, TimeOnly startTime, TimeOnly endTime, bool isWorkingDay, CancellationToken ct)
    {
        var schedule = new Schedule(departmentId, managerId, shiftType, day, startTime, endTime, isWorkingDay);
        await _repo.CreateAsync(schedule, ct);
        return schedule;
    }

    public Task<Schedule?> GetByIdAsync(string id, CancellationToken ct)
    {
        return _repo.GetByIdAsync(id, ct);
    }

    public Task<IReadOnlyList<Schedule>> ListByDepartmentAsync(string departmentId, CancellationToken ct)
    {
        return _repo.GetByDepartmentAsync(departmentId, ct);
    }

    public Task<IReadOnlyList<Schedule>> ListByManagerAsync(string managerId, CancellationToken ct)
    {
        return _repo.GetByManagerIdAsync(managerId, ct);
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
}
