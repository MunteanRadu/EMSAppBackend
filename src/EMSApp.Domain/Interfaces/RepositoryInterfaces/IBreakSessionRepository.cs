namespace EMSApp.Domain;

public interface IBreakSessionRepository
{
    Task<BreakSession?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BreakSession>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<BreakSession>> ListByPunchRecordAsync(string punchRecordId, CancellationToken cancellationToken = default);
    Task CreateAsync(BreakSession breakSession, CancellationToken cancellationToken = default);
    Task UpdateAsync(BreakSession breakSession, bool isUpsert, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
