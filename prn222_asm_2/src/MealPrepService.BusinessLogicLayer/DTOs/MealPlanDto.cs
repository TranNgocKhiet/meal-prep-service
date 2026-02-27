namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class MealPlanDto
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsAiGenerated { get; set; }
        public bool IsActive { get; set; }
        public List<MealDto> Meals { get; set; } = new List<MealDto>();

        // Calculated nutrition properties for UI
        public decimal TotalCalories => Meals?.Sum(m => m.TotalCalories) ?? 0;
        public decimal TotalProteinG => Meals?.Sum(m => m.TotalProteinG) ?? 0;
        public decimal TotalFatG => Meals?.Sum(m => m.TotalFatG) ?? 0;
        public decimal TotalCarbsG => Meals?.Sum(m => m.TotalCarbsG) ?? 0;
        
        public decimal FinishedCalories => Meals?.Where(m => m.IsFinished).Sum(m => m.TotalCalories) ?? 0;
        public decimal FinishedProteinG => Meals?.Where(m => m.IsFinished).Sum(m => m.TotalProteinG) ?? 0;
        public decimal FinishedFatG => Meals?.Where(m => m.IsFinished).Sum(m => m.TotalFatG) ?? 0;
        public decimal FinishedCarbsG => Meals?.Where(m => m.IsFinished).Sum(m => m.TotalCarbsG) ?? 0;
    }
}