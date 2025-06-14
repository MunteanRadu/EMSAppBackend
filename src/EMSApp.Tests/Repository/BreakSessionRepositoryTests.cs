using System.Threading;
using System.Threading.Tasks;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using EMSApp.Domain.Entities;
using EMSApp.Infrastructure;
using EMSApp.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Mongo2Go;
using MongoDB.Driver;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Repository")]
    public class BreakSessionRepositoryTests : IAsyncLifetime
    {
        private readonly MongoDbRunner _dbRunner;
        private readonly IMongoDbContext _dbContext;
        private readonly BreakSessionRepository _repo;
        private const string DbName = "TestDb";

        public BreakSessionRepositoryTests()
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
            _repo = new BreakSessionRepository(_dbContext);
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
            await db.DropCollectionAsync("Breaks");
        }

        [Fact]
        public async Task CreateAndFetch_BreakSession_Works()
        {
            var start = new TimeOnly(12, 0);
            var session = new BreakSession("p1", start);

            await _repo.CreateAsync(session);
            var fetched = await _repo.GetByIdAsync(session.Id);

            Assert.NotNull(fetched);
            Assert.Equal(session.Id, fetched!.Id);
            Assert.Equal(session.PunchRecordId, fetched.PunchRecordId);
            Assert.Equal(session.StartTime, fetched.StartTime);
        }

        [Fact]
        public async Task GetById_NonExistent_ReturnsNull() =>
            Assert.Null(await _repo.GetByIdAsync("nope"));

        [Fact]
        public async Task ListByPunchRecordAsync_FiltersCorrectly()
        {
            var start = new TimeOnly(13, 0);
            var b1 = new BreakSession("p1", start);
            var b2 = new BreakSession("p2", start);
            await _repo.CreateAsync(b1);
            await _repo.CreateAsync(b2);

            var list = await _repo.ListByPunchRecordAsync("p1");
            Assert.Single(list);
            Assert.Equal(b1.Id, list[0].Id);
        }

        [Fact]
        public async Task ListByPunchRecordAsync_NonExistent_ReturnsEmptyList() =>
            Assert.Empty(await _repo.ListByPunchRecordAsync("none"));

        [Fact]
        public async Task DeleteAsync_Existing_DeletesBreakSession()
        {
            var start = new TimeOnly(14, 0);
            var b = new BreakSession("p1", start);
            await _repo.CreateAsync(b);
            Assert.Single(await _repo.ListByPunchRecordAsync("p1"));

            await _repo.DeleteAsync(b.Id);
            Assert.Empty(await _repo.ListByPunchRecordAsync("p1"));
        }

        [Fact]
        public async Task DeleteAsync_NonExistent_ThrowsRepositoryException() =>
            await Assert.ThrowsAsync<RepositoryException>(() => _repo.DeleteAsync("nope"));

        [Fact]
        public async Task UpdateAsync_Existing_UpdatesBreakSession()
        {
            var start = new TimeOnly(15, 0);
            var session = new BreakSession("p1", start);
            await _repo.CreateAsync(session);
            var end = start.AddMinutes(30);
            session.End(end);

            await _repo.UpdateAsync(session);
            var fetched = await _repo.GetByIdAsync(session.Id);

            Assert.NotNull(fetched);
            Assert.Equal(end, fetched!.EndTime);
            Assert.Equal(session.Duration, fetched.Duration);
        }

        [Fact]
        public async Task UpdateAsync_NonExistent_ThrowsRepositoryException() =>
            await Assert.ThrowsAsync<RepositoryException>(() => _repo.UpdateAsync(
                new BreakSession("p1", new TimeOnly(16, 0))
            ));

        [Fact]
        public async Task UpdateAsync_Upsert_CreatesWhenMissing()
        {
            var start = new TimeOnly(17, 0);
            var session = new BreakSession("p1", start);
            Assert.Empty(await _repo.ListByPunchRecordAsync("p1"));

            await _repo.UpdateAsync(session, isUpsert: true);
            Assert.Single(await _repo.ListByPunchRecordAsync("p1"));
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllBreakSessions()
        {
            var start = new TimeOnly(18, 0);
            var b1 = new BreakSession("p1", start);
            var b2 = new BreakSession("p2", start);
            await _repo.CreateAsync(b1);
            await _repo.CreateAsync(b2);

            var all = await _repo.GetAllAsync(CancellationToken.None);
            Assert.Contains(all, x => x.Id == b1.Id);
            Assert.Contains(all, x => x.Id == b2.Id);
        }
    }
}
