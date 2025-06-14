using EMSApp.Domain;
using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace EMSApp.Infrastructure;

public class AssignmentRepository : IAssignmentRepository
{
    private readonly IMongoCollection<Assignment> _collection;

    public AssignmentRepository(IMongoDbContext dbContext)
    {
        _collection = dbContext.GetCollection<Assignment>("Assignments");
    }

    public async Task<Assignment?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Assignment>.Filter.Eq(t => t.Id, id);
        var result = await _collection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<Assignment>> ListByAssigneeAsync(string userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Assignment>.Filter.Eq(t => t.AssignedToId, userId);
        var result = await _collection
            .Find(filter)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<Assignment>> ListByStatusAsync(AssignmentStatus status, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Assignment>.Filter.Eq(t => t.Status, status);
        var result = await _collection
            .Find(filter)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<Assignment>> ListOverdueAsync(DateTime asOf, CancellationToken cancellationToken = default)
    {
        var openStatuses = new[]
        {
            AssignmentStatus.Pending,
            AssignmentStatus.InProgress
        };

        var filter = Builders<Assignment>.Filter.Lt(a => a.DueDate, asOf)
            & Builders<Assignment>.Filter.In(a => a.Status, openStatuses);
        var result = await _collection
            .Find(filter)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<Assignment>> ListAsync(Expression<Func<Assignment, bool>>? predicate, CancellationToken cancellationToken = default)
    {
        var filter = predicate == null
            ? Builders<Assignment>.Filter.Empty
            : Builders<Assignment>.Filter.Where(predicate);

        var list = await _collection
            .Find(filter)
            .ToListAsync(cancellationToken);

        return list;
    }

    public async Task CreateAsync(Assignment assignment, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(assignment, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Assignment>.Filter.Eq(t => t.Id, id);
        var result = await _collection.DeleteOneAsync(filter, cancellationToken: cancellationToken);

        if (result.DeletedCount == 0)
            throw new RepositoryException($"No result found with Id {id}");
    }

    public async Task UpdateAsync(Assignment assignment, bool isUpsert = false, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Assignment>.Filter.Eq(t => t.Id, assignment.Id);
        var result = await _collection.ReplaceOneAsync(
            filter, 
            assignment,
            new ReplaceOptions { IsUpsert = isUpsert },
            cancellationToken: cancellationToken);

        if (!isUpsert && result.MatchedCount == 0)
            throw new RepositoryException($"No result found with Id {assignment.Id}");
    }

    public async Task<IReadOnlyList<Assignment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await _collection
            .Find(Builders<Assignment>.Filter.Empty)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<Assignment>> FilterByAsync(Expression<Func<Assignment, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _collection
          .Find(predicate)
          .ToListAsync(cancellationToken);
    }
}
