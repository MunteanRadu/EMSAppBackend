using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Tests;

[Trait("Category", "Domain")]
public class UserProfileTests
{
    /// <summary>
    /// Shared test helper
    /// </summary>
    /// <param name="act"></param>
    /// <param name="expectedMessage"></param>
    private static void AssertThrowsUserProfileException(Action act, string expectedMessage)
    {
        var ex = Assert.Throws<DomainException>(act);
        Assert.Contains(expectedMessage, ex.Message);
    }

    /// <summary>
    /// Valid parameters for UserProfile
    /// </summary>
    /// <returns></returns>
    private static (string name, string jobTitle, int age, string phone, string address, string emergencyContact) GetValidParams()
        => ("New Valid Name", "Valid Job Title 1", 22, "0711111111", "Valid Address Street, no.2", "New Valid Emergency Contact");

    /// <summary>
    /// Creates valid UserProfile
    /// </summary>
    /// <returns></returns>
    private static UserProfile CreateValidUserProfile()
        => new("Valid Name", "Valid Job Title", 21, "0712345689", "Valid Address Street, no.1", "Valid Emergency Contact");

    /// CONSTRUCTOR TESTS

    [Theory]
    [InlineData("0712345689")]
    [InlineData("+40712345689")]
    [InlineData("0040712345689")]
    public void Constructor_ValidParameters_CreatesUserProfile(string phone)
    {
        // Arrange
        var valid = GetValidParams();

        // Act
        var userProfile = new UserProfile(valid.name, valid.jobTitle, valid.age, phone, valid.address, valid.emergencyContact);

        // Assert
        Assert.Equal(valid.name, userProfile.Name);
        Assert.Equal(valid.jobTitle, userProfile.JobTitle);
        Assert.Equal(valid.age, userProfile.Age);
        Assert.Equal(phone, userProfile.Phone);
        Assert.Equal(valid.address, userProfile.Address);
        Assert.Equal(valid.emergencyContact, userProfile.EmergencyContact);
    }

    // Name validation
    [Theory]
    [InlineData(null, "Name length must be between 3-50 characters")]
    [InlineData("", "Name length must be between 3-50 characters")]
    [InlineData("   ", "Name length must be between 3-50 characters")]
    [InlineData("1nV4l1dN4m3", "Name must only contain letters")]
    [InlineData("AB", "Name length must be between 3-50 characters")]
    [InlineData("qwertyuiopasdfghjklzxcvbnmqwertyuiopasdfghjklzxcvbnm", "Name length must be between 3-50 characters")]
    public void Constructor_InvalidName_ThrowsDomainException(string badName, string expectedMessage)
    {
        // Arrange
        var valid = GetValidParams();

        // Act & Assert
        AssertThrowsUserProfileException(() => 
            new UserProfile(badName, valid.jobTitle, valid.age, valid.phone, valid.address, valid.emergencyContact),
            expectedMessage
        );
    }

    // JobTitle validation
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidJobTitle_ThrowsDomainException(string badJobTitle)
    {
        // Arrange
        var valid = GetValidParams();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new UserProfile(valid.name, badJobTitle, valid.age, valid.phone, valid.address, valid.emergencyContact)
        );
        Assert.Contains("Job title cannot be empty", ex.Message);
    }

    // Age validation
    [Theory]
    [InlineData(17)]
    [InlineData(66)]
    public void Constructor_InvalidAge_ThrowsException(int badAge)
    {
        // Arrange
        var valid = GetValidParams();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new UserProfile(valid.name, valid.jobTitle, badAge, valid.phone, valid.address, valid.emergencyContact)
        );
        Assert.Contains("Age must be between 18 and 65", ex.Message);
    }

    // Phone validation
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("7012345689")]
    [InlineData("-40712345689")]
    [InlineData("+50712345689")]
    [InlineData("0740712345689")]
    public void Constructor_InvalidPhone_ThrowsException(string badPhone) 
    {
        // Arrange
        var valid = GetValidParams();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new UserProfile(valid.name, valid.jobTitle, valid.age, badPhone, valid.address, valid.emergencyContact)
        );
        Assert.Contains("Phone number must be in valid format", ex.Message);
    }

    // Address validation
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Cluj")]
    public void Constructor_InvalidAddress_ThrowsException(string badAddress)
    {
        // Arrange
        var valid = GetValidParams();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new UserProfile(valid.name, valid.jobTitle, valid.age, valid.phone, badAddress, valid.emergencyContact)
        );
        Assert.Contains("Address too short", ex.Message);
    }

    // EmergencyContact validation
    [Theory]
    [InlineData(null, "Emergency contact length must be between 3-50 characters")]
    [InlineData("", "Emergency contact length must be between 3-50 characters")]
    [InlineData("   ", "Emergency contact length must be between 3-50 characters")]
    [InlineData("1nV4l1dN4m3", "Emergency contact must only contain letters")]
    [InlineData("AB", "Emergency contact length must be between 3-50 characters")]
    [InlineData("qwertyuiopasdfghjklzxcvbnmqwertyuiopasdfghjklzxcvbnm", "Emergency contact length must be between 3-50 characters")]
    public void Constructor_InvalidEmergencyContact_ThrowsDomainException(string badEmergencyContact, string expectedMessage)
    {
        // Arrange
        var valid = GetValidParams();

        // Act & Assert
        AssertThrowsUserProfileException(() =>
            new UserProfile(valid.name, valid.jobTitle, valid.age, valid.phone, valid.address, badEmergencyContact),
            expectedMessage
        );
    }

    /// METHODS TESTS

    [Theory]
    [InlineData("0712345689")]
    [InlineData("+40712345689")]
    [InlineData("0040712345689")]
    public void Update_ValidParameters_UpdatesUserProfile(string phone)
    {
        // Arrange
        var userProfile = CreateValidUserProfile();
        var valid = GetValidParams();

        // Act
        userProfile.Update(valid.name, valid.jobTitle, valid.age, phone, valid.address, valid.emergencyContact);

        // Assert
        Assert.Equal(valid.name, userProfile.Name);
        Assert.Equal(valid.jobTitle, userProfile.JobTitle);
        Assert.Equal(valid.age, userProfile.Age);
        Assert.Equal(phone, userProfile.Phone);
        Assert.Equal(valid.address, userProfile.Address);
        Assert.Equal(valid.emergencyContact, userProfile.EmergencyContact);
    }

    // Name validation
    [Theory]
    [InlineData(null, "Name length must be between 3-50 characters")]
    [InlineData("", "Name length must be between 3-50 characters")]
    [InlineData("   ", "Name length must be between 3-50 characters")]
    [InlineData("1nV4l1dN4m3", "Name must only contain letters")]
    [InlineData("AB", "Name length must be between 3-50 characters")]
    [InlineData("qwertyuiopasdfghjklzxcvbnmqwertyuiopasdfghjklzxcvbnm", "Name length must be between 3-50 characters")]
    public void Update_InvalidName_ThrowsDomainException(string badName, string expectedMessage)
    {
        // Arrange
        var userProfile = CreateValidUserProfile();
        var valid = GetValidParams();

        // Act & Assert
        AssertThrowsUserProfileException(() => 
            userProfile.Update(badName, valid.jobTitle, valid.age, valid.phone, valid.address, valid.emergencyContact),
            expectedMessage
        );
    }

    // JobTitle validation
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_InvalidJobTitle_ThrowsDomainException(string badJobTitle)
    {
        // Arrange
        var userProfile = CreateValidUserProfile();
        var valid = GetValidParams();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            userProfile.Update(valid.name, badJobTitle, valid.age, valid.phone, valid.address, valid.emergencyContact)
        );
        Assert.Contains("Job title cannot be empty", ex.Message);
    }

    // Age validation
    [Theory]
    [InlineData(17)]
    [InlineData(66)]
    public void Update_InvalidAge_ThrowsException(int badAge)
    {
        // Arrange
        var userProfile = CreateValidUserProfile();
        var valid = GetValidParams();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            userProfile.Update(valid.name, valid.jobTitle, badAge, valid.phone, valid.address, valid.emergencyContact)
        );
        Assert.Contains("Age must be between 18 and 65", ex.Message);
    }

    // Phone validation
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("7012345689")]
    [InlineData("-40712345689")]
    [InlineData("+50712345689")]
    [InlineData("0740712345689")]
    public void Update_InvalidPhone_ThrowsException(string badPhone)
    {
        // Arrange
        var userProfile = CreateValidUserProfile();
        var valid = GetValidParams();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
             userProfile.Update(valid.name, valid.jobTitle, valid.age, badPhone, valid.address, valid.emergencyContact)
        );
        Assert.Contains("Phone number must be in valid format", ex.Message);
    }

    // Address validation
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Cluj")]
    public void Update_InvalidAddress_ThrowsException(string badAddress)
    {
        // Arrange
        var userProfile = CreateValidUserProfile();
        var valid = GetValidParams();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            userProfile.Update(valid.name, valid.jobTitle, valid.age, valid.phone, badAddress, valid.emergencyContact)
        );
        Assert.Contains("Address too short", ex.Message);
    }

    // EmergencyContact validation
    [Theory]
    [InlineData(null, "Emergency contact length must be between 3-50 characters")]
    [InlineData("", "Emergency contact length must be between 3-50 characters")]
    [InlineData("   ", "Emergency contact length must be between 3-50 characters")]
    [InlineData("1nV4l1dN4m3", "Emergency contact must only contain letters")]
    [InlineData("AB", "Emergency contact length must be between 3-50 characters")]
    [InlineData("qwertyuiopasdfghjklzxcvbnmqwertyuiopasdfghjklzxcvbnm", "Emergency contact length must be between 3-50 characters")]
    public void Update_InvalidEmergencyContact_ThrowsDomainException(string badEmergencyContact, string expectedMessage)
    {
        // Arrange
        var userProfile = CreateValidUserProfile();
        var valid = GetValidParams();

        // Act & Assert
        AssertThrowsUserProfileException(() =>
            userProfile.Update(valid.name, valid.jobTitle, valid.age, valid.phone, valid.address, badEmergencyContact),
            expectedMessage
        );
    }

    [Fact]
    public void IsEmpty_AllFieldsAreNullOrEmpty_ReturnsTrue()
    {
        // Arrange
        var userProfile = new UserProfile();

        // Assert
        Assert.True(userProfile.IsEmpty());
    }
    
    [Fact]
    public void IsEmpty_FieldsContainData_ReturnsFalse()
    {
        // Arrange
        var userProfile = CreateValidUserProfile();

        // Assert
        Assert.False(userProfile.IsEmpty());
    }

}
