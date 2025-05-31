using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using MongoDB.Driver;

namespace EMSApp.Infrastructure;

public class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly IMongoCollection<LeaveRequest> _collection;

    public LeaveRequestRepository(IMongoDbContext dbContext)
    {
        _collection = dbContext.GetCollection<LeaveRequest>("LeaveRequests");
    }

    public async Task<LeaveRequest?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<LeaveRequest>.Filter.Eq(l => l.Id, id);
        var result = await _collection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<LeaveRequest>> ListByManagerAsync(string managerId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<LeaveRequest>.Filter.Eq(l => l.ManagerId, managerId);
        var result = await _collection
            .Find(filter)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<LeaveRequest>> ListByStatusAsync(LeaveStatus status, CancellationToken cancellationToken = default)
    {
        var filter = Builders<LeaveRequest>.Filter.Eq(l => l.Status, status);
        var result = await _collection
            .Find(filter)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<LeaveRequest>> ListByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<LeaveRequest>.Filter.Eq(l => l.UserId, userId);
        var result = await _collection
            .Find(filter)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task CreateAsync(LeaveRequest request, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(request, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<LeaveRequest>.Filter.Eq(l => l.Id, id);
        var result = await _collection.DeleteOneAsync(filter, cancellationToken: cancellationToken);

        if (result.DeletedCount == 0)
            throw new RepositoryException($"No leave request found with Id {id}");
    }

    public async Task UpdateAsync(LeaveRequest request, bool isUpsert = false, CancellationToken cancellationToken = default)
    {
        var filter = Builders<LeaveRequest>.Filter.Eq(l => l.Id, request.Id);
        var result = await _collection.ReplaceOneAsync(
            filter,
            request,
            new ReplaceOptions { IsUpsert = isUpsert },
            cancellationToken: cancellationToken);

        if (!isUpsert && result.MatchedCount == 0)
            throw new RepositoryException($"No leave request found with Id {request.Id}");
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetAllAsync(CancellationToken ct)
    {
        var result = await _collection
            .Find(Builders<LeaveRequest>.Filter.Empty)
            .ToListAsync(ct);
        return result;
    }
}
