using EMSApp.Domain;
using MongoDB.Driver;

namespace EMSApp.Infrastructure;

public class ShiftRuleRepository : IShiftRuleRepository
{
    private readonly IMongoCollection<ShiftRule> _collection;

    public ShiftRuleRepository(IMongoDbContext database)
    {
        _collection = database.GetCollection<ShiftRule>("ShiftRules");
    }

    public async Task<ShiftRule?> GetByDepartmentAsync(string departmentId, CancellationToken ct)
    {
        var filter = Builders<ShiftRule>.Filter.Eq(r => r.DepartmentId, departmentId);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task UpsertAsync(ShiftRule rule, CancellationToken ct)
    {
        var filter = Builders<ShiftRule>.Filter.Eq(r => r.DepartmentId, rule.DepartmentId);
        await _collection.ReplaceOneAsync(
            filter,
            rule,
            new ReplaceOptions { IsUpsert = true },
            ct);
    }

    public async Task DeleteByDepartmentAsync(string departmentId, CancellationToken ct)
    {
        var filter = Builders<ShiftRule>.Filter.Eq(r => r.DepartmentId, departmentId);
        await _collection.DeleteOneAsync(filter, ct);
    }
}
