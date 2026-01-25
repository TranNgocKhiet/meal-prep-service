using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;

namespace MealPrepService.BusinessLogicLayer.Services
{
    /// <summary>
    /// Service for recipe management operations
    /// </summary>
    public class RecipeService : IRecipeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RecipeService> _logger;

        public RecipeService(IUnitOfWork unitOfWork, ILogger<RecipeService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<RecipeDto> CreateRecipeAsync(CreateRecipeDto dto)
        {
            _logger.LogInformation("Creating recipe with name: {RecipeName}", dto.RecipeName);

            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.RecipeName))
            {
                throw new BusinessException("Recipe name is required");
            }

            if (string.IsNullOrWhiteSpace(dto.Instructions))
            {
                throw new BusinessException("Recipe instructions are required");
            }

            var recipe = new Recipe
            {
                Id = Guid.NewGuid(),
                RecipeName = dto.RecipeName.Trim(),
                Instructions = dto.Instructions.Trim(),
                TotalCalories = 0, // Will be calculated when ingredients are added
                ProteinG = 0,
                FatG = 0,
                CarbsG = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Recipes.AddAsync(recipe);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Recipe created successfully with ID: {RecipeId}", recipe.Id);

            return MapToDto(recipe);
        }

        public async Task<RecipeDto> GetByIdAsync(Guid recipeId)
        {
            var recipe = await _unitOfWork.Recipes.GetByIdAsync(recipeId);
            
            if (recipe == null)
            {
                throw new BusinessException("Recipe not found");
            }

            return MapToDto(recipe);
        }

        public async Task<RecipeDto> GetByIdWithIngredientsAsync(Guid recipeId)
        {
            var recipe = await _unitOfWork.Recipes.GetByIdWithIngredientsAsync(recipeId);
            
            if (recipe == null)
            {
                throw new BusinessException("Recipe not found");
            }

            return MapToDtoWithIngredients(recipe);
        }

        public async Task<IEnumerable<RecipeDto>> GetAllAsync()
        {
            var recipes = await _unitOfWork.Recipes.GetAllAsync();
            return recipes.Select(MapToDto);
        }

        public async Task<IEnumerable<RecipeDto>> GetAllWithIngredientsAsync()
        {
            var recipes = await _unitOfWork.Recipes.GetAllWithIngredientsAsync();
            return recipes.Select(MapToDtoWithIngredients);
        }

        public async Task<RecipeDto> UpdateRecipeAsync(Guid recipeId, UpdateRecipeDto dto)
        {
            _logger.LogInformation("Updating recipe with ID: {RecipeId}", recipeId);

            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.RecipeName))
            {
                throw new BusinessException("Recipe name is required");
            }

            if (string.IsNullOrWhiteSpace(dto.Instructions))
            {
                throw new BusinessException("Recipe instructions are required");
            }

            var recipe = await _unitOfWork.Recipes.GetByIdAsync(recipeId);
            
            if (recipe == null)
            {
                throw new BusinessException("Recipe not found");
            }

            recipe.RecipeName = dto.RecipeName.Trim();
            recipe.Instructions = dto.Instructions.Trim();
            recipe.UpdatedAt = DateTime.UtcNow;

            // NOTE: Nutrition recalculation is deferred as per task requirements
            
            await _unitOfWork.Recipes.UpdateAsync(recipe);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Recipe updated successfully with ID: {RecipeId}", recipeId);

            return MapToDto(recipe);
        }

        public async Task DeleteRecipeAsync(Guid recipeId)
        {
            _logger.LogInformation("Deleting recipe with ID: {RecipeId}", recipeId);

            var recipe = await _unitOfWork.Recipes.GetByIdAsync(recipeId);
            
            if (recipe == null)
            {
                throw new BusinessException("Recipe not found");
            }

            // Check if recipe is used in active menu meals
            var isUsedInActiveMenu = await _unitOfWork.Recipes.IsUsedInActiveMenuAsync(recipeId);
            
            if (isUsedInActiveMenu)
            {
                throw new BusinessException("Cannot delete recipe that is used in active menu meals");
            }

            await _unitOfWork.Recipes.DeleteAsync(recipeId);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Recipe deleted successfully with ID: {RecipeId}", recipeId);
        }

        public async Task AddIngredientToRecipeAsync(Guid recipeId, RecipeIngredientDto ingredientDto)
        {
            _logger.LogInformation("Adding ingredient {IngredientId} to recipe {RecipeId}", 
                ingredientDto.IngredientId, recipeId);

            // Validate required fields
            if (ingredientDto.IngredientId == Guid.Empty)
            {
                throw new BusinessException("Ingredient ID is required");
            }

            if (ingredientDto.Amount <= 0)
            {
                throw new BusinessException("Ingredient amount must be positive");
            }

            // Verify recipe exists
            var recipe = await _unitOfWork.Recipes.GetByIdAsync(recipeId);
            if (recipe == null)
            {
                throw new BusinessException("Recipe not found");
            }

            // Verify ingredient exists
            var ingredient = await _unitOfWork.Ingredients.GetByIdAsync(ingredientDto.IngredientId);
            if (ingredient == null)
            {
                throw new BusinessException("Ingredient not found");
            }

            // Check if ingredient is already in recipe
            var existingRecipeIngredient = await _unitOfWork.RecipeIngredients
                .FirstOrDefaultAsync(ri => ri.RecipeId == recipeId && ri.IngredientId == ingredientDto.IngredientId);

            if (existingRecipeIngredient != null)
            {
                throw new BusinessException("Ingredient is already added to this recipe");
            }

            var recipeIngredient = new RecipeIngredient
            {
                RecipeId = recipeId,
                IngredientId = ingredientDto.IngredientId,
                Amount = ingredientDto.Amount
            };

            _unitOfWork.RecipeIngredients.Add(recipeIngredient);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Ingredient added successfully to recipe");
        }

        public async Task<IEnumerable<RecipeDto>> GetByIngredientsAsync(IEnumerable<Guid> ingredientIds)
        {
            var recipes = await _unitOfWork.Recipes.GetByIngredientsAsync(ingredientIds);
            return recipes.Select(MapToDto);
        }

        public async Task<IEnumerable<RecipeDto>> GetExcludingAllergensAsync(IEnumerable<Guid> allergyIds)
        {
            var recipes = await _unitOfWork.Recipes.GetExcludingAllergensAsync(allergyIds);
            return recipes.Select(MapToDto);
        }

        private static RecipeDto MapToDto(Recipe recipe)
        {
            return new RecipeDto
            {
                Id = recipe.Id,
                RecipeName = recipe.RecipeName,
                Instructions = recipe.Instructions,
                TotalCalories = recipe.TotalCalories,
                ProteinG = recipe.ProteinG,
                FatG = recipe.FatG,
                CarbsG = recipe.CarbsG
            };
        }

        private static RecipeDto MapToDtoWithIngredients(Recipe recipe)
        {
            return new RecipeDto
            {
                Id = recipe.Id,
                RecipeName = recipe.RecipeName,
                Instructions = recipe.Instructions,
                TotalCalories = recipe.TotalCalories,
                ProteinG = recipe.ProteinG,
                FatG = recipe.FatG,
                CarbsG = recipe.CarbsG,
                Ingredients = recipe.RecipeIngredients?.Select(ri => new RecipeIngredientDto
                {
                    IngredientId = ri.IngredientId,
                    IngredientName = ri.Ingredient?.IngredientName ?? string.Empty,
                    Amount = ri.Amount,
                    Unit = ri.Ingredient?.Unit ?? string.Empty
                }).ToList() ?? new List<RecipeIngredientDto>()
            };
        }
    }
}