using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Tests
{
    [Trait("Category", "Domain")]
    public class UserProfileTests
    {
        private static void AssertThrowsUserProfileException(Action act, string expectedMessage)
        {
            var ex = Assert.Throws<DomainException>(act);
            Assert.Contains(expectedMessage, ex.Message);
        }

        private static (string name, int age, string phone, string address, string emergencyContact) GetValidParams()
            => ("New Valid Name", 22, "0711111111", "Valid Address Street", "0798765432");

        private static UserProfile CreateValidUserProfile()
            => new UserProfile("Valid Name", 21, "0712345689", "Valid Address, no.1", "0791234567");

        [Theory]
        [InlineData("0712345689")]
        [InlineData("+40712345689")]
        [InlineData("0040712345689")]
        public void Constructor_ValidParameters_CreatesUserProfile(string phone)
        {
            var valid = GetValidParams();
            var profile = new UserProfile(valid.name, valid.age, phone, valid.address, valid.emergencyContact);
            Assert.Equal(valid.name, profile.Name);
            Assert.Equal(valid.age, profile.Age);
            Assert.Equal(phone, profile.Phone);
            Assert.Equal(valid.address, profile.Address);
            Assert.Equal(valid.emergencyContact, profile.EmergencyContact);
        }

        [Theory]
        [InlineData(null, "Name length must be between 3-50 characters")]
        [InlineData("", "Name length must be between 3-50 characters")]
        [InlineData("   ", "Name length must be between 3-50 characters")]
        [InlineData("1nV4l1dN4m3", "Name must only contain letters")]
        [InlineData("AB", "Name length must be between 3-50 characters")]
        [InlineData("qwertyuiopasdfghjklzxcvbnmqwertyuiopasdfghjklzxcvbnm", "Name length must be between 3-50 characters")]
        public void Constructor_InvalidName_ThrowsDomainException(string badName, string expectedMessage)
        {
            var valid = GetValidParams();
            AssertThrowsUserProfileException(() => new UserProfile(badName, valid.age, valid.phone, valid.address, valid.emergencyContact), expectedMessage);
        }

        [Theory]
        [InlineData(17)]
        [InlineData(66)]
        public void Constructor_InvalidAge_ThrowsDomainException(int badAge)
        {
            var valid = GetValidParams();
            AssertThrowsUserProfileException(() => new UserProfile(valid.name, badAge, valid.phone, valid.address, valid.emergencyContact), "Age must be between 18 and 65");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("7012345689")]
        [InlineData("-40712345689")]
        [InlineData("+50712345689")]
        [InlineData("0740712345689")]
        public void Constructor_InvalidPhone_ThrowsDomainException(string badPhone)
        {
            var valid = GetValidParams();
            AssertThrowsUserProfileException(() => new UserProfile(valid.name, valid.age, badPhone, valid.address, valid.emergencyContact), "Phone number must be in valid format");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("Cluj")]
        public void Constructor_InvalidAddress_ThrowsDomainException(string badAddress)
        {
            var valid = GetValidParams();
            AssertThrowsUserProfileException(() => new UserProfile(valid.name, valid.age, valid.phone, badAddress, valid.emergencyContact), "Address too short");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("12345")]
        [InlineData("abcdefghijk")]
        public void Constructor_InvalidEmergencyContact_ThrowsDomainException(string badContact)
        {
            var valid = GetValidParams();
            AssertThrowsUserProfileException(() => new UserProfile(valid.name, valid.age, valid.phone, valid.address, badContact), "Emergency contact phone number must be in valid format");
        }

        [Theory]
        [InlineData("0712345689", "0798765432")]
        [InlineData("+40712345689", "0040798765432")]
        public void Update_ValidParameters_UpdatesUserProfile(string phone, string emergencyContact)
        {
            var profile = CreateValidUserProfile();
            var valid = GetValidParams();
            profile.Update(valid.name, valid.age, phone, valid.address, emergencyContact);
            Assert.Equal(valid.name, profile.Name);
            Assert.Equal(valid.age, profile.Age);
            Assert.Equal(phone, profile.Phone);
            Assert.Equal(valid.address, profile.Address);
            Assert.Equal(emergencyContact, profile.EmergencyContact);
        }

        [Theory]
        [InlineData(null, "Name length must be between 3-50 characters")]
        [InlineData("", "Name length must be between 3-50 characters")]
        [InlineData("   ", "Name length must be between 3-50 characters")]
        [InlineData("1nV4l1dN4m3", "Name must only contain letters")]
        public void Update_InvalidName_ThrowsDomainException(string badName, string expectedMessage)
        {
            var profile = CreateValidUserProfile();
            var valid = GetValidParams();
            AssertThrowsUserProfileException(() => profile.Update(badName, valid.age, valid.phone, valid.address, valid.emergencyContact), expectedMessage);
        }

        [Theory]
        [InlineData(17)]
        [InlineData(66)]
        public void Update_InvalidAge_ThrowsDomainException(int badAge)
        {
            var profile = CreateValidUserProfile();
            var valid = GetValidParams();
            AssertThrowsUserProfileException(() => profile.Update(valid.name, badAge, valid.phone, valid.address, valid.emergencyContact), "Age must be between 18 and 65");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("7012345689")]
        public void Update_InvalidPhone_ThrowsDomainException(string badPhone)
        {
            var profile = CreateValidUserProfile();
            var valid = GetValidParams();
            AssertThrowsUserProfileException(() => profile.Update(valid.name, valid.age, badPhone, valid.address, valid.emergencyContact), "Phone number must be in valid format");
        }

        [Theory]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("Cluj")]
        public void Update_InvalidAddress_ThrowsDomainException(string badAddress)
        {
            var profile = CreateValidUserProfile();
            var valid = GetValidParams();
            AssertThrowsUserProfileException(() => profile.Update(valid.name, valid.age, valid.phone, badAddress, valid.emergencyContact), "Address too short");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("12345")]
        public void Update_InvalidEmergencyContact_ThrowsDomainException(string badContact)
        {
            var profile = CreateValidUserProfile();
            var valid = GetValidParams();
            AssertThrowsUserProfileException(() => profile.Update(valid.name, valid.age, valid.phone, valid.address, badContact), "Emergency contact phone number must be in valid format");
        }

        [Fact]
        public void IsEmpty_AllFieldsAreNullOrEmpty_ReturnsTrue()
        {
            var profile = new UserProfile();
            Assert.True(profile.IsEmpty());
        }

        [Fact]
        public void IsEmpty_FieldsContainData_ReturnsFalse()
        {
            var profile = CreateValidUserProfile();
            Assert.False(profile.IsEmpty());
        }
    }
}
