namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class DailyMenuDto
    {
        public Guid Id { get; set; }
        public DateTime MenuDate { get; set; }
        public string Status { get; set; } = string.Empty; // draft, active
        public List<MenuMealDto> MenuMeals { get; set; } = new List<MenuMealDto>();

        // Helper properties for UI
        public List<MenuMealDto> AvailableMeals => MenuMeals.Where(m => !m.IsSoldOut).ToList();
        public int TotalMeals => MenuMeals.Count;
        public bool HasMeals => MenuMeals.Any();
    }
}