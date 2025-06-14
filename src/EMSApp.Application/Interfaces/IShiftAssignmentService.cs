namespace EMSApp.Application;

using EMSApp.Domain;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IShiftAssignmentService
{
    Task GenerateWeeklyScheduleAsync(string departmentId, DateOnly weekStart, CancellationToken ct);
    Task <IReadOnlyList<ShiftAssignment>> GetAll(CancellationToken ct);
    Task<IEnumerable<ShiftAssignment>> GetUserScheduleAsync(string userId, DateOnly weekStart, CancellationToken ct);
    Task SaveGeneratedShiftsAsync(string departmentId, DateOnly weekStart, List<ShiftFromAiDto> shiftDtos, CancellationToken ct);

}
