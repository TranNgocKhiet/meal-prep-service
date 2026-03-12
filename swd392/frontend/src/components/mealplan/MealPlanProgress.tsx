import { useEffect, useState } from 'react';
import apiClient from '../../config/api';
import './MealPlanProgress.css';

interface MealPlanProgressData {
  mealPlanId: string;
  mealPlanName: string;
  totalMeals: number;
  finishedMeals: number;
  expiredMeals: number;
  pendingMeals: number;
  isCompleted: boolean;
  completionPercentage: number;
}

interface MealPlanProgressProps {
  mealPlanId: string;
}

const MealPlanProgress = ({ mealPlanId }: MealPlanProgressProps) => {
  const [progress, setProgress] = useState<MealPlanProgressData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    fetchProgress();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [mealPlanId]);

  const fetchProgress = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get(`/mealtracking/mealplans/${mealPlanId}/progress`);
      if (response.data.success) {
        setProgress(response.data.data);
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load progress');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="progress-widget loading">
        <div className="spinner-small"></div>
      </div>
    );
  }

  if (error || !progress) {
    return null;
  }

  return (
    <div className="meal-plan-progress-widget">
      <div className="progress-header">
        <h3>Meal Plan Progress</h3>
        {progress.isCompleted && (
          <span className="completed-badge">✓ Completed</span>
        )}
      </div>

      <div className="progress-bar-container">
        <div className="progress-bar">
          <div
            className="progress-fill"
            style={{ width: `${progress.completionPercentage}%` }}
          >
            <span className="progress-text">
              {Math.round(progress.completionPercentage)}%
            </span>
          </div>
        </div>
      </div>

      <div className="progress-stats">
        <div className="stat-item">
          <span className="stat-value">{progress.totalMeals}</span>
          <span className="stat-label">Total Meals</span>
        </div>
        <div className="stat-item stat-finished">
          <span className="stat-value">{progress.finishedMeals}</span>
          <span className="stat-label">Finished</span>
        </div>
        <div className="stat-item stat-pending">
          <span className="stat-value">{progress.pendingMeals}</span>
          <span className="stat-label">Pending</span>
        </div>
        <div className="stat-item stat-expired">
          <span className="stat-value">{progress.expiredMeals}</span>
          <span className="stat-label">Expired</span>
        </div>
      </div>
    </div>
  );
};

export default MealPlanProgress;
