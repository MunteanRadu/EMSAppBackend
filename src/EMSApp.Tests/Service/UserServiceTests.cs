using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EMSApp.Application;
using EMSApp.Domain.Entities;
using EMSApp.Domain;
using EMSApp.Infrastructure;
using Moq;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Service")]
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _repoMock;
        private readonly IUserService _service;
        private readonly CancellationToken _ct = CancellationToken.None;

        public UserServiceTests()
        {
            _repoMock = new Mock<IUserRepository>();
            _service = new UserService(_repoMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ValidData_CreatesUserAndCallsRepo()
        {
            // Arrange
            var email = "test@example.com";
            var username = "testuser";
            var password = "P@ssw0rd";
            var dept = "dept1";

            // Act
            var user = await _service.CreateAsync(email, username, password, dept, _ct);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(email, user.Email);
            Assert.Equal(username, user.Username);
            Assert.Equal(password, user.PasswordHash);
            Assert.Equal(dept, user.DepartmentId);

            _repoMock.Verify(r =>
                r.CreateAsync(It.Is<User>(u =>
                    u.Email == email &&
                    u.Username == username &&
                    u.PasswordHash == password &&
                    u.DepartmentId == dept
                ), _ct),
                Times.Once
            );
        }

        [Fact]
        public async Task GetByIdAsync_ExistingUser_ReturnsUser()
        {
            // Arrange
            var existing = new User("a@b.com", "abc", "password", "d");
            _repoMock.Setup(r => r.GetByIdAsync(existing.Id, _ct))
                     .ReturnsAsync(existing);

            // Act
            var result = await _service.GetByIdAsync(existing.Id, _ct);

            // Assert
            Assert.Same(existing, result);
            _repoMock.Verify(r => r.GetByIdAsync(existing.Id, _ct), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistent_ReturnsNull()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync("no-id", _ct))
                     .ReturnsAsync((User?)null);

            // Act
            var result = await _service.GetByIdAsync("no-id", _ct);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_CallsRepoAndReturnsList()
        {
            // Arrange
            var list = new List<User>
            {
                new User("x@x.com","xyz","password","d")
            };
            _repoMock.Setup(r => r.GetAllAsync(_ct)).ReturnsAsync(list);

            // Act
            var result = await _service.GetAllAsync(_ct);

            // Assert
            Assert.Same(list, result);
            _repoMock.Verify(r => r.GetAllAsync(_ct), Times.Once);
        }

        [Fact]
        public async Task ListByDepartmentAsync_CallsRepoAndReturnsList()
        {
            // Arrange
            var dept = "dept2";
            var list = new List<User>();
            _repoMock.Setup(r => r.ListByDepartmentAsync(dept, _ct)).ReturnsAsync(list);

            // Act
            var result = await _service.ListByDepartmentAsync(dept, _ct);

            // Assert
            Assert.Same(list, result);
            _repoMock.Verify(r => r.ListByDepartmentAsync(dept, _ct), Times.Once);
        }

        [Fact]
        public async Task ListByRoleAsync_CallsRepoAndReturnsList()
        {
            // Arrange
            var role = UserRole.Manager;
            var list = new List<User>();
            _repoMock.Setup(r => r.ListByRoleAsync(role, _ct)).ReturnsAsync(list);

            // Act
            var result = await _service.ListByRoleAsync(role, _ct);

            // Assert
            Assert.Same(list, result);
            _repoMock.Verify(r => r.ListByRoleAsync(role, _ct), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_CallsRepoWithUpsertFalse()
        {
            // Arrange
            var u = new User("e@e", "user", "password", "d");

            // Act
            await _service.UpdateAsync(u, _ct);

            // Assert
            _repoMock.Verify(r => r.UpdateAsync(u, false, _ct), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_CallsRepo()
        {
            // Act
            await _service.DeleteAsync("uid", _ct);

            // Assert
            _repoMock.Verify(r => r.DeleteAsync("uid", _ct), Times.Once);
        }
    }
}
