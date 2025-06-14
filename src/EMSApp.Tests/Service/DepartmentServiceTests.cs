using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;
using EMSApp.Infrastructure;
using Moq;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Service")]
    public class DepartmentServiceTests
    {
        private readonly Mock<IDepartmentRepository> _deptRepo;
        private readonly Mock<IUserRepository> _userRepo;
        private readonly IDepartmentService _service;
        private readonly CancellationToken _ct = CancellationToken.None;

        public DepartmentServiceTests()
        {
            _deptRepo = new Mock<IDepartmentRepository>();
            _userRepo = new Mock<IUserRepository>();
            _service = new DepartmentService(_deptRepo.Object, _userRepo.Object);
        }

        [Fact]
        public async Task CreateAsync_CreatesDepartment()
        {
            // Arrange
            var name = "Engineering";

            // Act
            var dept = await _service.CreateAsync(name, _ct);

            // Assert
            Assert.NotNull(dept);
            Assert.Equal(name, dept.Name);
            _deptRepo.Verify(r => r.CreateAsync(
                It.Is<Department>(d => d.Name == name),
                _ct),
                Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsFromRepository()
        {
            var list = new List<Department> { new Department("A"), new Department("B") };
            _deptRepo.Setup(r => r.GetAllAsync(_ct)).ReturnsAsync(list);

            var result = await _service.GetAllAsync(_ct);

            Assert.Same(list, result);
            _deptRepo.Verify(r => r.GetAllAsync(_ct), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_Found_ReturnsDepartment()
        {
            var d = new Department("Sales");
            _deptRepo.Setup(r => r.GetByIdAsync(d.Id, _ct)).ReturnsAsync(d);

            var result = await _service.GetByIdAsync(d.Id, _ct);

            Assert.Same(d, result);
            _deptRepo.Verify(r => r.GetByIdAsync(d.Id, _ct), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ReturnsNull()
        {
            _deptRepo.Setup(r => r.GetByIdAsync("no", _ct)).ReturnsAsync((Department?)null);

            var result = await _service.GetByIdAsync("no", _ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_CallsRepository()
        {
            var d = new Department("HR");
            await _service.UpdateAsync(d, _ct);

            _deptRepo.Verify(r => r.UpdateAsync(d, false, _ct), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_UnassignsUsersAndDeletes()
        {
            // Arrange
            var deptId = "dept1";
            var users = new List<User>
            {
                new User("a@e","alice","password","dept1"),
                new User("b@e","bob","password","dept1")
            };
            _userRepo.Setup(r => r.ListByDepartmentAsync(deptId, _ct))
                     .ReturnsAsync(users);

            // Act
            await _service.DeleteAsync(deptId, _ct);

            // Assert
            foreach (var u in users)
                Assert.Equal("", u.DepartmentId);

            foreach (var u in users)
                _userRepo.Verify(r => r.UpdateAsync(u, true, _ct), Times.Once);

            _deptRepo.Verify(r => r.DeleteAsync(deptId, _ct), Times.Once);
        }

        [Fact]
        public async Task AddEmployeeAsync_HappyPath()
        {
            // Arrange
            var deptId = "deptX";
            var userId = "userX";
            var user = new User("x@e", "xyz", "password", "old");
            var dept = new Department("D");

            _userRepo.Setup(r => r.GetByIdAsync(userId, _ct)).ReturnsAsync(user);
            _deptRepo.Setup(r => r.GetByIdAsync(deptId, _ct)).ReturnsAsync(dept);

            // Act
            await _service.AddEmployeeAsync(deptId, userId, _ct);

            // Assert
            Assert.Equal(deptId, user.DepartmentId);
            _userRepo.Verify(r => r.UpdateAsync(user, true, _ct), Times.Once);
            Assert.Contains(userId, dept.Employees);
            _deptRepo.Verify(r => r.UpdateAsync(dept, false, _ct), Times.Once);
        }

        [Fact]
        public async Task AddEmployeeAsync_UserNotFound_Throws()
        {
            _userRepo.Setup(r => r.GetByIdAsync("no", _ct)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(()
                => _service.AddEmployeeAsync("dept", "no", _ct));
        }

        [Fact]
        public async Task AddEmployeeAsync_DeptNotFound_Throws()
        {
            var user = new User("u@e", "user", "password", "old");
            _userRepo.Setup(r => r.GetByIdAsync("u", _ct)).ReturnsAsync(user);
            _deptRepo.Setup(r => r.GetByIdAsync("no", _ct)).ReturnsAsync((Department?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(()
                => _service.AddEmployeeAsync("no", "u", _ct));
        }

        [Fact]
        public async Task RemoveEmployeeAsync_HappyPath()
        {
            // Arrange
            var deptId = "deptY";
            var userId = "userY";
            var user = new User("y@e", "xyz", "password", "deptY");
            var dept = new Department("D");
            dept.AddEmployee(userId);

            _userRepo.Setup(r => r.GetByIdAsync(userId, _ct)).ReturnsAsync(user);
            _deptRepo.Setup(r => r.GetByIdAsync(deptId, _ct)).ReturnsAsync(dept);

            // Act
            await _service.RemoveEmployeeAsync(deptId, userId, _ct);

            // Assert
            Assert.Equal("", user.DepartmentId);
            _userRepo.Verify(r => r.UpdateAsync(user, true, _ct), Times.Once);
            Assert.DoesNotContain(userId, dept.Employees);
            _deptRepo.Verify(r => r.UpdateAsync(dept, false, _ct), Times.Once);
        }

        [Fact]
        public async Task RemoveEmployeeAsync_UserNotFound_Throws()
        {
            _userRepo.Setup(r => r.GetByIdAsync("no", _ct)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(()
                => _service.RemoveEmployeeAsync("dept", "no", _ct));
        }

        [Fact]
        public async Task RemoveEmployeeAsync_DeptNotFound_Throws()
        {
            var user = new User("u@e", "user", "password", "dept");
            _userRepo.Setup(r => r.GetByIdAsync("u", _ct)).ReturnsAsync(user);
            _deptRepo.Setup(r => r.GetByIdAsync("no", _ct)).ReturnsAsync((Department?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(()
                => _service.RemoveEmployeeAsync("no", "u", _ct));
        }
    }
}
