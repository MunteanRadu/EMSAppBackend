using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Infrastructure;
using Moq;

namespace EMSApp.Tests;

[Trait("Category", "Service")]
public class AssignmentFeedbackServiceTests
{
    private readonly Mock<IAssignmentFeedbackRepository> _repo;
    private readonly IAssignmentFeedbackService _service;
    private CancellationToken _ct = CancellationToken.None;

    public AssignmentFeedbackServiceTests()
    {
        _repo = new Mock<IAssignmentFeedbackRepository>();
        _service = new AssignmentFeedbackService(_repo.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidData_CreatesAndReturnsEntity()
    {
        // Arrange
        var assignmentId = "assignment-1";
        var userId = "user-1";
        var text = "good feedback";
        var type = FeedbackType.Manager;

        // Act
        var result = await _service.CreateAsync(assignmentId, userId, text , type, _ct);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(assignmentId, result.AssignmentId);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(text, result.Text);
        Assert.Equal(type, result.Type);

        _repo.Verify(r => r.CreateAsync(
            It.Is<AssignmentFeedback>(af =>
                af.AssignmentId == assignmentId &&
                af.UserId == userId &&
                af.Text == text &&
                af.Type == type),
            _ct),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_RepoReturnsEntity_ServiceReturnsSame()
    {
        // Arrange
        var af = new AssignmentFeedback("a1", "u1", "t1", FeedbackType.Employee);
        _repo.Setup(r => r.GetByIdAsync(af.Id, _ct)).ReturnsAsync(af);

        // Act
        var result = await _service.GetByIdAsync(af.Id, _ct);

        // Assert
        Assert.Same(af, result);
        _repo.Verify(r => r.GetByIdAsync(af.Id, _ct), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        // Arrange
        _repo.Setup(r => r.GetByIdAsync("nope", _ct)).ReturnsAsync((AssignmentFeedback?)null);

        // Act
        var result = await _service.GetByIdAsync("nope", _ct);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ListByAssignmentAsync_RepoReturnsList_ServiceReturnsSame()
    {
        // Arrange
        var list = new List<AssignmentFeedback>
        {
            new AssignmentFeedback("a1", "u1", "t1", FeedbackType.Employee),
            new AssignmentFeedback("a1", "u2", "t2", FeedbackType.Manager)
        };
        _repo.Setup(r => r.ListByAssignmentAsync("a1", _ct)).ReturnsAsync(list);

        // Act
        var result = await _service.ListByAssignmentAsync("a1", _ct);

        // Assert
        Assert.Same(list, result);
        _repo.Verify(r  => r.ListByAssignmentAsync("a1", _ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CallsRepository()
    {
        // Arrange
        var af = new AssignmentFeedback("a1", "u1", "t1", FeedbackType.Employee);

        // Act
        await _service.UpdateAsync(af, _ct);

        // Asssert
        _repo.Verify(r => r.UpdateAsync(af, false, _ct), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepository()
    {
        // Act
        await _service.DeleteAsync("a1", _ct);

        // Assert
        _repo.Verify(r => r.DeleteAsync("a1", _ct), Times.Once);
    }
}
