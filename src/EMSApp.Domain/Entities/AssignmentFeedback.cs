using EMSApp.Domain.Exceptions;

namespace EMSApp.Domain;

public enum FeedbackType { Employee, Manager }

public class AssignmentFeedback
{
    public string Id { get; private set; }
    public string AssignmentId { get; private set; }
    public string UserId { get; private set; }
    public string Text { get; private set; }
    public DateTime TimeStamp { get; private set; }
    public FeedbackType Type { get; private set; }

    public AssignmentFeedback(string taskId, string userId, string text, FeedbackType type)
    {
        // AssignmentId validation
        if (string.IsNullOrWhiteSpace(taskId))
            throw new DomainException("AssignmentId Id cannot be empty");

        // UserId validation
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId Id cannot be empty");

        // Text validation
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Feedback text cannot be empty");
        if (text.Length > 1000)
            throw new DomainException("Feedback text too long");

        // FeedbackType validation
        if (!Enum.IsDefined(typeof(FeedbackType), type))
            throw new DomainException("Feedback type is invalid");

        Id = Guid.NewGuid().ToString();
        AssignmentId = taskId;
        UserId = userId;
        Text = text.Trim();
        TimeStamp = DateTime.UtcNow;
        Type = type;
    }

    public void Edit(string newText)
    {
        if (string.IsNullOrWhiteSpace(newText))
            throw new DomainException("Feedback text cannot be empty");
        if (newText.Length > 1000)
            throw new DomainException("Feedback text too long");

        Text = newText.Trim();
    }
}
