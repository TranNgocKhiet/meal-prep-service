using MealPreparationService.Business.Constants;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Models;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;

namespace MealPreparationService.Business.Services;

public class IngredientService : IIngredientService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public IngredientService(IUnitOfWork unitOfWork, ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<List<IngredientDto>> SearchIngredientsAsync(IngredientSearchDto searchDto)
    {
        List<Ingredient> ingredients;

        // Search by name if search term provided
        if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
        {
            ingredients = await _unitOfWork.Ingredients.SearchAsync(searchDto.SearchTerm);
        }
        else
        {
            // Return all ingredients if no filters
            ingredients = await _unitOfWork.Ingredients.GetAllAsync();
        }

        // Convert to DTOs with allergy information
        var ingredientDtos = new List<IngredientDto>();
        foreach (var ingredient in ingredients)
        {
            var ingredientWithAllergies = await _unitOfWork.Ingredients.GetByIdWithAllergiesAsync(ingredient.Id);
            if (ingredientWithAllergies != null)
            {
                var dto = MapToIngredientDto(ingredientWithAllergies);
                
                // Highlight search term in name if provided
                if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
                {
                    dto.Name = HighlightSearchTerm(dto.Name, searchDto.SearchTerm);
                }
                
                ingredientDtos.Add(dto);
            }
        }

        return ingredientDtos;
    }

    public async Task<List<IngredientDto>> SearchIngredientsWithoutHighlightAsync(string searchTerm)
    {
        var ingredients = await _unitOfWork.Ingredients.SearchAsync(searchTerm);

        // Convert to DTOs without highlighting
        var ingredientDtos = new List<IngredientDto>();
        foreach (var ingredient in ingredients)
        {
            var ingredientWithAllergies = await _unitOfWork.Ingredients.GetByIdWithAllergiesAsync(ingredient.Id);
            if (ingredientWithAllergies != null)
            {
                ingredientDtos.Add(MapToIngredientDto(ingredientWithAllergies));
            }
        }

        return ingredientDtos;
    }

    public async Task<PaginatedResult<IngredientDto>> SearchIngredientsPaginatedAsync(IngredientSearchDto searchDto, PaginationParameters pagination)
    {
        List<Ingredient> ingredients;

        // Search by name if search term provided
        if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
        {
            ingredients = await _unitOfWork.Ingredients.SearchAsync(searchDto.SearchTerm);
        }
        else
        {
            // Return all ingredients if no filters
            ingredients = await _unitOfWork.Ingredients.GetAllAsync();
        }

        // Get total count before pagination
        var totalCount = ingredients.Count;

        // Apply pagination
        var paginatedIngredients = ingredients
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToList();

        // Convert to DTOs with allergy information
        var ingredientDtos = new List<IngredientDto>();
        foreach (var ingredient in paginatedIngredients)
        {
            var ingredientWithAllergies = await _unitOfWork.Ingredients.GetByIdWithAllergiesAsync(ingredient.Id);
            if (ingredientWithAllergies != null)
            {
                var dto = MapToIngredientDto(ingredientWithAllergies);
                
                // Highlight search term in name if provided
                if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
                {
                    dto.Name = HighlightSearchTerm(dto.Name, searchDto.SearchTerm);
                }
                
                ingredientDtos.Add(dto);
            }
        }

        return new PaginatedResult<IngredientDto>(ingredientDtos, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<IngredientDto?> GetIngredientByIdAsync(string ingredientId)
    {
        // Try to get from cache
        var cacheKey = CacheKeys.GetIngredientByIdKey(ingredientId);
        var cachedIngredient = await _cacheService.GetAsync<IngredientDto>(cacheKey);
        
        if (cachedIngredient != null)
        {
            return cachedIngredient;
        }

        // Get from database
        var ingredient = await _unitOfWork.Ingredients.GetByIdWithAllergiesAsync(ingredientId);
        if (ingredient == null)
        {
            return null;
        }

        var ingredientDto = MapToIngredientDto(ingredient);
        
        // Cache the ingredient
        await _cacheService.SetAsync(cacheKey, ingredientDto, CacheKeys.IngredientCacheExpiration);

        return ingredientDto;
    }


    private IngredientDto MapToIngredientDto(Ingredient ingredient)
    {
        return new IngredientDto
        {
            Id = ingredient.Id,
            Name = ingredient.Name,
            Unit = ingredient.Unit,
            ImageUrl = ingredient.ImageUrl,
            Allergies = ingredient.IngredientAllergies.Select(ia => new AllergyDto
            {
                Id = ia.Allergy.Id,
                Name = ia.Allergy.Name
            }).ToList()
        };
    }

    private string HighlightSearchTerm(string text, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(searchTerm))
        {
            return text;
        }

        // Simple highlighting by wrapping matches with <mark> tags
        var index = text.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            var matchedText = text.Substring(index, searchTerm.Length);
            return text.Replace(matchedText, $"<mark>{matchedText}</mark>");
        }

        return text;
    }
}
