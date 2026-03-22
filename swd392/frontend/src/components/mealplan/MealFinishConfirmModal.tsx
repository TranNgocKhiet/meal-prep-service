import './MealFinishConfirmModal.css';

interface IngredientCheck {
  ingredientId: string;
  ingredientName: string;
  unit: string;
  requiredAmount: number;
  availableAmount: number;
  missingAmount: number;
  isAvailable: boolean;
}

interface MealFinishCheck {
  mealId: string;
  ingredients: IngredientCheck[];
  canFinish: boolean;
  totalIngredients: number;
  availableIngredients: number;
  missingIngredients: number;
}

interface Props {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  checkData: MealFinishCheck | null;
  loading: boolean;
}

const MealFinishConfirmModal = ({ isOpen, onClose, onConfirm, checkData, loading }: Props) => {
  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Finish Meal Confirmation</h2>
          <button className="btn-close" onClick={onClose}>×</button>
        </div>

        <div className="modal-body">
          {loading ? (
            <div className="loading-state">
              <div className="spinner"></div>
              <p>Checking ingredients...</p>
            </div>
          ) : checkData ? (
            <>
              <div className="summary-section">
                <div className="summary-stats">
                  <div className="stat-item">
                    <span className="stat-label">Total Ingredients:</span>
                    <span className="stat-value">{checkData.totalIngredients}</span>
                  </div>
                  <div className="stat-item available">
                    <span className="stat-label">Available:</span>
                    <span className="stat-value">{checkData.availableIngredients}</span>
                  </div>
                  <div className="stat-item missing">
                    <span className="stat-label">Missing:</span>
                    <span className="stat-value">{checkData.missingIngredients}</span>
                  </div>
                </div>

                {checkData.missingIngredients > 0 && (
                  <div className="warning-message">
                    <span className="warning-icon">⚠️</span>
                    <span>Some ingredients are missing from your fridge. They will not be deducted.</span>
                  </div>
                )}
              </div>

              <div className="ingredients-list">
                <h3>Ingredient Details</h3>
                {checkData.ingredients.map((ingredient) => (
                  <div 
                    key={ingredient.ingredientId} 
                    className={`ingredient-item ${ingredient.isAvailable ? 'available' : 'missing'}`}
                  >
                    <div className="ingredient-header">
                      <span className="ingredient-name">{ingredient.ingredientName}</span>
                      <span className={`status-badge ${ingredient.isAvailable ? 'available' : 'missing'}`}>
                        {ingredient.isAvailable ? '✓ Available' : '✗ Missing'}
                      </span>
                    </div>
                    <div className="ingredient-details">
                      <div className="detail-row">
                        <span className="detail-label">Required:</span>
                        <span className="detail-value">{ingredient.requiredAmount} {ingredient.unit}</span>
                      </div>
                      <div className="detail-row">
                        <span className="detail-label">Available:</span>
                        <span className="detail-value">{ingredient.availableAmount} {ingredient.unit}</span>
                      </div>
                      {!ingredient.isAvailable && (
                        <div className="detail-row missing-row">
                          <span className="detail-label">Missing:</span>
                          <span className="detail-value">{ingredient.missingAmount} {ingredient.unit}</span>
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </>
          ) : (
            <div className="error-state">
              <p>Failed to load ingredient information</p>
            </div>
          )}
        </div>

        <div className="modal-footer">
          <button className="btn btn-secondary" onClick={onClose}>
            Cancel
          </button>
          <button 
            className="btn" 
            onClick={onConfirm}
            disabled={loading || !checkData}
          >
            Confirm & Finish Meal
          </button>
        </div>
      </div>
    </div>
  );
};

export default MealFinishConfirmModal;
