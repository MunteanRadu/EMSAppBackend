namespace EMSApp.Application.Interfaces;

public interface IScheduleGenerationService
{
    Task<string> GetScheduleSuggestionJsonAsync(string departmentId, DateOnly weekStart, CancellationToken ct);
}
