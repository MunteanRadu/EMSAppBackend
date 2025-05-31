
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using EMSApp.Infrastructure;
using EMSApp.Infrastructure.Settings;
using Mongo2Go;
using MongoDB.Driver;

namespace EMSApp.Tests;

[Trait("Category", "Repository")]
public class BreakSessionRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbRunner _dbRunner;
    private readonly IMongoDbContext _dbContext;
    private readonly BreakSessionRepository _repo;
    private const string _dbName = "TestDb";

    public BreakSessionRepositoryTests()
    {
        _dbRunner = MongoDbRunner.Start();

        var settings = new DatabaseSettings
        {
            ConnectionString = _dbRunner.ConnectionString,
            DatabaseName = _dbName
        };
        _dbContext = new MongoDbContext(settings);
        _repo = new BreakSessionRepository(_dbContext);
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
        await database.DropCollectionAsync("Breaks");
    }

    [Fact]
    public async Task CreateAndFetch_BreakSession_Works()
    {
        // Arrange
        var b = new BreakSession("punch-1", TimeOnly.FromDateTime(DateTime.UtcNow));

        // Act
        await _repo.CreateAsync(b);
        var byId = await _repo.GetByIdAsync(b.Id);

        // Assert
        Assert.NotNull(byId);
        Assert.Equal(byId.Id, b.Id);
        Assert.Equal(byId.PunchRecordId, b.PunchRecordId);
        Assert.Equal(byId.StartTime, b.StartTime);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNull()
    {
        // Arrange & Act
        var byId = await _repo.GetByIdAsync("nonexistent");

        // Assert
        Assert.Null(byId);
    }

    [Fact]
    public async Task ListByPunchId_ReturnsBreakSessions()
    {
        // Arrange
        var b1 = new BreakSession("punch-1", TimeOnly.FromDateTime(DateTime.UtcNow));
        var b2 = new BreakSession("punch-2", TimeOnly.FromDateTime(DateTime.UtcNow));
        await _repo.CreateAsync(b1);
        await _repo.CreateAsync(b2);

        // Act
        var list = await _repo.ListByPunchRecordAsync("punch-1");

        // Assert
        Assert.NotEmpty(list);
        Assert.Single(list);
        Assert.Contains(list, b => b.Id == b1.Id);
        Assert.DoesNotContain(list, b => b.Id == b2.Id);
    }

    [Fact]
    public async Task ListByPunchId_NonExistent_ReturnsEmptyList()
    {
        // Arrange & Act
        var got = await _repo.ListByPunchRecordAsync("fakepunch");

        // Assert
        Assert.Empty(got);
    }

    [Fact]
    public async Task DeleteBreakSession_Exists_DeletesBreakSession()
    {
        // Arrange
        var b = new BreakSession("punch-1", TimeOnly.FromDateTime(DateTime.UtcNow));
        await _repo.CreateAsync(b);

        // Act & Assert
        Assert.NotEmpty(await _repo.ListByPunchRecordAsync("punch-1"));
        await _repo.DeleteAsync(b.Id);
        Assert.Empty(await _repo.ListByPunchRecordAsync("punch-1"));
    }

    [Fact]
    public async Task DeleteBreakSession_NonExistent_ThrowsRepositoryException()
    {
        // Arrange
        var b = new BreakSession("punch-1", TimeOnly.FromDateTime(DateTime.UtcNow));

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() => 
            _repo.DeleteAsync(b.Id)
        );
    }

    [Fact]
    public async Task UpdateBreakSession_Exists_DeletesBreakSession()
    {
        // Arrange
        var start = TimeOnly.FromDateTime(DateTime.UtcNow);
        var b = new BreakSession("punch-1", start);
        await _repo.CreateAsync(b);

        Assert.Null(b.EndTime);

        var newEnd = start.AddMinutes(15);
        b.End(newEnd);

        // Act
        await _repo.UpdateAsync(b);
        var byId = await _repo.GetByIdAsync(b.Id);

        // Assert
        Assert.Equal(b.Id, byId?.Id);
        Assert.Equal(newEnd, b.EndTime);
    }

    [Fact]
    public async Task UpdateBreakSession_NonExistent_ThrowsRepositoryException()
    {
        // Arrange
        var b = new BreakSession("punch-1", TimeOnly.FromDateTime(DateTime.UtcNow));

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() => 
            _repo.UpdateAsync(b)
        );
    }

    [Fact]
    public async Task UpsertBreakSession_NonExistent_CreatesBreakSession()
    {
        // Arrange
        var b = new BreakSession("punch-1", TimeOnly.FromDateTime(DateTime.UtcNow));
        Assert.Empty(await _repo.ListByPunchRecordAsync("punch-1"));

        // Act 
        await _repo.UpdateAsync(b, true);
        var byId = await _repo.GetByIdAsync(b.Id);

        // Assert
        Assert.NotEmpty(await _repo.ListByPunchRecordAsync("punch-1"));
        Assert.Equal(b.Id, byId?.Id);
    }
}
