using EMSApp.Domain;

namespace EMSApp.Application;

public interface IPunchRecordService
{
    Task<PunchRecord> CreateAsync(string userId, DateOnly date, TimeOnly time, CancellationToken ct);
    Task<PunchRecordDto?> GetByIdAsync(string id, CancellationToken ct);
    Task<PunchRecordDto> PunchOutAsync(string id, TimeOnly timeOut, CancellationToken ct);
    Task<IReadOnlyList<PunchRecord>> ListByUserAsync(string userId, CancellationToken ct);
    Task<IReadOnlyList<DaySummaryDto>> GetMonthSummaryAsync(string userId, int year, int month, CancellationToken ct);
    Task<IReadOnlyList<PunchRecordDto>> GetByDateAsync(string userId, DateOnly date, CancellationToken ct);
    Task UpdateAsync(PunchRecord record, CancellationToken ct);
    Task DeleteAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<PunchRecord>> GetAllAsync(CancellationToken ct);
}
