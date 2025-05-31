using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using EMSApp.Infrastructure;
using EMSApp.Infrastructure.Settings;
using Mongo2Go;
using MongoDB.Driver;
using System.Diagnostics.Contracts;

namespace EMSApp.Tests;

[Trait("Category", "Repository")]
public class PunchRecordRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbRunner _dbRunner;
    private readonly IMongoDbContext _dbContext;
    private readonly PunchRecordRepository _repo;
    private static string _dbName = "TestDb";

    public PunchRecordRepositoryTests()
    {
        _dbRunner = MongoDbRunner.Start();

        var settings = new DatabaseSettings
        {
            ConnectionString = _dbRunner.ConnectionString,
            DatabaseName = _dbName,
        };

        _dbContext = new MongoDbContext(settings);
        _repo = new PunchRecordRepository(_dbContext);
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
        await database.DropCollectionAsync("PunchRecords");
    }

    [Fact]
    public async Task CreateAndFetch_PunchRecord_Works()
    {
        // Arrange
        var p = new PunchRecord("user-1", DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.Parse("08:00"));

        // Act
        await _repo.CreateAsync(p);
        var byId = await _repo.GetByIdAsync(p.Id);

        // Assert
        Assert.NotNull(byId);
        Assert.Equal(p.Id, byId.Id);
        Assert.Equal(p.UserId, byId.UserId);
        Assert.Equal(p.Date, byId.Date);
        Assert.Equal(p.TimeIn, byId.TimeIn);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNull()
    {
        // Arrange
        var byId = await _repo.GetByIdAsync("user-1");

        // Act & Assert
        Assert.Null(byId);
    }

    [Fact]
    public async Task ListByUser_ReturnsUsers()
    {
        // Arrange
        var p1 = new PunchRecord("user-1", DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.Parse("08:00"));
        var p2 = new PunchRecord("user-2", DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.Parse("08:15"));
        await _repo.CreateAsync(p1);
        await _repo.CreateAsync(p2);

        // Act
        var list = await _repo.ListByUserAsync("user-1");

        // Assert
        Assert.NotEmpty(list);
        Assert.Contains(list, p => p.Id == p1.Id);
    }

    [Fact]
    public async Task ListByUser_NonExistent_ReturnsEmptyList()
    {
        // Arrange
        var list = await _repo.ListByUserAsync("user-1");

        // Act & Assert
        Assert.Empty(list);
    }

    [Fact]
    public async Task DeletePunchRecord_Exists_DeletesPunchRecord()
    {
        // Arrange
        var p = new PunchRecord("user-1", DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.Parse("08:00"));
        await _repo.CreateAsync(p);
        Assert.NotEmpty(await _repo.ListByUserAsync("user-1"));

        // Act
        await _repo.DeleteAsync(p.Id);

        // Assert
        Assert.Empty(await _repo.ListByUserAsync("user-1"));
    }

    [Fact]
    public async Task DeletePunchRecord_NonExistent_ThrowsRepositoryException()
    {
        // Arrange
        var p = new PunchRecord("user-1", DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.Parse("08:00"));

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _repo.DeleteAsync(p.Id)
        );
    }

    [Fact]
    public async Task UpdatePunchRecord_Exists_UpdatesPunchRecord()
    {
        // Arrange
        var punchIn = TimeOnly.Parse("08:00");
        var punchOut = punchIn.AddHours(8);
        var p = new PunchRecord("user-1", DateOnly.FromDateTime(DateTime.UtcNow), punchIn);
        Assert.Null(p.TimeOut);
        await _repo.CreateAsync(p);

        p.PunchOut(punchOut);

        // Act
        await _repo.UpdateAsync(p);
        var byId = await _repo.GetByIdAsync(p.Id);

        // Assert
        Assert.NotNull(byId);
        Assert.Equal(p.TimeOut, byId.TimeOut);
    }

    [Fact]
    public async Task UpdatePunchRecord_NonExistent_ThrowsRepositoryException()
    {
        // Arrange
        var p = new PunchRecord("user-1", DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.Parse("08:00"));
        Assert.Null(await _repo.GetByIdAsync(p.Id));

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _repo.UpdateAsync(p)
        );
    }

    [Fact]
    public async Task UpsertPunchRecord_NonExistent_CreatesPunchRecord()
    {
        // Arrange
        var p = new PunchRecord("user-1", DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.Parse("08:00"));
        Assert.Null(await _repo.GetByIdAsync(p.Id));

        // Act
        await _repo.UpdateAsync(p, true);
        var byId = await _repo.GetByIdAsync(p.Id);

        // Assert
        Assert.NotNull(byId);
        Assert.Equal(p.Id, byId.Id);
    }
}
