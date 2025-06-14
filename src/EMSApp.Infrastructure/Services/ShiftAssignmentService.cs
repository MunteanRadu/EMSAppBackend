using EMSApp.Application;
using EMSApp.Application.Interfaces;
using EMSApp.Domain;
using System.Text.Json;

namespace EMSApp.Infrastructure;

public class ShiftAssignmentService : IShiftAssignmentService
{
    private readonly IScheduleRepository _scheduleRepo;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly ILeaveRequestRepository _leaveRepo;
    private readonly IUserRepository _userRepo;
    private readonly IShiftRuleRepository _shiftRuleRepo;
    private readonly IScheduleGenerationService _scheduleGenerationService;
    private readonly IShiftAssignmentRepository _repo;

    public ShiftAssignmentService(
        IScheduleRepository scheduleRepo,
        IDepartmentRepository departmentRepo,
        ILeaveRequestRepository leaveRepo,
        IUserRepository userRepo,
        IShiftRuleRepository shiftRuleRepo,
        IScheduleGenerationService scheduleGenerationService,
        IShiftAssignmentRepository repo)
    {
        _scheduleRepo = scheduleRepo;
        _departmentRepo = departmentRepo;
        _leaveRepo = leaveRepo;
        _userRepo = userRepo;
        _shiftRuleRepo = shiftRuleRepo;
        _scheduleGenerationService = scheduleGenerationService;
        _repo = repo;
    }

    public async Task GenerateWeeklyScheduleAsync(string departmentId, DateOnly weekStart, CancellationToken ct)
    {
        var schedules = await _scheduleRepo.GetByDepartmentAsync(departmentId, ct);
        var department = await _departmentRepo.GetByIdAsync(departmentId, ct);
        var employeeIds = department.Employees;
        var leaveRequests = await _leaveRepo.GetApprovedLeavesForWeekAsync(employeeIds, weekStart, ct);
        var rule = await _shiftRuleRepo.GetByDepartmentAsync(departmentId, ct);

        await _repo.DeleteByDepartmentAndWeekAsync(departmentId, weekStart, ct);

        var lastEndByUser = employeeIds.ToDictionary(id => id, id => (DateTime?)null);
        var consecNights = employeeIds.ToDictionary(id => id, id => 0);

        var newAssignments = new List<ShiftAssignment>();

        foreach (var dayOffset in Enumerable.Range(0, 7))
        {
            var date = weekStart.AddDays(dayOffset);
            var dayOfWeek = date.DayOfWeek;

            var daySchedules = schedules
                .Where(s => s.Day == dayOfWeek && s.IsWorkingDay)
                .OrderBy(s => s.StartTime)
                .ToList();

            var available = new List<string>(employeeIds
                .Where(id => !leaveRequests.Any(lr =>
                    lr.UserId == id &&
                    lr.StartDate <= date &&
                    lr.EndDate >= date)));

            foreach (var tpl in daySchedules)
            {
                var rawMin = tpl.ShiftType switch
                {
                    ShiftType.Shift1 => rule.MinPerShift1,
                    ShiftType.Shift2 => rule.MinPerShift2,
                    ShiftType.NightShift => rule.MinPerNightShift,
                    _ => 0
                };

                var desired = rawMin > 0 ? rawMin : 1;

                var candidates = available
                    .Where(userId =>
                    {
                        var lastEnd = lastEndByUser[userId];
                        if (lastEnd.HasValue)
                        {
                            var gap = (date.ToDateTime(tpl.StartTime) - lastEnd.Value).TotalHours;
                            if (gap < rule.MinRestHoursBetweenShifts)
                                return false;
                        }
                        if (tpl.ShiftType == ShiftType.NightShift
                            && consecNights[userId] >= rule.MaxConsecutiveNightShifts)
                        {
                            return false;
                        }
                        return true;
                    })
                    .OrderBy(userId => newAssignments.Count(a => a.UserId == userId))
                    .ToList();

                var target = Math.Min(desired, candidates.Count);

                var toAssign = candidates.Take(target).ToList();

                foreach (var userId in toAssign)
                {
                    var assignment = new ShiftAssignment(
                        userId: userId,
                        date: date,
                        shift: tpl.ShiftType,
                        startTime: tpl.StartTime,
                        endTime: tpl.EndTime,
                        departmentId: departmentId,
                        managerId: department.ManagerId
                    );
                    newAssignments.Add(assignment);

                    available.Remove(userId);
                    lastEndByUser[userId] = date.ToDateTime(tpl.EndTime);

                    if (tpl.ShiftType == ShiftType.NightShift)
                        consecNights[userId]++;
                    else
                        consecNights[userId] = 0;
                }
            }
        }

        if (newAssignments.Any())
            await _repo.AddManyAsync(newAssignments, ct);
    }


    public Task<IReadOnlyList<ShiftAssignment>> GetAll(CancellationToken ct)
    {
        return _repo.GetAllAsync(ct);
    }

    public async Task<IEnumerable<ShiftAssignment>> GetUserScheduleAsync(string userId, DateOnly weekStart, CancellationToken ct)
    {
        return await _repo.GetByUserAndWeekAsync(userId, weekStart, ct);
    }

    public async Task SaveGeneratedShiftsAsync(string departmentId, DateOnly weekStart, List<ShiftFromAiDto> shiftDtos, CancellationToken ct)
    {
        var department = await _departmentRepo.GetByIdAsync(departmentId, ct);

        await _repo.DeleteByDepartmentAndWeekAsync(departmentId, weekStart, ct);

        var newAssignments = new List<ShiftAssignment>();
        foreach (var dto in shiftDtos)
        {
            if (!Enum.TryParse<ShiftType>(dto.Shift, ignoreCase: true, out var shiftType))
            {
                continue;
            }

            var assignment = new ShiftAssignment(
                userId: dto.UserId,
                date: dto.Date,
                shift: shiftType,
                startTime: dto.StartTime,
                endTime: dto.EndTime,
                departmentId: departmentId,
                managerId: department.ManagerId
            );
            newAssignments.Add(assignment);
        }

        await _repo.AddManyAsync(newAssignments, ct);
    }
}
