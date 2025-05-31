using EMSApp.Infrastructure.Settings;
using EMSApp.Infrastructure;
using Mongo2Go;
using MongoDB.Driver;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Tests;

[Trait("Category", "Repository")]
public class PolicyRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbRunner _dbRunner;
    private readonly IMongoDbContext _dbContext;
    private readonly PolicyRepository _repo;
    private static string _dbName = "TestDb";

    public PolicyRepositoryTests()
    {
        _dbRunner = MongoDbRunner.Start();

        var settings = new DatabaseSettings
        {
            ConnectionString = _dbRunner.ConnectionString,
            DatabaseName = _dbName,
        };

        _dbContext = new MongoDbContext(settings);
        _repo = new PolicyRepository(_dbContext);
    }
    public Task DisposeAsync()
    {
        _dbRunner.Dispose();
        return Task.CompletedTask;
    }

    public async Task InitializeAsync()
    {
        var client = new MongoClient(_dbRunner.ConnectionString);
        var database = client.GetDatabase(_dbName);
        await database.DropCollectionAsync("Policies");
    }

    private static IDictionary<LeaveType, int> GetValidQuotas()
    {
        return Enum.GetValues(typeof(LeaveType))
                   .Cast<LeaveType>()
                   .ToDictionary(lt => lt, lt => 10);
    }

    [Fact]
    public async Task CreateAndFetch_Policy_Works()
    {
        // Arrange
        var leaveQuotas = GetValidQuotas();
        var p = new Policy(
            year: 2025,
            workDayStart: new TimeOnly(8, 0),
            workDayEnd: new TimeOnly(16, 0),
            punchInTolerance: TimeSpan.FromMinutes(15),
            punchOutTolerance: TimeSpan.FromMinutes(15),
            maxSingleBreak: TimeSpan.FromMinutes(30),
            maxTotalBreakPerDay: TimeSpan.FromHours(2),
            overtimeMultiplier: 1.5m,
            leaveQuotas: leaveQuotas
            );

        // Act
        await _repo.CreateAsync(p);
        var byYear = await _repo.GetByYearAsync(2025);

        // Assert
        Assert.NotNull(byYear);
        Assert.Equal(p.Year, byYear.Year);
        Assert.Equal(p.WorkDayStart, byYear.WorkDayStart);
        Assert.Equal(p.WorkDayEnd, byYear.WorkDayEnd);
        Assert.Equal(p.PunchInTolerance, byYear.PunchInTolerance);
        Assert.Equal(p.PunchOutTolerance, byYear.PunchOutTolerance);
        Assert.Equal(p.MaxSingleBreak, byYear.MaxSingleBreak);
        Assert.Equal(p.MaxTotalBreakPerDay, byYear.MaxTotalBreakPerDay);
        Assert.Equal(p.OvertimeMultiplier, byYear.OvertimeMultiplier);
        Assert.Equal(p.LeaveQuotas, byYear.LeaveQuotas);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNull()
    {
        // Arrange
        var byYear = await _repo.GetByYearAsync(2025);

        // Act & Assert
        Assert.Null(byYear);
    }

    [Fact]
    public async Task GetAll_ReturnsPolicies()
    {
        // Arrange
        var leaveQuotas = GetValidQuotas();
        var p1 = new Policy(
            year: 2025,
            workDayStart: new TimeOnly(8, 0),
            workDayEnd: new TimeOnly(16, 0),
            punchInTolerance: TimeSpan.FromMinutes(15),
            punchOutTolerance: TimeSpan.FromMinutes(15),
            maxSingleBreak: TimeSpan.FromMinutes(30),
            maxTotalBreakPerDay: TimeSpan.FromHours(2),
            overtimeMultiplier: 1.5m,
            leaveQuotas: leaveQuotas
            );
        var p2 = new Policy(
            year: 2024,
            workDayStart: new TimeOnly(8, 0),
            workDayEnd: new TimeOnly(16, 0),
            punchInTolerance: TimeSpan.FromMinutes(15),
            punchOutTolerance: TimeSpan.FromMinutes(15),
            maxSingleBreak: TimeSpan.FromMinutes(30),
            maxTotalBreakPerDay: TimeSpan.FromHours(2),
            overtimeMultiplier: 1.5m,
            leaveQuotas: leaveQuotas
            );
        await _repo.CreateAsync(p1);
        await _repo.CreateAsync(p2);

        // Act
        var list = await _repo.GetAllAsync();

        // Assert
        Assert.NotEmpty(list);
        Assert.Contains(list, p => p.Year == p1.Year);
        Assert.Contains(list, p => p.Year == p2.Year);
    }

    [Fact]
    public async Task GetAll_NonExistent_ReturnsEmptyList()
    {
        // Arrange
        var list = await _repo.GetAllAsync();

        // Act & Assert
        Assert.Empty(list);
    }

    [Fact]
    public async Task DeletePolicy_Exists_DeletesPolicy()
    {
        // Arrange
        var leaveQuotas = GetValidQuotas();
        var p = new Policy(
            year: 2025,
            workDayStart: new TimeOnly(8, 0),
            workDayEnd: new TimeOnly(16, 0),
            punchInTolerance: TimeSpan.FromMinutes(15),
            punchOutTolerance: TimeSpan.FromMinutes(15),
            maxSingleBreak: TimeSpan.FromMinutes(30),
            maxTotalBreakPerDay: TimeSpan.FromHours(2),
            overtimeMultiplier: 1.5m,
            leaveQuotas: leaveQuotas
            );
        await _repo.CreateAsync(p);
        Assert.NotEmpty(await _repo.GetAllAsync());

        // Act
        await _repo.DeleteAsync(p.Year);

        // Assert
        Assert.Empty(await _repo.GetAllAsync());
    }

    [Fact]
    public async Task DeletePolicy_NonExistent_ThrowsRepositoryException()
    {
        // Arrange
        var leaveQuotas = GetValidQuotas();
        var p = new Policy(
            year: 2025,
            workDayStart: new TimeOnly(8, 0),
            workDayEnd: new TimeOnly(16, 0),
            punchInTolerance: TimeSpan.FromMinutes(15),
            punchOutTolerance: TimeSpan.FromMinutes(15),
            maxSingleBreak: TimeSpan.FromMinutes(30),
            maxTotalBreakPerDay: TimeSpan.FromHours(2),
            overtimeMultiplier: 1.5m,
            leaveQuotas: leaveQuotas
            );
        Assert.Empty(await _repo.GetAllAsync());

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _repo.DeleteAsync(p.Year)
        );
    }

    [Fact]
    public async Task UpdatePolicy_Exists_UpdatesPolicy()
    {
        // Arrange
        var leaveQuotas = GetValidQuotas();
        var p = new Policy(
            year: 2025,
            workDayStart: new TimeOnly(8, 0),
            workDayEnd: new TimeOnly(16, 0),
            punchInTolerance: TimeSpan.FromMinutes(15),
            punchOutTolerance: TimeSpan.FromMinutes(15),
            maxSingleBreak: TimeSpan.FromMinutes(30),
            maxTotalBreakPerDay: TimeSpan.FromHours(2),
            overtimeMultiplier: 1.5m,
            leaveQuotas: leaveQuotas
            );
        await _repo.CreateAsync(p);
        var byYear = await _repo.GetByYearAsync(2025);
        Assert.Equal(p.WorkDayStart, byYear.WorkDayStart);
        Assert.Equal(p.WorkDayEnd, byYear.WorkDayEnd);

        var newStart = new TimeOnly(9, 0);
        var newEnd = new TimeOnly(17, 0);
        p.SetWorkingHours(newStart, newEnd);

        // Act
        await _repo.UpdateAsync(p);
        byYear = await _repo.GetByYearAsync(2025);

        // Assert
        Assert.NotNull(byYear);
        Assert.Equal(newStart, byYear.WorkDayStart);
        Assert.Equal(newEnd, byYear.WorkDayEnd);
    }

    [Fact]
    public async Task UpdatePolicy_NonExistent_ThrowsRepositoryException()
    {
        // Arrange
        var leaveQuotas = GetValidQuotas();
        var p = new Policy(
            year: 2025,
            workDayStart: new TimeOnly(8, 0),
            workDayEnd: new TimeOnly(16, 0),
            punchInTolerance: TimeSpan.FromMinutes(15),
            punchOutTolerance: TimeSpan.FromMinutes(15),
            maxSingleBreak: TimeSpan.FromMinutes(30),
            maxTotalBreakPerDay: TimeSpan.FromHours(2),
            overtimeMultiplier: 1.5m,
            leaveQuotas: leaveQuotas
            );
        Assert.Empty(await _repo.GetAllAsync());

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _repo.UpdateAsync(p)
        );
    }

    [Fact]
    public async Task UpsertPolicy_NonExistent_CreatesPolicy()
    {
        // Arrange
        var leaveQuotas = GetValidQuotas();
        var p = new Policy(
            year: 2025,
            workDayStart: new TimeOnly(8, 0),
            workDayEnd: new TimeOnly(16, 0),
            punchInTolerance: TimeSpan.FromMinutes(15),
            punchOutTolerance: TimeSpan.FromMinutes(15),
            maxSingleBreak: TimeSpan.FromMinutes(30),
            maxTotalBreakPerDay: TimeSpan.FromHours(2),
            overtimeMultiplier: 1.5m,
            leaveQuotas: leaveQuotas
            );
        Assert.Empty(await _repo.GetAllAsync());

        // Act
        await _repo.UpdateAsync(p, true);
        var byYear = await _repo.GetByYearAsync(2025);

        // Assert
        Assert.NotNull(byYear);
        Assert.Equal(p.Year, byYear.Year);
    }
}
