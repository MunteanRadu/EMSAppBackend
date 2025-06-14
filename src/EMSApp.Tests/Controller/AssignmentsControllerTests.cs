using AutoMapper;
using EMSApp.Api;
using EMSApp.Application;
using EMSApp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Controller")]
    public class AssignmentsControllerTests
    {
        private readonly Mock<IAssignmentService> _svc = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly AssignmentsController _ctrl;
        private readonly CancellationToken _ct = CancellationToken.None;

        public AssignmentsControllerTests()
        {
            _ctrl = new AssignmentsController(_svc.Object, _mapper.Object);
        }

        [Fact]
        public async Task Create_ReturnsCreatedAtAction_WithDto()
        {
            // Arrange
            var req = new CreateAssignmentRequest(
                Title: "T1",
                Description: "D1",
                DueDate: DateTime.Today.AddDays(5),
                DepartmentId: "dept1",
                ManagerId: "mgr1"
            );
            var entity = new Assignment(req.Title, req.Description, req.DueDate, req.DepartmentId, req.ManagerId);
            var dto = new AssignmentDto(
                Id: entity.Id,
                Title: entity.Title,
                Description: entity.Description,
                DueDate: entity.DueDate,
                DepartmentId: entity.DepartmentId,
                AssignedToId: entity.AssignedToId,
                Status: entity.Status.ToString()
            );

            _svc
              .Setup(s => s.CreateAsync(req.Title, req.Description, req.DueDate, req.DepartmentId, req.ManagerId, _ct))
              .ReturnsAsync(entity);

            _mapper
              .Setup(m => m.Map<AssignmentDto>(entity))
              .Returns(dto);

            // Act
            var action = await _ctrl.Create(req, _ct);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(action.Result);
            Assert.Equal(nameof(_ctrl.GetById), created.ActionName);
            Assert.Equal(dto, created.Value);

            _svc.Verify(s => s.CreateAsync(req.Title, req.Description, req.DueDate, req.DepartmentId, req.ManagerId, _ct), Times.Once);
            _mapper.Verify(m => m.Map<AssignmentDto>(entity), Times.Once);
        }

        [Fact]
        public async Task GetById_WhenFound_ReturnsDto()
        {
            // Arrange
            var entity = new Assignment("T2", "D2", DateTime.Today.AddDays(5), "dept2", "mgr2");
            var dto = new AssignmentDto(
                Id: entity.Id,
                Title: entity.Title,
                Description: entity.Description,
                DueDate: entity.DueDate,
                DepartmentId: entity.DepartmentId,
                AssignedToId: entity.AssignedToId,
                Status: entity.Status.ToString()
            );

            _svc
              .Setup(s => s.GetByIdAsync(entity.Id, _ct))
              .ReturnsAsync(entity);

            _mapper
              .Setup(m => m.Map<AssignmentDto>(entity))
              .Returns(dto);

            // Act
            var result = await _ctrl.GetById(entity.Id, _ct);

            // Assert
            Assert.Equal(dto, result.Value);
            _svc.Verify(s => s.GetByIdAsync(entity.Id, _ct), Times.Once);
            _mapper.Verify(m => m.Map<AssignmentDto>(entity), Times.Once);
        }

        [Fact]
        public async Task GetById_WhenNotFound_Returns404()
        {
            // Arrange
            _svc
              .Setup(s => s.GetByIdAsync("missing", _ct))
              .ReturnsAsync((Assignment?)null);

            // Act
            var result = await _ctrl.GetById("missing", _ct);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task ListAll_ReturnsOkWithAllDtos()
        {
            // Arrange
            var list = new List<Assignment>
            {
                new Assignment("T3", "D3", DateTime.Today, "dept3", "mgr3")
            };
            var dtos = list.Select(a => new AssignmentDto(
                Id: a.Id,
                Title: a.Title,
                Description: a.Description,
                DueDate: a.DueDate,
                DepartmentId: a.DepartmentId,
                AssignedToId: a.AssignedToId,
                Status: a.Status.ToString()
            )).ToList();

            _svc
              .Setup(s => s.GetAllAsync(_ct))
              .ReturnsAsync(list);

            _mapper
              .Setup(m => m.Map<IEnumerable<AssignmentDto>>(list))
              .Returns(dtos);

            // Act
            var action = await _ctrl.ListAll(_ct);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            Assert.Equal(dtos, ok.Value);
            _svc.Verify(s => s.GetAllAsync(_ct), Times.Once);
            _mapper.Verify(m => m.Map<IEnumerable<AssignmentDto>>(list), Times.Once);
        }

        [Fact]
        public async Task ListBySomething_ByAssignee_ReturnsOk()
        {
            // Arrange
            var a = new Assignment("T4", "D4", DateTime.Today, "dept4", "mgr4");
            a.Start("userX");
            var list = new List<Assignment> { a };
            var dtos = list.Select(a => new AssignmentDto(
                Id: a.Id,
                Title: a.Title,
                Description: a.Description,
                DueDate: a.DueDate,
                DepartmentId: a.DepartmentId,
                AssignedToId: "userX",
                Status: a.Status.ToString()
            )).ToList();

            _svc
              .Setup(s => s.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Assignment, bool>>>(), _ct))
              .ReturnsAsync(list);

            _mapper
              .Setup(m => m.Map<AssignmentDto>(list[0]))
              .Returns(dtos[0]);

            // Act
            var action = await _ctrl.ListBySomething(userId: "userX", asOf: null, status: null, departmentId: null, ct: _ct);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<AssignmentDto>>(ok.Value);
            Assert.Equal(dtos, returned.ToList());
            _svc.Verify(s => s.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Assignment, bool>>>(), _ct), Times.Once);
        }

        [Fact]
        public async Task StartAssignment_WhenFound_ReturnsOkDto()
        {
            // Arrange
            var entity = new Assignment("T5", "D5", DateTime.Today, "dept5", "mgr5");
            var dto = new AssignmentDto(
                Id: entity.Id,
                Title: entity.Title,
                Description: entity.Description,
                DueDate: entity.DueDate,
                DepartmentId: entity.DepartmentId,
                AssignedToId: "userY",
                Status: "InProgress"
            );
            _svc.Setup(s => s.GetByIdAsync(entity.Id, _ct)).ReturnsAsync(entity);
            _mapper.Setup(m => m.Map<AssignmentDto>(entity)).Returns(dto);

            // Act
            var result = await _ctrl.StartAssignment(entity.Id, "userY", _ct);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, ok.Value);
            _svc.Verify(s => s.UpdateAsync(entity, _ct), Times.Once);
        }

        [Fact]
        public async Task CompleteAssignment_WhenNotFound_Returns404()
        {
            // Arrange
            _svc.Setup(s => s.GetByIdAsync("no", _ct)).ReturnsAsync((Assignment?)null);

            // Act
            var result = await _ctrl.CompleteAssignment("no", _ct);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task RejectAssignment_WhenFound_InvokesService()
        {
            // Arrange
            var entity = new Assignment("T6", "D6", DateTime.Today, "dept6", "mgr6");
            entity.Start("userX");
            entity.Complete();
            _svc.Setup(s => s.GetByIdAsync(entity.Id, _ct)).ReturnsAsync(entity);
            _mapper.Setup(m => m.Map<AssignmentDto>(entity)).Returns(new AssignmentDto(
                entity.Id, entity.Title, entity.Description, entity.DueDate,
                entity.DepartmentId, entity.AssignedToId, entity.Status.ToString()));

            // Act
            var result = await _ctrl.RejectAssignment(entity.Id, _ct);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            _svc.Verify(s => s.UpdateAsync(entity, _ct), Times.Once);
        }

        [Fact]
        public async Task Update_WhenNotFound_Returns404()
        {
            // Arrange
            _svc.Setup(s => s.GetByIdAsync("missing", _ct)).ReturnsAsync((Assignment?)null);

            // Act
            var result = await _ctrl.Update("missing", new UpdateAssignmentRequest("title", "desc", DateTime.Today.AddDays(5), "userX", AssignmentStatus.InProgress), _ct);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_CallsService_AndReturnsNoContent()
        {
            // Act
            var result = await _ctrl.Delete("any", _ct);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _svc.Verify(s => s.DeleteAsync("any", _ct), Times.Once);
        }
    }
}
