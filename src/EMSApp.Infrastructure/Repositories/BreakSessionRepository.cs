using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using MongoDB.Driver;

namespace EMSApp.Infrastructure;

public class BreakSessionRepository : IBreakSessionRepository
{
    private readonly IMongoCollection<BreakSession> _collection;

    public BreakSessionRepository(IMongoDbContext dbContext)
    {
        _collection = dbContext.GetCollection<BreakSession>("Breaks");
    }

    public async Task<BreakSession?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BreakSession>.Filter.Eq(b => b.Id, id);
        var result = await _collection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<BreakSession>> ListByPunchRecordAsync(string punchRecordId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BreakSession>.Filter.Eq(b => b.PunchRecordId, punchRecordId);
        var result = await _collection
            .Find(filter)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task CreateAsync(BreakSession breakSession, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(breakSession, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BreakSession>.Filter.Eq(b => b.Id, id);
        var result = await _collection.DeleteOneAsync(filter, cancellationToken: cancellationToken);

        if (result.DeletedCount == 0)
            throw new RepositoryException($"No break session found with Id {id}");
    }

    public async Task UpdateAsync(BreakSession breakSession, bool isUpsert = false, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BreakSession>.Filter.Eq(b => b.Id, breakSession.Id);
        var result = await _collection.ReplaceOneAsync(
            filter, 
            breakSession, 
            new ReplaceOptions { IsUpsert = isUpsert },
            cancellationToken: cancellationToken);

        if (!isUpsert && result.MatchedCount == 0)
            throw new RepositoryException($"No break session found with Id {breakSession.Id}");
    }

    public async Task<IReadOnlyList<BreakSession>> GetAllAsync(CancellationToken ct)
    {
        var result = await _collection
            .Find(Builders<BreakSession>.Filter.Empty)
            .ToListAsync(ct);
        return result;
    }
}
