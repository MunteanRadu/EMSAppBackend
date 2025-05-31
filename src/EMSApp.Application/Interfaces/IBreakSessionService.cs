using EMSApp.Domain;

namespace EMSApp.Application;

public interface IBreakSessionService
{
    Task<BreakSessionDto?> CreateAsync(string punchRecordId, TimeOnly start, CancellationToken ct);
    Task<BreakSessionDto?> EndAsync(string punchId, string breakSessionId, TimeOnly endTime, CancellationToken ct);
    Task<BreakSessionDto?> GetByIdAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<BreakSessionDto>> ListByPunchRecordAsync(string punchRecordId, CancellationToken ct);
    Task UpdateAsync(BreakSession newBreakSession, CancellationToken ct);
    Task DeleteAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<BreakSession>> GetAllAsync(CancellationToken ct);
}
