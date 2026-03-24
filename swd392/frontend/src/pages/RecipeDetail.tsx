import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import './RecipeDetail.css';

interface RecipeIngredient {
  ingredientId: string;
  ingredientName: string;
  amount?: number;
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

const RecipeDetail = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [recipe, setRecipe] = useState<Recipe | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    if (id) {
      fetchRecipe(id);
    }
  }, [id]);

  const fetchRecipe = async (recipeId: string) => {
    try {
      setLoading(true);
      const response = await apiClient.get(`/recipes/${recipeId}`);
      if (response.data.success) {
        setRecipe(response.data.data);
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load recipe');
    } finally {
      setLoading(false);
    }
  };

  const toggleFavorite = async () => {
    if (!recipe) return;

    try {
      if (recipe.isFavorite) {
        await apiClient.delete(`/recipes/${recipe.id}/favorite`);
      } else {
        await apiClient.post(`/recipes/${recipe.id}/favorite`);
      }
      
      setRecipe({ ...recipe, isFavorite: !recipe.isFavorite });
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to update favorite');
    }
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

  if (loading) {
    return (
      <Container>
        <div className="loading-container">
          <div className="spinner"></div>
          <p>Loading recipe...</p>
        </div>
      </Container>
    );
  }

  if (error || !recipe) {
    return (
      <Container>
        <div className="error-state">
          <div className="error-icon">❌</div>
          <h2>Error Loading Recipe</h2>
          <p>{error || 'Recipe not found'}</p>
          <button className="btn btn-primary" onClick={() => navigate('/recipes')}>
            Back to Recipes
          </button>
        </div>
      </Container>
    );
  }

  return (
    <Container>
      <div className="recipe-detail-page">
        <div className="page-header">
          <button className="btn-back" onClick={() => navigate('/recipes')}>
            ← Back to Recipes
          </button>
        </div>

        <div className="recipe-detail-content">
          <div className="recipe-header">
            <h1>{recipe.recipeName ?? recipe.name}</h1>
            <button
              className={`favorite-btn-large ${recipe.isFavorite ? 'is-favorite' : ''}`}
              onClick={toggleFavorite}
            >
              {recipe.isFavorite ? '❤️ Favorited' : '🤍 Add to Favorites'}
            </button>
          </div>

          {recipe.hasAllergyWarning && (
            <div className="allergy-warning-prominent">
              <span className="warning-icon">⚠️</span>
              <div>
                <strong>Allergy Warning</strong>
                <p>This recipe contains: {recipe.allergens?.join(', ')}</p>
              </div>
            </div>
          )}

          <div className="recipe-meta-grid">
            <div className="meta-card">
              <span className="meta-label">Category</span>
              <span className="meta-value">{recipe.category}</span>
            </div>
            <div className="meta-card">
              <span className="meta-label">Prep Time</span>
              <span className="meta-value">{recipe.preparationTimeMinutes} min</span>
            </div>
            <div className="meta-card">
              <span className="meta-label">Difficulty</span>
              <span className={`difficulty-badge ${getDifficultyClass(recipe.difficultyLevel)}`}>
                {recipe.difficultyLevel}
              </span>
            </div>
            <div className="meta-card">
              <span className="meta-label">Servings</span>
              <span className="meta-value">{recipe.servings}</span>
            </div>
          </div>

          <div className="recipe-section">
            <h2>Instructions</h2>
            <p className="recipe-description-text">{recipe.instructions}</p>
          </div>

          {recipe.ingredients && recipe.ingredients.length > 0 && (
            <div className="recipe-section">
              <h2>Ingredients</h2>
              <ul className="ingredients-list">
                {recipe.ingredients.map((ing, index) => (
                  <li key={index}>
                    <span className="ingredient-quantity">
                      {ing.amount ?? ing.quantity} {ing.unit}
                    </span>
                    <span className="ingredient-name">{ing.ingredientName}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}

          <div className="recipe-section">
            <h2>Instructions</h2>
            <div className="instructions-content">
              {recipe.instructions.split('\n').map((line, index) => (
                line.trim() && <p key={index}>{line}</p>
              ))}
            </div>
          </div>
        </div>
      </div>
    </Container>
  );
};

export default RecipeDetail;
