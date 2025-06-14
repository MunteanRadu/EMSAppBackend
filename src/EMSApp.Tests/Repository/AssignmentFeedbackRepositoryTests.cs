
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using EMSApp.Infrastructure;
using EMSApp.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Mongo2Go;
using MongoDB.Driver;
using System.Xml.Linq;

namespace EMSApp.Tests;

[Trait("Category", "Repository")]
public class AssignmentFeedbackRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbRunner _dbRunner;
    private readonly IMongoDbContext _dbContext;
    private readonly AssignmentFeedbackRepository _repo;
    private static string DbName = "TestDb";

    public AssignmentFeedbackRepositoryTests()
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
        _repo = new AssignmentFeedbackRepository(_dbContext);
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
        await database.DropCollectionAsync("AssignmentFeedbacks");
    }

    [Fact]
    public async Task CreateAndFetch_AssignmentFeedback_Works()
    {
        // Arrange
        var af = new AssignmentFeedback("assignment-1", "user-1", "text", FeedbackType.Manager);

        // Act
        await _repo.CreateAsync(af);
        var byId = await _repo.GetByIdAsync(af.Id);

        // Assert
        Assert.NotNull(byId);
        Assert.Equal(af.Id, byId.Id);
        Assert.Equal(af.AssignmentId, byId.AssignmentId);
        Assert.Equal(af.UserId, byId.UserId);
        Assert.Equal(af.Text, byId.Text);
        Assert.Equal(af.Type, byId.Type);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNull()
    {
        // Arrange & Act
        var byId = await _repo.GetByIdAsync("feedback-1");

        // Act & Assert
        Assert.Null(byId);
    }

    [Fact]
    public async Task ListByAssignment_ReturnsAssignmentFeedbacks()
    {
        // Arrange
        var af1 = new AssignmentFeedback("assignment-1", "user-1", "text", FeedbackType.Manager);
        var af2 = new AssignmentFeedback("assignment-2", "user-2", "text2", FeedbackType.Employee);
        await _repo.CreateAsync(af1);
        await _repo.CreateAsync(af2);

        // Act
        var list = await _repo.ListByAssignmentAsync("assignment-1");

        // Assert
        Assert.NotEmpty(list);
        Assert.Contains(list, af => af.Id == af1.Id);
        Assert.DoesNotContain(list, af => af.Id == af2.Id);
    }

    [Fact]
    public async Task ListByAssignment_NonExistent_ReturnsEmptyList()
    {
        // Arrange & Act
        var list = await _repo.ListByAssignmentAsync("assignment-1");

        // Assert
        Assert.Empty(list);
    }

    [Fact]
    public async Task DeleteAssignmentFeedback_Exists_DeletesAssignmentFeedback()
    {
        // Arrange
        var af = new AssignmentFeedback("assignment-1", "user-1", "text", FeedbackType.Manager);
        await _repo.CreateAsync(af);
        Assert.NotEmpty(await _repo.ListByAssignmentAsync("assignment-1"));

        // Act
        await _repo.DeleteAsync(af.Id);
        var list = await _repo.ListByAssignmentAsync("assignment-1");

        // Assert
        Assert.Empty(list);
    }

    [Fact]
    public async Task DeleteAssignmentFeedback_NonExistent_ThrowsRepositoryException()
    {
        // Arrange
        var af = new AssignmentFeedback("assignment-1", "user-1", "text", FeedbackType.Manager);

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _repo.DeleteAsync(af.Id)
        );
    }

    [Fact]
    public async Task UpdateAssignmentFeedback_Exists_UpdatesAssignmentFeedback()
    {
        // Arrange
        var af = new AssignmentFeedback("assignment-1", "user-1", "text", FeedbackType.Manager);
        await _repo.CreateAsync(af);
        var byId = await _repo.GetByIdAsync(af.Id);
        Assert.Equal(af.Text, byId?.Text);

        af.Edit("new text");

        // Act
        await _repo.UpdateAsync(af);
        byId = await _repo.GetByIdAsync(af.Id);

        // Assert
        Assert.NotNull(byId);
        Assert.Equal(af.Id, byId.Id);
        Assert.Equal(af.Text, byId.Text);
    }

    [Fact]
    public async Task UpdateAssignmentFeedback_NonExistent_ThrowsRepositoryException()
    {
        // Arrange
        var af = new AssignmentFeedback("assignment-1", "user-1", "text", FeedbackType.Manager);

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _repo.UpdateAsync(af)
        );
    }

    [Fact]
    public async Task UpsertAssignmentFeedback_NonExistent_CreatesAssignmentFeedback()
    {
        // Arrange
        var af = new AssignmentFeedback("assignment-1", "user-1", "text", FeedbackType.Manager);
        Assert.Null(await _repo.GetByIdAsync(af.Id));

        // Act
        await _repo.UpdateAsync(af, true);
        var byId = await _repo.GetByIdAsync(af.Id);

        // Assert
        Assert.NotNull(byId);
        Assert.Equal(af.Id, byId.Id);

    }
}
