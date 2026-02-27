namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class AccountDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        // Helper property for UI badge styling
        public string RoleBadgeClass => Role switch
        {
            "Admin" => "badge bg-danger",
            "Manager" => "badge bg-info",
            "DeliveryMan" => "badge bg-success",
            "Customer" => "badge bg-primary",
            _ => "badge bg-secondary"
        };
    }
}