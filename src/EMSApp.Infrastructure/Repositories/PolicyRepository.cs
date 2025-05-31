using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using MongoDB.Driver;

namespace EMSApp.Infrastructure;

public class PolicyRepository : IPolicyRepository
{
    private readonly IMongoCollection<Policy> _collection;

    public PolicyRepository(IMongoDbContext dbContext)
    {
        _collection = dbContext.GetCollection<Policy>("Policies");
    }

    public async Task<IReadOnlyList<Policy>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await _collection
            .Find(Builders<Policy>.Filter.Empty)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task<Policy?> GetByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Policy>.Filter.Eq(p => p.Year, year);
        var result = await _collection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
        return result;
    }

    public async Task CreateAsync(Policy policy, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(policy, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(int year, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Policy>.Filter.Eq(p => p.Year, year);
        var result = await _collection.DeleteOneAsync(filter, cancellationToken: cancellationToken);

        if (result.DeletedCount == 0)
            throw new RepositoryException($"No policy found for year {year}");
    }

    public async Task UpdateAsync(Policy policy, bool isUpsert = false, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Policy>.Filter.Eq(p => p.Year, policy.Year);
        var result = await _collection.ReplaceOneAsync(
            filter,
            policy,
            new ReplaceOptions { IsUpsert =  isUpsert },
            cancellationToken: cancellationToken);

        if (!isUpsert && result.MatchedCount == 0) 
            throw new RepositoryException($"No policy found for year {policy.Year}");
    }
}
