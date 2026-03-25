import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import { useAuth } from '../hooks/useAuth';
import './MealPlans.css';

interface MealPlan {
  id: string;
  name: string;
  durationDays: number;
  startDate: string;
  endDate: string;
  status: string;
  isAiGenerated: boolean;
  isActive: boolean;
  createdAt: string;
}

const formatDate = (date: string) => new Date(date).toLocaleDateString();

const getStatusMeta = (plan: MealPlan) => {
  if (plan.isActive) {
    return { label: 'Active', className: 'status-active' };
  }

  const rawStatus = plan.status?.toLowerCase() || '';
  if (rawStatus.includes('complete')) {
    return { label: 'Completed', className: 'status-completed' };
  }

  if (rawStatus.includes('pending')) {
    return { label: 'Pending', className: 'status-pending' };
  }

  return { label: 'Inactive', className: 'status-inactive' };
};

const MealPlans = () => {
  const { user } = useAuth();
  const [mealPlans, setMealPlans] = useState<MealPlan[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [deletingPlan, setDeletingPlan] = useState<MealPlan | null>(null);
  const navigate = useNavigate();

  const currentCredits = user?.currentCredits || 0;

  useEffect(() => {
    fetchMealPlans();
  }, []);

  const fetchMealPlans = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get('/mealplans');
      if (response.data.success) {
        setMealPlans(response.data.data);
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load meal plans');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!deletingPlan) {
      return;
    }

    try {
      await apiClient.delete(`/mealplans/${deletingPlan.id}`);
      setMealPlans(prev => prev.filter(plan => plan.id !== deletingPlan.id));
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to delete meal plan');
    } finally {
      setShowDeleteModal(false);
      setDeletingPlan(null);
    }
  };

  const openDeleteModal = (plan: MealPlan) => {
    setDeletingPlan(plan);
    setShowDeleteModal(true);
  };

  const handleSetActive = async (id: string) => {
    try {
      const response = await apiClient.post(`/mealplans/${id}/set-active`);
      if (response.data.success) {
        // Refresh the meal plans list to show updated active status
        await fetchMealPlans();
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to set meal plan as active');
    }
  };

  const canCreateMorePlans = () => {
    // Allow unlimited meal plans for all users
    return true;
  };

  if (loading) {
    return (
      <Container>
        <div className="loading-container">
          <div className="spinner"></div>
          <p>Loading meal plans...</p>
        </div>
      </Container>
    );
  }

  return (
    <Container>
      <div className="meal-plans-page">
        <div className="page-header">
          <div className="header-left">
            <h1>My Meal Plans</h1>
            <div className="ai-credits-display">
              <span className="credits-icon">⚡</span>
              <span className="credits-text">AI Credits: </span>
              <span className="credits-amount">{currentCredits}</span>
            </div>
          </div>
          <div className="header-actions">
            <button
              className="btn"
              onClick={() => navigate('/meal-plans/create')}
              disabled={!canCreateMorePlans()}
            >
              Create Meal Plan
            </button>
          </div>
        </div>

        {!canCreateMorePlans() && (
          <div className="limit-warning">
            You've reached the limit of 5 meal plans. Upgrade to premium for unlimited plans!
          </div>
        )}

        {error && <div className="error-message">{error}</div>}

        {mealPlans.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">📅</div>
            <h2>No Meal Plans Yet</h2>
            <p>Create your first meal plan to start organizing your meals</p>
            <div className="empty-actions">
              <button
                className="btn btn-primary"
                onClick={() => navigate('/meal-plans/create')}
              >
                Create Meal Plan
              </button>
            </div>
          </div>
        ) : (
          <div className="meal-plans-grid">
            {mealPlans.map((plan) => {
              const status = getStatusMeta(plan);

              return (
              <div key={plan.id} className="meal-plan-card">
                <div className="card-header">
                  <div className="card-title-wrap">
                    <h3 className="meal-plan-title">{plan.name || 'Untitled Meal Plan'}</h3>
                    <div className="badge-row">
                      <span className={`status-badge ${status.className}`}>
                        {status.label}
                      </span>
                      <span className={`meta-badge ${plan.isAiGenerated ? 'meta-badge-ai' : 'meta-badge-manual'}`}>
                        {plan.isAiGenerated ? 'AI Plan' : 'Manual Plan'}
                      </span>
                    </div>
                  </div>
                </div>
                <div className="card-body">
                  <div className="plan-info">
                    <div className="info-item">
                      <span className="label">Duration</span>
                      <span className="value">{plan.durationDays} days</span>
                    </div>
                    <div className="info-item">
                      <span className="label">Start Date</span>
                      <span className="value">{formatDate(plan.startDate)}</span>
                    </div>
                    <div className="info-item">
                      <span className="label">End Date</span>
                      <span className="value">{formatDate(plan.endDate)}</span>
                    </div>
                  </div>

                  <div className="plan-meta-row" aria-label="Plan metadata">
                    <span className="meta-badge">Created {formatDate(plan.createdAt)}</span>
                    <span className="meta-badge">{plan.durationDays >= 7 ? 'Long-term' : 'Short-term'}</span>
                    {plan.isAiGenerated && <span className="meta-badge meta-badge-ai">AI Suggested</span>}
                  </div>
                </div>
                <div className="card-actions">
                  <button
                    className={`btn ${plan.isActive ? 'btn-secondary' : 'btn-success'}`}
                    onClick={() => handleSetActive(plan.id)}
                    disabled={plan.isActive}
                  >
                    {plan.isActive ? 'Active Now' : 'Set Active'}
                  </button>
                  <button
                    className="btn btn-detail"
                    onClick={() => navigate(`/meal-plans/${plan.id}`)}
                  >
                    Details
                  </button>
                  <button
                    className="btn btn-danger"
                    onClick={() => openDeleteModal(plan)}
                  >
                    Delete
                  </button>
                </div>
              </div>
              );
            })}
          </div>
        )}

        {showDeleteModal && (
          <div className="delete-modal-overlay" role="dialog" aria-modal="true" aria-labelledby="delete-meal-plan-title">
            <div className="delete-modal-card">
              <div className="delete-modal-header">
                <h3 id="delete-meal-plan-title">Confirm Delete Meal Plan</h3>
              </div>
              <div className="delete-modal-body">
                <p>
                  Are you sure you want to delete
                  {' '}
                  <strong>{deletingPlan?.name}</strong>
                  ?
                </p>
                <p>This action cannot be undone.</p>
              </div>
              <div className="delete-modal-actions">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => {
                    setShowDeleteModal(false);
                    setDeletingPlan(null);
                  }}
                >
                  Cancel
                </button>
                <button
                  type="button"
                  className="btn btn-danger"
                  onClick={handleDelete}
                >
                  Delete
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </Container>
  );
};

export default MealPlans;
