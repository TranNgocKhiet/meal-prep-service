import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import Container from '../components/layout/Container';
import MealPlanProgress from '../components/mealplan/MealPlanProgress';
import MealFinishConfirmModal from '../components/mealplan/MealFinishConfirmModal';
import MealUnfinishConfirmModal from '../components/mealplan/MealUnfinishConfirmModal';
import apiClient from '../config/api';
import './MealPlanDetail.css';

interface Recipe {
  id: string;
  recipeName: string;
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
  ingredient?: {
    id?: string;
    name?: string;
    unit?: string;
  };
  ingredientName?: string;
  amount?: number;
  quantity?: number;
  unit?: string;
}

interface Meal {
  id: string;
  mealTypeId: number;
  status: string;
  completedAt?: string;
  recipes?: Recipe[];
  totalCalories?: number;
  proteinG?: number;
  fatG?: number;
  carbsG?: number;
}

interface MealPlanDay {
  id?: string;
  dayNumber: number;
  date: string;
  meals: Meal[];
}

interface MealPlan {
  id: string;
  name: string;
  durationDays: number;
  startDate: string;
  endDate: string;
  isAiGenerated: boolean;
  status: string;
  days: MealPlanDay[];
  createdAt: string;
  age?: number;
  weight?: number;
  height?: number;
  gender?: string;
  healthNote?: string;
  caloriesGoal?: number;
}

const MealPlanDetail = () => {
  const { id } = useParams<{ id: string }>();
  const [mealPlan, setMealPlan] = useState<MealPlan | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [expandedMeal, setExpandedMeal] = useState<string | null>(null);
  const [showFinishModal, setShowFinishModal] = useState(false);
  const [selectedMealId, setSelectedMealId] = useState<string | null>(null);
  const [checkData, setCheckData] = useState<any>(null);
  const [checkingIngredients, setCheckingIngredients] = useState(false);
  const [showUnfinishModal, setShowUnfinishModal] = useState(false);
  const [unfinishCheckData, setUnfinishCheckData] = useState<any>(null);
  const [checkingUnfinish, setCheckingUnfinish] = useState(false);
  const [showRemoveModal, setShowRemoveModal] = useState(false);
  const [removeTarget, setRemoveTarget] = useState<{ mealId: string; recipeId: string; recipeName: string } | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    fetchMealPlan();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const getMealTypeName = (mealTypeId: number) => {
    switch (mealTypeId) {
      case 1: return 'Breakfast';
      case 2: return 'Lunch';
      case 3: return 'Dinner';
      default: return 'Unknown';
    }
  };

  const getDayHeaderClass = (dateValue: string) => {
    const datePart = dateValue.split('T')[0];
    const [year, month, day] = datePart.split('-').map(Number);
    const headerDate = new Date(year, month - 1, day);

    const today = new Date();
    const todayOnly = new Date(today.getFullYear(), today.getMonth(), today.getDate());

    if (headerDate < todayOnly) {
      return 'day-header-past';
    }

    if (headerDate.getTime() === todayOnly.getTime()) {
      return 'day-header-today';
    }

    return 'day-header-future';
  };

  const fetchMealPlan = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get(`/mealplans/${id}`);
      if (response.data.success) {
        setMealPlan(response.data.data);
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load meal plan');
    } finally {
      setLoading(false);
    }
  };

  const handleMarkMealFinished = async (mealId: string) => {
    setSelectedMealId(mealId);
    setShowFinishModal(true);
    setCheckingIngredients(true);
    
    try {
      const response = await apiClient.get(`/mealtracking/${id}/meals/${mealId}/check`);
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
      setSelectedMealId(null);
    } finally {
      setCheckingIngredients(false);
    }
  };

  const confirmFinishMeal = async () => {
    if (!selectedMealId) return;

    try {
      await apiClient.post(`/mealtracking/${id}/meals/${selectedMealId}/finish`);
      setShowFinishModal(false);
      setSelectedMealId(null);
      setCheckData(null);
      await fetchMealPlan();
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to mark meal as finished');
    }
  };

  const closeFinishModal = () => {
    setShowFinishModal(false);
    setSelectedMealId(null);
    setCheckData(null);
  };

  const handleMarkMealUnfinished = async (mealId: string) => {
    setSelectedMealId(mealId);
    setShowUnfinishModal(true);
    setCheckingUnfinish(true);
    
    try {
      const response = await apiClient.get(`/mealtracking/${id}/meals/${mealId}/unfinish-check`);
      if (response.data.success) {
        setUnfinishCheckData(response.data.data);
      } else {
        throw new Error(response.data.message || 'Failed to check ingredients');
      }
    } catch (err: any) {
      console.error('Error checking ingredients to return:', err);
      
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
      setShowUnfinishModal(false);
      setSelectedMealId(null);
    } finally {
      setCheckingUnfinish(false);
    }
  };

  const confirmUnfinishMeal = async (ingredients: any[]) => {
    if (!selectedMealId) return;

    try {
      await apiClient.post(`/mealtracking/${id}/meals/${selectedMealId}/unfinish`, {
        ingredients
      });
      setShowUnfinishModal(false);
      setSelectedMealId(null);
      setUnfinishCheckData(null);
      await fetchMealPlan();
    } catch (err: any) {
      console.error('Error unfinishing meal:', err);
      const errorMessage = err.response?.data?.message || 'Failed to mark meal as unfinished';
      alert(errorMessage);
    }
  };

  const closeUnfinishModal = () => {
    setShowUnfinishModal(false);
    setSelectedMealId(null);
    setUnfinishCheckData(null);
  };

  const requestRemoveRecipe = (mealId: string, recipeId: string, recipeName: string) => {
    setRemoveTarget({ mealId, recipeId, recipeName });
    setShowRemoveModal(true);
  };

  const closeRemoveModal = () => {
    setShowRemoveModal(false);
    setRemoveTarget(null);
  };

  const confirmRemoveRecipe = async () => {
    if (!removeTarget) return;

    try {
      // Get current meal's recipes
      const meal = mealPlan?.days
        .flatMap(d => d.meals)
        .find(m => m.id === removeTarget.mealId);
      
      if (!meal || !meal.recipes) return;

      // Filter out the recipe to remove
      const updatedRecipeIds = meal.recipes
        .filter(r => r.id !== removeTarget.recipeId)
        .map(r => r.id);

      // Update the meal with new recipe list
      await apiClient.put(`/mealplans/${id}`, {
        days: mealPlan?.days.map(day => ({
          id: day.id,
          dayNumber: day.dayNumber,
          date: day.date,
          meals: day.meals.map(m => ({
            id: m.id,
            mealTypeId: m.mealTypeId,
            recipeIds: m.id === removeTarget.mealId ? updatedRecipeIds : m.recipes?.map(r => r.id) || []
          }))
        }))
      });

      await fetchMealPlan();
      closeRemoveModal();
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to remove recipe');
    }
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

  const getMealCardClass = (status: string) => {
    switch (status.toLowerCase()) {
      case 'finished':
        return 'meal-card meal-card-finished';
      case 'expired':
        return 'meal-card meal-card-expired';
      default:
        return 'meal-card';
    }
  };

  const getIngredientDisplay = (ingredient: RecipeIngredient) => {
    const name = ingredient.ingredient?.name || ingredient.ingredientName || 'Unknown ingredient';
    const amount = ingredient.amount ?? ingredient.quantity;
    const unit = ingredient.ingredient?.unit || ingredient.unit;

    if (amount !== undefined && unit) {
      return `${amount} ${unit} ${name}`;
    }

    if (amount !== undefined) {
      return `${amount} ${name}`;
    }

    return name;
  };

  if (loading) {
    return (
      <Container>
        <div className="loading-container">
          <div className="spinner"></div>
          <p>Loading meal plan...</p>
        </div>
      </Container>
    );
  }

  if (error || !mealPlan) {
    return (
      <Container>
        <div className="error-container">
          <p>{error || 'Meal plan not found'}</p>
          <button className="btn btn-primary" onClick={() => navigate('/meal-plans')}>
            Back to Meal Plans
          </button>
        </div>
      </Container>
    );
  }

  return (
    <Container>
      <div className="meal-plan-detail-page">
        <div className="page-header">
          <div className="header-content">
            <h1>{mealPlan.name}</h1>
            {mealPlan.isAiGenerated && (
              <span className="ai-badge">AI Generated</span>
            )}
          </div>
          <div className="header-actions">
            <button
              className="btn btn-secondary"
              onClick={() => navigate(`/meal-plans/${id}/edit`)}
              style={{backgroundColor: '#FFDE21'}}
            >
              Edit
            </button>
            <button
              className="btn btn-secondary"
              onClick={() => navigate('/meal-plans')}
            >
              Back
            </button>
          </div>
        </div>

        <div className="plan-overview">
          <div className="overview-card">
            <h3 style={{ color: '#000' }}>Plan Details</h3>
            <div className="detail-grid">
              <div className="detail-item">
                <span className="label" style={{ color: '#666' }}>Duration:</span>
                <span className="value" style={{ color: '#000' }}>{mealPlan.durationDays} days</span>
              </div>
              <div className="detail-item">
                <span className="label" style={{ color: '#666' }}>Start Date:</span>
                <span className="value" style={{ color: '#000' }}>
                  {new Date(mealPlan.startDate).toLocaleDateString()}
                </span>
              </div>
              <div className="detail-item">
                <span className="label" style={{ color: '#666' }}>End Date:</span>
                <span className="value" style={{ color: '#000' }}>
                  {new Date(mealPlan.endDate).toLocaleDateString()}
                </span>
              </div>
              <div className="detail-item">
                <span className="label" style={{ color: '#666' }}>Status:</span>
                <span className={getStatusBadgeClass(mealPlan.status)} style={{ color: '#000' }}>
                  {mealPlan.status}
                </span>
              </div>
              <div className="detail-item">
                <span className="label" style={{ color: '#666' }}>Created:</span>
                <span className="value" style={{ color: '#000' }}>
                  {new Date(mealPlan.createdAt).toLocaleDateString()}
                </span>
              </div>
            </div>
          </div>

          <div className="overview-card">
            <h3 style={{ color: '#000' }}>Health Profile</h3>
            <div className="detail-grid">
              <div className="detail-item">
                <span className="label" style={{ color: '#666' }}>Age:</span>
                <span className="value" style={{ color: '#000' }}>{mealPlan.age ? `${mealPlan.age} years` : 'N/A'}</span>
              </div>
              <div className="detail-item">
                <span className="label" style={{ color: '#666' }}>Gender:</span>
                <span className="value" style={{ color: '#000' }}>{mealPlan.gender || 'N/A'}</span>
              </div>
              <div className="detail-item">
                <span className="label" style={{ color: '#666' }}>Weight:</span>
                <span className="value" style={{ color: '#000' }}>{mealPlan.weight ? `${mealPlan.weight} kg` : 'N/A'}</span>
              </div>
              <div className="detail-item">
                <span className="label" style={{ color: '#666' }}>Height:</span>
                <span className="value" style={{ color: '#000' }}>{mealPlan.height ? `${mealPlan.height} cm` : 'N/A'}</span>
              </div>
              <div className="detail-item">
                <span className="label" style={{ color: '#666' }}>Calories Goal:</span>
                <span className="value" style={{ color: '#000' }}>{mealPlan.caloriesGoal ? `${mealPlan.caloriesGoal} kcal/day` : 'N/A'}</span>
              </div>
              <div className="detail-item" style={{ gridColumn: '1 / -1' }}>
                <span className="label" style={{ color: '#666' }}>Health Note:</span>
                <span className="value" style={{ color: '#000', whiteSpace: 'pre-wrap' }}>{mealPlan.healthNote || 'N/A'}</span>
              </div>
            </div>
          </div>
        </div>

        <MealPlanProgress mealPlanId={id!} />

        <div className="daily-schedule">
          <h2>Daily Schedule</h2>
          {mealPlan.days.map((day) => (
            <div key={day.dayNumber} className="day-section">
              <div className={`day-header ${getDayHeaderClass(day.date)}`}>
                <h3 className="day-title">Day {day.dayNumber}</h3>
                <span className="day-date">
                  {new Date(day.date).toLocaleDateString('en-US', {
                    weekday: 'long',
                    month: 'long',
                    day: 'numeric'
                  })}
                </span>
              </div>

              <div className="meals-list">
                {day.meals.map((meal) => (
                  <div key={meal.id} className={getMealCardClass(meal.status)}>
                    <div className="meal-card-header">
                      <div className="meal-info">
                        <h4>{getMealTypeName(meal.mealTypeId)}</h4>
                        {meal.recipes && meal.recipes.length > 0 && (
                          <span className="recipe-count">
                            {meal.recipes.length} recipe{meal.recipes.length > 1 ? 's' : ''}
                          </span>
                        )}
                        {meal.status && (
                          <span className={getStatusBadgeClass(meal.status)}>
                            {meal.status}
                          </span>
                        )}
                      </div>
                      <div className="meal-actions">
                        {meal.status.toLowerCase() === 'pending' ? (
                          <button
                            className="btn btn-sm"
                            onClick={() => handleMarkMealFinished(meal.id)}
                          >
                            Mark Finished
                          </button>
                        ) : meal.status.toLowerCase() === 'finished' ? (
                          <button
                            className="btn btn-sm btn-warning"
                            onClick={() => handleMarkMealUnfinished(meal.id)}
                            style={{
                              backgroundColor: '#ffc107',
                              color: '#000',
                              border: 'none'
                            }}
                          >
                            Mark Unfinished
                          </button>
                        ) : null}
                        <button
                          className="btn btn-sm btn-secondary"
                          onClick={() => toggleMealExpansion(meal.id)}
                        >
                          {expandedMeal === meal.id ? 'Hide Details' : 'Show Details'}
                        </button>
                      </div>
                    </div>

                    {/* Nutrition Summary */}
                    {meal.recipes && meal.recipes.length > 0 && (
                      <div className="meal-nutrition">
                        <div className="nutrition-grid">
                          <div className="nutrition-card">
                            <span className="nutrition-label">Calories</span>
                            <span className="nutrition-value">{meal.totalCalories?.toFixed(0) || 0} <small>kcal</small></span>
                          </div>
                          <div className="nutrition-card">
                            <span className="nutrition-label">Protein</span>
                            <span className="nutrition-value">{meal.proteinG?.toFixed(1) || 0} <small>g</small></span>
                          </div>
                          <div className="nutrition-card">
                            <span className="nutrition-label">Fat</span>
                            <span className="nutrition-value">{meal.fatG?.toFixed(1) || 0} <small>g</small></span>
                          </div>
                          <div className="nutrition-card">
                            <span className="nutrition-label">Carbs</span>
                            <span className="nutrition-value">{meal.carbsG?.toFixed(1) || 0} <small>g</small></span>
                          </div>
                        </div>
                      </div>
                    )}

                    {expandedMeal === meal.id && (
                      <div className="meal-recipes">
                        {meal.recipes && meal.recipes.length > 0 ? (
                          meal.recipes.map((recipe) => (
                            <div key={recipe.id} className="recipe-detail">
                              <div className="recipe-header">
                                <div className="recipe-info">
                                  <h5>{recipe.recipeName}</h5>
                                  {recipe.hasAllergyWarning && recipe.allergens && (
                                    <div className="allergy-warning">
                                      ⚠️ Contains: {recipe.allergens.join(', ')}
                                    </div>
                                  )}
                                </div>
                                <button
                                  className="btn btn-sm btn-danger"
                                  onClick={() => requestRemoveRecipe(meal.id, recipe.id, recipe.recipeName)}
                                  style={{
                                    backgroundColor: '#dc3545',
                                    color: '#fff',
                                    border: 'none',
                                    padding: '4px 12px',
                                    borderRadius: '4px',
                                    cursor: 'pointer',
                                    fontSize: '14px'
                                  }}
                                >
                                  Remove
                                </button>
                              </div>

                              <div className="recipe-content">
                                <div className="recipe-section">
                                  <h6>Ingredients</h6>
                                  {recipe.ingredients && recipe.ingredients.length > 0 ? (
                                    <ul className="ingredients-list">
                                      {recipe.ingredients.map((ing, idx) => (
                                        <li key={ing.ingredient?.id || `${recipe.id}-${idx}`}>
                                          {getIngredientDisplay(ing)}
                                        </li>
                                      ))}
                                    </ul>
                                  ) : (
                                    <p className="instructions">No ingredient data available.</p>
                                  )}
                                </div>

                                <div className="recipe-section">
                                  <h6>Instructions</h6>
                                  <p className="instructions">{recipe.instructions}</p>
                                </div>
                              </div>
                            </div>
                          ))
                        ) : (
                          <div className="no-recipes">
                            <p style={{ color: '#000' }}>No recipes added to this meal yet.</p>
                            <button
                              className="btn btn-sm"
                              onClick={() => navigate(`/meal-plans/${id}/edit`)}
                            >
                              Add Recipes
                            </button>
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>

      <MealFinishConfirmModal
        isOpen={showFinishModal}
        onClose={closeFinishModal}
        onConfirm={confirmFinishMeal}
        checkData={checkData}
        loading={checkingIngredients}
      />

      <MealUnfinishConfirmModal
        isOpen={showUnfinishModal}
        onClose={closeUnfinishModal}
        onConfirm={confirmUnfinishMeal}
        ingredients={unfinishCheckData?.ingredients || []}
        loading={checkingUnfinish}
      />

      {showRemoveModal && removeTarget && (
        <div className="remove-confirm-overlay" onClick={closeRemoveModal}>
          <div className="remove-confirm-modal" onClick={(e) => e.stopPropagation()}>
            <div className="remove-confirm-header">
              <h3>Remove Recipe</h3>
              <button className="remove-confirm-close" onClick={closeRemoveModal}>×</button>
            </div>
            <div className="remove-confirm-body">
              <p style= {{color: '#000000'}}>
                Are you sure you want to remove <strong>{removeTarget.recipeName}</strong> from this meal?
              </p>
            </div>
            <div className="remove-confirm-actions">
              <button className="btn btn-secondary" onClick={closeRemoveModal}>
                Cancel
              </button>
              <button className="btn btn-danger" onClick={confirmRemoveRecipe}>
                Remove
              </button>
            </div>
          </div>
        </div>
      )}
    </Container>
  );
};

export default MealPlanDetail;
