using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;
using EMSApp.Infrastructure;
using EMSApp.Infrastructure.Settings;
using Mongo2Go;
using MongoDB.Driver;

namespace EMSApp.Tests;

[Trait("Category", "Repository")]
public class UserRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbRunner _dbRunner;
    private readonly IMongoDbContext _dbContext;
    private readonly UserRepository _repo;
    private const string _dbName = "TestDb";

    public UserRepositoryTests()
    {
        _dbRunner = MongoDbRunner.Start();

        var settings = new DatabaseSettings
        {
            ConnectionString = _dbRunner.ConnectionString,
            DatabaseName = _dbName
        };
        _dbContext = new MongoDbContext(settings);
        _repo = new UserRepository(_dbContext);
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
        await database.DropCollectionAsync("Users");
    }

    [Fact]
    public async Task CreateAndFetch_User_Works()
    {
        // Arrange
        var u = new User("a@b", "alice", "password123", "dept-1");

        // Act
        await _repo.CreateAsync(u);
        var byId = await _repo.GetByIdAsync(u.Id);
        var byEmail = await _repo.GetByEmailAsync(u.Email);

        // Assert
        Assert.NotNull(byEmail);
        Assert.NotNull(byId);
        Assert.Equal(byId!.Id, byEmail!.Id);
        Assert.Equal(u.Email, byId!.Email);
        Assert.Equal(u.Username, byId!.Username);
        Assert.Equal(u.PasswordHash, byId!.PasswordHash);
        Assert.Equal(u.DepartmentId, byId!.DepartmentId);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNull()
    {
        // Arrange & Act
        var got = await _repo.GetByIdAsync("nope");

        // Assert
        Assert.Null(got);
    }

    [Fact]
    public async Task GetByEmail_NonExistent_ReturnsNull()
    {
        // Arrange & Act
        var got = await _repo.GetByEmailAsync("nobody@example.com");

        // Assert
        Assert.Null(got);
    }


    [Fact]
    public async Task ListAll_ReturnsCreatedUsers()
    {
        // Arrange
        var u1 = new User("a@b", "alice", "password123", "dept-1");
        var u2 = new User("b@b", "bob", "123password", "dept-2");

        // Act
        await _repo.CreateAsync(u1);
        await _repo.CreateAsync(u2);
        var list = await _repo.GetAllAsync();

        // Assert
        Assert.Contains(list, u => u.Id == u1.Id);
        Assert.Contains(list, u => u.Id == u2.Id);
    }

    [Fact]
    public async Task ListAll_NonExistent_ReturnsEmptyList()
    {
        // Arrange & Act
        var got = await _repo.GetAllAsync();

        // Assert
        Assert.Empty(got);
    }

    [Fact]
    public async Task ListByDepartment_FiltersCorrectly()
    {
        // Arrange
        var u1 = new User("a@b", "alice", "password123", "dept-1");
        var u2 = new User("b@b", "bob", "123password", "dept-2");

        // Act
        await _repo.CreateAsync(u1);
        await _repo.CreateAsync(u2);
        var list = await _repo.ListByDepartmentAsync(u1.DepartmentId);

        // Assert
        Assert.Contains(list, u => u.Id == u1.Id);
        Assert.DoesNotContain(list, u => u.Id == u2.Id);
    }

    [Fact]
    public async Task ListByDepartment_NonExistent_ReturnsEmptyList()
    {
        // Arrange & Act
        var got = await _repo.ListByDepartmentAsync("fakeDepartment");

        // Assert
        Assert.Empty(got);
    }

    [Fact]
    public async Task DeleteUser_Exists_DeletesUser()
    {
        // Arrange
        var u = new User("a@b", "alice", "password123", "dept-1");

        // Act & Assert
        await _repo.CreateAsync(u);
        var users = await _repo.GetAllAsync();
        Assert.Single(users);
        await _repo.DeleteAsync(u.Id);
        users = await _repo.GetAllAsync();
        Assert.Empty(users);
    }

    [Fact]
    public async Task DeleteUser_NonExistent_ThrowsRepositoryException()
    {
        // Arrange
        var u = new User("a@b", "alice", "password123", "dept-1");

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _repo.DeleteAsync(u.Id)
        );
    }

    [Fact]
    public async Task UpdateUser_Exists_UpdatesUser()
    {
        // Arrange
        var u = new User("a@b", "alice", "password123", "dept-1");
        await _repo.CreateAsync(u);

        u.UpdateDepartment("new-dept");

        // Act
        await _repo.UpdateAsync(u);
        var user = await _repo.GetByIdAsync(u.Id);

        // Assert
        Assert.Equal(u.Id, user.Id);
        Assert.Equal("new-dept", user.DepartmentId);
    }

    [Fact]
    public async Task UpdateUser_NonExistent_ThrowsRepositoryException()
    {
        // Arrange
        var u = new User("a@b", "alice", "password123", "dept-1");

        u.UpdateDepartment("new-dept");

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _repo.UpdateAsync(u)
        );
    }

    [Fact]
    public async Task UpsertUser_NonExistent_CreatesUser()
    {
        // Arrange
        var u = new User("a@b", "alice", "password123", "dept-1");
        Assert.Empty(await _repo.GetAllAsync());

        // Act
        await _repo.UpdateAsync(u, true);
        var byId = await _repo.GetByIdAsync(u.Id);

        // Assert
        Assert.NotNull(byId);
        Assert.Equal(u.Id, byId.Id);
    }
}
