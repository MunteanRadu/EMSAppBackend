using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using MongoDB.Driver;

namespace EMSApp.Infrastructure;

public class AssignmentFeedbackRepository : IAssignmentFeedbackRepository
{
    private readonly IMongoCollection<AssignmentFeedback> _collection;

    public AssignmentFeedbackRepository(IMongoDbContext dbContext)
    {
        _collection = dbContext.GetCollection<AssignmentFeedback>("AssignmentFeedbacks");
    }

    public async Task<AssignmentFeedback?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AssignmentFeedback>.Filter.Eq(f => f.Id, id);
        var result = await _collection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<AssignmentFeedback>> ListByAssignmentAsync(string assignmentId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AssignmentFeedback>.Filter.Eq(f => f.AssignmentId, assignmentId);
        var result = await _collection
            .Find(filter)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task CreateAsync(AssignmentFeedback assignmentFeedback, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(assignmentFeedback, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AssignmentFeedback>.Filter.Eq(f => f.Id, id);
        var result = await _collection.DeleteOneAsync(filter, cancellationToken: cancellationToken);

        if (result.DeletedCount == 0)
            throw new RepositoryException($"No assignment feedback found with Id {id}");
    }

    public async Task UpdateAsync(AssignmentFeedback assignmentFeedback, bool isUpsert = false, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AssignmentFeedback>.Filter.Eq(f => f.Id, assignmentFeedback.Id);
        var result = await _collection.ReplaceOneAsync(
            filter, 
            assignmentFeedback,
            new ReplaceOptions { IsUpsert = isUpsert },
            cancellationToken: cancellationToken);

        if (!isUpsert && result.MatchedCount == 0)
            throw new RepositoryException($"No assignment feedback found with Id {assignmentFeedback.Id}");
    }

    public async Task<IReadOnlyList<AssignmentFeedback>> GetAllAsync(CancellationToken ct)
    {
        var result = await _collection
            .Find(Builders<AssignmentFeedback>.Filter.Empty)
            .ToListAsync(ct);
        return result;
    }
}
