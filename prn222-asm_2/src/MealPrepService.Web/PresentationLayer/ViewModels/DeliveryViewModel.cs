using System.ComponentModel.DataAnnotations;

namespace MealPrepService.Web.PresentationLayer.ViewModels
{
    public class UpdateDeliveryTimeViewModel
    {
        public Guid DeliveryId { get; set; }

        [Required(ErrorMessage = "New delivery time is required")]
        [Display(Name = "New Delivery Time")]
        [DataType(DataType.DateTime)]
        [FutureDate(ErrorMessage = "Delivery time must be in the future")]
        public DateTime NewDeliveryTime { get; set; }

        [Display(Name = "Reason for Change")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; } = string.Empty;

        // Current delivery information for display
        [Display(Name = "Current Delivery Time")]
        [DataType(DataType.DateTime)]
        public DateTime CurrentDeliveryTime { get; set; }

        [Display(Name = "Customer Address")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Order ID")]
        public Guid OrderId { get; set; }

        public string CurrentDeliveryTimeDisplay => CurrentDeliveryTime.ToString("dddd, MMMM dd, yyyy 'at' h:mm tt");
        public string NewDeliveryTimeDisplay => NewDeliveryTime.ToString("dddd, MMMM dd, yyyy 'at' h:mm tt");
    }

    public class MyDeliveriesViewModel
    {
        public List<DeliveryScheduleViewModel> UpcomingDeliveries { get; set; } = new List<DeliveryScheduleViewModel>();
        public List<DeliveryScheduleViewModel> CompletedDeliveries { get; set; } = new List<DeliveryScheduleViewModel>();

        public int TotalUpcoming => UpcomingDeliveries.Count;
        public int TotalCompleted => CompletedDeliveries.Count;
        public int OverdueCount => UpcomingDeliveries.Count(d => d.IsOverdue);
        public int UrgentCount => UpcomingDeliveries.Count(d => d.DeliveryTime <= DateTime.Now.AddHours(2) && !d.IsOverdue);

        public bool HasUpcomingDeliveries => UpcomingDeliveries.Any();
        public bool HasCompletedDeliveries => CompletedDeliveries.Any();
        public bool HasOverdueDeliveries => OverdueCount > 0;
    }

    public class DeliveryDetailsViewModel
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }

        [Display(Name = "Delivery Time")]
        [DataType(DataType.DateTime)]
        public DateTime DeliveryTime { get; set; }

        [Display(Name = "Delivery Address")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Driver Contact")]
        public string DriverContact { get; set; } = string.Empty;

        [Display(Name = "Order Status")]
        public string OrderStatus { get; set; } = string.Empty;

        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = string.Empty;

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Customer Information")]
        public CustomerInfoViewModel Customer { get; set; } = new CustomerInfoViewModel();

        [Display(Name = "Order Items")]
        public List<OrderItemSummaryViewModel> OrderItems { get; set; } = new List<OrderItemSummaryViewModel>();

        // Computed properties
        public bool CanConfirmPayment => PaymentMethod == "COD" && OrderStatus == "pending_payment";
        public bool CanCompleteDelivery => OrderStatus == "confirmed";
        public bool IsCompleted => OrderStatus == "delivered";
        public bool IsOverdue => DeliveryTime < DateTime.Now && !IsCompleted;

        public string DeliveryTimeDisplay => DeliveryTime.ToString("dddd, MMMM dd, yyyy 'at' h:mm tt");
        public string StatusDisplayName => OrderStatus switch
        {
            "pending" => "Pending Payment",
            "pending_payment" => "Awaiting Cash Payment",
            "confirmed" => "Ready for Delivery",
            "delivered" => "Delivered",
            "payment_failed" => "Payment Failed",
            _ => OrderStatus
        };

        public int TotalItems => OrderItems.Sum(i => i.Quantity);
    }

    public class CustomerInfoViewModel
    {
        [Display(Name = "Customer Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone")]
        public string Phone { get; set; } = string.Empty;
    }

    public class OrderItemSummaryViewModel
    {
        [Display(Name = "Recipe Name")]
        public string RecipeName { get; set; } = string.Empty;

        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        [Display(Name = "Unit Price")]
        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Total Price")]
        [DataType(DataType.Currency)]
        public decimal TotalPrice => UnitPrice * Quantity;
    }

    // Custom validation attribute for future dates
    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is DateTime dateTime)
            {
                return dateTime > DateTime.Now;
            }
            return false;
        }
    }
}