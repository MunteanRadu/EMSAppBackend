using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;
using EMSApp.Infrastructure;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Service")]
    public class BreakSessionServiceTests
    {
        private readonly Mock<IBreakSessionRepository> _breakRepo;
        private readonly Mock<IPunchRecordRepository> _punchRepo;
        private readonly Mock<IPolicyService> _policySvc;
        private readonly Mock<IMapper> _mapper;
        private readonly IBreakSessionService _service;
        private readonly CancellationToken _ct = CancellationToken.None;

        public BreakSessionServiceTests()
        {
            _breakRepo = new Mock<IBreakSessionRepository>();
            _punchRepo = new Mock<IPunchRecordRepository>();
            _policySvc = new Mock<IPolicyService>();
            _mapper = new Mock<IMapper>();

            _service = new BreakSessionService(
                _breakRepo.Object,
                _punchRepo.Object,
                _policySvc.Object,
                _mapper.Object);
        }

        private static IDictionary<LeaveType, int> GetValidQuotas() =>
            Enum.GetValues<LeaveType>()
                .Cast<LeaveType>()
                .ToDictionary(lt => lt, _ => 10);

        [Fact]
        public async Task CreateAsync_Valid_CreatesAndReturnsDto()
        {
            // Arrange
            var punch = new PunchRecord("u", DateOnly.FromDateTime(DateTime.UtcNow), new TimeOnly(9, 0));
            _punchRepo.Setup(r => r.GetByIdAsync("p1", _ct)).ReturnsAsync(punch);
            var policy = new Policy(
                punch.Date.Year,
                new TimeOnly(9, 0), new TimeOnly(17, 0),
                TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(30), TimeSpan.FromHours(2),
                1m, GetValidQuotas());
            _policySvc.Setup(s => s.GetByYearAsync(punch.Date.Year, _ct)).ReturnsAsync(policy);
            _breakRepo.Setup(r => r.ListByPunchRecordAsync("p1", _ct)).ReturnsAsync(Array.Empty<BreakSession>());

            _mapper.Setup(m => m.Map<BreakSessionDto>(It.IsAny<BreakSession>()))
                   .Returns((BreakSession bs) => new BreakSessionDto(bs.Id, bs.PunchRecordId, bs.StartTime, bs.EndTime, bs.Duration, bs.IsNonCompliant));

            // Act
            var dto = await _service.CreateAsync("p1", new TimeOnly(12, 0), _ct);

            // Assert
            Assert.Equal("p1", dto.PunchRecordId);
            Assert.Equal(new TimeOnly(12, 0), dto.StartTime);
            _breakRepo.Verify(r => r.CreateAsync(It.Is<BreakSession>(b => b.PunchRecordId == "p1" && b.StartTime == new TimeOnly(12, 0)), _ct), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_PunchNotFound_Throws()
        {
            _punchRepo.Setup(r => r.GetByIdAsync("x", _ct)).ReturnsAsync((PunchRecord?)null);
            await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync("x", new TimeOnly(0, 0), _ct));
        }

        [Fact]
        public async Task CreateAsync_OpenBreakExists_Throws()
        {
            var punch = new PunchRecord("u", DateOnly.FromDateTime(DateTime.UtcNow), new TimeOnly(9, 0));
            _punchRepo.Setup(r => r.GetByIdAsync("p", _ct)).ReturnsAsync(punch);
            var policy = new Policy(punch.Date.Year, new TimeOnly(9, 0), new TimeOnly(17, 0),
                                     TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromMinutes(30), TimeSpan.FromHours(1), 1m, GetValidQuotas());
            _policySvc.Setup(s => s.GetByYearAsync(punch.Date.Year, _ct)).ReturnsAsync(policy);

            var open = new BreakSession("p", new TimeOnly(10, 0));
            _breakRepo.Setup(r => r.ListByPunchRecordAsync("p", _ct)).ReturnsAsync(new[] { open });

            await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync("p", new TimeOnly(11, 0), _ct));
        }

        [Fact]
        public async Task EndAsync_Valid_EndsAndReturnsDto()
        {
            // Arrange
            var punch = new PunchRecord("u", DateOnly.FromDateTime(DateTime.UtcNow), new TimeOnly(9, 0));
            _punchRepo.Setup(r => r.GetByIdAsync("p", _ct)).ReturnsAsync(punch);
            var policy = new Policy(punch.Date.Year, new TimeOnly(9, 0), new TimeOnly(17, 0),
                                     TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromMinutes(30), TimeSpan.FromHours(2), 1m, GetValidQuotas());
            _policySvc.Setup(s => s.GetByYearAsync(punch.Date.Year, _ct)).ReturnsAsync(policy);

            var bs = new BreakSession("p", new TimeOnly(10, 0));
            _breakRepo.Setup(r => r.GetByIdAsync(bs.Id, _ct)).ReturnsAsync(bs);
            _breakRepo.Setup(r => r.ListByPunchRecordAsync("p", _ct)).ReturnsAsync(new[] { bs });

            _mapper.Setup(m => m.Map<BreakSessionDto>(It.IsAny<BreakSession>()))
                   .Returns((BreakSession x) => new BreakSessionDto(x.Id, x.PunchRecordId, x.StartTime, x.EndTime, x.Duration, x.IsNonCompliant));

            // Act
            var dto = await _service.EndAsync("p", bs.Id, new TimeOnly(10, 15), _ct);

            // Assert
            Assert.Equal(TimeOnly.Parse("10:15"), dto.EndTime);
            _breakRepo.Verify(r => r.UpdateAsync(bs, false, _ct), Times.Once);
        }

        [Fact]
        public async Task EndAsync_BreakNotFound_Throws()
        {
            var punch = new PunchRecord("u", DateOnly.FromDateTime(DateTime.UtcNow), new TimeOnly(9, 0));
            _punchRepo.Setup(r => r.GetByIdAsync("p", _ct)).ReturnsAsync(punch);
            _breakRepo.Setup(r => r.GetByIdAsync("no", _ct)).ReturnsAsync((BreakSession?)null);
            await Assert.ThrowsAsync<DomainException>(() => _service.EndAsync("p", "no", new TimeOnly(0, 0), _ct));
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsMappedDtoOrNull()
        {
            var bs = new BreakSession("p", new TimeOnly(12, 0));
            _breakRepo.Setup(r => r.GetByIdAsync("x", _ct)).ReturnsAsync(bs);
            _mapper.Setup(m => m.Map<BreakSessionDto>(bs))
                   .Returns(new BreakSessionDto(bs.Id, bs.PunchRecordId, bs.StartTime, bs.EndTime, bs.Duration, bs.IsNonCompliant));

            var some = await _service.GetByIdAsync("x", _ct);
            Assert.NotNull(some);

            var none = await _service.GetByIdAsync("no", _ct);
            Assert.Null(none);
        }

        [Fact]
        public async Task ListByPunchRecordAsync_MapsAll()
        {
            var list = new List<BreakSession>
            {
                new BreakSession("p",new TimeOnly(9,0)),
                new BreakSession("p",new TimeOnly(9,5))
            };
            _breakRepo.Setup(r => r.ListByPunchRecordAsync("p", _ct)).ReturnsAsync(list);
            _mapper.Setup(m => m.Map<BreakSessionDto>(It.IsAny<BreakSession>()))
                   .Returns<BreakSession>(bs => new BreakSessionDto(bs.Id, bs.PunchRecordId, bs.StartTime, bs.EndTime, bs.Duration, bs.IsNonCompliant));

            var dtos = await _service.ListByPunchRecordAsync("p", _ct);
            Assert.Equal(2, dtos.Count);
        }

        [Fact]
        public async Task UpdateAsync_CallsRepo()
        {
            var bs = new BreakSession("p", new TimeOnly(9, 0));
            await _service.UpdateAsync(bs, _ct);
            _breakRepo.Verify(r => r.UpdateAsync(bs, false, _ct), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_CallsRepo()
        {
            await _service.DeleteAsync("b", _ct);
            _breakRepo.Verify(r => r.DeleteAsync("b", _ct), Times.Once);
        }
    }
}
