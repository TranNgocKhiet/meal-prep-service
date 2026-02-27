using FluentValidation;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Validators
{
    public class OrderDetailDtoValidator : AbstractValidator<OrderDetailDto>
    {
        public OrderDetailDtoValidator()
        {
            RuleFor(x => x.MenuMealId)
                .NotEmpty()
                .WithMessage("Menu meal ID is required");

            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than 0")
                .LessThanOrEqualTo(100)
                .WithMessage("Quantity cannot exceed 100");

            RuleFor(x => x.UnitPrice)
                .GreaterThan(0)
                .WithMessage("Unit price must be greater than 0");
        }
    }
}