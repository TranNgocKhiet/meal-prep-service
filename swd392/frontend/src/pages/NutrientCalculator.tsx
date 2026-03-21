import Container from '../components/layout/Container';
import { useMemo, useState } from 'react';
import apiClient from '../config/api';
import './NutrientCalculator.css';

interface IngredientInput {
  ingredientName: string;
  quantity: string;
  unit: string;
}

interface CustomMealNutritionResponse {
  mealSummary: string;
  totalCalories: number;
  proteinG: number;
  carbsG: number;
  fatG: number;
  fiberG: number;
  sugarG: number;
  sodiumMg: number;
  ingredientConflicts: string[];
  bestConsumptionAdvice: string;
  overallNote: string;
}

const NutrientCalculator = () => {
  const [mealDescription, setMealDescription] = useState('');
  const [ingredients, setIngredients] = useState<IngredientInput[]>([
    { ingredientName: '', quantity: '', unit: '' }
  ]);
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<CustomMealNutritionResponse | null>(null);

  const canAnalyze = useMemo(() => {
    if (!mealDescription.trim()) {
      return false;
    }

    const validIngredients = ingredients.filter((item) =>
      item.ingredientName.trim() &&
      item.unit.trim() &&
      Number(item.quantity) > 0
    );

    return validIngredients.length > 0;
  }, [mealDescription, ingredients]);

  const addIngredientRow = () => {
    setIngredients((prev) => [...prev, { ingredientName: '', quantity: '', unit: '' }]);
  };

  const removeIngredientRow = (index: number) => {
    setIngredients((prev) => {
      if (prev.length === 1) {
        return prev;
      }
      return prev.filter((_, idx) => idx !== index);
    });
  };

  const updateIngredient = (index: number, field: keyof IngredientInput, value: string) => {
    setIngredients((prev) =>
      prev.map((item, idx) => (idx === index ? { ...item, [field]: value } : item))
    );
  };

  const analyzeNutrition = async () => {
    setError('');
    setResult(null);

    const cleanedIngredients = ingredients
      .map((item) => ({
        ingredientName: item.ingredientName.trim(),
        quantity: Number(item.quantity),
        unit: item.unit.trim()
      }))
      .filter((item) => item.ingredientName && item.unit && !Number.isNaN(item.quantity) && item.quantity > 0);

    if (!mealDescription.trim()) {
      setError('Please enter meal description.');
      return;
    }

    if (cleanedIngredients.length === 0) {
      setError('Please enter at least one valid ingredient.');
      return;
    }

    try {
      setIsAnalyzing(true);

      const response = await apiClient.post('/nutrients/analyze-custom', {
        mealDescription: mealDescription.trim(),
        ingredients: cleanedIngredients
      });

      if (!response.data?.success) {
        throw new Error(response.data?.message || 'Failed to analyze meal nutrition.');
      }

      setResult(response.data.data as CustomMealNutritionResponse);
    } catch (err: any) {
      setError(err?.response?.data?.message || err?.message || 'Unexpected error while analyzing nutrition.');
    } finally {
      setIsAnalyzing(false);
    }
  };

  return (
    <Container>
      <div className="nutrition-analyzer-page">
        <div className="nutrition-header">
          <h1>Custom Meal Nutrition Analyzer</h1>
          <p>Describe your meal and ingredients. AI will estimate nutrients and provide advice.</p>
        </div>

        <section className="nutrition-input-card">
          <h2>Meal Input</h2>

          <label htmlFor="mealDescription" className="input-label">
            Meal Description
          </label>
          <textarea
            id="mealDescription"
            className="meal-description"
            rows={3}
            value={mealDescription}
            onChange={(event) => setMealDescription(event.target.value)}
            placeholder="Example: Grilled salmon with spinach salad and yogurt dressing"
          />

          <div className="ingredients-header">
            <h3>Ingredients</h3>
            <button type="button" className="add-row-btn" onClick={addIngredientRow}>
              + Add Ingredient
            </button>
          </div>

          <div className="ingredients-table-wrapper">
            <table className="ingredients-table">
              <thead>
                <tr>
                  <th>Ingredient Name</th>
                  <th>Quantity</th>
                  <th>Unit</th>
                  <th>Action</th>
                </tr>
              </thead>
              <tbody>
                {ingredients.map((ingredient, index) => (
                  <tr key={`${index}-${ingredient.ingredientName}`}>
                    <td>
                      <input
                        type="text"
                        value={ingredient.ingredientName}
                        onChange={(event) => updateIngredient(index, 'ingredientName', event.target.value)}
                        placeholder="Example: Chicken breast"
                      />
                    </td>
                    <td>
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        value={ingredient.quantity}
                        onChange={(event) => updateIngredient(index, 'quantity', event.target.value)}
                        placeholder="200"
                      />
                    </td>
                    <td>
                      <input
                        type="text"
                        value={ingredient.unit}
                        onChange={(event) => updateIngredient(index, 'unit', event.target.value)}
                        placeholder="gram"
                      />
                    </td>
                    <td>
                      <button
                        type="button"
                        className="remove-row-btn"
                        onClick={() => removeIngredientRow(index)}
                        disabled={ingredients.length === 1}
                      >
                        Remove
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {error && <div className="analyzer-error">{error}</div>}

          <button
            type="button"
            className="analyze-btn"
            onClick={analyzeNutrition}
            disabled={!canAnalyze || isAnalyzing}
          >
            {isAnalyzing ? 'Analyzing...' : 'Analyze Nutrition'}
          </button>
        </section>

        {result && (
          <section className="nutrition-result-card">
            <h2>Meal Nutrition Result</h2>
            <p className="meal-summary">{result.mealSummary || 'Meal nutrition analysis'}</p>

            <div className="nutrition-grid">
              <div className="nutrition-item">
                <span>Calories</span>
                <strong>{Number(result.totalCalories || 0).toFixed(2)} kcal</strong>
              </div>
              <div className="nutrition-item">
                <span>Protein</span>
                <strong>{Number(result.proteinG || 0).toFixed(2)} g</strong>
              </div>
              <div className="nutrition-item">
                <span>Carbs</span>
                <strong>{Number(result.carbsG || 0).toFixed(2)} g</strong>
              </div>
              <div className="nutrition-item">
                <span>Fat</span>
                <strong>{Number(result.fatG || 0).toFixed(2)} g</strong>
              </div>
              <div className="nutrition-item">
                <span>Fiber</span>
                <strong>{Number(result.fiberG || 0).toFixed(2)} g</strong>
              </div>
              <div className="nutrition-item">
                <span>Sugar</span>
                <strong>{Number(result.sugarG || 0).toFixed(2)} g</strong>
              </div>
              <div className="nutrition-item">
                <span>Sodium</span>
                <strong>{Number(result.sodiumMg || 0).toFixed(2)} mg</strong>
              </div>
            </div>

            <div className="result-section">
              <h3>Ingredient Conflict Notes</h3>
              <ul>
                {(result.ingredientConflicts || []).map((conflict, index) => (
                  <li key={`${index}-${conflict}`}>{conflict}</li>
                ))}
              </ul>
            </div>

            <div className="result-section">
              <h3>Best Consumption Advice</h3>
              <p>{result.bestConsumptionAdvice || 'No advice provided.'}</p>
            </div>

            <div className="result-section">
              <h3>Overall Note</h3>
              <p>{result.overallNote || 'No additional note.'}</p>
            </div>
          </section>
        )}

        {isAnalyzing && (
          <div className="analyzer-loading-overlay">
            <div className="analyzer-loading-content">
              <div className="loading-spinner" />
              <h3>AI is analyzing your meal...</h3>
              <p>Estimating nutrients and checking ingredient compatibility.</p>
            </div>
          </div>
        )}
      </div>
    </Container>
  );
};

export default NutrientCalculator;
