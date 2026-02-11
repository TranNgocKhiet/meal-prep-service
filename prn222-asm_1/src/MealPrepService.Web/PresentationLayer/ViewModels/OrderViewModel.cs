using System.ComponentModel.DataAnnotations;

namespace MealPrepService.Web.PresentationLayer.ViewModels
{
    public class CreateOrderViewModel
    {
        [Display(Name = "Selected Items")]
        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();

        [Display(Name = "Delivery Address")]
        [Required(ErrorMessage = "Delivery address is required")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Display(Name = "Delivery Notes")]
        [StringLength(1000, ErrorMessage = "Delivery notes cannot exceed 1000 characters")]
        public string DeliveryNotes { get; set; } = string.Empty;

        [Display(Name = "Preferred Delivery Time")]
        [DataType(DataType.DateTime)]
        public DateTime? PreferredDeliveryTime { get; set; }

        // Calculated properties
        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount => OrderItems.Sum(item => item.TotalPrice);

        [Display(Name = "Total Items")]
        public int TotalItems => OrderItems.Sum(item => item.Quantity);

        public bool HasItems => OrderItems.Any(item => item.Quantity > 0);

        // For display purposes
        public DateTime MenuDate { get; set; }
        public List<PublicMenuMealViewModel> AvailableMeals { get; set; } = new List<PublicMenuMealViewModel>();

        // Validation method
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!HasItems)
            {
                yield return new ValidationResult("Please select at least one item to order", new[] { nameof(OrderItems) });
            }

            if (PreferredDeliveryTime.HasValue && PreferredDeliveryTime.Value <= DateTime.Now)
            {
                yield return new ValidationResult("Preferred delivery time must be in the future", new[] { nameof(PreferredDeliveryTime) });
            }

            foreach (var item in OrderItems.Where(i => i.Quantity > 0))
            {
                if (item.Quantity > item.AvailableQuantity)
                {
                    yield return new ValidationResult($"Quantity for {item.RecipeName} exceeds available stock ({item.AvailableQuantity})", new[] { nameof(OrderItems) });
                }
            }
        }
    }

    public class OrderItemViewModel
    {
        public Guid MenuMealId { get; set; }

        [Display(Name = "Recipe Name")]
        public string RecipeName { get; set; } = string.Empty;

        [Display(Name = "Price")]
        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Quantity")]
        [Range(0, 50, ErrorMessage = "Quantity must be between 0 and 50")]
        public int Quantity { get; set; }

        [Display(Name = "Available")]
        public int AvailableQuantity { get; set; }

        [Display(Name = "Total")]
        [DataType(DataType.Currency)]
        public decimal TotalPrice => UnitPrice * Quantity;

        // Recipe details for display
        public RecipeDetailsViewModel Recipe { get; set; } = new RecipeDetailsViewModel();

        public bool IsSelected => Quantity > 0;
        public bool IsAvailable => AvailableQuantity > 0;
        public string AvailabilityStatus => AvailableQuantity > 0 ? $"{AvailableQuantity} available" : "Sold out";
    }

    public class PaymentViewModel
    {
        public Guid OrderId { get; set; }

        [Display(Name = "Order Total")]
        [DataType(DataType.Currency)]
        public decimal OrderTotal { get; set; }

        [Required(ErrorMessage = "Payment method is required")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = string.Empty;

        // Order details for display
        public List<OrderDetailDisplayViewModel> OrderDetails { get; set; } = new List<OrderDetailDisplayViewModel>();
        public string DeliveryAddress { get; set; } = string.Empty;
        public DateTime? PreferredDeliveryTime { get; set; }

        // Payment method options for enhanced payment
        public List<PaymentMethodOption> PaymentMethods { get; set; } = new List<PaymentMethodOption>
        {
            new PaymentMethodOption { Value = "COD", Text = "Cash on Delivery (COD)" },
            new PaymentMethodOption { Value = "VNPAY", Text = "Online Payment (VNPAY)" }
        };
    }

    public class PaymentMethodOption
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class OrderListViewModel
    {
        [Display(Name = "Order ID")]
        public Guid Id { get; set; }

        [Display(Name = "Order Date")]
        [DataType(DataType.DateTime)]
        public DateTime OrderDate { get; set; }

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Items Count")]
        public int ItemsCount { get; set; }

        [Display(Name = "Delivery Time")]
        [DataType(DataType.DateTime)]
        public DateTime? DeliveryTime { get; set; }

        [Display(Name = "Delivery Address")]
        public string DeliveryAddress { get; set; } = string.Empty;

        // Status display properties
        public string StatusDisplayName => Status switch
        {
            "pending" => "Pending Payment",
            "pending_payment" => "Awaiting Cash Payment",
            "paid" => "Paid",
            "payment_failed" => "Payment Failed",
            "confirmed" => "Confirmed",
            "delivered" => "Delivered",
            _ => Status
        };

        public string StatusCssClass => Status switch
        {
            "pending" => "text-warning",
            "pending_payment" => "text-info",
            "paid" => "text-info",
            "payment_failed" => "text-danger",
            "confirmed" => "text-success",
            "delivered" => "text-success",
            _ => "text-secondary"
        };

        public bool CanPay => Status.Equals("pending", StringComparison.OrdinalIgnoreCase) || 
                             Status.Equals("payment_failed", StringComparison.OrdinalIgnoreCase);
        
        public bool IsDelivered => Status.Equals("delivered", StringComparison.OrdinalIgnoreCase);
        public bool IsConfirmed => Status.Equals("confirmed", StringComparison.OrdinalIgnoreCase);
        public bool IsPendingPayment => Status.Equals("pending_payment", StringComparison.OrdinalIgnoreCase);
    }

    public class OrderDetailsViewModel
    {
        [Display(Name = "Order ID")]
        public Guid Id { get; set; }

        [Display(Name = "Order Date")]
        [DataType(DataType.DateTime)]
        public DateTime OrderDate { get; set; }

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Order Items")]
        public List<OrderDetailDisplayViewModel> OrderDetails { get; set; } = new List<OrderDetailDisplayViewModel>();

        [Display(Name = "Delivery Information")]
        public DeliveryInfoViewModel? DeliveryInfo { get; set; }

        // Status display properties
        public string StatusDisplayName => Status switch
        {
            "pending" => "Pending Payment",
            "pending_payment" => "Awaiting Cash Payment",
            "paid" => "Paid",
            "payment_failed" => "Payment Failed",
            "confirmed" => "Confirmed",
            "delivered" => "Delivered",
            _ => Status
        };

        public string StatusCssClass => Status switch
        {
            "pending" => "text-warning",
            "pending_payment" => "text-info",
            "paid" => "text-info",
            "payment_failed" => "text-danger",
            "confirmed" => "text-success",
            "delivered" => "text-success",
            _ => "text-secondary"
        };

        public bool CanPay => Status.Equals("pending", StringComparison.OrdinalIgnoreCase) || 
                             Status.Equals("payment_failed", StringComparison.OrdinalIgnoreCase);
        
        public bool IsDelivered => Status.Equals("delivered", StringComparison.OrdinalIgnoreCase);
        public bool IsConfirmed => Status.Equals("confirmed", StringComparison.OrdinalIgnoreCase);
        public bool IsPendingPayment => Status.Equals("pending_payment", StringComparison.OrdinalIgnoreCase);

        // Calculated properties
        public int TotalItems => OrderDetails.Sum(d => d.Quantity);
        public string OrderDateDisplay => OrderDate.ToString("dddd, MMMM dd, yyyy 'at' h:mm tt");
    }

    public class OrderDetailDisplayViewModel
    {
        public Guid Id { get; set; }

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

        // Recipe details for display
        public RecipeDetailsViewModel Recipe { get; set; } = new RecipeDetailsViewModel();
    }

    public class DeliveryInfoViewModel
    {
        [Display(Name = "Delivery Time")]
        [DataType(DataType.DateTime)]
        public DateTime DeliveryTime { get; set; }

        [Display(Name = "Delivery Address")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Driver Contact")]
        public string DriverContact { get; set; } = string.Empty;

        public string DeliveryTimeDisplay => DeliveryTime.ToString("dddd, MMMM dd, yyyy 'at' h:mm tt");
        public bool IsScheduled => DeliveryTime > DateTime.Now;
        public bool IsOverdue => DeliveryTime < DateTime.Now;
    }

    public class OrderConfirmationViewModel
    {
        [Display(Name = "Order ID")]
        public Guid OrderId { get; set; }

        [Display(Name = "Order Date")]
        [DataType(DataType.DateTime)]
        public DateTime OrderDate { get; set; }

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Order Items")]
        public List<OrderDetailDisplayViewModel> OrderDetails { get; set; } = new List<OrderDetailDisplayViewModel>();

        [Display(Name = "Delivery Information")]
        public DeliveryInfoViewModel? DeliveryInfo { get; set; }

        public bool PaymentSuccessful => Status.Equals("confirmed", StringComparison.OrdinalIgnoreCase);
        public bool PaymentFailed => Status.Equals("payment_failed", StringComparison.OrdinalIgnoreCase);

        public string ConfirmationMessage => PaymentSuccessful 
            ? "Your order has been confirmed and will be delivered as scheduled."
            : "There was an issue processing your payment. Please try again or contact support.";

        public string ConfirmationCssClass => PaymentSuccessful ? "alert-success" : "alert-danger";
    }

    public class AssignedDeliveriesViewModel
    {
        public List<DeliveryScheduleViewModel> Deliveries { get; set; } = new List<DeliveryScheduleViewModel>();
        
        public int TotalDeliveries => Deliveries.Count;
        public int PendingPaymentCount => Deliveries.Count(d => d.CanConfirmPayment);
        public int ReadyForDeliveryCount => Deliveries.Count(d => d.CanCompleteDelivery);
        public int CompletedCount => Deliveries.Count(d => d.OrderStatus == "delivered");
    }

    public class DeliveryScheduleViewModel
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

        // Additional properties for delivery man interface
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Display(Name = "Customer Contact")]
        public string CustomerContact { get; set; } = string.Empty;

        [Display(Name = "Delivery Notes")]
        public string DeliveryNotes { get; set; } = string.Empty;

        [Display(Name = "Items Count")]
        public int ItemsCount { get; set; }

        [Display(Name = "Order Total")]
        [DataType(DataType.Currency)]
        public decimal OrderTotal { get; set; }

        // Computed properties for delivery man interface
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

        public string StatusCssClass => OrderStatus switch
        {
            "pending" => "text-warning",
            "pending_payment" => "text-info",
            "confirmed" => "text-success",
            "delivered" => "text-muted",
            "payment_failed" => "text-danger",
            _ => "text-secondary"
        };

        public string PaymentMethodDisplay => PaymentMethod switch
        {
            "COD" => "Cash on Delivery",
            "VNPAY" => "Online Payment",
            _ => PaymentMethod
        };
    }
}