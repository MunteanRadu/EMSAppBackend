using System;
using System.Collections.Generic;
using System.Linq;
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
    public class PolicyRepositoryTests : IAsyncLifetime
    {
        private readonly MongoDbRunner _dbRunner;
        private readonly IMongoDbContext _dbContext;
        private readonly PolicyRepository _repo;
        private const string DbName = "TestDb";

        public PolicyRepositoryTests()
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
            _repo = new PolicyRepository(_dbContext);
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
            await db.DropCollectionAsync("Policies");
        }

        private static IDictionary<LeaveType, int> GetQuotas()
        {
            return Enum.GetValues<LeaveType>()
                       .ToDictionary(lt => lt, lt => 10);
        }

        [Fact]
        public async Task CreateAndFetch_ByYear_Works()
        {
            var quotas = GetQuotas();
            var policy = new Policy(
                year: 2025,
                workDayStart: new TimeOnly(8, 0),
                workDayEnd: new TimeOnly(16, 0),
                punchInTolerance: TimeSpan.FromMinutes(15),
                punchOutTolerance: TimeSpan.FromMinutes(10),
                maxSingleBreak: TimeSpan.FromMinutes(30),
                maxTotalBreakPerDay: TimeSpan.FromHours(2),
                overtimeMultiplier: 1.5m,
                leaveQuotas: quotas
            );
            await _repo.CreateAsync(policy);
            var fetched = await _repo.GetByYearAsync(2025);

            Assert.NotNull(fetched);
            Assert.Equal(2025, fetched!.Year);
            Assert.Equal(policy.WorkDayStart, fetched.WorkDayStart);
            Assert.Equal(policy.WorkDayEnd, fetched.WorkDayEnd);
            Assert.Equal(policy.PunchInTolerance, fetched.PunchInTolerance);
            Assert.Equal(policy.PunchOutTolerance, fetched.PunchOutTolerance);
            Assert.Equal(policy.MaxSingleBreak, fetched.MaxSingleBreak);
            Assert.Equal(policy.MaxTotalBreakPerDay, fetched.MaxTotalBreakPerDay);
            Assert.Equal(policy.OvertimeMultiplier, fetched.OvertimeMultiplier);
            Assert.Equal(quotas, fetched.LeaveQuotas);
        }

        [Fact]
        public async Task GetByYear_NonExistent_ReturnsNull() =>
            Assert.Null(await _repo.GetByYearAsync(2020));

        [Fact]
        public async Task GetAllAsync_ReturnsAllPolicies()
        {
            var quotas = GetQuotas();
            var p1 = new Policy(2025, new TimeOnly(8, 0), new TimeOnly(16, 0), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2), 1.5m, quotas);
            var p2 = new Policy(2024, new TimeOnly(9, 0), new TimeOnly(17, 0), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(20), TimeSpan.FromHours(1), 2.0m, quotas);
            await _repo.CreateAsync(p1);
            await _repo.CreateAsync(p2);

            var all = await _repo.GetAllAsync();
            Assert.Contains(all, p => p.Year == 2025);
            Assert.Contains(all, p => p.Year == 2024);
        }

        [Fact]
        public async Task GetAllAsync_Empty_ReturnsEmptyList() =>
            Assert.Empty(await _repo.GetAllAsync());

        [Fact]
        public async Task DeleteAsync_Existing_DeletesPolicy()
        {
            var quotas = GetQuotas();
            var p = new Policy(2025, new TimeOnly(8, 0), new TimeOnly(16, 0), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2), 1.5m, quotas);
            await _repo.CreateAsync(p);

            await _repo.DeleteAsync(2025);
            Assert.Empty(await _repo.GetAllAsync());
        }

        [Fact]
        public async Task DeleteAsync_NonExistent_ThrowsRepositoryException() =>
            await Assert.ThrowsAsync<RepositoryException>(() => _repo.DeleteAsync(2030));

        [Fact]
        public async Task UpdateAsync_Existing_UpdatesPolicy()
        {
            var quotas = GetQuotas();
            var p = new Policy(2025, new TimeOnly(8, 0), new TimeOnly(16, 0), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2), 1.5m, quotas);
            await _repo.CreateAsync(p);

            p.SetWorkingHours(new TimeOnly(9, 0), new TimeOnly(17, 0));
            await _repo.UpdateAsync(p);
            var fetched = await _repo.GetByYearAsync(2025);

            Assert.Equal(new TimeOnly(9, 0), fetched!.WorkDayStart);
            Assert.Equal(new TimeOnly(17, 0), fetched.WorkDayEnd);
        }

        [Fact]
        public async Task UpdateAsync_NonExistent_ThrowsRepositoryException() =>
            await Assert.ThrowsAsync<RepositoryException>(() =>
                _repo.UpdateAsync(new Policy(2030, new TimeOnly(8, 0), new TimeOnly(16, 0), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2), 1.5m, GetQuotas()))
            );

        [Fact]
        public async Task UpdateAsync_Upsert_CreatesWhenMissing()
        {
            var quotas = GetQuotas();
            var p = new Policy(2031, new TimeOnly(8, 0), new TimeOnly(16, 0), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2), 1.5m, quotas);
            await _repo.UpdateAsync(p, isUpsert: true);
            Assert.NotNull(await _repo.GetByYearAsync(2031));
        }
    }
}
