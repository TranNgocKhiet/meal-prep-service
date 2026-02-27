using FluentValidation;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Validators
{
    public class MenuMealDtoValidator : AbstractValidator<MenuMealDto>
    {
        public MenuMealDtoValidator()
        {
            RuleFor(x => x.MenuId)
                .NotEmpty()
                .WithMessage("Menu ID is required");

            RuleFor(x => x.RecipeId)
                .NotEmpty()
                .WithMessage("Recipe ID is required");

            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Price must be greater than 0")
                .LessThanOrEqualTo(1000)
                .WithMessage("Price cannot exceed 1000");

            RuleFor(x => x.AvailableQuantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Available quantity must be non-negative")
                .LessThanOrEqualTo(1000)
                .WithMessage("Available quantity cannot exceed 1000");

            RuleFor(x => x.RecipeName)
                .NotEmpty()
                .WithMessage("Recipe name is required");
        }
    }
}