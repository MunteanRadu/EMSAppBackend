using EMSApp.Infrastructure.Settings;
using EMSApp.Infrastructure;
using Mongo2Go;
using MongoDB.Driver;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Tests;

[Trait("Category", "Repository")]
public class ScheduleRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbRunner _dbRunner;
    private readonly IMongoDbContext _dbContext;
    private readonly ScheduleRepository _repo;
    private static string _dbName = "TestDb";

    public ScheduleRepositoryTests()
    {
        _dbRunner = MongoDbRunner.Start();

        var settings = new DatabaseSettings
        {
            ConnectionString = _dbRunner.ConnectionString,
            DatabaseName = _dbName,
        };

        _dbContext = new MongoDbContext(settings);
        _repo = new ScheduleRepository(_dbContext);
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
        await database.DropCollectionAsync("Schedules");
    }

    [Fact]
    public async Task CreateAndFetch_Schedule_Works()
    {
        // Arrange
        var startTime = TimeOnly.Parse("08:00");
        var endTime = startTime.AddHours(8);
        var s = new Schedule("dept-1", "manager-1", DayOfWeek.Monday, startTime, endTime, true);

        // Act
        await _repo.CreateAsync(s);
        var byId = await _repo.GetByIdAsync(s.Id);

        // Assert
        Assert.NotNull(byId);
        Assert.Equal(s.Id, byId.Id);
        Assert.Equal(s.DepartmentId, byId.DepartmentId);
        Assert.Equal(s.ManagerId, byId.ManagerId);
        Assert.Equal(s.Day, byId.Day);
        Assert.Equal(s.StartTime, byId.StartTime);
        Assert.Equal(s.EndTime, byId.EndTime);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNull()
    {
        // Assert
        var byId = await _repo.GetByIdAsync("schedule-1");

        // Act & Assert
        Assert.Null(byId);
    }

    [Fact]
    public async Task ListByDepartment_ReturnsSchedules()
    {
        // Arrange
        var startTime = TimeOnly.Parse("08:00");
        var endTime = startTime.AddHours(8);
        var s1 = new Schedule("dept-1", "manager-1", DayOfWeek.Monday, startTime, endTime, true);
        var s2= new Schedule("dept-2", "manager-2", DayOfWeek.Tuesday, startTime, endTime, true);
        await _repo.CreateAsync(s1);
        await _repo.CreateAsync(s2);

        // Act
        var list = await _repo.ListByDepartmentAsync("dept-1");

        // Assert
        Assert.NotEmpty(list);
        Assert.Contains(list, s => s.Id == s1.Id);
        Assert.DoesNotContain(list, s => s.Id == s2.Id);
    }

    [Fact]
    public async Task ListByDepartment_NonExistent_ReturnsEmptyList()
    {
        // Assert
        var list = await _repo.ListByDepartmentAsync("dept-1");

        // Act & Assert
        Assert.Empty(list);
    }

    [Fact]
    public async Task ListByManager_ReturnsSchedules()
    {
        // Arrange
        var startTime = TimeOnly.Parse("08:00");
        var endTime = startTime.AddHours(8);
        var s1 = new Schedule("dept-1", "manager-1", DayOfWeek.Monday, startTime, endTime, true);
        var s2 = new Schedule("dept-2", "manager-2", DayOfWeek.Tuesday, startTime, endTime, true);
        await _repo.CreateAsync(s1);
        await _repo.CreateAsync(s2);

        // Act
        var list = await _repo.ListByManagerAsync("manager-2");

        // Assert
        Assert.NotEmpty(list);
        Assert.Contains(list, s => s.Id == s2.Id);
        Assert.DoesNotContain(list, s => s.Id == s1.Id);
    }

    [Fact]
    public async Task ListByManager_NonExistent_ReturnsEmptyList()
    {
        // Assert
        var list = await _repo.ListByManagerAsync("manager-1");

        // Act & Assert
        Assert.Empty(list);
    }

    [Fact]
    public async Task DeleteSchedule_Exists_DeletesSchedule() 
    {
        // Arrange
        var startTime = TimeOnly.Parse("08:00");
        var endTime = startTime.AddHours(8);
        var s = new Schedule("dept-1", "manager-1", DayOfWeek.Monday, startTime, endTime, true);
        await _repo.CreateAsync(s);
        Assert.NotEmpty(await _repo.ListByManagerAsync("manager-1"));

        // Act
        await _repo.DeleteAsync(s.Id);

        // Assert
        Assert.Empty(await _repo.ListByManagerAsync("manager-1"));
    }

    [Fact]
    public async Task DeleteSchedule_NonExistent_ThrowsRepositoryException()
    {
        // Arrange
        var startTime = TimeOnly.Parse("08:00");
        var endTime = startTime.AddHours(8);
        var s = new Schedule("dept-1", "manager-1", DayOfWeek.Monday, startTime, endTime, true);
        Assert.Empty(await _repo.ListByManagerAsync("manager-1"));

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _repo.DeleteAsync(s.Id)
        );
    }

    [Fact]
    public async Task UpdateSchedule_Exists_UpdatesSchedule()
    {
        // Arrange
        var startTime = TimeOnly.Parse("08:00");
        var endTime = startTime.AddHours(8);
        var s = new Schedule("dept-1", "manager-1", DayOfWeek.Monday, startTime, endTime, true);
        await _repo.CreateAsync(s);
        Assert.Equal(startTime, s.StartTime);
        Assert.Equal(endTime, s.EndTime);

        var newStart = startTime.AddHours(2);
        var newEnd = newStart.AddHours(4);
        s.UpdateShift(newStart, newEnd, true);

        // Act
        await _repo.UpdateAsync(s);
        var byId = await _repo.GetByIdAsync(s.Id);

        // Assert
        Assert.NotNull(byId);
        Assert.Equal(s.Id, byId.Id);
    }

    [Fact]
    public async Task UpdateSchedule_NonExsitent_ThrowsRepositoryException()
    {
        // Arrange
        var startTime = TimeOnly.Parse("08:00");
        var endTime = startTime.AddHours(8);
        var s = new Schedule("dept-1", "manager-1", DayOfWeek.Monday, startTime, endTime, true);
        Assert.Null(await _repo.GetByIdAsync(s.Id));

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _repo.UpdateAsync(s)
        );
    }

    [Fact]
    public async Task UpsertSchedule_NonExistent_CreatesSchedule()
    {
        // Arrange
        var startTime = TimeOnly.Parse("08:00");
        var endTime = startTime.AddHours(8);
        var s = new Schedule("dept-1", "manager-1", DayOfWeek.Monday, startTime, endTime, true);
        Assert.Null(await _repo.GetByIdAsync(s.Id));

        // Act
        await _repo.UpdateAsync(s, true);
        var byId = await _repo.GetByIdAsync(s.Id);

        // Assert
        Assert.NotNull(byId);
        Assert.Equal(s.Id, byId?.Id);
    }
}
