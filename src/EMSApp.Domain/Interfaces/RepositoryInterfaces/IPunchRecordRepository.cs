using System.Linq.Expressions;

namespace EMSApp.Domain;

public interface IPunchRecordRepository
{
    Task<PunchRecord?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PunchRecord>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<PunchRecord>> ListByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task CreateAsync(PunchRecord record, CancellationToken cancellationToken = default);
    Task UpdateAsync(PunchRecord record, bool isUpsert, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PunchRecord>> FilterByAsync(Expression<Func<PunchRecord, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PunchRecord>> ListByUserAndMonthAsync(string userId, int year, int month, CancellationToken cancellationToken = default);
}
