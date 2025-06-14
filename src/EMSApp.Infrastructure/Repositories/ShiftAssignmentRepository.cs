using EMSApp.Domain;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EMSApp.Infrastructure;

public class ShiftAssignmentRepository : IShiftAssignmentRepository
{
    private readonly IMongoCollection<ShiftAssignment> _collection;

    public ShiftAssignmentRepository(IMongoDbContext database)
    {
        _collection = database.GetCollection<ShiftAssignment>("ShiftAssignments");
    }

    public async Task<IEnumerable<ShiftAssignment>> GetByDepartmentAndWeekAsync(string departmentId, DateOnly weekStart, CancellationToken ct)
    {
        var weekEnd = weekStart.AddDays(6);

        var filter = Builders<ShiftAssignment>.Filter.And(
            Builders<ShiftAssignment>.Filter.Eq(x => x.DepartmentId, departmentId),
            Builders<ShiftAssignment>.Filter.Gte(x => x.Date, weekStart),
            Builders<ShiftAssignment>.Filter.Lte(x => x.Date, weekEnd)
        );

        return await _collection.Find(filter).ToListAsync(ct);
    }

    public async Task<IEnumerable<ShiftAssignment>> GetByUserAndWeekAsync(string userId, DateOnly weekStart, CancellationToken ct)
    {
        var weekEnd = weekStart.AddDays(6);

        var filter = Builders<ShiftAssignment>.Filter.And(
            Builders<ShiftAssignment>.Filter.Eq(x => x.UserId, userId),
            Builders<ShiftAssignment>.Filter.Gte(x => x.Date, weekStart),
            Builders<ShiftAssignment>.Filter.Lte(x => x.Date, weekEnd)
        );

        return await _collection.Find(filter).ToListAsync(ct);
    }

    public async Task<ShiftAssignment?> GetForUserOnDateAsync(string userId, DateOnly date, CancellationToken ct)
    {
        var filter = Builders<ShiftAssignment>.Filter.And(
            Builders<ShiftAssignment>.Filter.Eq(x => x.UserId, userId),
            Builders<ShiftAssignment>.Filter.Eq(x => x.Date, date)
        );

        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(ShiftAssignment assignment, CancellationToken ct)
    {
        await _collection.InsertOneAsync(assignment, cancellationToken: ct);
    }

    public async Task AddManyAsync(IEnumerable<ShiftAssignment> assignments, CancellationToken ct)
    {
        await _collection.InsertManyAsync(assignments, cancellationToken: ct);
    }

    public async Task DeleteByDepartmentAndWeekAsync(string departmentId, DateOnly weekStart, CancellationToken ct)
    {
        var weekEnd = weekStart.AddDays(6);

        var filter = Builders<ShiftAssignment>.Filter.And(
            Builders<ShiftAssignment>.Filter.Eq(x => x.DepartmentId, departmentId),
            Builders<ShiftAssignment>.Filter.Gte(x => x.Date, weekStart),
            Builders<ShiftAssignment>.Filter.Lte(x => x.Date, weekEnd)
        );

        await _collection.DeleteManyAsync(filter, ct);
    }

    public async Task<IReadOnlyList<ShiftAssignment>> GetAllAsync(CancellationToken ct)
    {
        var result = await _collection
            .Find(Builders<ShiftAssignment>.Filter.Empty)
            .ToListAsync(ct);
        return result;
    }
}
