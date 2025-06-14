using AutoMapper;
using EMSApp.Api.Controllers;
using EMSApp.Api;
using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using static System.Collections.Specialized.BitVector32;

namespace EMSApp.Tests
{
    [Trait("Category", "Controller")]
    public class PunchRecordsControllerTests
    {
        private readonly Mock<IPunchRecordService> _svc = new();
        private readonly Mock<ILeaveRequestService> _leaveSvc = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly PunchRecordsController _ctrl;
        private readonly CancellationToken _ct = CancellationToken.None;

        public PunchRecordsControllerTests()
        {
            _ctrl = new PunchRecordsController(_svc.Object, _leaveSvc.Object, _mapper.Object);
        }

        [Fact]
        public async Task Create_Success_ReturnsCreatedDto()
        {
            var req = new CreatePunchRecordRequest("u1", DateOnly.Parse("2025-05-01"), TimeOnly.Parse("09:00"));
            var pr = new PunchRecord("u1", req.Date, req.TimeIn);
            var dto = new PunchRecordDto(pr.Id, pr.UserId, pr.Date, pr.TimeIn, null, null, false);

            _svc.Setup(s => s.CreateAsync(req.UserId, req.Date, req.TimeIn, _ct))
                .ReturnsAsync(pr);
            _mapper.Setup(m => m.Map<PunchRecordDto>(pr)).Returns(dto);

            var action = await _ctrl.Create(req, _ct);
            var created = Assert.IsType<CreatedAtActionResult>(action.Result);

            Assert.Equal(nameof(_ctrl.GetById), created.ActionName);
            Assert.Same(dto, created.Value);
        }

        [Fact]
        public async Task Create_DomainError_ReturnsBadRequest()
        {
            var req = new CreatePunchRecordRequest("u1", default, TimeOnly.Parse("09:00"));
            _svc.Setup(s => s.CreateAsync(req.UserId, It.IsAny<DateOnly>(), req.TimeIn, _ct))
                .ThrowsAsync(new DomainException("oops"));

            var action = await _ctrl.Create(req, _ct);
            var bad = Assert.IsType<BadRequestObjectResult>(action.Result);

            Assert.Contains("oops", bad.Value as string);
        }

        [Fact]
        public async Task PunchOut_Success_ReturnsOkDto()
        {
            var dto = new PunchRecordDto("id", "u", DateOnly.FromDateTime(DateTime.Today), TimeOnly.Parse("08:00"), TimeOnly.Parse("17:00"), TimeSpan.FromHours(9), false);
            _svc.Setup(s => s.PunchOutAsync("id", dto.TimeOut!.Value, _ct))
                .ReturnsAsync(dto);

            var req = new UpdatePunchRecordRequest(dto.Date, dto.TimeIn, dto.TimeOut!.Value);
            var ok = await _ctrl.PunchOut("id", req, _ct) as OkObjectResult;

            Assert.Equal(dto, ok.Value);
        }

        [Fact]
        public async Task PunchOut_NotFound_Returns404()
        {
            _svc.Setup(s => s.PunchOutAsync("bad", It.IsAny<TimeOnly>(), _ct))
                .ThrowsAsync(new KeyNotFoundException());

            var req = new UpdatePunchRecordRequest(DateOnly.FromDateTime(DateTime.Today),TimeOnly.Parse("09:00"), TimeOnly.Parse("17:00"));
            Assert.IsType<NotFoundResult>(await _ctrl.PunchOut("bad", req, _ct));
        }

        [Fact]
        public async Task PunchOut_DomainError_ReturnsBadRequest()
        {
            _svc.Setup(s => s.PunchOutAsync("id", It.IsAny<TimeOnly>(), _ct))
                .ThrowsAsync(new DomainException("late"));

            var req = new UpdatePunchRecordRequest(DateOnly.FromDateTime(DateTime.Today), TimeOnly.Parse("08:00"), TimeOnly.Parse("23:00"));
            var bad = await _ctrl.PunchOut("id", req, _ct) as BadRequestObjectResult;

            Assert.Contains("late", bad.Value as string);
        }

        [Fact]
        public async Task GetById_Found_ReturnsDto()
        {
            var pr = new PunchRecord("u", DateOnly.Parse("2025-05-02"), TimeOnly.Parse("10:00"));
            var dto = new PunchRecordDto(pr.Id, pr.UserId, pr.Date, pr.TimeIn, null, null, false);
            _svc.Setup(s => s.GetByIdAsync(pr.Id, _ct)).ReturnsAsync(dto);
            _mapper.Setup(m => m.Map<PunchRecordDto>(dto)).Returns(dto);

            var result = await _ctrl.GetById(pr.Id, _ct);
            Assert.Equal(dto, result.Value);
        }

        [Fact]
        public async Task GetById_NotFound_Returns404()
        {
            _svc.Setup(s => s.GetByIdAsync("no", _ct)).ReturnsAsync((PunchRecordDto?)null);

            var action = await _ctrl.GetById("no", _ct);
            Assert.IsType<NotFoundResult>(action.Result);
        }

        [Fact]
        public async Task ListByUser_WithParam_CallsListByUser()
        {
            var list = new[] { new PunchRecord("u", DateOnly.FromDateTime(DateTime.Today), TimeOnly.Parse("08:00")) };
            var dtoList = new[] { new PunchRecordDto(list[0].Id, "u", list[0].Date, list[0].TimeIn, null, null, false) };
            _svc.Setup(s => s.ListByUserAsync("u", _ct)).ReturnsAsync(list);
            _mapper.Setup(m => m.Map<IEnumerable<PunchRecordDto>>(list)).Returns(dtoList);

            ActionResult<IEnumerable<PunchRecordDto>> action = await _ctrl.ListByUser("u", _ct);

            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<PunchRecordDto>>(ok.Value);
            Assert.Equal(dtoList, returned);
        }

        [Fact]
        public async Task ListByUser_NoParam_CallsGetAll()
        {
            var list = Array.Empty<PunchRecord>();
            var dtoList = Array.Empty<PunchRecordDto>();
            _svc.Setup(s => s.GetAllAsync(_ct)).ReturnsAsync(list);
            _mapper.Setup(m => m.Map<IEnumerable<PunchRecordDto>>(list)).Returns(dtoList);

            ActionResult<IEnumerable<PunchRecordDto>> action = await _ctrl.ListAll(_ct);

            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<PunchRecordDto>>(ok.Value);
            Assert.Equal(dtoList, returned);
        }

        [Fact]
        public async Task ListAll_CallsGetAll()
        {
            var list = Array.Empty<PunchRecord>();
            var dtoList = Array.Empty<PunchRecordDto>();
            _svc.Setup(s => s.GetAllAsync(_ct)).ReturnsAsync(list);
            _mapper.Setup(m => m.Map<IEnumerable<PunchRecordDto>>(list)).Returns(dtoList);

            ActionResult<IEnumerable<PunchRecordDto>> action = await _ctrl.ListAll(_ct);

            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<PunchRecordDto>>(ok.Value);
            Assert.Equal(dtoList, returned);
        }

        [Fact]
        public async Task MonthSummary_ReturnsList()
        {
            var summary = new[] { new DaySummaryDto(DateOnly.FromDateTime(DateTime.Today), true) };
            _svc.Setup(s => s.GetMonthSummaryAsync("u", 2025, 5, _ct)).ReturnsAsync(summary);

            var ok = await _ctrl.MonthSummary("u", 2025, 5, _ct) as OkObjectResult;
            Assert.Equal(summary, ok.Value);
        }

        [Fact]
        public async Task ListByDate_ReturnsDtos()
        {
            var date = DateOnly.FromDateTime(DateTime.Today);
            var dtoList = new[]
            {
                new PunchRecordDto("id", "u", date, TimeOnly.Parse("08:00"), null, null, false)
            };

            _svc.Setup(s => s.GetByDateAsync("u", date, _ct)).ReturnsAsync(dtoList);
            var action = await _ctrl.ListByDate("u", date, _ct);

            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var payload = Assert.IsAssignableFrom<IEnumerable<PunchRecordDto>>(ok.Value);
            Assert.Equal(dtoList, payload);
        }

        [Fact]
        public async Task Update_Success_ReturnsNoContent()
        {
            var pr = new PunchRecord("u", DateOnly.Parse("2025-05-03"), TimeOnly.Parse("11:00"));
            var req = new UpdatePunchRecordRequest(pr.Date, TimeOnly.Parse("17:00"), pr.TimeIn);
            _svc.Setup(s => s.GetByIdForUpdateAsync(pr.Id, _ct)).ReturnsAsync(pr);

            var r = await _ctrl.Update(pr.Id, req, _ct);
            Assert.IsType<NoContentResult>(r);
            _svc.Verify(s => s.UpdateAsync(pr, _ct), Times.Once);
        }

        [Fact]
        public async Task Update_NotFound_Returns404()
        {
            _svc.Setup(s => s.GetByIdForUpdateAsync("no", _ct)).ReturnsAsync((PunchRecord?)null);
            Assert.IsType<NotFoundResult>(await _ctrl.Update("no", new UpdatePunchRecordRequest(DateOnly.FromDateTime(DateTime.Today), TimeOnly.Parse("08:00"), TimeOnly.Parse("16:00")), _ct));
        }

        [Fact]
        public async Task Delete_CallsService()
        {
            var r = await _ctrl.Delete("x", _ct);
            Assert.IsType<NoContentResult>(r);
            _svc.Verify(s => s.DeleteAsync("x", _ct), Times.Once);
        }
    }
}
