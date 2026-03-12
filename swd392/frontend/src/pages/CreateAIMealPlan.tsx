import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import './CreateAIMealPlan.css';

const CreateAIMealPlan = () => {
  const [name, setName] = useState('');
  const [durationDays, setDurationDays] = useState(3);
  const [startDate, setStartDate] = useState(new Date().toISOString().split('T')[0]);
  const [healthInfo, setHealthInfo] = useState('');
  const [goals, setGoals] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!name.trim()) {
      setError('Please enter a meal plan name');
      return;
    }

    if (!healthInfo.trim() || !goals.trim()) {
      setError('Please provide health information and goals');
      return;
    }

    setLoading(true);
    setError('');

    try {
      const payload = {
        name: name.trim(),
        durationDays,
        startDate,
        healthInformation: healthInfo.trim(),
        goals: goals.trim()
      };

      const response = await apiClient.post('/mealplans/ai-generated', payload);
      
      if (response.data.success) {
        navigate('/meal-plans');
      } else {
        setError(response.data.message || 'Failed to generate meal plan');
      }
    } catch (err) {
      const error = err as { code?: string; message?: string; response?: { data?: { message?: string } } };
      if (error.code === 'ECONNABORTED' || error.message?.includes('timeout')) {
        setError('AI generation timed out. Please try again or create a custom meal plan.');
      } else {
        setError(error.response?.data?.message || 'Failed to generate meal plan');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container>
      <div className="create-ai-meal-plan-page">
        <div className="page-header">
          <h1>AI-Generated Meal Plan</h1>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => navigate('/meal-plans')}
          >
            Cancel
          </button>
        </div>

        <div className="ai-info-banner">
          <div className="ai-icon">🤖</div>
          <div className="ai-info-content">
            <h3>Let AI Create Your Perfect Meal Plan</h3>
            <p>
              Our AI will analyze your virtual fridge contents, health information, and goals 
              to generate a personalized meal plan that prioritizes ingredients with nearest expiry dates.
            </p>
          </div>
        </div>

        {error && <div className="error-message">{error}</div>}

        <form onSubmit={handleSubmit} className="ai-meal-plan-form">
          <div className="form-section">
            <h2>Basic Information</h2>
            
            <div className="form-group">
              <label htmlFor="name">Plan Name *</label>
              <input
                type="text"
                id="name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="e.g., AI Weekly Plan"
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
            <h2>Health & Goals</h2>
            <p className="section-description">
              Provide detailed information to help AI create the best meal plan for you.
            </p>

            <div className="form-group">
              <label htmlFor="healthInfo">Health Information *</label>
              <textarea
                id="healthInfo"
                value={healthInfo}
                onChange={(e) => setHealthInfo(e.target.value)}
                placeholder="Include dietary restrictions, allergies, medical conditions, etc.&#10;Example: Vegetarian, lactose intolerant, diabetic, no nuts"
                rows={4}
                required
              />
              <small className="form-hint">
                Be specific about dietary restrictions and allergies for better recommendations
              </small>
            </div>

            <div className="form-group">
              <label htmlFor="goals">Goals *</label>
              <textarea
                id="goals"
                value={goals}
                onChange={(e) => setGoals(e.target.value)}
                placeholder="What do you want to achieve with this meal plan?&#10;Example: Weight loss, muscle gain, balanced nutrition, use expiring ingredients"
                rows={4}
                required
              />
              <small className="form-hint">
                Clear goals help AI suggest more relevant recipes
              </small>
            </div>
          </div>

          <div className="ai-features">
            <h3>AI Will Consider:</h3>
            <ul>
              <li>✓ Your virtual fridge contents</li>
              <li>✓ Ingredients with nearest expiry dates</li>
              <li>✓ Your recorded allergies</li>
              <li>✓ Available recipes in the database</li>
              <li>✓ Your health information and goals</li>
            </ul>
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
              className="btn btn-primary"
              disabled={loading}
            >
              {loading ? (
                <>
                  <span className="spinner-small"></span>
                  Generating... (may take up to 30s)
                </>
              ) : (
                'Generate Meal Plan'
              )}
            </button>
          </div>
        </form>
      </div>
    </Container>
  );
};

export default CreateAIMealPlan;
