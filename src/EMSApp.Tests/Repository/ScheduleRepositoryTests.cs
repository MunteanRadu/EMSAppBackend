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
    public class ScheduleRepositoryTests : IAsyncLifetime
    {
        private readonly MongoDbRunner _dbRunner;
        private readonly IMongoDbContext _dbContext;
        private readonly ScheduleRepository _repo;
        private const string DbName = "TestDb";

        public ScheduleRepositoryTests()
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
            var database = client.GetDatabase(DbName);
            await database.DropCollectionAsync("Schedules");
        }

        [Fact]
        public async Task CreateAndFetch_Schedule_Works()
        {
            var startTime = TimeOnly.Parse("08:00");
            var endTime = startTime.AddHours(8);
            var s = new Schedule("dept-1", "mgr-1", ShiftType.Shift1, DayOfWeek.Monday, startTime, endTime, true);

            await _repo.CreateAsync(s);
            var byId = await _repo.GetByIdAsync(s.Id);

            Assert.NotNull(byId);
            Assert.Equal(s.Id, byId!.Id);
            Assert.Equal(s.DepartmentId, byId.DepartmentId);
            Assert.Equal(s.ManagerId, byId.ManagerId);
            Assert.Equal(s.ShiftType, byId.ShiftType);
            Assert.Equal(s.Day, byId.Day);
            Assert.Equal(s.StartTime, byId.StartTime);
            Assert.Equal(s.EndTime, byId.EndTime);
            Assert.Equal(s.IsWorkingDay, byId.IsWorkingDay);
        }

        [Fact]
        public async Task GetById_NonExistent_ReturnsNull()
        {
            var got = await _repo.GetByIdAsync("nope");
            Assert.Null(got);
        }

        [Fact]
        public async Task GetByDepartmentAsync_FiltersCorrectly()
        {
            var startTime = TimeOnly.Parse("08:00");
            var endTime = startTime.AddHours(8);
            var s1 = new Schedule("dept-1", "mgr-1", ShiftType.Shift1, DayOfWeek.Monday, startTime, endTime, true);
            var s2 = new Schedule("dept-2", "mgr-2", ShiftType.Shift2, DayOfWeek.Tuesday, startTime, endTime, true);
            await _repo.CreateAsync(s1);
            await _repo.CreateAsync(s2);

            var list = await _repo.GetByDepartmentAsync("dept-1");

            Assert.Single(list);
            Assert.Equal(s1.Id, list[0].Id);
        }

        [Fact]
        public async Task GetByDepartmentAsync_NonExistent_ReturnsEmptyList()
        {
            var list = await _repo.GetByDepartmentAsync("dept-x");
            Assert.Empty(list);
        }

        [Fact]
        public async Task GetByManagerIdAsync_FiltersCorrectly()
        {
            var startTime = TimeOnly.Parse("08:00");
            var endTime = startTime.AddHours(8);
            var s1 = new Schedule("d1", "mgr-1", ShiftType.Shift1, DayOfWeek.Wednesday, startTime, endTime, true);
            var s2 = new Schedule("d2", "mgr-2", ShiftType.Shift2, DayOfWeek.Thursday, startTime, endTime, true);
            await _repo.CreateAsync(s1);
            await _repo.CreateAsync(s2);

            var list = await _repo.GetByManagerIdAsync("mgr-2");

            Assert.Single(list);
            Assert.Equal(s2.Id, list[0].Id);
        }

        [Fact]
        public async Task GetByManagerIdAsync_NonExistent_ReturnsEmptyList()
        {
            var list = await _repo.GetByManagerIdAsync("mgr-x");
            Assert.Empty(list);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllSchedules()
        {
            var startTime = TimeOnly.Parse("08:00");
            var endTime = startTime.AddHours(8);
            var s1 = new Schedule("d1", "m1", ShiftType.Shift1, DayOfWeek.Monday, startTime, endTime, true);
            var s2 = new Schedule("d2", "m2", ShiftType.Shift2, DayOfWeek.Tuesday, startTime, endTime, true);
            await _repo.CreateAsync(s1);
            await _repo.CreateAsync(s2);

            var all = await _repo.GetAllAsync();

            Assert.Contains(all, x => x.Id == s1.Id);
            Assert.Contains(all, x => x.Id == s2.Id);
        }

        [Fact]
        public async Task DeleteAsync_Existing_DeletesSchedule()
        {
            var s = new Schedule("d", "m", ShiftType.Shift1, DayOfWeek.Friday, TimeOnly.Parse("09:00"), TimeOnly.Parse("17:00"), true);
            await _repo.CreateAsync(s);
            Assert.NotNull(await _repo.GetByIdAsync(s.Id));

            await _repo.DeleteAsync(s.Id);
            Assert.Null(await _repo.GetByIdAsync(s.Id));
        }

        [Fact]
        public async Task DeleteAsync_NonExistent_ThrowsRepositoryException()
        {
            await Assert.ThrowsAsync<RepositoryException>(() =>
                _repo.DeleteAsync("nope")
            );
        }

        [Fact]
        public async Task UpdateAsync_Existing_UpdatesSchedule()
        {
            var s = new Schedule("d", "m", ShiftType.Shift1, DayOfWeek.Monday, TimeOnly.Parse("08:00"), TimeOnly.Parse("16:00"), true);
            await _repo.CreateAsync(s);

            var newStart = TimeOnly.Parse("10:00");
            var newEnd = newStart.AddHours(6);
            s.UpdateShift(ShiftType.NightShift, newStart, newEnd, false);

            await _repo.UpdateAsync(s);
            var fetched = await _repo.GetByIdAsync(s.Id);

            Assert.Equal(newStart, fetched!.StartTime);
            Assert.Equal(newEnd, fetched.EndTime);
            Assert.Equal(ShiftType.NightShift, fetched.ShiftType);
            Assert.False(fetched.IsWorkingDay);
        }

        [Fact]
        public async Task UpdateAsync_NonExistent_ThrowsRepositoryException()
        {
            var s = new Schedule("d", "m", ShiftType.Shift1, DayOfWeek.Tuesday, TimeOnly.Parse("07:00"), TimeOnly.Parse("15:00"), true);
            await Assert.ThrowsAsync<RepositoryException>(() =>
                _repo.UpdateAsync(s)
            );
        }

        [Fact]
        public async Task UpdateAsync_Upsert_CreatesWhenMissing()
        {
            var s = new Schedule("d", "m", ShiftType.Shift1, DayOfWeek.Wednesday, TimeOnly.Parse("06:00"), TimeOnly.Parse("14:00"), true);
            await _repo.UpdateAsync(s, isUpsert: true);
            Assert.NotNull(await _repo.GetByIdAsync(s.Id));
        }
    }
}
