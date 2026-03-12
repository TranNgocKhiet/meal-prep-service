import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import Container from '../components/layout/Container';
import RecipeSelector from '../components/mealplan/RecipeSelector';
import apiClient from '../config/api';
import './CreateMealPlan.css';

interface Recipe {
  id: string;
  recipeName: string;
  category: string;
  preparationTimeMinutes: number;
  imageUrl: string;
  hasAllergyWarning: boolean;
  allergens: string[];
}

interface Meal {
  id: string;
  mealTypeId: number;
  recipeIds: string[];
  recipes?: Recipe[];
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
}

interface ApiError {
  response?: {
    data?: {
      message?: string;
    };
  };
  message?: string;
}

const EditMealPlan = () => {
  const { id } = useParams<{ id: string }>();
  const [name, setName] = useState('');
  const [days, setDays] = useState<MealPlanDay[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [showRecipeSelector, setShowRecipeSelector] = useState(false);
  const [currentSelection, setCurrentSelection] = useState<{ dayIndex: number; mealIndex: number } | null>(null);
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

  const fetchMealPlan = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get(`/mealplans/${id}`);
      if (response.data.success) {
        const plan: MealPlan = response.data.data;
        setName(plan.name);
        
        // Transform the data to match our editing structure
        const transformedDays = plan.days.map(day => ({
          id: day.id,
          dayNumber: day.dayNumber,
          date: day.date,
          meals: day.meals.map(meal => ({
            id: meal.id,
            mealTypeId: meal.mealTypeId,
            recipeIds: meal.recipes?.map(r => r.id) || [],
            recipes: meal.recipes
          }))
        }));
        
        setDays(transformedDays);
      }
    } catch (err) {
      setError((err as ApiError).response?.data?.message || 'Failed to load meal plan');
    } finally {
      setLoading(false);
    }
  };

  const handleSelectRecipes = (dayIndex: number, mealIndex: number) => {
    setCurrentSelection({ dayIndex, mealIndex });
    setShowRecipeSelector(true);
  };

  const handleRecipesSelected = (selectedRecipes: Recipe[]) => {
    if (!currentSelection) return;

    const { dayIndex, mealIndex } = currentSelection;
    const newDays = [...days];
    newDays[dayIndex].meals[mealIndex].recipeIds = selectedRecipes.map(r => r.id);
    setDays(newDays);
    setShowRecipeSelector(false);
    setCurrentSelection(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!name.trim()) {
      setError('Please enter a meal plan name');
      return;
    }

    setSaving(true);
    setError('');

    try {
      const payload = {
        name: name.trim(),
        days: days.map(day => ({
          id: day.id,
          dayNumber: day.dayNumber,
          date: day.date,
          meals: day.meals.map(meal => ({
            id: meal.id,
            mealTypeId: meal.mealTypeId,
            recipeIds: meal.recipeIds
          }))
        }))
      };

      const response = await apiClient.put(`/mealplans/${id}`, payload);
      
      if (response.data.success) {
        navigate(`/meal-plans/${id}`);
      } else {
        setError(response.data.message || 'Failed to update meal plan');
      }
    } catch (err) {
      setError((err as ApiError).response?.data?.message || 'Failed to update meal plan');
    } finally {
      setSaving(false);
    }
  };

  const getRecipeCount = (dayIndex: number, mealIndex: number) => {
    return days[dayIndex]?.meals[mealIndex]?.recipeIds.length || 0;
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

  return (
    <Container>
      <div className="create-meal-plan-page">
        <div className="page-header">
          <h1>Edit Meal Plan</h1>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => navigate(`/meal-plans/${id}`)}
          >
            Cancel
          </button>
        </div>

        {error && <div className="error-message">{error}</div>}

        <form onSubmit={handleSubmit} className="meal-plan-form">
          <div className="form-section">
            <h2>Basic Information</h2>
            
            <div className="form-group">
              <label htmlFor="name">Plan Name *</label>
              <input
                type="text"
                id="name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="e.g., Weekly Healthy Meals"
                required
              />
            </div>
          </div>

          <div className="form-section">
            <h2>Meal Schedule</h2>
            <p className="section-description">
              Update recipes for each meal. You can add up to 10 recipes per meal slot.
            </p>

            <div className="days-container">
              {days.map((day, dayIndex) => (
                <div key={day.dayNumber} className="day-card">
                  <h3>Day {day.dayNumber}</h3>
                  <p className="day-date" style={{ color: '#666', fontSize: '14px', marginBottom: '12px' }}>
                    {new Date(day.date).toLocaleDateString('en-US', {
                      weekday: 'long',
                      month: 'long',
                      day: 'numeric'
                    })}
                  </p>
                  <div className="meals-grid">
                    {day.meals.map((meal, mealIndex) => (
                      <div key={meal.id} className="meal-slot">
                        <div className="meal-header">
                          <h4>{getMealTypeName(meal.mealTypeId)}</h4>
                          <span className="recipe-count">
                            {getRecipeCount(dayIndex, mealIndex)}/10 recipes
                          </span>
                        </div>
                        <button
                          type="button"
                          className="btn btn-sm btn-secondary"
                          onClick={() => handleSelectRecipes(dayIndex, mealIndex)}
                          disabled={getRecipeCount(dayIndex, mealIndex) >= 10}
                        >
                          {getRecipeCount(dayIndex, mealIndex) === 0 
                            ? 'Add Recipes' 
                            : 'Manage Recipes'}
                        </button>
                        {meal.recipeIds.length > 0 && (
                          <div className="selected-recipes-preview">
                            {meal.recipeIds.length} recipe{meal.recipeIds.length > 1 ? 's' : ''} selected
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="form-actions">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => navigate(`/meal-plans/${id}`)}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={saving}
            >
              {saving ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        </form>

        {showRecipeSelector && currentSelection && (
          <RecipeSelector
            maxSelection={10}
            selectedRecipeIds={days[currentSelection.dayIndex].meals[currentSelection.mealIndex].recipeIds}
            onConfirm={handleRecipesSelected}
            onCancel={() => {
              setShowRecipeSelector(false);
              setCurrentSelection(null);
            }}
          />
        )}
      </div>
    </Container>
  );
};

export default EditMealPlan;
