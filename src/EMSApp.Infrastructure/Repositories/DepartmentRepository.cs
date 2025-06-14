using EMSApp.Domain;
using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;
using MongoDB.Driver;
using ZstdSharp.Unsafe;

namespace EMSApp.Infrastructure;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly IMongoCollection<Department> _collection;

    public DepartmentRepository(IMongoDbContext dbContext)
    {
        _collection = dbContext.GetCollection<Department>("Departments");
    }

    public async Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await _collection
            .Find(Builders<Department>.Filter.Empty)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task<Department?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Querying Department with id={id}");
        var filter = Builders<Department>.Filter.Eq(d => d.Id, id);
        var result = await _collection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
        return result;
    }

    public async Task CreateAsync(Department department, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(department, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Department>.Filter.Eq(d => d.Id, id);
        var result = await _collection.DeleteOneAsync(filter, cancellationToken: cancellationToken);

        if (result.DeletedCount == 0)
            throw new RepositoryException($"No Department found with Id {id}");
    }

    public async Task UpdateAsync(Department department, bool isUpsert = false, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Department>.Filter.Eq(d => d.Id, department.Id);
        var result = await _collection.ReplaceOneAsync(
            filter, 
            department,
            new ReplaceOptions { IsUpsert = isUpsert },
            cancellationToken: cancellationToken);

        if (!isUpsert && result.MatchedCount == 0)
            throw new RepositoryException($"No department found with Id {department.Id}");
    }
}
