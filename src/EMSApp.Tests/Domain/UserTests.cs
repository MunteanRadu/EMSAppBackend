using EMSApp.Domain;
using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Tests;

[Trait("Category", "Domain")]
public class UserTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesUser()
    {
        // Arrange
        var email = "validemail@gmail.com";
        var username = "ValidUsername";
        var password = "ValidPassword";
        var department = "ValidDepartment";

        // Act
        var user = new User(email, username, password, department);

        // Assert
        Assert.Equal(email, user.Email);
        Assert.Equal(username, user.Username);
        Assert.Equal(password, user.PasswordHash);
        Assert.Equal(department, user.DepartmentId);
        Assert.Equal(UserRole.Employee, user.Role);
        Assert.True(user.Profile.IsEmpty());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyEmail_ThrowsDomainException(string badEmail)
    {
        // Arrange
        var username = "ValidUsername";
        var password = "ValidPassword";
        var department = "ValidDepartment";

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new User(badEmail, username, password, department)
        );
        Assert.Contains("Username cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData("abc")]
    public void Constructor_InvalidEmail_ThrowsDomainException(string badEmail)
    {
        // Arrange
        var username = "ValidUsername";
        var password = "ValidPassword";
        var department = "ValidDepartment";

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new User(badEmail, username, password, department)
        );
        Assert.Contains("Username format is invalid", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData("qwertyuiopasdfghjklzxcvbnmqwertyuiopasdfghjklzxcvbnm")]
    public void Constructor_InvalidUsername_ThrowsDomainException(string badUsername)
    {
        // Arrange
        var email = "validemail@gmail.com";
        var password = "ValidPassword";
        var department = "ValidDepartment";

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new User(email, badUsername, password, department)
        );
        Assert.Contains("Username length must be between 3-50 characters", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("parola")]
    public void Constructor_ShortPassword_ThrowsDomainException(string badPassword)
    {
        // Arrange
        var email = "validemail@gmail.com";
        var username = "ValidUsername";
        var department = "ValidDepartment";

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new User(email, username, badPassword, department)
        );
        Assert.Contains("Password too short", ex.Message);
    }

    [Fact]
    public void Constructor_LongPassword_ThrowsDomainException() 
    {
        // Arrange
        var email = "validemail@gmail.com";
        var username = "ValidUsername";
        var department = "ValidDepartment";

        var badPassword = new string('x', 51);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new User(email, username, badPassword, department)
        );
        Assert.Contains("Password too long", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidDepartment_ThrowsDomainException(string badDepartment)
    {
        // Arrange
        var email = "validemail@gmail.com";
        var username = "ValidUsername";
        var password = "ValidPassword";

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new User(email, username, password, badDepartment)
        );
        Assert.Contains("DepartmentId Id cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Admin)]
    public void AssignRole_ValidRole_AssignsRole(UserRole newRole)
    {
        // Arrange
        var email = "validemail@gmail.com";
        var username = "ValidUsername";
        var password = "ValidPassword";
        var department = "ValidDepartment";
        var user = new User(email, username, password, department);

        // Act
        Assert.Equal(UserRole.Employee, user.Role);
        user.UpdateRole(newRole);

        // Assert
        Assert.Equal(newRole, user.Role);
    }

    [Theory]
    [InlineData((UserRole)999)]
    public void AssignRole_InvalidRole_ThrowsDomainException(UserRole badRole)
    {
        // Arrange
        var email = "validemail@gmail.com";
        var username = "ValidUsername";
        var password = "ValidPassword";
        var department = "ValidDepartment";
        var user = new User(email, username, password, department);

        // Act & Assert
        Assert.Equal(UserRole.Employee, user.Role);
        var ex = Assert.Throws<DomainException>(() =>
            user.UpdateRole(badRole)
        );
        Assert.Contains("Role is invalid", ex.Message);
    }

    [Fact]
    public void ChangeDepartment_ValidDepartment_ChangesDepartment()
    {
        // Arrange
        var email = "validemail@gmail.com";
        var username = "ValidUsername";
        var password = "ValidPassword";
        var department = "ValidDepartment";
        var user = new User(email, username, password, department);

        var newDepartment = "NewDepartment";

        // Act
        Assert.Equal(department, user.DepartmentId);
        user.UpdateDepartment(newDepartment);

        // Assert
        Assert.Equal(newDepartment, user.DepartmentId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangeDepartment_InvalidDepartment_ThrowsDomainException(string badDepartment)
    {
        // Arrange
        var email = "validemail@gmail.com";
        var username = "ValidUsername";
        var password = "ValidPassword";
        var department = "ValidDepartment";
        var user = new User(email, username, password, department);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            user.UpdateDepartment(badDepartment)
        );
        Assert.Contains("DepartmentId cannot be empty", ex.Message);
    }

    [Fact]
    public void UpdateProfile_ValidProfile_UpdatesProfile()
    {
        // Arrange
        /// UserId
        var email = "validemail@gmail.com";
        var username = "ValidUsername";
        var password = "ValidPassword";
        var department = "ValidDepartment";
        var user = new User(email, username, password, department);

        /// UserProfile
        var name = "Valid Name";
        var jobTitle = "Valid Job Title";
        var age = 21;
        var phone = "0730232414";
        var address = "Valid Address Street, no. 15";
        var emergencyContact = "Valid EmergencyContact Name";
        var userProfile = new UserProfile(name, jobTitle, age, phone, address, emergencyContact);

        // Act
        user.UpdateProfile(userProfile);

        // Assert
        Assert.Equal(userProfile, user.Profile);
    }

    [Theory]
    [InlineData(null)]
    public void UpdateProfile_InvalidProfile_ThrowsDomainException(UserProfile badProfile)
    {
        // Arrange
        var email = "validemail@gmail.com";
        var username = "ValidUsername";
        var password = "ValidPassword";
        var department = "ValidDepartment";
        var user = new User(email, username, password, department);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            user.UpdateProfile(badProfile)
        );
        Assert.Contains("Profile data is required", ex.Message);
    }

    [Fact]
    public void ChangePassword_ValidPassword_ChangesPassword()
    {
        // Arrange
        var email = "validemail@gmail.com";
        var username = "ValidUsername";
        var password = "ValidPassword";
        var department = "ValidDepartment";
        var user = new User(email, username, password, department);

        var newPassword = "ValidNewPassword";

        // Act
        user.UpdatePassword(newPassword);

        // Assert
        Assert.Equal(newPassword, user.PasswordHash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("parola")]
    public void ChangePassword_ShortPassword_ThrowsDomainException(string badPassword)
    {
        // Arrange
        var email = "email@abc.xyz";
        var username = "ValidUsername";
        var password = "ValidPassword";
        var department = "ValidDepartment";
        var user = new User(email, username, password, department);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            user.UpdatePassword(badPassword)
        );
        Assert.Contains("Password too short", ex.Message);
    }

    [Fact]
    public void ChangePassword_LongPassword_ThrowsDomainException()
    {
        // Arrange
        var email = "email@abc.xyz";
        var username = "ValidUsername";
        var password = "ValidPassword";
        var department = "ValidDepartment";
        var user = new User(email, username, password, department);

        var badPassword = new string('x', 51);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            user.UpdatePassword(badPassword)
        );
        Assert.Contains("Password too long", ex.Message);
    }
}
