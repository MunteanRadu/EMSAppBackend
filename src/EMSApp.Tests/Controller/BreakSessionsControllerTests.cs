using AutoMapper;
using EMSApp.Api.Controllers;
using EMSApp.Api;
using EMSApp.Application;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using EMSApp.Domain;

namespace EMSApp.Tests
{
    [Trait("Category", "Controller")]
    public class BreakSessionsControllerTests
    {
        private const string PunchId = "pr1";
        private readonly Mock<IBreakSessionService> _svc = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly BreakSessionsController _ctrl;
        private readonly CancellationToken _ct = CancellationToken.None;

        public BreakSessionsControllerTests()
        {
            _ctrl = new BreakSessionsController(_svc.Object, _mapper.Object);
        }

        [Fact]
        public async Task Create_Post_ReturnsCreatedAtAction_WithDto()
        {
            // Arrange
            var req = new CreateBreakSessionRequest(StartTime: TimeOnly.Parse("12:00"));
            var bs = new BreakSession(PunchId, TimeOnly.Parse("12:00"));
            var dto = new BreakSessionDto(
                bs.Id,
                bs.PunchRecordId,
                bs.StartTime,
                bs.EndTime,
                bs.Duration,
                bs.IsNonCompliant);

            _svc.Setup(s => s.CreateAsync(PunchId, req.StartTime, _ct)).ReturnsAsync(dto);
            _mapper.Setup(m => m.Map<BreakSessionDto>(dto)).Returns(dto);

            // Act
            var action = await _ctrl.Create(PunchId, req, _ct);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(action.Result);
            Assert.Equal(nameof(_ctrl.GetById), created.ActionName);

            // route values
            Assert.Equal(PunchId, created.RouteValues["punchId"]);
            Assert.Equal(bs.Id, created.RouteValues["id"]);

            Assert.Equal(dto, created.Value);
            _svc.Verify(s => s.CreateAsync(PunchId, req.StartTime, _ct), Times.Once);
        }

        [Fact]
        public async Task End_Patch_ReturnsOk_WhenFound()
        {
            // Arrange
            var req = new UpdateBreakSessionRequest(EndTime: TimeOnly.Parse("12:30"));
            var dto = new BreakSessionDto(
                Id: "bs1",
                PunchRecordId: PunchId,
                StartTime: TimeOnly.Parse("12:00"),
                EndTime: req.EndTime,
                Duration: TimeSpan.FromMinutes(30),
                IsNonCompliant: false);

            _svc
                .Setup(s => s.EndAsync(PunchId, dto.Id, req.EndTime, _ct))
                .ReturnsAsync(dto);

            // Act
            var action = await _ctrl.End(PunchId, dto.Id, req, _ct);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(action);
            Assert.Equal(dto, ok.Value);
            _svc.Verify(s => s.EndAsync(PunchId, dto.Id, req.EndTime, _ct), Times.Once);
        }

        [Fact]
        public async Task End_Patch_ReturnsNotFound_WhenServiceReturnsNull()
        {
            // Arrange
            var req = new UpdateBreakSessionRequest(EndTime: TimeOnly.Parse("12:30"));
            _svc
                .Setup(s => s.EndAsync(PunchId, "bad-id", req.EndTime, _ct))
                .ReturnsAsync((BreakSessionDto?)null);

            // Act
            var action = await _ctrl.End(PunchId, "bad-id", req, _ct);

            // Assert
            Assert.IsType<NotFoundResult>(action);
        }

        [Fact]
        public async Task GetById_Get_ReturnsDto_WhenMatch()
        {
            // Arrange
            var bs = new BreakSession(PunchId, TimeOnly.Parse("13:00"));
            var dto = new BreakSessionDto(
                bs.Id,
                bs.PunchRecordId,
                bs.StartTime,
                bs.EndTime,
                bs.Duration,
                bs.IsNonCompliant);

            _svc.Setup(s => s.GetByIdAsync(bs.Id, _ct)).ReturnsAsync(dto);
            _mapper.Setup(m => m.Map<BreakSessionDto>(dto)).Returns(dto);

            // Act
            var action = await _ctrl.GetById(PunchId, bs.Id, _ct);

            // Assert
            Assert.Equal(dto, action.Value);
            _svc.Verify(s => s.GetByIdAsync(bs.Id, _ct), Times.Once);
        }

        [Fact]
        public async Task GetById_Get_ReturnsNotFound_WhenNullOrMismatch()
        {
            var badDto = new BreakSessionDto(
                Id: "bs1",
                PunchRecordId: "otherPunch",
                StartTime: TimeOnly.Parse("14:00"),
                EndTime: null,
                Duration: null,
                IsNonCompliant: false);
            _svc.Setup(s => s.GetByIdAsync(badDto.Id, _ct)).ReturnsAsync(badDto);

            // Act
            var action = await _ctrl.GetById(PunchId, badDto.Id, _ct);

            // Assert: wrong punchId route
            Assert.IsType<NotFoundResult>(action.Result);

            // Arrange: service returns null
            _svc.Setup(s => s.GetByIdAsync("nope", _ct)).ReturnsAsync((BreakSessionDto?)null);
            var action2 = await _ctrl.GetById(PunchId, "nope", _ct);
            Assert.IsType<NotFoundResult>(action2.Result);
        }

        [Fact]
        public async Task List_Get_ReturnsOkObject_WithDtos()
        {
            // Arrange
            var dto1 = new BreakSessionDto(
                Id: "bs1",
                PunchRecordId: PunchId,
                StartTime: TimeOnly.Parse("08:00"),
                EndTime: null,
                Duration: null,
                IsNonCompliant: false);
            var dto2 = new BreakSessionDto(
                Id: "bs2",
                PunchRecordId: PunchId,
                StartTime: TimeOnly.Parse("09:00"),
                EndTime: null,
                Duration: null,
                IsNonCompliant: false);
            var dtoList = new List<BreakSessionDto> { dto1, dto2 };

            _svc.Setup(s => s.ListByPunchRecordAsync(PunchId, _ct)).ReturnsAsync(dtoList);
            _mapper.Setup(m => m.Map<IEnumerable<BreakSessionDto>>(dtoList)).Returns(dtoList);

            // Act
            var action = await _ctrl.List(PunchId, _ct);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            Assert.Equal(dtoList, ok.Value);
            _svc.Verify(s => s.ListByPunchRecordAsync(PunchId, _ct), Times.Once);
        }

        [Fact]
        public async Task Delete_Delete_ReturnsNoContent_AndCallsService()
        {
            // Act
            var result = await _ctrl.Delete("bs1", _ct);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _svc.Verify(s => s.DeleteAsync("bs1", _ct), Times.Once);
        }
    }
}
