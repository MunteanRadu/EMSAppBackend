using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace EMSApp.Infrastructure;

public class PunchRecordRepository : IPunchRecordRepository
{
    private readonly IMongoCollection<PunchRecord> _collection;

    public PunchRecordRepository(IMongoDbContext dbContext)
    {
        _collection = dbContext.GetCollection<PunchRecord>("PunchRecords");
    }

    public async Task<PunchRecord?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<PunchRecord>.Filter.Eq(p => p.Id, id);
        var result = await _collection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<PunchRecord>> ListByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<PunchRecord>.Filter.Eq(p => p.UserId, userId);
        var result = await _collection
            .Find(filter)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task CreateAsync(PunchRecord record, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(record, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<PunchRecord>.Filter.Eq(p => p.Id, id);
        var result = await _collection.DeleteOneAsync(filter, cancellationToken: cancellationToken);

        if(result.DeletedCount == 0)
            throw new RepositoryException($"No punch record found for Id {id}");
    }

    public async Task UpdateAsync(PunchRecord record, bool isUpsert = false, CancellationToken cancellationToken = default)
    {
        var filter = Builders<PunchRecord>.Filter.Eq(p => p.Id, record.Id);
        var result = await _collection.ReplaceOneAsync(
            filter,
            record,
            new ReplaceOptions { IsUpsert = isUpsert },
            cancellationToken: cancellationToken);

        if (!isUpsert && result.MatchedCount == 0)
            throw new RepositoryException($"No punch record found for Id {record.Id}");
    }

    public async Task<IReadOnlyList<PunchRecord>> GetAllAsync(CancellationToken ct)
    {
        var result = await _collection
            .Find(Builders<PunchRecord>.Filter.Empty)
            .ToListAsync(ct);
        return result;
    }

    public async Task<IReadOnlyList<PunchRecord>> FilterByAsync(Expression<Func<PunchRecord, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _collection
          .Find(predicate)
          .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PunchRecord>> ListByUserAndMonthAsync(string userId, int year, int month, CancellationToken cancellationToken = default)
    {
        var monthStart = new DateOnly(year, month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var filter = Builders<PunchRecord>.Filter.And(
            Builders<PunchRecord>.Filter.Eq(pr => pr.UserId, userId),
            Builders<PunchRecord>.Filter.Gte(pr => pr.Date, monthStart),
            Builders<PunchRecord>.Filter.Lt(pr => pr.Date, nextMonth)
        );

        return await _collection
          .Find(filter)
          .ToListAsync(cancellationToken);
    }
}
