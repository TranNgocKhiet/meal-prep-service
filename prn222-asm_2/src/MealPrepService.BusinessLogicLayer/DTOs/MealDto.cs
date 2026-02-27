namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class MealDto
    {
        public Guid Id { get; set; }
        public Guid PlanId { get; set; }
        public string MealType { get; set; } = string.Empty; // breakfast, lunch, dinner
        public DateTime ServeDate { get; set; }
        public bool MealFinished { get; set; } = false;
        public List<RecipeDto> Recipes { get; set; } = new List<RecipeDto>();

        // Calculated nutrition properties for UI
        public decimal TotalCalories => Recipes?.Sum(r => (decimal)r.TotalCalories) ?? 0;
        public decimal TotalProteinG => Recipes?.Sum(r => (decimal)r.ProteinG) ?? 0;
        public decimal TotalFatG => Recipes?.Sum(r => (decimal)r.FatG) ?? 0;
        public decimal TotalCarbsG => Recipes?.Sum(r => (decimal)r.CarbsG) ?? 0;
        
        // Aliases for view compatibility
        public decimal MealCalories => TotalCalories;
        public decimal MealProteinG => TotalProteinG;
        public decimal MealFatG => TotalFatG;
        public decimal MealCarbsG => TotalCarbsG;
        
        // Alias for consistency with view expectations
        public bool IsFinished => MealFinished;
    }
}
