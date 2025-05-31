
using EMSApp.Domain;
using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;
using EMSApp.Infrastructure;
using EMSApp.Infrastructure.Settings;
using Mongo2Go;
using MongoDB.Driver;

namespace EMSApp.Tests;

[Trait("Category", "Repository")]
public class AssignmentRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbRunner _dbRunner;
    private readonly IMongoDbContext _dbContext;
    private readonly AssignmentRepository _repo;
    private static string _dbName = "TestDb";

    public AssignmentRepositoryTests()
    {
        _dbRunner = MongoDbRunner.Start();

        var settings = new DatabaseSettings
        {
            ConnectionString = _dbRunner.ConnectionString,
            DatabaseName = _dbName,
        };

        _dbContext = new MongoDbContext(settings);
        _repo = new AssignmentRepository(_dbContext);
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
        await database.DropCollectionAsync("Assignments");
    }

    [Fact]
    public async Task CreateAndFetch_Assignment_Works()
    {
        // Arrange
        var a = new Assignment("title", "description", DateTime.UtcNow.AddDays(10), "user-1");

        // Act
        await _repo.CreateAsync(a);
        var byId = await _repo.GetByIdAsync(a.Id);

        // Assert
        Assert.NotNull(byId);
        Assert.Equal(a.Id, byId.Id);
        Assert.Equal(a.Title, byId.Title);
        Assert.Equal(a.Description, byId.Description);
        Assert.Equal(a.AssignedToId, byId.AssignedToId);
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
    public async Task ListByAsignee_ReturnsAssignments()
    {
        // Arrange
        var a1 = new Assignment("title", "description", DateTime.UtcNow.AddDays(10), "user-1");
        var a2 = new Assignment("title2", "description2", DateTime.UtcNow.AddDays(5), "user-2");
        await _repo.CreateAsync(a1);
        await _repo.CreateAsync(a2);

        // Act
        var list = await _repo.ListByAssigneeAsync("user-1");

        // Assert
        Assert.NotEmpty(list);
        Assert.Contains(list, a => a.Id == a1.Id);
    }

    [Fact]
    public async Task ListByAsignee_NonExistent_ReturnsEmptyList()
    {
        // Arrange & Act
        var list = await _repo.ListByAssigneeAsync("user-1");

        // Assert
        Assert.Empty(list);
    }

    [Fact]
    public async Task ListByStatus_ReturnsAssignments()
    {
        // Arrange
        var a1 = new Assignment("title", "description", DateTime.UtcNow.AddDays(10), "user-1");
        a1.Start();
        var a2 = new Assignment("title2", "description2", DateTime.UtcNow.AddDays(5), "user-2");
        a2.Start();
        await _repo.CreateAsync(a1);
        await _repo.CreateAsync(a2);

        // Act
        var listPending = await _repo.ListByStatusAsync(AssignmentStatus.Pending);
        var listStarted = await _repo.ListByStatusAsync(AssignmentStatus.InProgress);
        var listDone = await _repo.ListByStatusAsync(AssignmentStatus.Done);
        var listReviewed = await _repo.ListByStatusAsync(AssignmentStatus.Approved);

        // Assert
        Assert.Empty(listPending);
        Assert.Empty(listDone);
        Assert.Empty(listReviewed);
        Assert.NotEmpty(listStarted);
        Assert.Contains(listStarted, a => a.Id == a1.Id);
        Assert.Contains(listStarted, a => a.Id == a2.Id);
    }

    [Fact]
    public async Task ListByStatus_NonExistent_ReturnsEmptyList()
    {
        // Arrange & Act
        var list = await _repo.ListByStatusAsync(AssignmentStatus.Approved);

        // Assert
        Assert.Empty(list);
    }

    [Fact]
    public async Task ListOverdue_ReturnsAssignments()
    {
        // Arrange
        var a1 = new Assignment("title", "description", DateTime.UtcNow.AddDays(10), "user-1");
        a1.Start();
        var a2 = new Assignment("title2", "description2", DateTime.UtcNow.AddDays(5), "user-2");
        await _repo.CreateAsync(a1);
        await _repo.CreateAsync(a2);

        // Act
        var listOverdue = await _repo.ListOverdueAsync(DateTime.UtcNow.AddDays(8));

        // Assert
        Assert.NotEmpty(listOverdue);
        Assert.DoesNotContain(listOverdue, a => a.Id == a1.Id);
        Assert.Contains(listOverdue, a => a.Id == a2.Id);
    }

    [Fact]
    public async Task ListOverdue_NonExistent_ReturnsEmptyList()
    {
        // Arrange & Act
        var list = await _repo.ListOverdueAsync(DateTime.UtcNow);

        // Assert
        Assert.Empty(list);
    }

    [Fact]
    public async Task DeleteAssignment_Exists_DeletesAssignment()
    {
        // Arrange
        var a = new Assignment("title", "description", DateTime.UtcNow.AddDays(10), "user-1");
        await _repo.CreateAsync(a);
        Assert.NotEmpty(await _repo.ListByAssigneeAsync("user-1"));

        // Act
        await _repo.DeleteAsync(a.Id);

        // Assert
        Assert.Empty(await _repo.ListByAssigneeAsync("user-1"));
    }

    [Fact]
    public async Task DeleteAssignment_NonExistent_ThrowsRepositoryException()
    {
        // Arrange
        var a = new Assignment("title", "description", DateTime.UtcNow.AddDays(10), "user-1");

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _repo.DeleteAsync(a.Id)
        );
    }

    [Fact]
    public async Task UpdateAssignment_Exists_UpdatesAssignment()
    {
        // Arrange
        var a = new Assignment("title", "description", DateTime.UtcNow.AddDays(10), "user-1");
        await _repo.CreateAsync(a);
        Assert.Equal(AssignmentStatus.Pending, a.Status);
        a.Start();

        // Act
        await _repo.UpdateAsync(a);

        // Assert
        Assert.Equal(AssignmentStatus.InProgress, a.Status);
    }

    [Fact]
    public async Task UpdateAssignment_NonExistent_ThrowsRepositoryException()
    {
        // Arrange
        var a = new Assignment("title", "description", DateTime.UtcNow.AddDays(10), "user-1");

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _repo.UpdateAsync(a)
        );
    }

    [Fact]
    public async Task UpsertAssignment_NonExistent_CreatesAssignment()
    {
        // Arrange
        var a1 = new Assignment("title", "description", DateTime.UtcNow.AddDays(10), "user-1");
        Assert.Empty(await _repo.ListByAssigneeAsync("user-1"));

        // Act
        await _repo.UpdateAsync(a1, true);
        var list = await _repo.ListByAssigneeAsync("user-1");

        // Assert
        Assert.NotEmpty(list);
        Assert.Contains(list, a => a.Id == a1.Id);
    }
}
