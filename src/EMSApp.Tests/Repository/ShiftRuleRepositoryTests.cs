using System.Threading;
using System.Threading.Tasks;
using EMSApp.Domain;
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
    public class ShiftRuleRepositoryTests : IAsyncLifetime
    {
        private const string DbName = "TestDb";
        private readonly MongoDbRunner _runner;
        private readonly IMongoDbContext _context;
        private readonly ShiftRuleRepository _repo;

        public ShiftRuleRepositoryTests()
        {
            _runner = MongoDbRunner.Start();
            var settings = new DatabaseSettings
            {
                ConnectionString = _runner.ConnectionString,
                DatabaseName = DbName
            };
            var options = Options.Create(settings);
            var client = new MongoClient(_runner.ConnectionString);
            _context = new MongoDbContext(client, options);
            _repo = new ShiftRuleRepository(_context);
        }

        public async Task InitializeAsync()
        {
            var client = new MongoClient(_runner.ConnectionString);
            await client.GetDatabase(DbName).DropCollectionAsync("ShiftRules");
        }

        public Task DisposeAsync()
        {
            _runner.Dispose();
            return Task.CompletedTask;
        }

        [Fact]
        public async Task GetByDepartmentAsync_NonExistent_ReturnsNull()
        {
            var result = await _repo.GetByDepartmentAsync("deptX", CancellationToken.None);
            Assert.Null(result);
        }

        [Fact]
        public async Task UpsertAsync_InsertsNewRule_AndGetByDepartmentAsync_Works()
        {
            // Arrange
            var rule = new ShiftRule(departmentId: "dept1", minShift1: 480, minShift2: 450, minNightShift: 360, maxConsecutiveNight: 2, minRestHoursBetweenShifts: 8.0);

            // Act
            await _repo.UpsertAsync(rule, CancellationToken.None);
            var fetched = await _repo.GetByDepartmentAsync("dept1", CancellationToken.None);

            // Assert
            Assert.NotNull(fetched);
            Assert.Equal(rule.DepartmentId, fetched.DepartmentId);
            Assert.Equal(rule.MinPerShift1, fetched.MinPerShift1);
            Assert.Equal(rule.MaxConsecutiveNightShifts, fetched.MaxConsecutiveNightShifts);
        }

        [Fact]
        public async Task UpsertAsync_UpdatesExistingRule()
        {
            // Arrange initial
            var rule = new ShiftRule("dept2", 300, 300, 300, 1, 6.0);
            await _repo.UpsertAsync(rule, CancellationToken.None);

            // Modify and upsert
            rule.Update(minShift1: 200, minShift2: 200, minNightShift: 200, maxConsecutiveNight: 3, minRestHours: 10.0);
            await _repo.UpsertAsync(rule, CancellationToken.None);

            // Act
            var fetched = await _repo.GetByDepartmentAsync("dept2", CancellationToken.None);

            // Assert
            Assert.NotNull(fetched);
            Assert.Equal(200, fetched.MinPerShift1);
            Assert.Equal(3, fetched.MaxConsecutiveNightShifts);
            Assert.Equal(10.0, fetched.MinRestHoursBetweenShifts);
        }

        [Fact]
        public async Task DeleteByDepartmentAsync_RemovesRule()
        {
            // Arrange
            var rule = new ShiftRule("dept3", 100, 100, 100, 1, 5.0);
            await _repo.UpsertAsync(rule, CancellationToken.None);
            Assert.NotNull(await _repo.GetByDepartmentAsync("dept3", CancellationToken.None));

            // Act
            await _repo.DeleteByDepartmentAsync("dept3", CancellationToken.None);
            var fetched = await _repo.GetByDepartmentAsync("dept3", CancellationToken.None);

            // Assert
            Assert.Null(fetched);
        }

        [Fact]
        public async Task DeleteByDepartmentAsync_NonExistent_DoesNotThrow()
        {
            // Act & Assert
            await _repo.DeleteByDepartmentAsync("no-dept", CancellationToken.None);
            // still null
            Assert.Null(await _repo.GetByDepartmentAsync("no-dept", CancellationToken.None));
        }
    }
}
