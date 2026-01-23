using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Tests
{
    public class ExceptionTests
    {
        [Fact]
        public void BusinessException_Should_Set_Message()
        {
            // Arrange
            var message = "Business rule violation";

            // Act
            var exception = new BusinessException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void AuthenticationException_Should_Set_Message()
        {
            // Arrange
            var message = "Authentication failed";

            // Act
            var exception = new AuthenticationException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void ValidationException_Should_Set_Errors()
        {
            // Arrange
            var message = "Validation failed";
            var errors = new Dictionary<string, string[]>
            {
                { "Email", new[] { "Email is required" } },
                { "Password", new[] { "Password is too short" } }
            };

            // Act
            var exception = new ValidationException(message, errors);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(errors, exception.Errors);
        }

        [Fact]
        public void NotFoundException_Should_Set_EntityInfo()
        {
            // Arrange
            var entityName = "User";
            var entityId = Guid.NewGuid();

            // Act
            var exception = new NotFoundException(entityName, entityId);

            // Assert
            Assert.Equal(entityName, exception.EntityName);
            Assert.Equal(entityId, exception.EntityId);
            Assert.Contains(entityName, exception.Message);
            Assert.Contains(entityId.ToString(), exception.Message);
        }

        [Fact]
        public void ConstraintViolationException_Should_Set_ConstraintName()
        {
            // Arrange
            var constraintName = "FK_User_Email";
            var message = "Foreign key constraint violation";

            // Act
            var exception = new ConstraintViolationException(constraintName, message);

            // Assert
            Assert.Equal(constraintName, exception.ConstraintName);
            Assert.Equal(message, exception.Message);
        }
    }
}