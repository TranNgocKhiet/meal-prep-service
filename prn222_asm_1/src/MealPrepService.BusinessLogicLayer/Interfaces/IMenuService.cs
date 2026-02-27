using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    public interface IMenuService
    {
        Task<DailyMenuDto> CreateDailyMenuAsync(DateTime menuDate);
        Task<DailyMenuDto?> GetByDateAsync(DateTime date);
        Task<IEnumerable<DailyMenuDto>> GetWeeklyMenuAsync(DateTime startDate);
        Task AddMealToMenuAsync(Guid menuId, MenuMealDto menuMealDto);
        Task RemoveMealFromMenuAsync(Guid menuMealId);
        Task PublishMenuAsync(Guid menuId);
        Task DeactivateMenuAsync(Guid menuId);
        Task ReactivateMenuAsync(Guid menuId);
        Task UpdateMealQuantityAsync(Guid menuMealId, int newQuantity);
    }
}