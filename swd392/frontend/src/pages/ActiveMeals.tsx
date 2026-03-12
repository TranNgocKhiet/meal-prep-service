import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import Container from '../components/layout/Container';
import MealFinishConfirmModal from '../components/mealplan/MealFinishConfirmModal';
import MealUnfinishConfirmModal from '../components/mealplan/MealUnfinishConfirmModal';
import apiClient from '../config/api';
import './ActiveMeals.css';

interface Recipe {
  id: string;
  name: string;
  description: string;
  instructions: string;
  category: string;
  preparationTimeMinutes: number;
  difficultyLevel: string;
  servings: number;
  imageUrl: string;
  ingredients: RecipeIngredient[];
  hasAllergyWarning: boolean;
  allergens: string[];
}

interface RecipeIngredient {
  ingredient: {
    id: string;
    name: string;
    unit: string;
  };
  quantity: number;
  unit: string;
  isOptional: boolean;
}

interface Meal {
  id: string;
  mealType: string;
  status: string;
  date: string;
  completedAt?: string;
  mealPlanId: string;
  recipes: Recipe[];
}

const ActiveMeals = () => {
  const [meals, setMeals] = useState<Meal[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [expandedMeal, setExpandedMeal] = useState<string | null>(null);
  const [showFinishModal, setShowFinishModal] = useState(false);
  const [selectedMeal, setSelectedMeal] = useState<{ mealPlanId: string; mealId: string } | null>(null);
  const [checkData, setCheckData] = useState<any>(null);
  const [checkingIngredients, setCheckingIngredients] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    fetchActiveMeals();
  }, []);

  const fetchActiveMeals = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get('/mealtracking/active');
      if (response.data.success) {
        setMeals(response.data.data);
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load active meals');
    } finally {
      setLoading(false);
    }
  };

  const handleMarkMealFinished = async (mealPlanId: string, mealId: string) => {
    setSelectedMeal({ mealPlanId, mealId });
    setShowFinishModal(true);
    setCheckingIngredients(true);
    
    try {
      const response = await apiClient.get(`/mealtracking/${mealPlanId}/meals/${mealId}/check`);
      if (response.data.success) {
        setCheckData(response.data.data);
      } else {
        throw new Error(response.data.message || 'Failed to check ingredients');
      }
    } catch (err: any) {
      console.error('Error checking ingredients:', err);
      
      let errorMessage = 'Failed to check ingredients. Please try again.';
      
      if (err.response) {
        if (err.response.status === 401) {
          errorMessage = 'Please log in to continue.';
        } else if (err.response.status === 404) {
          errorMessage = 'Meal not found. Please refresh the page.';
        } else if (err.response.data?.message) {
          errorMessage = err.response.data.message;
        }
      }
      
      alert(errorMessage);
      setShowFinishModal(false);
      setSelectedMeal(null);
    } finally {
      setCheckingIngredients(false);
    }
  };

  const confirmFinishMeal = async () => {
    if (!selectedMeal) return;

    try {
      await apiClient.post(`/mealtracking/${selectedMeal.mealPlanId}/meals/${selectedMeal.mealId}/finish`);
      setShowFinishModal(false);
      setSelectedMeal(null);
      setCheckData(null);
      await fetchActiveMeals();
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to mark meal as finished');
    }
  };

  const closeFinishModal = () => {
    setShowFinishModal(false);
    setSelectedMeal(null);
    setCheckData(null);
  };

  const toggleMealExpansion = (mealId: string) => {
    setExpandedMeal(expandedMeal === mealId ? null : mealId);
  };

  const getStatusBadgeClass = (status: string) => {
    switch (status.toLowerCase()) {
      case 'finished':
        return 'status-badge status-finished';
      case 'expired':
        return 'status-badge status-expired';
      case 'pending':
        return 'status-badge status-pending';
      default:
        return 'status-badge';
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    const today = new Date();
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);

    if (date.toDateString() === today.toDateString()) {
      return 'Today';
    } else if (date.toDateString() === tomorrow.toDateString()) {
      return 'Tomorrow';
    } else {
      return date.toLocaleDateString('en-US', {
        weekday: 'short',
        month: 'short',
        day: 'numeric'
      });
    }
  };

  if (loading) {
    return (
      <Container>
        <div className="loading-container">
          <div className="spinner"></div>
          <p>Loading active meals...</p>
        </div>
      </Container>
    );
  }

  if (error) {
    return (
      <Container>
        <div className="error-container">
          <p>{error}</p>
          <button className="btn btn-primary" onClick={() => navigate('/meal-plans')}>
            Go to Meal Plans
          </button>
        </div>
      </Container>
    );
  }

  return (
    <Container>
      <div className="active-meals-page">
        <div className="page-header">
          <h1>Active Meals</h1>
          <button
            className="btn btn-secondary"
            onClick={() => navigate('/meal-plans')}
          >
            View All Plans
          </button>
        </div>

        {meals.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">🍽️</div>
            <h2>No Active Meals</h2>
            <p>You don't have any pending meals at the moment</p>
            <button
              className="btn btn-primary"
              onClick={() => navigate('/meal-plans/create')}
            >
              Create Meal Plan
            </button>
          </div>
        ) : (
          <div className="meals-list">
            {meals.map((meal) => (
              <div key={meal.id} className="meal-card">
                <div className="meal-card-header">
                  <div className="meal-info">
                    <h3>{meal.mealType}</h3>
                    <span className="meal-date">{formatDate(meal.date)}</span>
                    <span className={getStatusBadgeClass(meal.status)}>
                      {meal.status}
                    </span>
                  </div>
                  <div className="meal-actions">
                    {meal.status.toLowerCase() === 'pending' && (
                      <button
                        className="btn btn-sm btn-primary"
                        onClick={() => handleMarkMealFinished(meal.mealPlanId, meal.id)}
                      >
                        Mark Finished
                      </button>
                    )}
                    <button
                      className="btn btn-sm btn-secondary"
                      onClick={() => toggleMealExpansion(meal.id)}
                    >
                      {expandedMeal === meal.id ? 'Hide Details' : 'Show Details'}
                    </button>
                  </div>
                </div>

                {expandedMeal === meal.id && (
                  <div className="meal-details">
                    {meal.recipes.map((recipe) => (
                      <div key={recipe.id} className="recipe-detail">
                        <div className="recipe-header">
                          <div className="recipe-info">
                            <h4>{recipe.recipeName}</h4>
                            <p className="recipe-description">{recipe.instructions}</p>
                            <div className="recipe-meta">
                              <span>⏱️ {recipe.preparationTimeMinutes} min</span>
                              <span>👨‍🍳 {recipe.difficultyLevel}</span>
                              <span>🍽️ {recipe.servings} servings</span>
                              <span>📁 {recipe.category}</span>
                            </div>
                            {recipe.hasAllergyWarning && (
                              <div className="allergy-warning">
                                ⚠️ Contains: {recipe.allergens.join(', ')}
                              </div>
                            )}
                          </div>
                        </div>

                        <div className="recipe-content">
                          <div className="recipe-section">
                            <h5>Ingredients</h5>
                            <ul className="ingredients-list">
                              {recipe.ingredients.map((ing, idx) => (
                                <li key={idx}>
                                  <span className="ingredient-quantity">
                                    {ing.quantity} {ing.unit}
                                  </span>
                                  <span className="ingredient-name">
                                    {ing.ingredient.name}
                                  </span>
                                  {ing.isOptional && (
                                    <span className="optional-badge">optional</span>
                                  )}
                                </li>
                              ))}
                            </ul>
                          </div>

                          <div className="recipe-section">
                            <h5>Instructions</h5>
                            <div className="instructions">
                              {recipe.instructions}
                            </div>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>

      <MealFinishConfirmModal
        isOpen={showFinishModal}
        onClose={closeFinishModal}
        onConfirm={confirmFinishMeal}
        checkData={checkData}
        loading={checkingIngredients}
      />

      <MealUnfinishConfirmModal
        isOpen={false}
        onClose={() => {}}
        onConfirm={() => {}}
        ingredients={[]}
        loading={false}
      />
    </Container>
  );
};

export default ActiveMeals;
