using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Infrastructure;
using Moq;
using Xunit;

namespace EMSApp.Tests.Service
{
    [Trait("Category", "Service")]
    public class ShiftRuleServiceTests
    {
        private readonly Mock<IShiftRuleRepository> _ruleRepo;
        private readonly IShiftRuleService _service;
        private readonly CancellationToken _ct = CancellationToken.None;

        public ShiftRuleServiceTests()
        {
            _ruleRepo = new Mock<IShiftRuleRepository>();
            _service = new ShiftRuleService(_ruleRepo.Object);
        }

        [Fact]
        public async Task GetRuleByDepartmentAsync_RepoReturnsRule_ServiceReturnsSame()
        {
            // Arrange
            var dept = "dept-1";
            var rule = new ShiftRule(dept, 1, 2, 3, 4, 5.5);
            _ruleRepo.Setup(r => r.GetByDepartmentAsync(dept, _ct))
                     .ReturnsAsync(rule);

            // Act
            var result = await _service.GetRuleByDepartmentAsync(dept, _ct);

            // Assert
            Assert.Same(rule, result);
            _ruleRepo.Verify(r => r.GetByDepartmentAsync(dept, _ct), Times.Once);
        }

        [Fact]
        public async Task GetRuleByDepartmentAsync_NoRule_ReturnsNull()
        {
            // Arrange
            var dept = "dept-1";
            _ruleRepo.Setup(r => r.GetByDepartmentAsync(dept, _ct))
                     .ReturnsAsync((ShiftRule?)null);

            // Act
            var result = await _service.GetRuleByDepartmentAsync(dept, _ct);

            // Assert
            Assert.Null(result);
            _ruleRepo.Verify(r => r.GetByDepartmentAsync(dept, _ct), Times.Once);
        }

        [Fact]
        public async Task CreateOrUpdateRuleAsync_NoExisting_CreatesAndReturnsNew()
        {
            // Arrange
            var dept = "dept-1";
            _ruleRepo.Setup(r => r.GetByDepartmentAsync(dept, _ct))
                     .ReturnsAsync((ShiftRule?)null);

            // Act
            var result = await _service.CreateOrUpdateRuleAsync(
                departmentId: dept,
                minShift1: 1,
                minShift2: 2,
                minNightShift: 3,
                maxConsecutiveNight: 4,
                minRestHoursBetweenShifts: 5.5,
                ct: _ct);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dept, result.DepartmentId);
            Assert.Equal(1, result.MinPerShift1);
            Assert.Equal(2, result.MinPerShift2);
            Assert.Equal(3, result.MinPerNightShift);
            Assert.Equal(4, result.MaxConsecutiveNightShifts);
            Assert.Equal(5.5, result.MinRestHoursBetweenShifts);

            _ruleRepo.Verify(r =>
                r.UpsertAsync(
                    It.Is<ShiftRule>(x =>
                        x.DepartmentId == dept &&
                        x.MinPerShift1 == 1 &&
                        x.MinPerShift2 == 2 &&
                        x.MinPerNightShift == 3 &&
                        x.MaxConsecutiveNightShifts == 4 &&
                        x.MinRestHoursBetweenShifts == 5.5),
                    _ct),
                Times.Once);
        }

        [Fact]
        public async Task CreateOrUpdateRuleAsync_WithExisting_UpdatesAndReturnsSame()
        {
            // Arrange
            var dept = "dept-1";
            var existing = new ShiftRule(dept, 1, 1, 1, 1, 1.0);
            _ruleRepo.Setup(r => r.GetByDepartmentAsync(dept, _ct))
                     .ReturnsAsync(existing);

            // Act
            var result = await _service.CreateOrUpdateRuleAsync(
                departmentId: dept,
                minShift1: 10,
                minShift2: 20,
                minNightShift: 30,
                maxConsecutiveNight: 40,
                minRestHoursBetweenShifts: 6.5,
                ct: _ct);

            // Assert
            Assert.Same(existing, result);
            Assert.Equal(10, existing.MinPerShift1);
            Assert.Equal(20, existing.MinPerShift2);
            Assert.Equal(30, existing.MinPerNightShift);
            Assert.Equal(40, existing.MaxConsecutiveNightShifts);
            Assert.Equal(6.5, existing.MinRestHoursBetweenShifts);

            _ruleRepo.Verify(r =>
                r.UpsertAsync(existing, _ct),
                Times.Once);
        }

        [Fact]
        public async Task DeleteRuleByDepartmentAsync_CallsRepository()
        {
            // Arrange
            var dept = "dept-1";

            // Act
            await _service.DeleteRuleByDepartmentAsync(dept, _ct);

            // Assert
            _ruleRepo.Verify(r => r.DeleteByDepartmentAsync(dept, _ct), Times.Once);
        }
    }
}
