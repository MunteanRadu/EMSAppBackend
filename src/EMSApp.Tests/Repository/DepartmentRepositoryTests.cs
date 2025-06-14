using System.Threading;
using System.Threading.Tasks;
using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;
using EMSApp.Infrastructure;
using EMSApp.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Mongo2Go;
using MongoDB.Driver;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Repository")]
    public class DepartmentRepositoryTests : IAsyncLifetime
    {
        private readonly MongoDbRunner _dbRunner;
        private readonly IMongoDbContext _dbContext;
        private readonly DepartmentRepository _repo;
        private const string DbName = "TestDb";

        public DepartmentRepositoryTests()
        {
            _dbRunner = MongoDbRunner.Start();
            var settings = new DatabaseSettings
            {
                ConnectionString = _dbRunner.ConnectionString,
                DatabaseName = DbName
            };
            var options = Options.Create(settings);
            var client = new MongoClient(_dbRunner.ConnectionString);
            _dbContext = new MongoDbContext(client, options);
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
            var db = client.GetDatabase(DbName);
            await db.DropCollectionAsync("Departments");
        }

        [Fact]
        public async Task CreateAndFetch_Department_Works()
        {
            var dept = new Department("HR");
            dept.AssignManager("mgr-1");

            await _repo.CreateAsync(dept);
            var fetched = await _repo.GetByIdAsync(dept.Id);
            var list = await _repo.GetAllAsync();

            Assert.NotNull(fetched);
            Assert.Single(list);
            Assert.Equal(dept.Id, fetched!.Id);
            Assert.Equal(dept.Name, fetched.Name);
            Assert.Equal(dept.ManagerId, fetched.ManagerId);
        }

        [Fact]
        public async Task GetById_NonExistent_ReturnsNull() =>
            Assert.Null(await _repo.GetByIdAsync("no-id"));

        [Fact]
        public async Task GetAllAsync_Empty_ReturnsEmptyList() =>
            Assert.Empty(await _repo.GetAllAsync());

        [Fact]
        public async Task DeleteAsync_Existing_DeletesDepartment()
        {
            var dept = new Department("IT");
            await _repo.CreateAsync(dept);
            Assert.Single(await _repo.GetAllAsync());

            await _repo.DeleteAsync(dept.Id);
            Assert.Empty(await _repo.GetAllAsync());
        }

        [Fact]
        public async Task DeleteAsync_NonExistent_ThrowsRepositoryException() =>
            await Assert.ThrowsAsync<RepositoryException>(() =>
                _repo.DeleteAsync("no-id")
            );

        [Fact]
        public async Task UpdateAsync_Existing_UpdatesDepartment()
        {
            var dept = new Department("Sales");
            await _repo.CreateAsync(dept);

            dept.AddEmployee("emp-1");
            await _repo.UpdateAsync(dept);

            var fetched = await _repo.GetByIdAsync(dept.Id);
            Assert.NotNull(fetched);
            Assert.Contains("emp-1", fetched!.Employees);
        }

        [Fact]
        public async Task UpdateAsync_NonExistent_ThrowsRepositoryException() =>
            await Assert.ThrowsAsync<RepositoryException>(() =>
                _repo.UpdateAsync(new Department("NonExistent"))
            );

        [Fact]
        public async Task UpdateAsync_Upsert_CreatesWhenMissing()
        {
            var dept = new Department("Marketing");
            Assert.Empty(await _repo.GetAllAsync());

            await _repo.UpdateAsync(dept, isUpsert: true);
            var fetched = await _repo.GetByIdAsync(dept.Id);
            Assert.NotNull(fetched);
        }
    }
}
