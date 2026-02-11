namespace MealPrepService.BusinessLogicLayer.DTOs
{
    /// <summary>
    /// DTO for creating a new recipe
    /// </summary>
    public class CreateRecipeDto
    {
        public string RecipeName { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
    }
}