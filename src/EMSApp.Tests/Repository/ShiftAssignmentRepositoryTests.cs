using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EMSApp.Domain;
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
    public class ShiftAssignmentRepositoryTests : IAsyncLifetime
    {
        private const string DbName = "TestDb";
        private readonly MongoDbRunner _runner;
        private readonly IMongoDbContext _context;
        private readonly ShiftAssignmentRepository _repo;

        public ShiftAssignmentRepositoryTests()
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
            _repo = new ShiftAssignmentRepository(_context);
        }

        public async Task InitializeAsync()
        {
            var client = new MongoClient(_runner.ConnectionString);
            await client.GetDatabase(DbName).DropCollectionAsync("ShiftAssignments");
        }

        public Task DisposeAsync()
        {
            _runner.Dispose();
            return Task.CompletedTask;
        }

        [Fact]
        public async Task AddAsync_And_GetForUserOnDateAsync_Works()
        {
            // Arrange
            var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var start = new TimeOnly(8, 0);
            var end = new TimeOnly(16, 0);
            var assignment = new ShiftAssignment(
                userId: "user1",
                date: date,
                shift: ShiftType.Shift1,
                startTime: start,
                endTime: end,
                departmentId: "dept1",
                managerId: "mgr1"
            );

            // Act
            await _repo.AddAsync(assignment, CancellationToken.None);
            var fetched = await _repo.GetForUserOnDateAsync("user1", date, CancellationToken.None);

            // Assert
            Assert.NotNull(fetched);
            Assert.Equal(assignment.Id, fetched!.Id);
            Assert.Equal(ShiftType.Shift1, fetched.Shift);
        }

        [Fact]
        public async Task GetForUserOnDateAsync_NonExistent_ReturnsNull() =>
            Assert.Null(await _repo.GetForUserOnDateAsync("noUser", DateOnly.FromDateTime(DateTime.UtcNow), CancellationToken.None));

        [Fact]
        public async Task AddManyAsync_And_GetAllAsync_Works()
        {
            // Arrange
            var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var a1 = new ShiftAssignment("u1", date, ShiftType.Shift1, new TimeOnly(8, 0), new TimeOnly(16, 0), "deptA", "mgrA");
            var a2 = new ShiftAssignment("u2", date, ShiftType.NightShift, new TimeOnly(16, 0), new TimeOnly(23, 0), "deptA", "mgrA");

            // Act
            await _repo.AddManyAsync(new[] { a1, a2 }, CancellationToken.None);
            var all = (await _repo.GetAllAsync(CancellationToken.None)).ToList();

            // Assert
            Assert.Contains(all, x => x.Id == a1.Id);
            Assert.Contains(all, x => x.Id == a2.Id);
        }

        [Fact]
        public async Task GetByDepartmentAndWeekAsync_FiltersCorrectly()
        {
            // Arrange week
            var weekStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var inWeek = weekStart.AddDays(2);
            var outWeek = weekStart.AddDays(8);
            var a1 = new ShiftAssignment("u1", inWeek, ShiftType.Shift1, new TimeOnly(8, 0), new TimeOnly(12, 0), "deptX", "mgrX");
            var a2 = new ShiftAssignment("u2", outWeek, ShiftType.Shift1, new TimeOnly(8, 0), new TimeOnly(12, 0), "deptX", "mgrX");
            var a3 = new ShiftAssignment("u3", inWeek, ShiftType.Shift1, new TimeOnly(8, 0), new TimeOnly(12, 0), "deptY", "mgrY");
            await _repo.AddManyAsync(new[] { a1, a2, a3 }, CancellationToken.None);

            // Act
            var list = (await _repo.GetByDepartmentAndWeekAsync("deptX", weekStart, CancellationToken.None)).ToList();

            // Assert
            Assert.Single(list);
            Assert.Equal(a1.Id, list[0].Id);
        }

        [Fact]
        public async Task GetByUserAndWeekAsync_FiltersCorrectly()
        {
            // Arrange week
            var weekStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var inWeek = weekStart.AddDays(3);
            var outWeek = weekStart.AddDays(10);
            var a1 = new ShiftAssignment("userX", inWeek, ShiftType.Shift2, new TimeOnly(7, 0), new TimeOnly(15, 0), "deptZ", "mgrZ");
            var a2 = new ShiftAssignment("userX", outWeek, ShiftType.Shift2, new TimeOnly(7, 0), new TimeOnly(15, 0), "deptZ", "mgrZ");
            await _repo.AddManyAsync(new[] { a1, a2 }, CancellationToken.None);

            // Act
            var list = (await _repo.GetByUserAndWeekAsync("userX", weekStart, CancellationToken.None)).ToList();

            // Assert
            Assert.Single(list);
            Assert.Equal(a1.Id, list[0].Id);
        }

        [Fact]
        public async Task DeleteByDepartmentAndWeekAsync_RemovesOnlyMatching()
        {
            // Arrange week
            var weekStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var inWeek = weekStart.AddDays(1);
            var outWeek = weekStart.AddDays(8);
            var a1 = new ShiftAssignment("u1", inWeek, ShiftType.Shift1, new TimeOnly(6, 0), new TimeOnly(14, 0), "deptDel", "mgrD");
            var a2 = new ShiftAssignment("u2", outWeek, ShiftType.Shift1, new TimeOnly(6, 0), new TimeOnly(14, 0), "deptDel", "mgrD");
            await _repo.AddManyAsync(new[] { a1, a2 }, CancellationToken.None);

            // Act
            await _repo.DeleteByDepartmentAndWeekAsync("deptDel", weekStart, CancellationToken.None);
            var remaining = (await _repo.GetAllAsync(CancellationToken.None)).ToList();

            // Assert
            Assert.Single(remaining);
            Assert.Equal(a2.Id, remaining[0].Id);
        }
    }
}
