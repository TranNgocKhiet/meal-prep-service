using FluentValidation;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Validators
{
    public class OrderDtoValidator : AbstractValidator<OrderDto>
    {
        public OrderDtoValidator()
        {
            RuleFor(x => x.AccountId)
                .NotEmpty()
                .WithMessage("Account ID is required");

            RuleFor(x => x.OrderDate)
                .NotEmpty()
                .WithMessage("Order date is required");

            RuleFor(x => x.TotalAmount)
                .GreaterThan(0)
                .WithMessage("Total amount must be greater than 0");

            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required")
                .Must(BeValidStatus)
                .WithMessage("Status is not valid for the order workflow");

            RuleFor(x => x.OrderDetails)
                .NotEmpty()
                .WithMessage("Order must have at least one item");

            RuleForEach(x => x.OrderDetails)
                .SetValidator(new OrderDetailDtoValidator());
        }

        private bool BeValidStatus(string status)
        {
            var validStatuses = new[]
            {
                "pending",
                "awaiting_online_payment",
                "payment_failed",
                "pending_confirmation",
                "confirmed",
                "preparing",
                "preparing_failed",
                "prepared",
                "delivering",
                "customer_received",
                "customer_reject",
                "failed",
                "cancelled"
            };
            return validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
        }
    }
}