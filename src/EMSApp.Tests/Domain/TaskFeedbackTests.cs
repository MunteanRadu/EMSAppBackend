using System;
using System.Collections.Generic;
using EMSApp.Domain;
using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Domain")]
    public class TaskFeedbackTests
    {
        private static void AssertThrowsTaskFeedbackException(Action act, string expectedMessage)
        {
            var ex = Assert.Throws<DomainException>(act);
            Assert.Contains(expectedMessage, ex.Message);
        }

        private static AssignmentFeedback CreateValidTaskFeedback()
            => new AssignmentFeedback("task-321", "user-321", "Another Valid Text", FeedbackType.Employee);

        private static (string taskId, string userId, string text, FeedbackType type) GetValidParameters()
            => ("task-123", "user-123", "Valid Text", FeedbackType.Manager);

        public static IEnumerable<object[]> InvalidText => new List<object[]>
        {
            new object[] { null,    "Feedback text cannot be empty" },
            new object[] { "",      "Feedback text cannot be empty" },
            new object[] { "   ",   "Feedback text cannot be empty" },
            new object[] { new string('x', 1001), "Feedback text too long" }
        };

        [Fact]
        public void Constructor_ValidParameters_CreatesTaskFeedback()
        {
            var valid = GetValidParameters();
            var before = DateTime.UtcNow.AddSeconds(-1);

            var feedback = new AssignmentFeedback(valid.taskId, valid.userId, valid.text, valid.type);

            Assert.Equal(valid.taskId, feedback.AssignmentId);
            Assert.Equal(valid.userId, feedback.UserId);
            Assert.Equal(valid.text, feedback.Text);
            Assert.Equal(valid.type, feedback.Type);
            Assert.InRange(feedback.TimeStamp, before, DateTime.UtcNow.AddSeconds(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidAssignmentId_ThrowsDomainException(string badTaskId)
        {
            var valid = GetValidParameters();
            AssertThrowsTaskFeedbackException(
                () => new AssignmentFeedback(badTaskId, valid.userId, valid.text, valid.type),
                "AssignmentId cannot be empty"
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidUserId_ThrowsDomainException(string badUserId)
        {
            var valid = GetValidParameters();
            AssertThrowsTaskFeedbackException(
                () => new AssignmentFeedback(valid.taskId, badUserId, valid.text, valid.type),
                "UserId cannot be empty"
            );
        }

        [Theory]
        [MemberData(nameof(InvalidText))]
        public void Constructor_InvalidText_ThrowsDomainException(string badText, string expectedMessage)
        {
            var valid = GetValidParameters();
            AssertThrowsTaskFeedbackException(
                () => new AssignmentFeedback(valid.taskId, valid.userId, badText, valid.type),
                expectedMessage
            );
        }

        [Theory]
        [InlineData((FeedbackType)999)]
        public void Constructor_InvalidFeedbackType_ThrowsDomainException(FeedbackType badType)
        {
            var valid = GetValidParameters();
            AssertThrowsTaskFeedbackException(
                () => new AssignmentFeedback(valid.taskId, valid.userId, valid.text, badType),
                "Feedback type is invalid"
            );
        }

        [Fact]
        public void Edit_ValidText_EditsFeedbackText()
        {
            var valid = GetValidParameters();
            var feedback = CreateValidTaskFeedback();

            Assert.NotEqual(valid.text, feedback.Text);
            feedback.Edit(valid.text);
            Assert.Equal(valid.text, feedback.Text);
        }

        [Theory]
        [MemberData(nameof(InvalidText))]
        public void Edit_InvalidText_ThrowsDomainException(string badText, string expectedMessage)
        {
            var feedback = CreateValidTaskFeedback();
            AssertThrowsTaskFeedbackException(
                () => feedback.Edit(badText),
                expectedMessage
            );
        }
    }
}
