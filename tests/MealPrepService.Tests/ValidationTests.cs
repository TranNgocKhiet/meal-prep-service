using FluentValidation.TestHelper;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Validators;

namespace MealPrepService.Tests
{
    public class ValidationTests
    {
        [Fact]
        public void CreateAccountDtoValidator_Should_Have_Error_When_Email_Is_Empty()
        {
            // Arrange
            var validator = new CreateAccountDtoValidator();
            var dto = new CreateAccountDto { Email = "", Password = "password123", FullName = "John Doe" };

            // Act
            var result = validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void CreateAccountDtoValidator_Should_Have_Error_When_Email_Is_Invalid()
        {
            // Arrange
            var validator = new CreateAccountDtoValidator();
            var dto = new CreateAccountDto { Email = "invalid-email", Password = "password123", FullName = "John Doe" };

            // Act
            var result = validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void CreateAccountDtoValidator_Should_Not_Have_Error_When_Valid()
        {
            // Arrange
            var validator = new CreateAccountDtoValidator();
            var dto = new CreateAccountDto { Email = "test@example.com", Password = "password123", FullName = "John Doe" };

            // Act
            var result = validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void HealthProfileDtoValidator_Should_Have_Error_When_Age_Is_Zero()
        {
            // Arrange
            var validator = new HealthProfileDtoValidator();
            var dto = new HealthProfileDto { Age = 0, Weight = 70, Height = 175, Gender = "Male" };

            // Act
            var result = validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Age);
        }

        [Fact]
        public void HealthProfileDtoValidator_Should_Have_Error_When_Weight_Is_Negative()
        {
            // Arrange
            var validator = new HealthProfileDtoValidator();
            var dto = new HealthProfileDto { Age = 25, Weight = -10, Height = 175, Gender = "Male" };

            // Act
            var result = validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Weight);
        }
    }
}