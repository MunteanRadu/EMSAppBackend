using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using MongoDB.Driver;

namespace EMSApp.Infrastructure;

public class ScheduleRepository : IScheduleRepository
{
    private readonly IMongoCollection<Schedule> _collection;

    public ScheduleRepository(IMongoDbContext dbContext)
    {
        _collection = dbContext.GetCollection<Schedule>("Schedules");
    }

    public async Task<Schedule?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Schedule>.Filter.Eq(s => s.Id, id);
        var result = await _collection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<Schedule>> GetByDepartmentAsync(string departmentId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Schedule>.Filter.Eq(s => s.DepartmentId, departmentId);
        var result = await _collection
            .Find(filter)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<Schedule>> GetByManagerIdAsync(string managerId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Schedule>.Filter.Eq(s => s.ManagerId, managerId);
        var result = await _collection
            .Find(filter)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task CreateAsync(Schedule schedule, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(schedule, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Schedule>.Filter.Eq(s => s.Id, id);
        var result = await _collection.DeleteOneAsync(filter, cancellationToken: cancellationToken);

        if (result.DeletedCount == 0)
            throw new RepositoryException($"No schedule found for Id {id}");
    }

    public async Task UpdateAsync(Schedule schedule, bool isUpsert = false, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Schedule>.Filter.Eq(s => s.Id, schedule.Id);
        var result = await _collection.ReplaceOneAsync(
            filter,
            schedule,
            new ReplaceOptions { IsUpsert = isUpsert },
            cancellationToken: cancellationToken
            );

        if (!isUpsert && result.MatchedCount == 0)
            throw new RepositoryException($"No schedule found for Id {schedule.Id}");
    }

    public async Task<IReadOnlyList<Schedule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await _collection
            .Find(Builders<Schedule>.Filter.Empty)
            .ToListAsync(cancellationToken);
        return result;
    }
}
