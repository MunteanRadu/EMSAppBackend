using System;
using System.Linq;
using System.Linq.Expressions;
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
    public class AssignmentRepositoryTests : IAsyncLifetime
    {
        private readonly MongoDbRunner _dbRunner;
        private readonly IMongoDbContext _dbContext;
        private readonly AssignmentRepository _repo;
        private const string DbName = "TestDb";

        public AssignmentRepositoryTests()
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
            _repo = new AssignmentRepository(_dbContext);
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
            await db.DropCollectionAsync("Assignments");
        }

        [Fact]
        public async Task CreateAndFetch_ById_Works()
        {
            var due = DateTime.UtcNow.AddDays(5);
            var a = new Assignment("title", "description", due, "dept-1", "mgr-1");

            await _repo.CreateAsync(a);
            var fetched = await _repo.GetByIdAsync(a.Id);
            Assert.NotNull(fetched);

            var diff = (a.DueDate - fetched!.DueDate).Duration();
            Assert.True(diff < TimeSpan.FromMilliseconds(10), $"DueDate differs by {diff}");
        }

        [Fact]
        public async Task GetById_NonExistent_ReturnsNull() =>
            Assert.Null(await _repo.GetByIdAsync("no-id"));

        [Fact]
        public async Task ListByAssignee_FiltersCorrectly()
        {
            var d1 = DateTime.UtcNow.AddDays(3);
            var d2 = DateTime.UtcNow.AddDays(4);
            var a1 = new Assignment("T1", "D1", d1, "dept", "M");
            var a2 = new Assignment("T2", "D2", d2, "dept", "M");
            a1.Start("user-1");
            a2.Start("user-2");

            await _repo.CreateAsync(a1);
            await _repo.CreateAsync(a2);

            var list = await _repo.ListByAssigneeAsync("user-1");
            Assert.Single(list);
            Assert.Equal(a1.Id, list[0].Id);
        }

        [Fact]
        public async Task ListByStatus_FiltersCorrectly()
        {
            var due = DateTime.UtcNow.AddDays(2);
            var a1 = new Assignment("T1", "D1", due, "d", "m");
            var a2 = new Assignment("T2", "D2", due, "d", "m");
            a1.Start("u");
            a1.Complete();
            a2.Start("u");

            await _repo.CreateAsync(a1);
            await _repo.CreateAsync(a2);

            var done = await _repo.ListByStatusAsync(AssignmentStatus.Done);
            var inProg = await _repo.ListByStatusAsync(AssignmentStatus.InProgress);

            Assert.Single(done);
            Assert.Equal(a1.Id, done[0].Id);
            Assert.Single(inProg);
            Assert.Equal(a2.Id, inProg[0].Id);
        }

        [Fact]
        public async Task ListOverdue_FiltersCorrectly()
        {
            var now = DateTime.UtcNow;
            var dueNotOverdue = now.AddDays(10);
            var dueOverdue = now.AddDays(5);
            var asOf = now.AddDays(8);

            var a1 = new Assignment("t1", "d1", dueNotOverdue, "dept", "mgr");
            a1.Start("user-1");

            var a2 = new Assignment("t2", "d2", dueOverdue, "dept", "mgr");
            a2.Start("user-2");

            await _repo.CreateAsync(a1);
            await _repo.CreateAsync(a2);

            var list = await _repo.ListOverdueAsync(asOf);

            Assert.DoesNotContain(list, x => x.Id == a1.Id);
            Assert.Contains(list, x => x.Id == a2.Id);
        }

        [Fact]
        public async Task ListAsync_WithPredicate_Works()
        {
            var due = DateTime.UtcNow.AddDays(3);
            var a1 = new Assignment("ABC", "D", due, "d", "m");
            var a2 = new Assignment("XYZ", "D", due, "d", "m");
            await _repo.CreateAsync(a1);
            await _repo.CreateAsync(a2);

            Expression<Func<Assignment, bool>> pred = x => x.Title.Contains("A");
            var list = await _repo.ListAsync(pred);
            Assert.Single(list);
            Assert.Equal(a1.Id, list[0].Id);
        }

        [Fact]
        public async Task DeleteAsync_Existing_Deletes()
        {
            var a = new Assignment("T", "D", DateTime.UtcNow.AddDays(2), "d", "m");
            await _repo.CreateAsync(a);
            Assert.NotNull(await _repo.GetByIdAsync(a.Id));

            await _repo.DeleteAsync(a.Id);
            Assert.Null(await _repo.GetByIdAsync(a.Id));
        }

        [Fact]
        public async Task DeleteAsync_NonExistent_Throws() =>
            await Assert.ThrowsAsync<RepositoryException>(() => _repo.DeleteAsync("no-id"));

        [Fact]
        public async Task UpdateAsync_Existing_Updates()
        {
            var a = new Assignment("T", "D", DateTime.UtcNow.AddDays(2), "d", "m");
            await _repo.CreateAsync(a);
            a.Start("u");

            await _repo.UpdateAsync(a);
            var fetched = await _repo.GetByIdAsync(a.Id);
            Assert.Equal(AssignmentStatus.InProgress, fetched!.Status);
        }

        [Fact]
        public async Task UpdateAsync_NonExistent_Throws() =>
            await Assert.ThrowsAsync<RepositoryException>(() => _repo.UpdateAsync(
                new Assignment("T", "D", DateTime.UtcNow.AddDays(2), "d", "m")));

        [Fact]
        public async Task UpdateAsync_Upsert_CreatesWhenMissing()
        {
            var a = new Assignment("T", "D", DateTime.UtcNow.AddDays(2), "d", "m");
            await _repo.UpdateAsync(a, isUpsert: true);
            Assert.NotNull(await _repo.GetByIdAsync(a.Id));
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAll()
        {
            var a1 = new Assignment("T1", "D", DateTime.UtcNow.AddDays(2), "d", "m");
            var a2 = new Assignment("T2", "D", DateTime.UtcNow.AddDays(3), "d", "m");
            await _repo.CreateAsync(a1);
            await _repo.CreateAsync(a2);

            var all = await _repo.GetAllAsync(CancellationToken.None);
            Assert.Contains(all, x => x.Id == a1.Id);
            Assert.Contains(all, x => x.Id == a2.Id);
        }

        [Fact]
        public async Task FilterByAsync_Works()
        {
            var a1 = new Assignment("AAA", "D", DateTime.UtcNow.AddDays(2), "d", "m");
            var a2 = new Assignment("BBB", "D", DateTime.UtcNow.AddDays(2), "d", "m");
            await _repo.CreateAsync(a1);
            await _repo.CreateAsync(a2);

            var filtered = await _repo.FilterByAsync(x => x.Title.StartsWith("B"));
            Assert.Single(filtered);
            Assert.Equal(a2.Id, filtered.First().Id);
        }
    }
}
