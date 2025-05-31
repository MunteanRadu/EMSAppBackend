using EMSApp.Domain.Exceptions;
using Microsoft.VisualBasic;

namespace EMSApp.Domain.Entities;

public enum AssignmentStatus { Pending, InProgress, Done, Approved, Rejected }

public class Assignment
{
    public string Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public DateTime DueDate { get; private set; }
    public string DepartmentId { get; private set; }
    public string ManagerId { get; private set; }
    public string AssignedToId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public AssignmentStatus Status { get; private set; }

    public Assignment(string title, string description, DateTime dueDate, string departmentId, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("AssignmentId title cannot be empty");
        if (dueDate.Date < DateTime.UtcNow.Date)
            throw new DomainException("Due date must be in the future");
        if (string.IsNullOrWhiteSpace(departmentId))
            throw new DomainException("DepartmentId cannot be empty");
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new DomainException("ManagerId cannot be empty");

        Id = Guid.NewGuid().ToString();
        Title = title;
        Description = description?.Trim() ?? "";
        CreatedAt = DateTime.UtcNow;
        DueDate = dueDate;
        DepartmentId = departmentId;
        ManagerId = createdBy;
        Status = AssignmentStatus.Pending;
        ManagerId = createdBy;
    }

    public void Start(string assignee)
    {
        if (Status != AssignmentStatus.Pending)
            throw new DomainException("Can only start a pending task");
        if (string.IsNullOrWhiteSpace(assignee))
            throw new DomainException("Assignment must be assigned to an employee");
        AssignedToId = assignee;
        Status = AssignmentStatus.InProgress;
    }

    public void Complete()
    {
        if (Status != AssignmentStatus.InProgress)
            throw new DomainException("Can only complete an in-progress task");
        Status = AssignmentStatus.Done;
    }

    public void Approve()
    {
        if (Status != AssignmentStatus.Done)
            throw new DomainException("Can only approve a completed task");
        Status = AssignmentStatus.Approved;
    }

    public void Reject()
    {
        if (Status != AssignmentStatus.Done)
            throw new DomainException("Can only approve a completed task");
        Status = AssignmentStatus.Rejected;
    }

    public void UpdateTitle(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new DomainException("Title cannot be empty");
        
        Title = newTitle.Trim();
    }

    public void UpdateDescription(string newDescription) 
    {
        if (string.IsNullOrWhiteSpace(newDescription))
            throw new DomainException("Description cannot be empty");

        Description = newDescription.Trim();
    }

    public void UpdateDueDate(DateTime newDueDate)
    {
        if (newDueDate.Date < DateTime.UtcNow.Date)
            throw new DomainException("Due date must be in the future");

        DueDate = newDueDate;
    }

    public void UpdateAssignedToId(string newAssignedToId)
    {
        if (string.IsNullOrWhiteSpace(newAssignedToId))
            throw new DomainException("Assignee Id cannot be empty");

        AssignedToId = newAssignedToId;
    }

    public void UpdateStatus(AssignmentStatus newStatus)
    {
        if (!Enum.IsDefined(typeof(AssignmentStatus), newStatus))
            throw new DomainException("Assignment status is invalid");

        Status = newStatus;
    }
}
