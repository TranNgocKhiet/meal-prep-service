using FluentValidation;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Validators
{
    public class UpdateRecipeDtoValidator : AbstractValidator<UpdateRecipeDto>
    {
        public UpdateRecipeDtoValidator()
        {
            RuleFor(x => x.RecipeName)
                .NotEmpty()
                .WithMessage("Recipe name is required")
                .MaximumLength(200)
                .WithMessage("Recipe name must not exceed 200 characters");

            RuleFor(x => x.Instructions)
                .NotEmpty()
                .WithMessage("Instructions are required")
                .MaximumLength(2000)
                .WithMessage("Instructions must not exceed 2000 characters");
        }
    }
}