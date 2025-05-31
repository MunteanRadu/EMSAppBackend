using EMSApp.Infrastructure.Settings;
using EMSApp.Infrastructure;
using Mongo2Go;
using MongoDB.Driver;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Tests;

[Trait("Category", "Repository")]
public class LeaveRequestRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbRunner _dbRunner;
    private readonly IMongoDbContext _dbContext;
    private readonly LeaveRequestRepository _repo;
    private static string _dbName = "TestDb";

    public LeaveRequestRepositoryTests()
    {
        _dbRunner = MongoDbRunner.Start();

        var settings = new DatabaseSettings
        {
            ConnectionString = _dbRunner.ConnectionString,
            DatabaseName = _dbName,
        };

        _dbContext = new MongoDbContext(settings);
        _repo = new LeaveRequestRepository(_dbContext);
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
        await database.DropCollectionAsync("LeaveRequests");
    }

    [Fact]
    public async Task CreateAndFetch_LeaveRequest_Works()
    {
        // Arrange
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var l = new LeaveRequest("user-1", LeaveType.Paid, start, start.AddDays(5), "goodreason");

        // Act
        await _repo.CreateAsync(l);
        var byId = await _repo.GetByIdAsync(l.Id);

        // Assert
        Assert.NotNull(byId);
        Assert.Equal(l.Id, byId.Id);
        Assert.Equal(l.Type, byId.Type);
        Assert.Equal(l.StartDate, byId.StartDate);
        Assert.Equal(l.EndDate, byId.EndDate);
        Assert.Equal(l.Reason, byId.Reason);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNull()
    {
        // Arrange
        var byId = await _repo.GetByIdAsync("leave-1");

        // Act & Assert
        Assert.Null(byId);
    }

    [Fact]
    public async Task ListByManager_ReturnsLeaves()
    {
        // Arrange
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var l1 = new LeaveRequest("user-1", LeaveType.Paid, start, start.AddDays(5), "goodreason");
        l1.Approve("manager-1");
        var l2 = new LeaveRequest("user-2", LeaveType.Paid, start.AddDays(5), start.AddDays(10), "verygoodreason");
        l2.Approve("manager-2");
        await _repo.CreateAsync(l1);
        await _repo.CreateAsync(l2);

        // Act
        var list = await _repo.ListByManagerAsync("manager-1");

        // Assert
        Assert.NotEmpty(list);
        Assert.Contains(list, l => l.Id == l1.Id);
    }

    [Fact]
    public async Task ListByManager_NonExistent_ReturnsEmptyList()
    {
        // Arrange
        var list = await _repo.ListByManagerAsync("manager-1");

        // Act & Assert
        Assert.Empty(list);
    }

    [Fact]
    public async Task ListByStatus_ReturnsLeaves()
    {
        // Arrange
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var l1 = new LeaveRequest("user-1", LeaveType.Paid, start, start.AddDays(5), "goodreason");
        l1.Approve("manager-1");
        var l2 = new LeaveRequest("user-2", LeaveType.Paid, start.AddDays(5), start.AddDays(10), "verygoodreason");
        await _repo.CreateAsync(l1);
        await _repo.CreateAsync(l2);

        // Act
        var list = await _repo.ListByStatusAsync(LeaveStatus.Pending);

        // Assert
        Assert.NotEmpty(list);
        Assert.Contains(list, l => l.Id == l2.Id);
    }

    [Fact]
    public async Task ListByStatus_NonExistent_ReturnsEmptyList()
    {
        // Arrange
        var list = await _repo.ListByStatusAsync(LeaveStatus.Pending);

        // Act & Assert
        Assert.Empty(list);
    }

    [Fact]
    public async Task ListByUser_ReturnsLeaves()
    {
        // Arrange
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var l1 = new LeaveRequest("user-1", LeaveType.Paid, start, start.AddDays(5), "goodreason");
        l1.Approve("manager-1");
        var l2 = new LeaveRequest("user-2", LeaveType.Paid, start.AddDays(5), start.AddDays(10), "verygoodreason");
        await _repo.CreateAsync(l1);
        await _repo.CreateAsync(l2);

        // Act
        var list = await _repo.ListByUserAsync("user-1");

        // Assert
        Assert.NotEmpty(list);
        Assert.Contains(list, l => l.Id == l1.Id);
        Assert.DoesNotContain(list, l => l.Id == l2.Id);
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
    public async Task DeleteLeaveRequest_Exists_DeletesLeaveRequest()
    {
        // Arrange
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var l = new LeaveRequest("user-1", LeaveType.Paid, start, start.AddDays(5), "goodreason");
        await _repo.CreateAsync(l);
        Assert.NotEmpty(await _repo.ListByUserAsync("user-1"));

        // Act
        await _repo.DeleteAsync(l.Id);

        // Assert
        Assert.Empty(await _repo.ListByUserAsync("user-1"));
    }

    [Fact]
    public async Task DeleteLeaveRequest_NonExistent_ThrowsRepositoryException()
    {
        // Arrange
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var l = new LeaveRequest("user-1", LeaveType.Paid, start, start.AddDays(5), "goodreason");

        // Act
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _repo.DeleteAsync(l.Id)
        );
    }

    [Fact]
    public async Task UpdateLeaveRequest_Exists_UpdatesLeaveRequest()
    {
        // Arrange
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var l = new LeaveRequest("user-1", LeaveType.Paid, start, start.AddDays(5), "goodreason");
        Assert.Equal(LeaveStatus.Pending, l.Status);
        await _repo.CreateAsync(l);

        l.Approve("manager-1");
        Assert.Equal(LeaveStatus.Approved, l.Status);

        // Act
        await _repo.UpdateAsync(l);
        var byId = await _repo.GetByIdAsync(l.Id);

        // Assert
        Assert.NotNull(byId);
        Assert.Equal(l.Status, byId.Status);
    }

    [Fact]
    public async Task UpdateLeaveRequest_NonExistent_ThrowsRepositoryException()
    {
        // Arrange
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var l = new LeaveRequest("user-1", LeaveType.Paid, start, start.AddDays(5), "goodreason");

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _repo.UpdateAsync(l)
        );
    }

    [Fact]
    public async Task UpsertLeaveRequest_NonExistent_CreatesLeaveRequest()
    {
        // Arrange
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var l = new LeaveRequest("user-1", LeaveType.Paid, start, start.AddDays(5), "goodreason");
        Assert.Null(await _repo.GetByIdAsync(l.Id));

        // Act
        await _repo.UpdateAsync(l, true);
        var byId = await _repo.GetByIdAsync(l.Id);

        // Assert
        Assert.NotNull(byId);
        Assert.Equal(l.Id, byId.Id);
    }
}
