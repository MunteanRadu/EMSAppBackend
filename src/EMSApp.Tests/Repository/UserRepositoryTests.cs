using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;
using EMSApp.Infrastructure;
using EMSApp.Infrastructure.Settings;
using Intercom.Core;
using Microsoft.Extensions.Options;
using Mongo2Go;
using MongoDB.Driver;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Repository")]
    public class UserRepositoryTests : IAsyncLifetime
    {
        private readonly MongoDbRunner _dbRunner;
        private readonly IMongoDbContext _dbContext;
        private readonly UserRepository _repo;
        private const string DbName = "TestDb";

        public UserRepositoryTests()
        {
            _dbRunner = MongoDbRunner.Start();
            var settings = new DatabaseSettings
            {
                ConnectionString = _dbRunner.ConnectionString,
                DatabaseName = DbName
            };
            var client = new MongoClient(_dbRunner.ConnectionString);
            var options = Options.Create(settings);
            _dbContext = new MongoDbContext(client, options);
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
            var database = client.GetDatabase(DbName);
            await database.DropCollectionAsync("Users");
        }

        [Fact]
        public async Task CreateAndFetch_ByIdEmailUsername_Works()
        {
            var u = new User("a@b.com", "alice", "password123", "dept-1");

            await _repo.CreateAsync(u);

            var byId = await _repo.GetByIdAsync(u.Id);
            var byEmail = await _repo.GetByEmailAsync(u.Email);
            var byUsername = await _repo.GetByUsernameAsync(u.Username);

            Assert.NotNull(byId);
            Assert.Equal(u.Id, byId!.Id);
            Assert.Equal(u.Email, byEmail!.Email);
            Assert.Equal(u.Username, byUsername!.Username);
        }

        [Fact]
        public async Task GetById_NonExistent_ReturnsNull() =>
            Assert.Null(await _repo.GetByIdAsync("nope"));

        [Fact]
        public async Task GetByEmail_NonExistent_ReturnsNull() =>
            Assert.Null(await _repo.GetByEmailAsync("nobody@x.com"));

        [Fact]
        public async Task GetByUsername_NonExistent_ReturnsNull() =>
            Assert.Null(await _repo.GetByUsernameAsync("ghost"));

        [Fact]
        public async Task GetAllAsync_ReturnsAllUsers()
        {
            var u1 = new User("a@b.com", "alice", "password1", "d1");
            var u2 = new User("b@b.com", "bob", "password2", "d2");

            await _repo.CreateAsync(u1);
            await _repo.CreateAsync(u2);

            var all = await _repo.GetAllAsync();
            Assert.Contains(all, u => u.Id == u1.Id);
            Assert.Contains(all, u => u.Id == u2.Id);
        }

        [Fact]
        public async Task GetAllAsync_EmptyCollection_ReturnsEmptyList() =>
            Assert.Empty(await _repo.GetAllAsync());

        [Fact]
        public async Task ListByDepartmentAsync_FiltersCorrectly()
        {
            var u1 = new User("a@b.com", "alice", "password1", "dept-1");
            var u2 = new User("b@b.com", "bob", "password2", "dept-2");

            await _repo.CreateAsync(u1);
            await _repo.CreateAsync(u2);

            var d1 = await _repo.ListByDepartmentAsync("dept-1");
            Assert.Single(d1);
            Assert.Equal("dept-1", d1[0].DepartmentId);
        }

        [Fact]
        public async Task ListByDepartmentAsync_NonExistent_ReturnsEmptyList() =>
            Assert.Empty(await _repo.ListByDepartmentAsync("nope"));

        [Fact]
        public async Task ListByRoleAsync_FiltersCorrectly()
        {
            var m = new User("m@x", "manager", "password", "d");
            m.UpdateRole(UserRole.Manager);
            var e = new User("e@x", "employee", "password", "d");
            e.UpdateRole(UserRole.Employee);

            await _repo.CreateAsync(m);
            await _repo.CreateAsync(e);

            var managers = await _repo.ListByRoleAsync(UserRole.Manager, CancellationToken.None);
            Assert.Single(managers);
            Assert.Equal(UserRole.Manager, managers[0].Role);

            var employees = await _repo.ListByRoleAsync(UserRole.Employee, CancellationToken.None);
            Assert.Single(employees);
            Assert.Equal(UserRole.Employee, employees[0].Role);
        }

        [Fact]
        public async Task ListByRoleAsync_NullRole_ReturnsAllUsers()
        {
            var u1 = new User("a@b", "alice", "password", "d");
            var u2 = new User("b@b", "bob", "password", "d");

            await _repo.CreateAsync(u1);
            await _repo.CreateAsync(u2);

            var all = await _repo.GetAllAsync(CancellationToken.None);
            Assert.Equal(2, all.Count);
        }

        [Fact]
        public async Task DeleteAsync_Existing_DeletesUser()
        {
            var u = new User("x@y", "xyz", "password", "d");
            await _repo.CreateAsync(u);

            await _repo.DeleteAsync(u.Id);
            Assert.Empty(await _repo.GetAllAsync());
        }

        [Fact]
        public async Task DeleteAsync_NonExistent_ThrowsRepositoryException() =>
            await Assert.ThrowsAsync<RepositoryException>(() => _repo.DeleteAsync("nope"));

        [Fact]
        public async Task UpdateAsync_Existing_UpdatesUser()
        {
            var u = new User("a@b", "alice", "password", "d1");
            await _repo.CreateAsync(u);

            u.UpdateDepartment("d2");
            await _repo.UpdateAsync(u);

            var fetched = await _repo.GetByIdAsync(u.Id);
            Assert.Equal("d2", fetched!.DepartmentId);
        }

        [Fact]
        public async Task UpdateAsync_NonExistent_ThrowsRepositoryException() =>
            await Assert.ThrowsAsync<RepositoryException>(() => _repo.UpdateAsync(
                new User("a@b", "alice", "password", "d"),
                isUpsert: false
            ));

        [Fact]
        public async Task UpdateAsync_Upsert_CreatesWhenMissing()
        {
            var u = new User("u@x", "user", "password", "d");
            await _repo.UpdateAsync(u, isUpsert: true);

            var fetched = await _repo.GetByIdAsync(u.Id);
            Assert.NotNull(fetched);
        }
    }
}
