using System;
using System.Linq.Expressions;
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
    public class PunchRecordRepositoryTests : IAsyncLifetime
    {
        private readonly MongoDbRunner _dbRunner;
        private readonly IMongoDbContext _dbContext;
        private readonly PunchRecordRepository _repo;
        private const string DbName = "TestDb";

        public PunchRecordRepositoryTests()
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
            _repo = new PunchRecordRepository(_dbContext);
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
            await db.DropCollectionAsync("PunchRecords");
        }

        [Fact]
        public async Task CreateAndFetch_ById_Works()
        {
            var record = new PunchRecord("u1", DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.Parse("08:00"));
            await _repo.CreateAsync(record);

            var fetched = await _repo.GetByIdAsync(record.Id);
            Assert.NotNull(fetched);
            Assert.Equal(record.Id, fetched!.Id);
            Assert.Equal(record.UserId, fetched.UserId);
        }

        [Fact]
        public async Task GetById_NonExistent_ReturnsNull() =>
            Assert.Null(await _repo.GetByIdAsync("nope"));

        [Fact]
        public async Task ListByUser_FiltersCorrectly()
        {
            var p1 = new PunchRecord("u1", DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.Parse("08:00"));
            var p2 = new PunchRecord("u2", p1.Date, TimeOnly.Parse("09:00"));
            await _repo.CreateAsync(p1);
            await _repo.CreateAsync(p2);

            var list = await _repo.ListByUserAsync("u1");
            Assert.Single(list);
            Assert.Equal(p1.Id, list[0].Id);
        }

        [Fact]
        public async Task ListByUser_NonExistent_ReturnsEmptyList() =>
            Assert.Empty(await _repo.ListByUserAsync("uX"));

        [Fact]
        public async Task GetAllAsync_ReturnsAllRecords()
        {
            var p1 = new PunchRecord("u1", DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.Parse("08:00"));
            var p2 = new PunchRecord("u2", p1.Date, TimeOnly.Parse("09:00"));
            await _repo.CreateAsync(p1);
            await _repo.CreateAsync(p2);

            var all = await _repo.GetAllAsync(CancellationToken.None);
            Assert.Contains(all, p => p.Id == p1.Id);
            Assert.Contains(all, p => p.Id == p2.Id);
        }

        [Fact]
        public async Task FilterByAsync_WithPredicate_Works()
        {
            // Arrange
            var p1 = new PunchRecord("u1", new DateOnly(2025, 4, 1), TimeOnly.Parse("08:00"));
            var p2 = new PunchRecord("u2", new DateOnly(2025, 4, 2), TimeOnly.Parse("09:00"));
            await _repo.CreateAsync(p1);
            await _repo.CreateAsync(p2);

            // Act
            var filteredByUser = await _repo.FilterByAsync(p => p.UserId == "u2");
            var filteredByDate = await _repo.FilterByAsync(p => p.Date == new DateOnly(2025, 4, 1));

            // Assert
            Assert.Single(filteredByUser);
            Assert.Equal(p2.Id, filteredByUser[0].Id);
            Assert.Single(filteredByDate);
            Assert.Equal(p1.Id, filteredByDate[0].Id);
        }

        [Fact]
        public async Task ListByUserAndMonthAsync_FiltersCorrectly()
        {
            var dateInApr = new DateOnly(2025, 4, 15);
            var dateInMay = new DateOnly(2025, 5, 1);
            var p1 = new PunchRecord("u1", dateInApr, TimeOnly.Parse("08:00"));
            var p2 = new PunchRecord("u1", dateInMay, TimeOnly.Parse("08:00"));
            await _repo.CreateAsync(p1);
            await _repo.CreateAsync(p2);

            var listApr = await _repo.ListByUserAndMonthAsync("u1", 2025, 4);
            Assert.Single(listApr);
            Assert.Equal(p1.Id, listApr[0].Id);
        }

        [Fact]
        public async Task DeleteAsync_Existing_DeletesRecord()
        {
            var p = new PunchRecord("u1", DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.Parse("08:00"));
            await _repo.CreateAsync(p);
            Assert.NotNull(await _repo.GetByIdAsync(p.Id));

            await _repo.DeleteAsync(p.Id);
            Assert.Null(await _repo.GetByIdAsync(p.Id));
        }

        [Fact]
        public async Task DeleteAsync_NonExistent_ThrowsRepositoryException() =>
            await Assert.ThrowsAsync<RepositoryException>(() => _repo.DeleteAsync("nope"));

        [Fact]
        public async Task UpdateAsync_Existing_UpdatesRecord()
        {
            var p = new PunchRecord("u1", DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.Parse("08:00"));
            await _repo.CreateAsync(p);
            p.PunchOut(TimeOnly.Parse("16:00"));

            await _repo.UpdateAsync(p);
            var fetched = await _repo.GetByIdAsync(p.Id);
            Assert.Equal(p.TimeOut, fetched!.TimeOut);
        }

        [Fact]
        public async Task UpdateAsync_NonExistent_ThrowsRepositoryException() =>
            await Assert.ThrowsAsync<RepositoryException>(() => _repo.UpdateAsync(
                new PunchRecord("u1", DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.Parse("08:00"))
            ));

        [Fact]
        public async Task UpdateAsync_Upsert_CreatesWhenMissing()
        {
            var p = new PunchRecord("u1", DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.Parse("08:00"));
            await _repo.UpdateAsync(p, isUpsert: true);
            Assert.NotNull(await _repo.GetByIdAsync(p.Id));
        }
    }
}
