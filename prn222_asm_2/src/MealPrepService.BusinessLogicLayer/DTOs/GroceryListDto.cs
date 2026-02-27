namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class GroceryListDto
    {
        public Guid AccountId { get; set; }
        public Guid MealPlanId { get; set; }
        public string MealPlanName { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public List<GroceryItemDto> MissingIngredients { get; set; } = new List<GroceryItemDto>();
    }

    public class GroceryItemDto
    {
        public Guid IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public float RequiredAmount { get; set; }
        public float CurrentAmount { get; set; }
        public float NeededAmount { get; set; }
        public bool IsNeededSoon { get; set; } // Needed for today or tomorrow's meals
        public DateTime? EarliestNeededDate { get; set; } // When this ingredient is first needed

        // Helper properties for UI display
        public string CurrentAmountDisplay => $"{CurrentAmount:F2} {Unit}";
        public string RequiredAmountDisplay => $"{RequiredAmount:F2} {Unit}";
        public string NeededAmountDisplay => $"{NeededAmount:F2} {Unit}";
    }
}