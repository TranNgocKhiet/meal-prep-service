import { useState, useEffect } from 'react';
import apiClient from '../../config/api';
import './RecipeSelector.css';

interface Recipe {
  id: string;
  recipeName: string;
  category: string;
  preparationTimeMinutes: number;
  difficultyLevel: string;
  servings: number;
  imageUrl: string;
  hasAllergyWarning: boolean;
  allergens: string[];
  isFavorite: boolean;
}

interface ApiError {
  response?: {
    data?: {
      message?: string;
    };
  };
  message?: string;
}

interface RecipeSelectorProps {
  maxSelection: number;
  selectedRecipeIds: string[];
  onConfirm: (recipes: Recipe[]) => void;
  onCancel: () => void;
}

const RecipeSelector = ({ maxSelection, selectedRecipeIds, onConfirm, onCancel }: RecipeSelectorProps) => {
  const [recipes, setRecipes] = useState<Recipe[]>([]);
  const [selectedRecipes, setSelectedRecipes] = useState<Recipe[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [showAllergyWarning, setShowAllergyWarning] = useState(false);
  const [allergyRecipe, setAllergyRecipe] = useState<Recipe | null>(null);
  const [hasSearched, setHasSearched] = useState(false);
  const [searchTimeout, setSearchTimeout] = useState<NodeJS.Timeout | null>(null);

  useEffect(() => {
    // Don't fetch recipes on mount
    // Only fetch when user searches
  }, []);

  useEffect(() => {
    // Pre-select recipes based on selectedRecipeIds
    if (recipes.length > 0 && selectedRecipeIds.length > 0) {
      const preSelected = recipes.filter(r => selectedRecipeIds.includes(r.id));
      setSelectedRecipes(preSelected);
    }
  }, [recipes, selectedRecipeIds]);

  const fetchRecipes = async () => {
    try {
      setLoading(true);
      setError('');
      setHasSearched(true);
      
      const payload: {
        searchTerm?: string;
        excludeAllergens: boolean;
      } = {
        excludeAllergens: true
      };
      
      if (searchTerm && searchTerm.trim()) {
        payload.searchTerm = searchTerm.trim();
      }
      
      const response = await apiClient.post('/recipes/search', payload);
      
      if (response.data.success) {
        setRecipes(response.data.data || []);
      } else {
        setError(response.data.message || 'Failed to load recipes');
      }
    } catch (err) {
      console.error('Recipe search error:', err);
      const error = err as ApiError;
      setError(error.response?.data?.message || 'Failed to load recipes. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = () => {
    fetchRecipes();
  };

  const handleSearchChange = (value: string) => {
    setSearchTerm(value);
    
    // Clear existing timeout
    if (searchTimeout) {
      clearTimeout(searchTimeout);
    }
    
    // If search is empty, clear results
    if (!value.trim()) {
      setRecipes([]);
      setHasSearched(false);
      return;
    }
    
    // Set new timeout to search after user stops typing
    const timeout = setTimeout(() => {
      fetchRecipes();
    }, 500); // Wait 500ms after user stops typing
    
    setSearchTimeout(timeout);
  };

  const handleClearSearch = () => {
    setSearchTerm('');
    setRecipes([]);
    setHasSearched(false);
    setError('');
    if (searchTimeout) {
      clearTimeout(searchTimeout);
    }
  };

  const handleShowAll = () => {
    setSearchTerm('');
    fetchRecipes();
  };

  const handleToggleRecipe = (recipe: Recipe) => {
    if (recipe.hasAllergyWarning && !selectedRecipes.find(r => r.id === recipe.id)) {
      setAllergyRecipe(recipe);
      setShowAllergyWarning(true);
      return;
    }

    toggleRecipeSelection(recipe);
  };

  const toggleRecipeSelection = (recipe: Recipe) => {
    const isSelected = selectedRecipes.find(r => r.id === recipe.id);
    
    if (isSelected) {
      setSelectedRecipes(selectedRecipes.filter(r => r.id !== recipe.id));
    } else {
      if (selectedRecipes.length >= maxSelection) {
        alert(`You can only select up to ${maxSelection} recipes`);
        return;
      }
      setSelectedRecipes([...selectedRecipes, recipe]);
    }
  };

  const handleConfirmAllergyWarning = () => {
    if (allergyRecipe) {
      toggleRecipeSelection(allergyRecipe);
    }
    setShowAllergyWarning(false);
    setAllergyRecipe(null);
  };

  const handleCancelAllergyWarning = () => {
    setShowAllergyWarning(false);
    setAllergyRecipe(null);
  };

  const handleConfirm = () => {
    onConfirm(selectedRecipes);
  };

  const isRecipeSelected = (recipeId: string) => {
    return selectedRecipes.some(r => r.id === recipeId);
  };

  return (
    <div className="recipe-selector-overlay">
      <div className="recipe-selector-modal">
        <div className="modal-header">
          <h2>Select Recipes</h2>
          <button className="close-btn" onClick={onCancel}>×</button>
        </div>

        <div className="selection-counter">
          Selected: {selectedRecipes.length} / {maxSelection}
        </div>

        <div className="search-filters">
          <div className="search-bar">
            <input
              type="text"
              placeholder="Search recipes..."
              value={searchTerm}
              onChange={(e) => handleSearchChange(e.target.value)}
              onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
            />
            {searchTerm.trim() ? (
              <>
                <button className="btn btn-primary" onClick={handleSearch}>
                  Search
                </button>
                <button className="btn btn-secondary" onClick={handleClearSearch}>
                  Clear
                </button>
              </>
            ) : (
              <button className="btn btn-primary" onClick={handleShowAll}>
                Show All
              </button>
            )}
          </div>
        </div>

        {error && (
          <div className="error-message" style={{ 
            margin: '0 var(--spacing-lg)', 
            padding: 'var(--spacing-md)', 
            backgroundColor: '#f8d7da', 
            color: '#721c24', 
            borderRadius: 'var(--border-radius-md)',
            border: '1px solid #f5c6cb'
          }}>
            {error}
          </div>
        )}

        {loading ? (
          <div className="loading-container">
            <div className="spinner"></div>
            <p>Loading recipes...</p>
          </div>
        ) : !hasSearched ? (
          <div className="empty-state" style={{ backgroundColor: '#fff' }}>
            <p style={{ color: '#000' }}>Search for recipes to add to your meal plan</p>
          </div>
        ) : (
          <div className="recipes-grid">
            {recipes.length === 0 ? (
              <div className="empty-state">
                <p style={{ color: '#000' }}>No recipes found. Try a different search term or click "Show All".</p>
              </div>
            ) : (
              recipes.map((recipe) => (
                <div
                  key={recipe.id}
                  className={`recipe-card ${isRecipeSelected(recipe.id) ? 'selected' : ''}`}
                  onClick={() => handleToggleRecipe(recipe)}
                >
                  <div className="recipe-content">
                    <h4>{recipe.recipeName}</h4>
                    {recipe.hasAllergyWarning && (
                      <div className="allergy-badge">⚠️ Contains allergens</div>
                    )}
                    {recipe.isFavorite && (
                      <div className="favorite-badge">⭐ Favorite</div>
                    )}
                  </div>
                  {isRecipeSelected(recipe.id) && (
                    <div className="selected-indicator">✓</div>
                  )}
                </div>
              ))
            )}
          </div>
        )}

        {selectedRecipes.length > 0 && (
          <div className="selected-recipes-section">
            <h3>Selected Recipes ({selectedRecipes.length}/{maxSelection})</h3>
            <div className="selected-recipes-list">
              {selectedRecipes.map((recipe) => (
                <div key={recipe.id} className="selected-recipe-item">
                  <span className="recipe-name">{recipe.recipeName}</span>
                  <button
                    className="btn-remove-recipe"
                    onClick={() => toggleRecipeSelection(recipe)}
                    title="Remove from selection"
                  >
                    ✕
                  </button>
                </div>
              ))}
            </div>
          </div>
        )}

        <div className="modal-actions">
          <button className="btn btn-secondary" onClick={onCancel}>
            Cancel
          </button>
          <button
            className="btn btn-primary"
            onClick={handleConfirm}
            disabled={selectedRecipes.length === 0}
          >
            Confirm Selection ({selectedRecipes.length})
          </button>
        </div>

        {showAllergyWarning && allergyRecipe && (
          <div className="allergy-warning-overlay">
            <div className="allergy-warning-modal">
              <h3>⚠️ Allergy Warning</h3>
              <p>
                The recipe "<strong>{allergyRecipe.recipeName}</strong>" contains the following allergens:
              </p>
              <ul className="allergens-list">
                {allergyRecipe.allergens.map((allergen, idx) => (
                  <li key={idx}>{allergen}</li>
                ))}
              </ul>
              <p>Do you want to add this recipe anyway?</p>
              <div className="warning-actions">
                <button className="btn btn-secondary" onClick={handleCancelAllergyWarning}>
                  Cancel
                </button>
                <button className="btn btn-warning" onClick={handleConfirmAllergyWarning}>
                  Add Anyway
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default RecipeSelector;
