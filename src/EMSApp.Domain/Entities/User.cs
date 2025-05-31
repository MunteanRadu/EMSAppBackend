using EMSApp.Domain.Exceptions;

namespace EMSApp.Domain.Entities;

public enum UserRole { Admin, Manager, Employee }

public class User
{
    public string Id { get; private set; }
    public string Email { get; private set; }
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public string DepartmentId { get; private set; }
    public UserProfile Profile { get; private set; }
    public decimal Salary { get; private set; }
    public string JobTitle { get; private set; } = "";

    public User(string email, string username, string passwordHash, UserRole role)
    {
        Id = Guid.NewGuid().ToString();
        Email = email.Trim();
        Username = username.Trim();
        PasswordHash = passwordHash.Trim();
        Role = role;
    }

    public User(string email, string username, string passwordHash, string departmentId)
    {
        // Email validation
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty");
        try { _ = new System.Net.Mail.MailAddress(email); }
        catch { throw new DomainException("Email format is invalid"); }

        // Username validation
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 50) 
            throw new DomainException("Username length must be between 3-50 characters");

        // Password validation
        if (string.IsNullOrWhiteSpace(passwordHash) || passwordHash.Length < 8)
            throw new DomainException("Password too short");
        if (passwordHash.Length > 256)
            throw new DomainException("Password too long");

        // DepartmentId validation
        if (string.IsNullOrWhiteSpace(departmentId))
            throw new DomainException("DepartmentId Id cannot be empty");

        Id = Guid.NewGuid().ToString();
        Email = email.Trim();
        Username = username.Trim();
        PasswordHash = passwordHash;
        DepartmentId = departmentId.Trim();

        Role = UserRole.Employee;
        Profile = new UserProfile();
    }

    public void UpdateRole(UserRole newRole)
    {
        if (!Enum.IsDefined(typeof(UserRole), newRole))
            throw new DomainException("Role is invalid");
        Role = newRole;
    }

    public void UpdateDepartment(string newDepartment)
    {
        DepartmentId = newDepartment;
    }

    public void UpdateProfile(UserProfile newProfile)
    {
        if (newProfile == null)
            throw new DomainException("Profile data is required");
        Profile = newProfile;
    }

    public void UpdatePassword(string hash)
    {
        PasswordHash = hash;
    }

    public void UpdateEmail(string newEmail)
    {
        if (string.IsNullOrWhiteSpace(newEmail))
            throw new DomainException("Email cannot be empty");
        try { _ = new System.Net.Mail.MailAddress(newEmail); }
        catch { throw new DomainException("Email format is invalid"); }
        Email = newEmail.Trim();
    }

    public void UpdateUsername(string newUsername)
    {
        if (string.IsNullOrWhiteSpace(newUsername) || newUsername.Length < 3 || newUsername.Length > 50)
            throw new DomainException("Username length must be between 3-50 characters");
        Username = newUsername.Trim();
    }

    public void UpdateSalary(decimal newSalary, string performedByRole)
    {
        if (newSalary < 0)
            throw new DomainException("Salary cannot be negative");
        Salary = newSalary;
    }

    public void UpdateJobTitle(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new DomainException("Job title cannot be empty");
        JobTitle = newTitle.Trim();
    }
}
