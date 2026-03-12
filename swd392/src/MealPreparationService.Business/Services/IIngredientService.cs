using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Models;

namespace MealPreparationService.Business.Services;

public interface IIngredientService
{
    Task<List<IngredientDto>> SearchIngredientsAsync(IngredientSearchDto searchDto);
    Task<List<IngredientDto>> SearchIngredientsWithoutHighlightAsync(string searchTerm);
    Task<PaginatedResult<IngredientDto>> SearchIngredientsPaginatedAsync(IngredientSearchDto searchDto, PaginationParameters pagination);
    Task<IngredientDto?> GetIngredientByIdAsync(string ingredientId);
}
