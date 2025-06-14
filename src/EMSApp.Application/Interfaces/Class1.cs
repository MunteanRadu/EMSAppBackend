namespace EMSApp.Application.Interfaces;

public interface IScheduleGenerationService
{
    Task GenerateAndSaveWeeklyScheduleAsync(string departmentId, DateOnly weekStart, CancellationToken ct);
}
