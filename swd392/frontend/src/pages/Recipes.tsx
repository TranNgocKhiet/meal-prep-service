import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import './Recipes.css';

interface RecipeIngredient {
  ingredientId: string;
  ingredientName: string;
  quantity: number;
  unit: string;
  isOptional: boolean;
}

interface Recipe {
  id: string;
  recipeName?: string;
  name: string;
  description: string;
  instructions: string;
  category: string;
  preparationTimeMinutes: number;
  difficultyLevel: string;
  servings: number;
  imageUrl: string;
  ingredients?: RecipeIngredient[];
  hasAllergyWarning: boolean;
  allergens?: string[];
  isFavorite: boolean;
}

const Recipes = () => {
  const [recipes, setRecipes] = useState<Recipe[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  // Search and filter state
  const [searchTerm, setSearchTerm] = useState('');
  const [category, setCategory] = useState('');
  const [maxPrepTime, setMaxPrepTime] = useState('');
  const [difficultyLevel, setDifficultyLevel] = useState('');
  const [showFavoritesOnly, setShowFavoritesOnly] = useState(false);

  const categories = ['Breakfast', 'Lunch', 'Dinner', 'Snack', 'Dessert', 'Appetizer'];
  const difficulties = ['Easy', 'Medium', 'Hard'];

  useEffect(() => {
    const loadRecipes = async () => {
      try {
        setLoading(true);
        setError('');

        const searchDto = {
          searchTerm: undefined,
          category: undefined,
          maxPreparationTime: undefined,
          difficultyLevel: undefined,
          excludeAllergens: true,
        };

        const response = await apiClient.post('/recipes/search', searchDto);
        if (response.data.success) {
          setRecipes(response.data.data);
        }
      } catch (err) {
        const error = err as { response?: { data?: { message?: string } } };
        setError(error.response?.data?.message || 'Failed to search recipes');
      } finally {
        setLoading(false);
      }
    };

    loadRecipes();
  }, []);

  const searchRecipes = async () => {
    try {
      setLoading(true);
      setError('');

      const searchDto = {
        searchTerm: searchTerm || undefined,
        category: category || undefined,
        maxPreparationTime: maxPrepTime ? parseInt(maxPrepTime) : undefined,
        difficultyLevel: difficultyLevel || undefined,
        excludeAllergens: true,
      };

      const response = await apiClient.post('/recipes/search', searchDto);
      if (response.data.success) {
        let results = response.data.data;
        
        // Filter favorites if needed
        if (showFavoritesOnly) {
          results = results.filter((r: Recipe) => r.isFavorite);
        }
        
        setRecipes(results);
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to search recipes');
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    searchRecipes();
  };

  const handleReset = () => {
    setSearchTerm('');
    setCategory('');
    setMaxPrepTime('');
    setDifficultyLevel('');
    setShowFavoritesOnly(false);
  };

  const viewRecipeDetail = (recipeId: string) => {
    navigate(`/recipes/${recipeId}`);
  };

  const highlightSearchTerm = (text: string) => {
    if (!searchTerm) return text;
    
    const regex = new RegExp(`(${searchTerm})`, 'gi');
    const parts = text.split(regex);
    
    return parts.map((part, index) => 
      regex.test(part) ? <mark key={index}>{part}</mark> : part
    );
  };

  const getDifficultyClass = (difficulty: string) => {
    switch (difficulty.toLowerCase()) {
      case 'easy':
        return 'difficulty-easy';
      case 'medium':
        return 'difficulty-medium';
      case 'hard':
        return 'difficulty-hard';
      default:
        return '';
    }
  };

  return (
    <Container>
      <div className="recipes-page">
        <div className="page-header">
          <h1>Recipe Search</h1>
        </div>

        <div className="search-section">
          <form onSubmit={handleSearch} className="search-form">
            <div className="search-row">
              <div className="form-group flex-grow">
                <input
                  type="text"
                  placeholder="Search by name or ingredient..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="search-input"
                />
              </div>
              <button type="submit" className="btn btn-primary" disabled={loading}>
                {loading ? 'Searching...' : 'Search'}
              </button>
            </div>

            <div className="filters-row">
              <div className="form-group">
                <label htmlFor="category">Category</label>
                <select
                  id="category"
                  value={category}
                  onChange={(e) => setCategory(e.target.value)}
                  className="filter-select"
                >
                  <option value="">All Categories</option>
                  {categories.map(cat => (
                    <option key={cat} value={cat}>{cat}</option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label htmlFor="prepTime">Max Prep Time (min)</label>
                <input
                  type="number"
                  id="prepTime"
                  value={maxPrepTime}
                  onChange={(e) => setMaxPrepTime(e.target.value)}
                  placeholder="Any"
                  min="0"
                  className="filter-input"
                />
              </div>

              <div className="form-group">
                <label htmlFor="difficulty">Difficulty</label>
                <select
                  id="difficulty"
                  value={difficultyLevel}
                  onChange={(e) => setDifficultyLevel(e.target.value)}
                  className="filter-select"
                >
                  <option value="">All Levels</option>
                  {difficulties.map(diff => (
                    <option key={diff} value={diff}>{diff}</option>
                  ))}
                </select>
              </div>

              <div className="form-group checkbox-group">
                <label className="checkbox-label">
                  <input
                    type="checkbox"
                    checked={showFavoritesOnly}
                    onChange={(e) => setShowFavoritesOnly(e.target.checked)}
                  />
                  <span>Favorites Only</span>
                </label>
              </div>

              <button type="button" onClick={handleReset} className="btn btn-secondary">
                Reset
              </button>
            </div>
          </form>
        </div>

        {error && <div className="error-message">{error}</div>}

        {loading ? (
          <div className="loading-container">
            <div className="spinner"></div>
            <p>Searching recipes...</p>
          </div>
        ) : recipes.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">🍳</div>
            <h2>No Recipes Found</h2>
            <p>Try adjusting your search filters</p>
          </div>
        ) : (
          <>
            <div className="results-summary">
              Found {recipes.length} recipe{recipes.length !== 1 ? 's' : ''}
            </div>

            <div className="recipes-grid">
              {recipes.map((recipe) => (
                <div key={recipe.id} className="recipe-card">
                  <div className="recipe-content">
                    <h3>{highlightSearchTerm(recipe.recipeName ?? recipe.name)}</h3>
                    <p className="recipe-description">
                      {highlightSearchTerm(recipe.instructions)}
                    </p>

                    {recipe.hasAllergyWarning && (
                      <div className="allergy-warning">
                        ⚠️ Contains allergens: {recipe.allergens?.join(', ')}
                      </div>
                    )}

                    <div className="recipe-meta">
                      <span className="meta-item">
                        <span className="meta-icon">📁</span>
                        {recipe.category}
                      </span>
                      <span className="meta-item">
                        <span className="meta-icon">⏱️</span>
                        {recipe.preparationTimeMinutes} min
                      </span>
                      <span className="meta-item">
                        <span className="meta-icon">👥</span>
                        {recipe.servings} servings
                      </span>
                    </div>

                    <div className="recipe-footer">
                      <span className={`difficulty-badge ${getDifficultyClass(recipe.difficultyLevel)}`}>
                        {recipe.difficultyLevel}
                      </span>
                      <button
                        className="btn btn-sm btn-primary"
                        onClick={() => viewRecipeDetail(recipe.id)}
                      >
                        View Details
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </>
        )}
      </div>
    </Container>
  );
};

export default Recipes;
