using AutoMapper;
using EMSApp.Api.Controllers;
using EMSApp.Api;
using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Controller")]
    public class AssignmentFeedbacksControllerTests
    {
        private readonly Mock<IAssignmentFeedbackService> _svc = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly AssignmentFeedbacksController _ctrl;
        private readonly CancellationToken _ct = CancellationToken.None;

        public AssignmentFeedbacksControllerTests()
        {
            _ctrl = new AssignmentFeedbacksController(_svc.Object, _mapper.Object);
        }

        [Fact]
        public async Task Create_ReturnsCreatedAtAction_WithDto()
        {
            // Arrange
            var req = new CreateAssignmentFeedbackRequest(
                AssignmentId: "assign1",
                UserId: "user1",
                Text: "Great job",
                Type: FeedbackType.Manager
            );
            var entity = new AssignmentFeedback(req.AssignmentId, req.UserId, req.Text, req.Type);
            var dto = new AssignmentFeedbackDto(
                Id: entity.Id,
                AssignmentId: entity.AssignmentId,
                UserId: entity.UserId,
                Text: entity.Text,
                TimeStamp: entity.TimeStamp,
                Type: entity.Type
            );

            _svc
              .Setup(s => s.CreateAsync(req.AssignmentId, req.UserId, req.Text, req.Type, _ct))
              .ReturnsAsync(entity);

            _mapper
              .Setup(m => m.Map<AssignmentFeedbackDto>(entity))
              .Returns(dto);

            // Act
            var action = await _ctrl.Create(req, _ct);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(action.Result);
            Assert.Equal(nameof(_ctrl.GetById), created.ActionName);
            Assert.Equal(dto, created.Value);

            _svc.Verify(s => s.CreateAsync(req.AssignmentId, req.UserId, req.Text, req.Type, _ct), Times.Once);
            _mapper.Verify(m => m.Map<AssignmentFeedbackDto>(entity), Times.Once);
        }

        [Fact]
        public async Task GetById_WhenFound_ReturnsDto()
        {
            // Arrange
            var entity = new AssignmentFeedback("assign2", "user2", "Nice work", FeedbackType.Employee);
            var dto = new AssignmentFeedbackDto(
                Id: entity.Id,
                AssignmentId: entity.AssignmentId,
                UserId: entity.UserId,
                Text: entity.Text,
                TimeStamp: entity.TimeStamp,
                Type: entity.Type
            );

            _svc
              .Setup(s => s.GetByIdAsync(entity.Id, _ct))
              .ReturnsAsync(entity);

            _mapper
              .Setup(m => m.Map<AssignmentFeedbackDto>(entity))
              .Returns(dto);

            // Act
            var result = await _ctrl.GetById(entity.Id, _ct);

            // Assert
            Assert.Equal(dto, result.Value);
            _svc.Verify(s => s.GetByIdAsync(entity.Id, _ct), Times.Once);
            _mapper.Verify(m => m.Map<AssignmentFeedbackDto>(entity), Times.Once);
        }

        [Fact]
        public async Task GetById_WhenNotFound_Returns404()
        {
            // Arrange
            _svc
              .Setup(s => s.GetByIdAsync("missing", _ct))
              .ReturnsAsync((AssignmentFeedback?)null);

            // Act
            var result = await _ctrl.GetById("missing", _ct);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task List_ReturnsOkWithAllDtos()
        {
            // Arrange
            var list = new List<AssignmentFeedback>
            {
                new AssignmentFeedback("assign3", "user3", "Well done", FeedbackType.Employee)
            };
            var dtos = new List<AssignmentFeedbackDto>
            {
                new AssignmentFeedbackDto(
                    Id: list[0].Id,
                    AssignmentId: list[0].AssignmentId,
                    UserId: list[0].UserId,
                    Text: list[0].Text,
                    TimeStamp: list[0].TimeStamp,
                    Type: list[0].Type
                )
            };

            _svc
              .Setup(s => s.GetAllAsync(_ct))
              .ReturnsAsync(list);

            _mapper
              .Setup(m => m.Map<IEnumerable<AssignmentFeedbackDto>>(list))
              .Returns(dtos);

            // Act
            var action = await _ctrl.List(_ct);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            Assert.Equal(dtos, ok.Value);
            _svc.Verify(s => s.GetAllAsync(_ct), Times.Once);
            _mapper.Verify(m => m.Map<IEnumerable<AssignmentFeedbackDto>>(list), Times.Once);
        }

        [Fact]
        public async Task ListByAssignment_ReturnsOkWithDtos()
        {
            // Arrange
            var list = new List<AssignmentFeedback>
            {
                new AssignmentFeedback("assign4", "user4", "Keep it up", FeedbackType.Manager)
            };
            var dtos = new List<AssignmentFeedbackDto>
            {
                new AssignmentFeedbackDto(
                    Id: list[0].Id,
                    AssignmentId: list[0].AssignmentId,
                    UserId: list[0].UserId,
                    Text: list[0].Text,
                    TimeStamp: list[0].TimeStamp,
                    Type: list[0].Type
                )
            };

            _svc
              .Setup(s => s.ListByAssignmentAsync("assign4", _ct))
              .ReturnsAsync(list);

            _mapper
              .Setup(m => m.Map<IEnumerable<AssignmentFeedbackDto>>(list))
              .Returns(dtos);

            // Act
            var action = await _ctrl.ListByAssignment("assign4", _ct);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            Assert.Equal(dtos, ok.Value);
            _svc.Verify(s => s.ListByAssignmentAsync("assign4", _ct), Times.Once);
            _mapper.Verify(m => m.Map<IEnumerable<AssignmentFeedbackDto>>(list), Times.Once);
        }

        [Fact]
        public async Task Update_WhenFound_CallsServiceAndReturnsNoContent()
        {
            // Arrange
            var entity = new AssignmentFeedback("assign5", "user5", "Original", FeedbackType.Employee);
            var req = new UpdateAssignmentFeedbackRequest(Text: "Updated text");
            _svc
              .Setup(s => s.GetByIdAsync(entity.Id, _ct))
              .ReturnsAsync(entity);

            // Act
            var result = await _ctrl.Update(entity.Id, req, _ct);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _svc.Verify(s => s.UpdateAsync(entity, _ct), Times.Once);
        }

        [Fact]
        public async Task Update_WhenNotFound_Returns404()
        {
            // Arrange
            _svc
              .Setup(s => s.GetByIdAsync("missing", _ct))
              .ReturnsAsync((AssignmentFeedback?)null);

            // Act
            var result = await _ctrl.Update("missing", new UpdateAssignmentFeedbackRequest(Text: "x"), _ct);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_CallsService_AndReturnsNoContent()
        {
            // Act
            var result = await _ctrl.Delete("feedbackId", _ct);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _svc.Verify(s => s.DeleteAsync("feedbackId", _ct), Times.Once);
        }
    }
}
