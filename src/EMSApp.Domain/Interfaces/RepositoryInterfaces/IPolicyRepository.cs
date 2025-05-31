using System.Globalization;

namespace EMSApp.Domain;

public interface IPolicyRepository
{
    Task<Policy?> GetByYearAsync(int year, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Policy>> GetAllAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(Policy policy, CancellationToken cancellationToken = default);
    Task UpdateAsync(Policy policy, bool isUpsert, CancellationToken cancellationToken = default);
    Task DeleteAsync(int year, CancellationToken cancellationToken = default);
}
