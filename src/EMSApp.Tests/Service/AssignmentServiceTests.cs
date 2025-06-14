using EMSApp.Application;
using EMSApp.Domain.Entities;
using EMSApp.Domain;
using EMSApp.Infrastructure;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Linq.Expressions;

namespace EMSApp.Tests
{
    [Trait("Category", "Service")]
    public class AssignmentServiceTests
    {
        private readonly Mock<IAssignmentRepository> _repo;
        private readonly IAssignmentService _service;
        private readonly CancellationToken _ct = CancellationToken.None;

        public AssignmentServiceTests()
        {
            _repo = new Mock<IAssignmentRepository>();
            _service = new AssignmentService(_repo.Object);
        }

        [Fact]
        public async Task CreateAsync_ValidData_CreatesAndReturns()
        {
            // Arrange
            var title = "Do Task";
            var desc = "Details";
            var due = DateTime.UtcNow.AddDays(5);
            var dept = "dept-1";
            var mgr = "mgr-1";

            // Act
            var assignment = await _service.CreateAsync(title, desc, due, dept, mgr, _ct);

            // Assert
            Assert.Equal(title, assignment.Title);
            Assert.Equal(desc, assignment.Description);
            Assert.Equal(due, assignment.DueDate);
            Assert.Equal(dept, assignment.DepartmentId);
            Assert.Equal(mgr, assignment.ManagerId);

            _repo.Verify(r => r.CreateAsync(
                It.Is<Assignment>(a =>
                    a.Title == title &&
                    a.Description == desc &&
                    a.DueDate == due &&
                    a.DepartmentId == dept &&
                    a.ManagerId == mgr),
                _ct),
                Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_Found_ReturnsAssignment()
        {
            var a = new Assignment("T", "D", DateTime.UtcNow.AddDays(1), "d", "m");
            _repo.Setup(r => r.GetByIdAsync(a.Id, _ct)).ReturnsAsync(a);

            var result = await _service.GetByIdAsync(a.Id, _ct);

            Assert.Same(a, result);
            _repo.Verify(r => r.GetByIdAsync(a.Id, _ct), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ReturnsNull()
        {
            _repo.Setup(r => r.GetByIdAsync("no", _ct)).ReturnsAsync((Assignment?)null);
            Assert.Null(await _service.GetByIdAsync("no", _ct));
        }

        [Fact]
        public async Task ListAsync_NullPredicate_CallsGetAll()
        {
            // Arrange
            var list = new List<Assignment>
            {
                new Assignment("T","D", DateTime.UtcNow.AddDays(1),"d","m")
            };
            _repo.Setup(r => r.ListAsync((Expression<Func<Assignment, bool>>?)null, _ct)).ReturnsAsync(list);

            // Act
            var result = await _service.ListAsync(null, _ct);

            // Assert
            Assert.Same(list, result);
            _repo.Verify(r => r.ListAsync((Expression<Func<Assignment, bool>>?)null, _ct), Times.Once);
        }


        [Fact]
        public async Task ListAsync_WithPredicate_CallsList()
        {
            // Arrange
            var filtered = new List<Assignment>();
            System.Linq.Expressions.Expression<Func<Assignment, bool>> predicate = a => a.Status == AssignmentStatus.Pending;

            _repo
              .Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Assignment, bool>>>(), _ct))
              .ReturnsAsync(filtered);

            // Act
            var result = await _service.ListAsync(predicate, _ct);

            // Assert
            Assert.Same(filtered, result);
            _repo.Verify(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Assignment, bool>>>(), _ct), Times.Once);
        }

    [Fact]
        public async Task UpdateAsync_CallsRepository()
        {
            var a = new Assignment("T", "D", DateTime.UtcNow.AddDays(1), "d", "m");
            await _service.UpdateAsync(a, _ct);
            _repo.Verify(r => r.UpdateAsync(a, false, _ct), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_CallsRepository()
        {
            await _service.DeleteAsync("id", _ct);
            _repo.Verify(r => r.DeleteAsync("id", _ct), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_CallsRepository()
        {
            var all = new List<Assignment>();
            _repo.Setup(r => r.GetAllAsync(_ct)).ReturnsAsync(all);

            var result = await _service.GetAllAsync(_ct);

            Assert.Same(all, result);
            _repo.Verify(r => r.GetAllAsync(_ct), Times.Once);
        }
    }
}
