import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import './CreateMealPlan.css';

interface Allergy {
  id: string;
  name: string;
  description: string;
}

interface Ingredient {
  id: string;
  name: string;
  category: string;
}

const CreateMealPlan = () => {
  const [name, setName] = useState('');
  const [durationDays, setDurationDays] = useState(7);
  const [startDate, setStartDate] = useState(new Date().toISOString().split('T')[0]);
  const [useAI, setUseAI] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [showConfirmModal, setShowConfirmModal] = useState(false);
  
  // Personal Information
  const [age, setAge] = useState('');
  const [weight, setWeight] = useState('');
  const [height, setHeight] = useState('');
  const [gender, setGender] = useState('');
  const [healthNote, setHealthNote] = useState('');
  const [caloriesGoal, setCaloriesGoal] = useState('');
  
  // Health Profile data
  const [allergies, setAllergies] = useState<Allergy[]>([]);
  const [selectedAllergies, setSelectedAllergies] = useState<string[]>([]);
  const [ingredientPreferences, setIngredientPreferences] = useState<Record<string, 'like' | 'dislike' | 'allergy' | 'none'>>({});
  const [selectedIngredients, setSelectedIngredients] = useState<Record<string, Ingredient>>({});
  
  // Search state
  const [ingredientSearch, setIngredientSearch] = useState('');
  const [searchResults, setSearchResults] = useState<Ingredient[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  
  const navigate = useNavigate();

  useEffect(() => {
    fetchAllergies();
  }, []);

  const fetchAllergies = async () => {
    try {
      const response = await apiClient.get('/allergies/all');
      if (response.data.success) {
        setAllergies(response.data.data);
      }
    } catch (err) {
      console.error('Failed to load allergies', err);
    }
  };

  const searchIngredients = async (searchTerm: string) => {
    if (!searchTerm.trim()) {
      setSearchResults([]);
      return;
    }

    setIsSearching(true);
    try {
      const response = await apiClient.post('/ingredients/search', {
        searchTerm: searchTerm.trim()
      });
      if (response.data.success) {
        setSearchResults(response.data.data);
      }
    } catch (err) {
      console.error('Failed to search ingredients', err);
    } finally {
      setIsSearching(false);
    }
  };

  const handleSearchChange = (value: string) => {
    setIngredientSearch(value);
    if (value.trim().length >= 2) {
      searchIngredients(value);
    } else {
      setSearchResults([]);
    }
  };

  const handleAllergyToggle = (allergyId: string) => {
    setSelectedAllergies(prev =>
      prev.includes(allergyId)
        ? prev.filter(id => id !== allergyId)
        : [...prev, allergyId]
    );
  };

  const stripHtml = (html: string): string => {
    const tmp = document.createElement('DIV');
    tmp.innerHTML = html;
    return tmp.textContent || tmp.innerText || '';
  };

  const handleIngredientPreference = (ingredient: Ingredient, preference: 'like' | 'dislike' | 'allergy' | 'none') => {
    setIngredientPreferences(prev => {
      const updated = { ...prev };
      if (preference === 'none') {
        delete updated[ingredient.id];
      } else {
        updated[ingredient.id] = preference;
      }
      return updated;
    });
    
    // Store ingredient data
    if (preference !== 'none') {
      setSelectedIngredients(prev => ({
        ...prev,
        [ingredient.id]: ingredient
      }));
      // Clear search after selection
      setIngredientSearch('');
      setSearchResults([]);
    } else {
      setSelectedIngredients(prev => {
        const updated = { ...prev };
        delete updated[ingredient.id];
        return updated;
      });
    }
  };

  const getIngredientPreference = (ingredientId: string): 'like' | 'dislike' | 'allergy' | 'none' => {
    return ingredientPreferences[ingredientId] || 'none';
  };

  const getSelectedIngredientsList = (): Ingredient[] => {
    return Object.values(selectedIngredients);
  };

  const removeIngredient = (ingredientId: string) => {
    setIngredientPreferences(prev => {
      const updated = { ...prev };
      delete updated[ingredientId];
      return updated;
    });
    setSelectedIngredients(prev => {
      const updated = { ...prev };
      delete updated[ingredientId];
      return updated;
    });
  };

  const validateForm = () => {
    if (!name.trim()) {
      setError('Please enter a meal plan name');
      return false;
    }

    if (name.trim().length < 3) {
      setError('Plan name must be at least 3 characters');
      return false;
    }

    if (!age.trim()) {
      setError('Please enter age');
      return false;
    }

    if (!weight.trim()) {
      setError('Please enter weight');
      return false;
    }

    if (!height.trim()) {
      setError('Please enter height');
      return false;
    }

    if (!gender) {
      setError('Please select gender');
      return false;
    }

    if (!caloriesGoal.trim()) {
      setError('Please enter calories goal');
      return false;
    }

    const parsedAge = Number(age);
    const parsedWeight = Number(weight);
    const parsedHeight = Number(height);
    const parsedCaloriesGoal = Number(caloriesGoal);

    if (!Number.isFinite(parsedAge) || parsedAge <= 0) {
      setError('Age must be greater than 0');
      return false;
    }

    if (!Number.isFinite(parsedWeight) || parsedWeight <= 0) {
      setError('Weight must be greater than 0');
      return false;
    }

    if (!Number.isFinite(parsedHeight) || parsedHeight <= 0) {
      setError('Height must be greater than 0');
      return false;
    }

    if (!Number.isFinite(parsedCaloriesGoal) || parsedCaloriesGoal < 500 || parsedCaloriesGoal > 10000) {
      setError('Calories goal must be between 500 and 10000');
      return false;
    }

    return true;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    setError('');
    setSuccessMessage('');

    if (!validateForm()) {
      return;
    }

    setShowConfirmModal(true);
  };

  const handleConfirmCreate = async () => {
    setShowConfirmModal(false);

    setLoading(true);

    try {
      const parsedAge = Number(age);
      const parsedWeight = Number(weight);
      const parsedHeight = Number(height);
      const parsedCaloriesGoal = Number(caloriesGoal);

      // Build arrays from ingredient preferences
      const likedIngredients = Object.entries(ingredientPreferences)
        .filter(([, pref]) => pref === 'like')
        .map(([id]) => id);
      
      const dislikedIngredients = Object.entries(ingredientPreferences)
        .filter(([, pref]) => pref === 'dislike')
        .map(([id]) => id);
      
      const allergyIngredients = Object.entries(ingredientPreferences)
        .filter(([, pref]) => pref === 'allergy')
        .map(([id]) => id);
      
      const payload = {
        name: name.trim(),
        durationDays,
        startDate,
        age: parsedAge,
        weight: parsedWeight,
        height: parsedHeight,
        gender,
        healthNote: healthNote.trim() || null,
        caloriesGoal: parsedCaloriesGoal,
        allergies: selectedAllergies,
        likedIngredients,
        dislikedIngredients,
        allergyIngredients
      };

      const endpoint = useAI ? '/mealplans/ai-generate' : '/mealplans/custom';
      const response = await apiClient.post(endpoint, payload);
      
      if (response.data.success) {
        setSuccessMessage('Meal plan created successfully!');
        setTimeout(() => {
          navigate('/meal-plans');
        }, 1200);
      } else {
        setError(response.data.message || 'Failed to create meal plan');
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } }; message?: string };
      setError(error.response?.data?.message || error.message || 'Failed to create meal plan');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container>
      <div className="create-meal-plan-page">
        <div className="page-header">
          <h1>Create Meal Plan</h1>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => navigate('/meal-plans')}
          >
            Cancel
          </button>
        </div>

        {error && <div className="error-message">{error}</div>}
        {successMessage && <div className="success-message-box">{successMessage}</div>}

        <form onSubmit={handleSubmit} className="meal-plan-form">
          <div className="form-section">
            <h2>Basic Information</h2>
            <p className="section-description">
              Create an empty meal plan. You can add meals to it later.
            </p>
            
            <div className="form-group">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={useAI}
                  onChange={(e) => setUseAI(e.target.checked)}
                />
                <span>Use AI to generate meal plan with recipes</span>
              </label>
              {useAI && (
                <p className="help-text" style={{ color: '#666', fontSize: '14px', marginTop: '8px' }}>
                  AI will analyze your health profile, allergies, preferences, and available ingredients to create a personalized meal plan with recipes.
                </p>
              )}
            </div>
            
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

            <div className="form-row">
              <div className="form-group">
                <label htmlFor="duration">Duration (days) *</label>
                <select
                  id="duration"
                  value={durationDays}
                  onChange={(e) => setDurationDays(Number(e.target.value))}
                  required
                >
                  {[1, 2, 3, 4, 5, 6, 7].map(num => (
                    <option key={num} value={num}>{num} day{num > 1 ? 's' : ''}</option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label htmlFor="startDate">Start Date *</label>
                <input
                  type="date"
                  id="startDate"
                  value={startDate}
                  onChange={(e) => setStartDate(e.target.value)}
                  min={new Date().toISOString().split('T')[0]}
                  required
                />
              </div>
            </div>
          </div>

          <div className="form-section">
            <h2>Personal Information</h2>
            
            <div className="form-row">
              <div className="form-group">
                <label htmlFor="age">Age *</label>
                <input
                  type="number"
                  id="age"
                  value={age}
                  onChange={(e) => setAge(e.target.value)}
                  placeholder="e.g., 25"
                  min="1"
                  max="120"
                  required
                />
              </div>

              <div className="form-group">
                <label htmlFor="gender">Gender *</label>
                <select
                  id="gender"
                  value={gender}
                  onChange={(e) => setGender(e.target.value)}
                  required
                >
                  <option value="">Select gender</option>
                  <option value="Male">Male</option>
                  <option value="Female">Female</option>
                  <option value="Other">Other</option>
                </select>
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label htmlFor="weight">Weight (kg) *</label>
                <input
                  type="number"
                  id="weight"
                  value={weight}
                  onChange={(e) => setWeight(e.target.value)}
                  placeholder="e.g., 70"
                  min="1"
                  max="500"
                  step="0.1"
                  required
                />
              </div>

              <div className="form-group">
                <label htmlFor="height">Height (cm) *</label>
                <input
                  type="number"
                  id="height"
                  value={height}
                  onChange={(e) => setHeight(e.target.value)}
                  placeholder="e.g., 170"
                  min="1"
                  max="300"
                  step="0.1"
                  required
                />
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="caloriesGoal">Calories Goal (kcal/day) *</label>
              <input
                type="number"
                id="caloriesGoal"
                value={caloriesGoal}
                onChange={(e) => setCaloriesGoal(e.target.value)}
                placeholder="e.g., 2000"
                min="500"
                max="10000"
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="healthNote">Health Note</label>
              <textarea
                id="healthNote"
                value={healthNote}
                onChange={(e) => setHealthNote(e.target.value)}
                placeholder="Any health conditions, dietary restrictions, or special notes..."
                rows={4}
              />
            </div>
          </div>

          <div className="form-section">
            <h2>Health Information</h2>
            
            <div className="form-group">
              <label>Allergies</label>
              <p className="field-description">Select any allergies you have</p>
              <div className="checkbox-grid">
                {allergies.map(allergy => (
                  <label key={allergy.id} className="checkbox-item">
                    <input
                      type="checkbox"
                      checked={selectedAllergies.includes(allergy.id)}
                      onChange={() => handleAllergyToggle(allergy.id)}
                    />
                    <span>{allergy.name}</span>
                  </label>
                ))}
              </div>
            </div>

            <div className="form-group">
              <label>Food Preferences</label>
              <p className="field-description">Search and select ingredients you like or dislike</p>
              
              <div className="ingredient-search-container">
                <input
                  type="text"
                  className="ingredient-search-input"
                  placeholder="Search for ingredients (e.g., chicken, tomato, rice)..."
                  value={ingredientSearch}
                  onChange={(e) => handleSearchChange(e.target.value)}
                />
                {ingredientSearch && (
                  <button
                    type="button"
                    className="search-clear-btn"
                    onClick={() => {
                      setIngredientSearch('');
                      setSearchResults([]);
                    }}
                    title="Clear search"
                  >
                    ✕
                  </button>
                )}
                
                {isSearching && (
                  <div className="search-loading">Searching...</div>
                )}
                
                {searchResults.length > 0 && ingredientSearch.trim() && (
                  <div className="search-results">
                    {searchResults.map(ingredient => {
                      const preference = getIngredientPreference(ingredient.id);
                      const cleanName = stripHtml(ingredient.name);
                      
                      return (
                        <div key={ingredient.id} className="search-result-item">
                          <span className="ingredient-name">{cleanName}</span>
                          <select
                            className="preference-select"
                            value={preference}
                            onChange={(e) => handleIngredientPreference(ingredient, e.target.value as 'like' | 'dislike' | 'allergy' | 'none')}
                          >
                            <option value="none">None</option>
                            <option value="like">Like</option>
                            <option value="dislike">Dislike</option>
                            <option value="allergy">Allergy</option>
                          </select>
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>

              {getSelectedIngredientsList().length > 0 && (
                <div className="selected-ingredients">
                  <h4>Selected Preferences:</h4>
                  <div className="preference-list">
                    {getSelectedIngredientsList().map(ingredient => {
                      const preference = getIngredientPreference(ingredient.id);
                      const cleanName = stripHtml(ingredient.name);
                      return (
                        <div key={ingredient.id} className="preference-item">
                          <div className="preference-info">
                            <span className="ingredient-name">{cleanName}</span>
                            <span className={`preference-badge ${preference}`}>
                              {preference.charAt(0).toUpperCase() + preference.slice(1)}
                            </span>
                          </div>
                          <button
                            type="button"
                            className="btn-remove"
                            onClick={() => removeIngredient(ingredient.id)}
                            title="Remove"
                          >
                            ✕
                          </button>
                        </div>
                      );
                    })}
                  </div>
                </div>
              )}
            </div>
          </div>

          <div className="form-actions">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => navigate('/meal-plans')}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn"
              disabled={loading}
            >
              {loading ? 'Creating...' : 'Create Meal Plan'}
            </button>
          </div>
        </form>

        {showConfirmModal && (
          <div className="confirm-modal-overlay" role="dialog" aria-modal="true" aria-labelledby="confirm-create-title">
            <div className="confirm-modal-card">
              <h3 id="confirm-create-title" style = {{color: '#000000'}}>Confirm Create Meal Plan</h3>
              <p style = {{color: '#000000'}}>Are you sure you want to create this meal plan?</p>
              <div className="confirm-modal-actions">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setShowConfirmModal(false)}
                  disabled={loading}
                >
                  Cancel
                </button>
                <button
                  type="button"
                  className="btn btn-primary"
                  onClick={handleConfirmCreate}
                  disabled={loading}
                >
                  Confirm
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </Container>
  );
};

export default CreateMealPlan;
