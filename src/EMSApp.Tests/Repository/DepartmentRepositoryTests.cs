
using EMSApp.Infrastructure.Settings;
using EMSApp.Infrastructure;
using Mongo2Go;
using MongoDB.Driver;
using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Tests;

[Trait("Category", "Repository")]
public class DepartmentRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbRunner _dbRunner;
    private readonly IMongoDbContext _dbContext;
    private readonly DepartmentRepository _repo;
    private static string _dbName = "TestDb";

    public DepartmentRepositoryTests()
    {
        _dbRunner = MongoDbRunner.Start();

        var settings = new DatabaseSettings
        {
            ConnectionString = _dbRunner.ConnectionString,
            DatabaseName = _dbName,
        };

        _dbContext = new MongoDbContext(settings);
        _repo = new DepartmentRepository(_dbContext);
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
        await database.DropCollectionAsync("Departments");
    }

    [Fact]
    public async Task CreateAndFetch_Department_Works()
    {
        // Arrange
        var d = new Department("dept-1", "manager-1");

        // Act
        await _repo.CreateAsync(d);
        var byId = await _repo.GetByIdAsync(d.Id);
        var list = await _repo.GetAllAsync();

        // Assert
        Assert.NotNull(byId);
        Assert.NotEmpty(list);
        Assert.Contains(list, dept => dept.Id == d.Id);
        Assert.Equal(d.Id, byId.Id);
        Assert.Equal(d.Name, byId.Name);
        Assert.Equal(d.ManagerId, byId.ManagerId);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNull()
    {
        // Arrange
        var byId = await _repo.GetByIdAsync("dept-1");

        // Act & Assert
        Assert.Null(byId);
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
    public async Task DeleteDepartment_Exists_DeletesDepartment()
    {
        // Arrange
        var d = new Department("dept-1", "manager-1");
        await _repo.CreateAsync(d);
        Assert.NotEmpty(await _repo.GetAllAsync());

        // Act
        await _repo.DeleteAsync(d.Id);

        // Assert
        Assert.Empty(await _repo.GetAllAsync());
    }

    [Fact]
    public async Task DeleteDepartment_NonExistent_ThrowsRepositoryException()
    {
        // Arrange
        var d = new Department("dept-1", "manager-1");

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _repo.DeleteAsync(d.Id)
        );
    }

    [Fact]
    public async Task UpdateDepartment_Exists_UpdatesDepartment()
    {
        // Arrange
        var d = new Department("dept-1", "manager-1");
        Assert.Empty(d.Employees);
        await _repo.CreateAsync(d);

        d.AddEmployee("employee-1");
        Assert.Contains(d.Employees, e => e == "employee-1");

        // Act
        await _repo.UpdateAsync(d);
        var byId = await _repo.GetByIdAsync(d.Id);

        // Assert
        Assert.NotNull(byId);
        Assert.Equal(d.Id, byId.Id);
        Assert.Equal(d.Employees, byId!.Employees);
    }

    [Fact]
    public async Task UpdateDepartment_NonExistent_ThrowsRepositoryException()
    {
        // Arrange
        var d = new Department("dept-1", "manager-1");

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _repo.UpdateAsync(d)
        );
    }

    [Fact]
    public async Task UpsertDepartment_NonExistent_CreatesDepartment()
    {
        // Arrange
        var d = new Department("dept-1", "manager-1");
        Assert.Empty(await _repo.GetAllAsync());

        // Act
        await _repo.UpdateAsync(d, true);
        var byId = await _repo.GetByIdAsync(d.Id);

        // Assert
        Assert.NotNull(byId);
        Assert.Equal(d.Id, byId.Id);
    }
}
