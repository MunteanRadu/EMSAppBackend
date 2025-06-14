using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Tests
{
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
            Assert.Contains("Email cannot be empty", ex.Message);
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
            Assert.Contains("Email format is invalid", ex.Message);
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
            var badPassword = new string('x', 257);

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() =>
                new User(email, username, badPassword, department)
            );
            Assert.Contains("Password too long", ex.Message);
        }

        [Fact]
        public void AssignRole_ValidRole_AssignsRole()
        {
            // Arrange
            var user = new User("e@mail.com", "User", "Password123", "Dept1");

            // Act
            Assert.Equal(UserRole.Employee, user.Role);
            user.UpdateRole(UserRole.Manager);

            // Assert
            Assert.Equal(UserRole.Manager, user.Role);
        }

        [Theory]
        [InlineData((UserRole)999)]
        public void AssignRole_InvalidRole_ThrowsDomainException(UserRole badRole)
        {
            // Arrange
            var user = new User("e@mail.com", "User", "Password123", "Dept1");

            // Act & Assert
            Assert.Equal(UserRole.Employee, user.Role);
            var ex = Assert.Throws<DomainException>(() => user.UpdateRole(badRole));
            Assert.Contains("Role is invalid", ex.Message);
        }

        [Fact]
        public void ChangeDepartment_ValidDepartment_ChangesDepartment()
        {
            // Arrange
            var user = new User("e@mail.com", "User", "Password123", "Dept1");

            // Act
            user.UpdateDepartment("NewDept");

            // Assert
            Assert.Equal("NewDept", user.DepartmentId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ChangeDepartment_InvalidDepartment_IsAccepted(string badDept)
        {
            // Arrange
            var user = new User("e@mail.com", "User", "Password123", "Dept1");

            // Act
            user.UpdateDepartment(badDept);

            // Assert
            Assert.Equal(badDept, user.DepartmentId);
        }

        [Fact]
        public void UpdateProfile_ValidProfile_UpdatesProfile()
        {
            // Arrange
            var user = new User("e@mail.com", "User", "Password123", "Dept1");
            var profile = new UserProfile("Name Test", 30, "0712345678", "Address Test", "0711111111");

            // Act
            user.UpdateProfile(profile);

            // Assert
            Assert.Same(profile, user.Profile);
        }

        [Fact]
        public void UpdateProfile_NullProfile_ThrowsDomainException()
        {
            // Arrange
            var user = new User("e@mail.com", "User", "Password123", "Dept1");

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() => user.UpdateProfile(null));
            Assert.Contains("Profile data is required", ex.Message);
        }

        [Fact]
        public void ChangePassword_ValidPassword_ChangesPassword()
        {
            // Arrange
            var user = new User("e@mail.com", "User", "OldPass123", "Dept1");

            // Act
            user.UpdatePassword("NewPass456");

            // Assert
            Assert.Equal("NewPass456", user.PasswordHash);
        }
    }
}