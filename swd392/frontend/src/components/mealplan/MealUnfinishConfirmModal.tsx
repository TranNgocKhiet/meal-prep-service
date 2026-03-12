import { useState, useEffect } from 'react';
import './MealUnfinishConfirmModal.css';

interface IngredientReturn {
  ingredientId: string;
  ingredientName: string;
  unit: string;
  amount: number;
  expiryDate: string;
}

interface Props {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (ingredients: IngredientReturn[]) => void;
  ingredients: IngredientReturn[];
  loading: boolean;
}

const MealUnfinishConfirmModal = ({ isOpen, onClose, onConfirm, ingredients, loading }: Props) => {
  const [editableIngredients, setEditableIngredients] = useState<IngredientReturn[]>([]);

  useEffect(() => {
    if (ingredients.length > 0) {
      setEditableIngredients(ingredients);
    }
  }, [ingredients]);

  const getDefaultExpiryDate = () => {
    const date = new Date();
    date.setDate(date.getDate() + 7); // Default 7 days from now
    return date.toISOString().split('T')[0];
  };

  const updateAmount = (ingredientId: string, amount: number) => {
    setEditableIngredients(prev =>
      prev.map(ing =>
        ing.ingredientId === ingredientId
          ? { ...ing, amount }
          : ing
      )
    );
  };

  const updateExpiryDate = (ingredientId: string, expiryDate: string) => {
    setEditableIngredients(prev =>
      prev.map(ing =>
        ing.ingredientId === ingredientId
          ? { ...ing, expiryDate }
          : ing
      )
    );
  };

  const handleConfirm = () => {
    // Validate all ingredients have valid data
    const invalidItems = editableIngredients.filter(
      ing => !ing.amount || ing.amount <= 0 || !ing.expiryDate
    );

    if (invalidItems.length > 0) {
      alert('Please ensure all ingredients have valid amount and expiry date');
      return;
    }

    onConfirm(editableIngredients);
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content unfinish-modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Unfinish Meal Confirmation</h2>
          <button className="btn-close" onClick={onClose}>×</button>
        </div>

        <div className="modal-body">
          {loading ? (
            <div className="loading-state">
              <div className="spinner"></div>
              <p>Loading ingredients...</p>
            </div>
          ) : editableIngredients.length > 0 ? (
            <>
              <div className="info-message">
                <span className="info-icon">ℹ️</span>
                <span>These ingredients will be returned to your virtual fridge. You can adjust the amount and expiry date for each item.</span>
              </div>

              <div className="ingredients-list">
                <h3>Ingredients to Return ({editableIngredients.length} items)</h3>
                {editableIngredients.map((ingredient) => (
                  <div key={ingredient.ingredientId} className="ingredient-item return-item">
                    <div className="ingredient-header">
                      <span className="ingredient-name">{ingredient.ingredientName}</span>
                    </div>
                    <div className="ingredient-inputs">
                      <div className="input-group">
                        <label htmlFor={`amount-${ingredient.ingredientId}`}>
                          Amount ({ingredient.unit})
                        </label>
                        <input
                          type="number"
                          id={`amount-${ingredient.ingredientId}`}
                          value={ingredient.amount}
                          onChange={(e) => updateAmount(ingredient.ingredientId, parseFloat(e.target.value))}
                          min="0.01"
                          step="0.01"
                          required
                        />
                      </div>
                      <div className="input-group">
                        <label htmlFor={`expiry-${ingredient.ingredientId}`}>
                          Expiry Date
                        </label>
                        <input
                          type="date"
                          id={`expiry-${ingredient.ingredientId}`}
                          value={ingredient.expiryDate}
                          onChange={(e) => updateExpiryDate(ingredient.ingredientId, e.target.value)}
                          min={new Date().toISOString().split('T')[0]}
                          required
                        />
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </>
          ) : (
            <div className="error-state">
              <p>No ingredients to return</p>
            </div>
          )}
        </div>

        <div className="modal-footer">
          <button className="btn btn-secondary" onClick={onClose}>
            Cancel
          </button>
          <button 
            className="btn btn-warning" 
            onClick={handleConfirm}
            disabled={loading || editableIngredients.length === 0}
            style={{
              backgroundColor: '#ffc107',
              color: '#000',
              border: 'none'
            }}
          >
            Confirm & Unfinish Meal
          </button>
        </div>
      </div>
    </div>
  );
};

export default MealUnfinishConfirmModal;
