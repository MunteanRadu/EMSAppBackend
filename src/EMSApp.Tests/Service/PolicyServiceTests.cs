using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Infrastructure;
using Moq;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Service")]
    public class PolicyServiceTests
    {
        private readonly Mock<IPolicyRepository> _repoMock;
        private readonly IPolicyService _service;
        private readonly CancellationToken _ct = CancellationToken.None;

        public PolicyServiceTests()
        {
            _repoMock = new Mock<IPolicyRepository>();
            _service = new PolicyService(_repoMock.Object);
        }

        private static IDictionary<LeaveType, int> GetValidQuotas() =>
            Enum.GetValues<LeaveType>()
                .Cast<LeaveType>()
                .ToDictionary(lt => lt, _ => 10);

        [Fact]
        public async Task CreateAsync_ValidData_CreatesPolicyAndCallsRepo()
        {
            // Arrange
            var year = 2025;
            var start = new TimeOnly(8, 0);
            var end = start.AddHours(8);
            var pit = TimeSpan.FromMinutes(15);
            var pot = TimeSpan.FromMinutes(15);
            var msb = TimeSpan.FromMinutes(45);
            var mtb = TimeSpan.FromHours(2);
            var om = 1.5m;
            var quotas = GetValidQuotas();

            // Act
            var policy = await _service.CreateAsync(
                year, start, end, pit, pot, msb, mtb, om, quotas, _ct);

            // Assert
            Assert.Equal(year, policy.Year);
            Assert.Equal(start, policy.WorkDayStart);
            Assert.Equal(end, policy.WorkDayEnd);
            Assert.Equal(pit, policy.PunchInTolerance);
            Assert.Equal(pot, policy.PunchOutTolerance);
            Assert.Equal(msb, policy.MaxSingleBreak);
            Assert.Equal(mtb, policy.MaxTotalBreakPerDay);
            Assert.Equal(om, policy.OvertimeMultiplier);
            Assert.Equal(quotas, policy.LeaveQuotas);

            _repoMock.Verify(r => r.CreateAsync(
                It.Is<Policy>(p =>
                    p.Year == year &&
                    p.WorkDayStart == start &&
                    p.WorkDayEnd == end &&
                    p.PunchInTolerance == pit &&
                    p.PunchOutTolerance == pot &&
                    p.MaxSingleBreak == msb &&
                    p.MaxTotalBreakPerDay == mtb &&
                    p.OvertimeMultiplier == om &&
                    quotas.All(kv => p.LeaveQuotas[kv.Key] == kv.Value)
                ),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetByYearAsync_Existing_ReturnsPolicy()
        {
            var p = new Policy(
                2025,
                new TimeOnly(8, 0),
                new TimeOnly(16, 0),
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(30),
                TimeSpan.FromHours(2),
                1.5m,
                GetValidQuotas()
            );
            _repoMock.Setup(r => r.GetByYearAsync(2025, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(p);

            var result = await _service.GetByYearAsync(2025, _ct);

            Assert.Same(p, result);
            _repoMock.Verify(r => r.GetByYearAsync(2025, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetByYearAsync_NonExistent_ReturnsNull()
        {
            _repoMock.Setup(r => r.GetByYearAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Policy?)null);

            var result = await _service.GetByYearAsync(1, _ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_RepoReturnsList_ServiceReturnsSame()
        {
            var list = new List<Policy>
            {
                new Policy(
                    2025,
                    new TimeOnly(8,0),
                    new TimeOnly(16,0),
                    TimeSpan.FromMinutes(15),
                    TimeSpan.FromMinutes(10),
                    TimeSpan.FromMinutes(30),
                    TimeSpan.FromHours(2),
                    1.5m,
                    GetValidQuotas()
                )
            };
            _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync(list);

            var result = await _service.GetAllAsync(_ct);

            Assert.Same(list, result);
            _repoMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_CallsRepoWithUpsertFalse()
        {
            var p = new Policy(
                2025,
                new TimeOnly(8, 0),
                new TimeOnly(16, 0),
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(30),
                TimeSpan.FromHours(2),
                1.5m,
                GetValidQuotas()
            );

            await _service.UpdateAsync(p, _ct);

            _repoMock.Verify(r => r.UpdateAsync(p, false, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_CallsRepository()
        {
            await _service.DeleteAsync(2025, _ct);

            _repoMock.Verify(r => r.DeleteAsync(2025, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
