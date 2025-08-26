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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EMSApp.Tests
{
    [Trait("Category", "Repository")]
    public class LeaveRequestRepositoryTests : IAsyncLifetime
    {
        private readonly MongoDbRunner _dbRunner;
        private readonly IMongoDbContext _dbContext;
        private readonly LeaveRequestRepository _repo;
        private const string DbName = "TestDb";

        public LeaveRequestRepositoryTests()
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
            var db = client.GetDatabase(DbName);
            await db.DropCollectionAsync("LeaveRequests");
        }

        [Fact]
        public async Task CreateAndFetch_ById_Works()
        {
            var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var req = new LeaveRequest("u1", LeaveType.Paid, start, start.AddDays(3), "reason");

            await _repo.CreateAsync(req);
            var fetched = await _repo.GetByIdAsync(req.Id);

            Assert.NotNull(fetched);
            Assert.Equal(req.Id, fetched!.Id);
            Assert.Equal(req.UserId, fetched.UserId);
            Assert.Equal(req.Type, fetched.Type);
            Assert.Equal(req.StartDate, fetched.StartDate);
            Assert.Equal(req.EndDate, fetched.EndDate);
            Assert.Equal(req.Reason, fetched.Reason);
        }

        [Fact]
        public async Task GetById_NonExistent_ReturnsNull() =>
            Assert.Null(await _repo.GetByIdAsync("nope"));

        [Fact]
        public async Task GetByManagerAsync_FiltersCorrectly()
        {
            var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var r1 = new LeaveRequest("u1", LeaveType.Sick, start, start.AddDays(2), "r1");
            r1.Approve("mgr1");
            var r2 = new LeaveRequest("u2", LeaveType.Sick, start, start.AddDays(2), "r2");
            r2.Approve("mgr2");

            await _repo.CreateAsync(r1);
            await _repo.CreateAsync(r2);

            var list = await _repo.GetByManagerAsync("mgr1");
            Assert.Single(list);
            Assert.Equal(r1.Id, list[0].Id);
        }

        [Fact]
        public async Task GetByManagerAsync_NonExistent_ReturnsEmptyList() =>
            Assert.Empty(await _repo.GetByManagerAsync("x"));

        [Fact]
        public async Task GetByStatusAsync_FiltersCorrectly()
        {
            var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var r1 = new LeaveRequest("u1", LeaveType.Paid, start, start.AddDays(1), "r1"); // Pending
            var r2 = new LeaveRequest("u2", LeaveType.Paid, start, start.AddDays(1), "r2");
            r2.Approve("mgr");

            await _repo.CreateAsync(r1);
            await _repo.CreateAsync(r2);

            var pending = await _repo.GetByStatusAsync(LeaveStatus.Pending);
            var approved = await _repo.GetByStatusAsync(LeaveStatus.Approved);

            Assert.Single(pending);
            Assert.Equal(r1.Id, pending[0].Id);
            Assert.Single(approved);
            Assert.Equal(r2.Id, approved[0].Id);
        }

        [Fact]
        public async Task GetByUserAsync_FiltersCorrectly()
        {
            var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var r1 = new LeaveRequest("u1", LeaveType.Paid, start, start.AddDays(1), "r1");
            var r2 = new LeaveRequest("u2", LeaveType.Paid, start, start.AddDays(1), "r2");

            await _repo.CreateAsync(r1);
            await _repo.CreateAsync(r2);

            var list = await _repo.GetByUserAsync("u1");
            Assert.Single(list);
            Assert.Equal(r1.Id, list[0].Id);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllRequests()
        {
            var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var r1 = new LeaveRequest("u1", LeaveType.Paid, start, start.AddDays(1), "r1");
            var r2 = new LeaveRequest("u2", LeaveType.Paid, start, start.AddDays(1), "r2");

            await _repo.CreateAsync(r1);
            await _repo.CreateAsync(r2);

            var all = await _repo.GetAllAsync(CancellationToken.None);
            Assert.Contains(all, x => x.Id == r1.Id);
            Assert.Contains(all, x => x.Id == r2.Id);
        }

        [Fact]
        public async Task GetApprovedLeavesForWeekAsync_FiltersCorrectly()
        {
            var date = DateTime.UtcNow;
            int daysToAdd = ((int)DayOfWeek.Monday - (int)date.DayOfWeek + 7) % 7;
            if (daysToAdd == 0)
                daysToAdd = 7;
            var monday = date.AddDays(daysToAdd);
            var weekStart = new DateOnly(monday.Year, monday.Month, monday.Day);
            var r1 = new LeaveRequest("u1", LeaveType.Paid, weekStart.AddDays(1), weekStart.AddDays(2), "r1");
            r1.Approve("mgr");
            var r2 = new LeaveRequest("u1", LeaveType.Paid, weekStart.AddDays(-1), weekStart.AddDays(-1), "r2");
            r2.Approve("mgr");

            await _repo.CreateAsync(r1);
            await _repo.CreateAsync(r2);

            var list = await _repo.GetApprovedLeavesForWeekAsync(new[] { "u1" }, weekStart, CancellationToken.None);
            Assert.Single(list);
            Assert.Equal(r1.Id, list.First().Id);
        }

        [Fact]
        public async Task FilterByAsync_Works()
        {
            var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var r1 = new LeaveRequest("u1", LeaveType.Paid, start, start.AddDays(1), "r1");
            var r2 = new LeaveRequest("u2", LeaveType.Paid, start, start.AddDays(1), "r2");

            await _repo.CreateAsync(r1);
            await _repo.CreateAsync(r2);

            var filtered = await _repo.FilterByAsync(r => r.UserId == "u2");
            Assert.Single(filtered);
            Assert.Equal(r2.Id, filtered.First().Id);
        }

        [Fact]
        public async Task DeleteAsync_Existing_DeletesRequest()
        {
            var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var r = new LeaveRequest("u1", LeaveType.Paid, start, start.AddDays(1), "r");
            await _repo.CreateAsync(r);

            await _repo.DeleteAsync(r.Id);
            Assert.Null(await _repo.GetByIdAsync(r.Id));
        }

        [Fact]
        public async Task DeleteAsync_NonExistent_ThrowsRepositoryException() =>
            await Assert.ThrowsAsync<RepositoryException>(() => _repo.DeleteAsync("nope"));

        [Fact]
        public async Task UpdateAsync_Existing_UpdatesRequest()
        {
            var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var r = new LeaveRequest("u1", LeaveType.Paid, start, start.AddDays(1), "r");
            await _repo.CreateAsync(r);

            r.Approve("mgr");
            await _repo.UpdateAsync(r);
            var fetched = await _repo.GetByIdAsync(r.Id);
            Assert.Equal(LeaveStatus.Approved, fetched!.Status);
        }

        [Fact]
        public async Task UpdateAsync_NonExistent_ThrowsRepositoryException() =>
            await Assert.ThrowsAsync<RepositoryException>(() => _repo.UpdateAsync(
                new LeaveRequest("uX", LeaveType.Paid, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2), "r")
            ));

        [Fact]
        public async Task UpdateAsync_Upsert_CreatesWhenMissing()
        {
            var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var r = new LeaveRequest("u1", LeaveType.Paid, start, start.AddDays(1), "r");
            await _repo.UpdateAsync(r, isUpsert: true);
            Assert.NotNull(await _repo.GetByIdAsync(r.Id));
        }
    }
}
