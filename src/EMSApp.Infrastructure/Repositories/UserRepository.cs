using EMSApp.Domain;
using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;
using MongoDB.Driver;

namespace EMSApp.Infrastructure;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _collection;

    public UserRepository(IMongoDbContext dbContext)
    {
        _collection = dbContext.GetCollection<User>("Users");
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Email, email);
        var result = await _collection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
        return result;
    }
    
    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Username, username);
        var result = await _collection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
        return result;
    }

    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, id);
        var result = await _collection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _collection
            .Find(Builders<User>.Filter.Empty)
            .ToListAsync(cancellationToken);
        return users;
    }

    public async Task<IReadOnlyList<User>> ListByDepartmentAsync(string departmentId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<User>.Filter.Eq(u => u.DepartmentId, departmentId);
        var result = await _collection
            .Find(filter)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(user, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, id);
        var result = await _collection.DeleteOneAsync(filter, cancellationToken: cancellationToken);

        if (result.DeletedCount == 0)
            throw new RepositoryException($"No user found with Id {id}");
    }

    public async Task UpdateAsync(User user, bool isUpsert = false, CancellationToken cancellationToken = default)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
        var result = await _collection.ReplaceOneAsync(
            filter, 
            user, 
            new ReplaceOptions { IsUpsert = isUpsert }, 
            cancellationToken: cancellationToken);

        if (!isUpsert && result.MatchedCount == 0)
            throw new RepositoryException($"No result found with Id {user.Id}");
    }

    public async Task<IReadOnlyList<User>> ListByRoleAsync(UserRole? role, CancellationToken ct)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Role, role);
        var result = await _collection
            .Find(filter)
            .ToListAsync(ct);
        return result;
    }
}
