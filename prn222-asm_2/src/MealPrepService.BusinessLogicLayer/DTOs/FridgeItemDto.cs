namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class FridgeItemDto
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public Guid IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public float CurrentAmount { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsExpiring { get; set; } // Calculated field for items expiring within 3 days
        public bool IsExpired { get; set; } // Calculated field for expired items

        // Helper properties for UI
        public int DaysUntilExpiry => (ExpiryDate.Date - DateTime.Today).Days;
        public string ExpiryStatusClass => IsExpired ? "text-danger" : IsExpiring ? "text-warning" : "text-success";
    }
}