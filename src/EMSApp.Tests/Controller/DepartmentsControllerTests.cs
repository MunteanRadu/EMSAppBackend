using AutoMapper;
using EMSApp.Api.Controllers;
using EMSApp.Api;
using EMSApp.Application;
using EMSApp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Controller")]
    public class DepartmentsControllerTests
    {
        private readonly Mock<IDepartmentService> _svc = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly DepartmentsController _ctrl;
        private readonly CancellationToken _ct = CancellationToken.None;

        public DepartmentsControllerTests()
        {
            _ctrl = new DepartmentsController(_svc.Object, _mapper.Object);
        }

        [Fact]
        public async Task Create_ReturnsCreatedAtActionResult_WithDto()
        {
            // Arrange
            var req = new CreateDepartmentRequest(Name: "HR");
            var dept = new Department("HR");
            var dto = new DepartmentDto(dept.Id, "HR", "mngr", new List<string>());

            _svc
                .Setup(s => s.CreateAsync(req.Name, _ct))
                .ReturnsAsync(dept);

            _mapper
                .Setup(m => m.Map<DepartmentDto>(dept))
                .Returns(dto);

            // Act
            var action = await _ctrl.Create(req, _ct);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(action.Result);
            Assert.Equal(nameof(_ctrl.GetById), created.ActionName);
            Assert.Equal(dto, created.Value);

            _svc.Verify(s => s.CreateAsync("HR", _ct), Times.Once);
            _mapper.Verify(m => m.Map<DepartmentDto>(dept), Times.Once);
        }

        [Fact]
        public async Task GetById_Existing_ReturnsDto()
        {
            // Arrange
            var dept = new Department("IT");
            var dto = new DepartmentDto(dept.Id, "IT", "mngr", new List<string>());

            _svc.Setup(s => s.GetByIdAsync(dept.Id, _ct)).ReturnsAsync(dept);
            _mapper.Setup(m => m.Map<DepartmentDto>(dept)).Returns(dto);

            // Act
            var action = await _ctrl.GetById(dept.Id, _ct);

            // Assert
            Assert.Equal(dto, action.Value);
            _svc.Verify(s => s.GetByIdAsync(dept.Id, _ct), Times.Once);
        }

        [Fact]
        public async Task GetById_NotFound_ReturnsNotFound()
        {
            // Arrange
            _svc.Setup(s => s.GetByIdAsync("nope", _ct)).ReturnsAsync((Department?)null);

            // Act
            var action = await _ctrl.GetById("nope", _ct);

            // Assert
            Assert.IsType<NotFoundResult>(action.Result);
        }

        [Fact]
        public async Task List_ReturnsOkObjectResult_WithDtos()
        {
            // Arrange
            var list = new List<Department>
            {
                new Department("Ops"),
                new Department("Sales")
            };
            var dtos = new List<DepartmentDto>
            {
                new DepartmentDto(list[0].Id, "Ops", "mngr",new List<string>()),
                new DepartmentDto(list[1].Id, "Sales", "mngr", new List<string>())
            };

            _svc.Setup(s => s.GetAllAsync(_ct)).ReturnsAsync(list);
            _mapper.Setup(m => m.Map<IEnumerable<DepartmentDto>>(list)).Returns(dtos);

            // Act
            var action = await _ctrl.List(_ct);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var body = Assert.IsAssignableFrom<IEnumerable<DepartmentDto>>(ok.Value);
            Assert.Equal(2, body.Count());
            _svc.Verify(s => s.GetAllAsync(_ct), Times.Once);
            _mapper.Verify(m => m.Map<IEnumerable<DepartmentDto>>(list), Times.Once);
        }

        [Fact]
        public async Task Update_Existing_CallsServiceAndReturnsNoContent()
        {
            // Arrange
            var dept = new Department("Support");
            var req = new UpdateDepartmentRequest(Name: "Support HQ", ManagerId: null);

            _svc.Setup(s => s.GetByIdAsync(dept.Id, _ct)).ReturnsAsync(dept);

            // Act
            var result = await _ctrl.Update(dept.Id, req, _ct);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal("Support HQ", dept.Name);
            _svc.Verify(s => s.UpdateAsync(dept, _ct), Times.Once);
        }

        [Fact]
        public async Task Update_NotFound_ReturnsNotFound()
        {
            // Arrange
            _svc.Setup(s => s.GetByIdAsync("nope", _ct)).ReturnsAsync((Department?)null);

            // Act
            var result = await _ctrl.Update("nope", new UpdateDepartmentRequest(Name: "X", ManagerId: null), _ct);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Always_CallsServiceAndReturnsNoContent()
        {
            // Act
            var result = await _ctrl.Delete("dept123", _ct);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _svc.Verify(s => s.DeleteAsync("dept123", _ct), Times.Once);
        }

        [Fact]
        public async Task AddEmployee_Always_CallsServiceAndReturnsNoContent()
        {
            // Act
            var result = await _ctrl.AddEmployee("dept1", new AddDepartmentEmployeeRequest(UserId: "u1"), _ct);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _svc.Verify(s => s.AddEmployeeAsync("dept1", "u1", _ct), Times.Once);
        }

        [Fact]
        public async Task RemoveEmployee_Always_CallsServiceAndReturnsNoContent()
        {
            // Act
            var result = await _ctrl.RemoveEmployee("dept1", "u2", _ct);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _svc.Verify(s => s.RemoveEmployeeAsync("dept1", "u2", _ct), Times.Once);
        }
    }
}
