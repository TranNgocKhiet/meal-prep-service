import Container from '../components/layout/Container';
import { useMemo, useState } from 'react';
import apiClient from '../config/api';
import { jsPDF } from 'jspdf';
import AILoadingOverlay from '../components/ai/AILoadingOverlay';
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

const parseStructuredAdvice = (rawText: string) => {
  const text = (rawText || '').trim();
  if (!text) {
    return '';
  }

  const withoutFence = text
    .replace(/^```(?:json)?/i, '')
    .replace(/```$/i, '')
    .trim();

  const first = withoutFence[0];
  const last = withoutFence[withoutFence.length - 1];
  if ((first === '{' && last === '}') || (first === '[' && last === ']')) {
    try {
      const parsed = JSON.parse(withoutFence);

      if (typeof parsed === 'string') {
        return parsed.trim();
      }

      if (parsed && typeof parsed === 'object') {
        const candidateKeys = ['health_advice', 'advice', 'bestConsumptionAdvice', 'message', 'note'];
        for (const key of candidateKeys) {
          const value = (parsed as Record<string, unknown>)[key];
          if (typeof value === 'string' && value.trim()) {
            return value.trim();
          }
        }

        const firstString = Object.values(parsed as Record<string, unknown>)
          .find((value) => typeof value === 'string' && value.trim()) as string | undefined;

        if (firstString) {
          return firstString.trim();
        }
      }
    } catch {
      return withoutFence;
    }
  }

  return withoutFence;
};

const createOverallInsight = (result: CustomMealNutritionResponse) => {
  const caloriesBand = result.totalCalories < 350
    ? 'light'
    : result.totalCalories <= 700
      ? 'moderate'
      : 'high-energy';

  const primaryMacro = [
    { label: 'protein', value: result.proteinG },
    { label: 'carbohydrate', value: result.carbsG },
    { label: 'fat', value: result.fatG }
  ].sort((a, b) => b.value - a.value)[0].label;

  const sodiumFlag = result.sodiumMg > 800 ? 'Sodium is on the high side, so pair with low-sodium meals later.' : '';
  const fiberFlag = result.fiberG < 5 ? 'Fiber appears low; add vegetables, legumes, or whole grains if possible.' : '';

  return `This is a ${caloriesBand} meal with ${primaryMacro} as the dominant macro. ${sodiumFlag} ${fiberFlag}`.trim();
};

const NutrientCalculator = () => {
  const [mealDescription, setMealDescription] = useState('');
  const [ingredients, setIngredients] = useState<IngredientInput[]>([
    { ingredientName: '', quantity: '', unit: '' }
  ]);
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<CustomMealNutritionResponse | null>(null);

  const cleanedAdvice = useMemo(
    () => parseStructuredAdvice(result?.bestConsumptionAdvice || ''),
    [result?.bestConsumptionAdvice]
  );

  const cleanedOverallNote = useMemo(
    () => parseStructuredAdvice(result?.overallNote || ''),
    [result?.overallNote]
  );

  const overallHealthBadge = useMemo(() => {
    if (!result) {
      return 'Health Insight';
    }

    if (result.sodiumMg > 1000 || result.sugarG > 35) {
      return 'Watch Intake';
    }

    if (result.proteinG >= 25 && result.fiberG >= 5) {
      return 'Balanced Choice';
    }

    if (result.totalCalories < 400) {
      return 'Light Meal';
    }

    return 'Health Insight';
  }, [result]);

  const nutritionStats = useMemo(() => {
    if (!result) {
      return [];
    }

    return [
      { label: 'Calories', value: `${Number(result.totalCalories || 0).toFixed(2)} kcal`, badge: 'Energy' },
      { label: 'Protein', value: `${Number(result.proteinG || 0).toFixed(2)} g`, badge: 'Muscle' },
      { label: 'Carbs', value: `${Number(result.carbsG || 0).toFixed(2)} g`, badge: 'Fuel' },
      { label: 'Fat', value: `${Number(result.fatG || 0).toFixed(2)} g`, badge: 'Balance' },
      { label: 'Fiber', value: `${Number(result.fiberG || 0).toFixed(2)} g`, badge: 'Gut Health' },
      { label: 'Sugar', value: `${Number(result.sugarG || 0).toFixed(2)} g`, badge: 'Sugar Control' },
      { label: 'Sodium', value: `${Number(result.sodiumMg || 0).toFixed(2)} mg`, badge: 'Hydration' }
    ];
  }, [result]);

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

  const exportPdf = () => {
    if (!result) {
      return;
    }

    const doc = new jsPDF();
    const pageWidth = doc.internal.pageSize.getWidth();
    const pageHeight = doc.internal.pageSize.getHeight();
    const margin = 14;
    const contentWidth = pageWidth - margin * 2;
    const nowText = new Date().toLocaleString();
    let y = 16;

    const ensureSpace = (neededHeight: number) => {
      if (y + neededHeight > pageHeight - margin) {
        doc.addPage();
        y = 16;
      }
    };

    const drawSectionTitle = (title: string) => {
      ensureSpace(14);
      doc.setFillColor(240, 249, 255);
      doc.roundedRect(margin, y, contentWidth, 9, 2, 2, 'F');
      doc.setTextColor(3, 105, 161);
      doc.setFont('helvetica', 'bold');
      doc.setFontSize(11);
      doc.text(title, margin + 3, y + 6.2);
      y += 12;
    };

    const drawParagraph = (text: string) => {
      const lines = doc.splitTextToSize(text || '-', contentWidth - 6);
      ensureSpace(lines.length * 5 + 8);
      doc.setTextColor(30, 41, 59);
      doc.setFont('helvetica', 'normal');
      doc.setFontSize(10.5);
      doc.text(lines, margin + 3, y + 4);
      y += lines.length * 5 + 5;
    };

    const drawBullets = (items: string[]) => {
      const normalized = items.length ? items : ['-'];
      normalized.forEach((item) => {
        const clean = item?.trim() || '-';
        const lines = doc.splitTextToSize(clean, contentWidth - 12);
        ensureSpace(lines.length * 5 + 4);
        doc.setTextColor(30, 41, 59);
        doc.setFont('helvetica', 'normal');
        doc.setFontSize(10.5);
        doc.text('•', margin + 3, y + 4);
        doc.text(lines, margin + 8, y + 4);
        y += lines.length * 5 + 2;
      });
      y += 2;
    };

    const nutritionStats = [
      { label: 'Calories', value: `${Number(result.totalCalories || 0).toFixed(2)} kcal` },
      { label: 'Protein', value: `${Number(result.proteinG || 0).toFixed(2)} g` },
      { label: 'Carbs', value: `${Number(result.carbsG || 0).toFixed(2)} g` },
      { label: 'Fat', value: `${Number(result.fatG || 0).toFixed(2)} g` },
      { label: 'Fiber', value: `${Number(result.fiberG || 0).toFixed(2)} g` },
      { label: 'Sugar', value: `${Number(result.sugarG || 0).toFixed(2)} g` },
      { label: 'Sodium', value: `${Number(result.sodiumMg || 0).toFixed(2)} mg` }
    ];

    const drawNutritionCards = () => {
      const cols = 2;
      const gap = 4;
      const cardWidth = (contentWidth - gap) / cols;
      const cardHeight = 12;

      nutritionStats.forEach((stat, index) => {
        if (index % cols === 0) {
          ensureSpace(cardHeight + 3);
        }

        const col = index % cols;
        const row = Math.floor(index / cols);
        const x = margin + col * (cardWidth + gap);
        const rowY = y + row * (cardHeight + 3);

        doc.setFillColor(248, 250, 252);
        doc.setDrawColor(226, 232, 240);
        doc.roundedRect(x, rowY, cardWidth, cardHeight, 2, 2, 'FD');

        doc.setFont('helvetica', 'bold');
        doc.setFontSize(9.5);
        doc.setTextColor(51, 65, 85);
        doc.text(stat.label, x + 3, rowY + 4.4);

        doc.setFont('helvetica', 'normal');
        doc.setFontSize(10);
        doc.setTextColor(15, 23, 42);
        doc.text(stat.value, x + 3, rowY + 9.2);
      });

      y += Math.ceil(nutritionStats.length / cols) * (cardHeight + 3) + 1;
    };

    doc.setFillColor(15, 23, 42);
    doc.roundedRect(margin, y, contentWidth, 18, 3, 3, 'F');
    doc.setTextColor(248, 250, 252);
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(17);
    doc.text('Nutrition Analyzer Report', margin + 4, y + 7.2);
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10.5);
    doc.setTextColor(186, 230, 253);
    doc.text(`Generated: ${nowText}`, margin + 4, y + 13.4);
    y += 22;

    drawSectionTitle('Meal Summary');
    drawParagraph(result.mealSummary || mealDescription || '-');

    drawSectionTitle('Key Nutrition');
    drawNutritionCards();

    drawSectionTitle('Ingredients');
    drawBullets(
      ingredients
        .filter((item) => item.ingredientName.trim())
        .map((item) => `${item.ingredientName} (${item.quantity || '?'} ${item.unit || ''})`)
    );

    drawSectionTitle('Ingredient Conflict Notes');
    drawBullets((result.ingredientConflicts || []).filter(Boolean));

    drawSectionTitle('Best Consumption Advice');
    drawParagraph(cleanedAdvice || 'No advice provided.');

    drawSectionTitle('Overall Note');
    drawParagraph(cleanedOverallNote || createOverallInsight(result));

    doc.save(`nutrition-report-${new Date().toISOString().slice(0, 10)}.pdf`);
  };

  return (
    <Container>
      <div className="nutrition-analyzer-page">
        <div className="nutrition-header">
          <span className="nutrition-kicker">AI Nutrition Lab</span>
          <h1>Custom Meal Nutrition Analyzer</h1>
          <p>Describe your meal and ingredients. AI will estimate nutrients and provide advice.</p>
          <div className="nutrition-header-meta">
            <span>Macro estimate</span>
            <span>Ingredient conflict checks</span>
            <span>Best consumption advice</span>
          </div>
        </div>

        <section className="nutrition-input-card">
          <div className="input-card-head">
            <div>
              <span className="result-kicker input-kicker">Input Form</span>
              <h2><span className="input-icon-badge" aria-hidden="true">🍽</span> Meal Input</h2>
            </div>
          </div>

          <div className="input-section-head">
            <label htmlFor="mealDescription" className="input-label">
              <span className="input-icon-badge" aria-hidden="true">🍴</span> Meal Description
            </label>
            <span className="result-badge result-badge--info">Describe Meal</span>
          </div>
          <textarea
            id="mealDescription"
            className="meal-description"
            rows={3}
            value={mealDescription}
            onChange={(event) => setMealDescription(event.target.value)}
            placeholder="Example: 🍽 Grilled salmon with spinach salad and yogurt dressing"
          />

          <div className="ingredients-header">
            <div className="input-section-title-group">
              <h3>Ingredients</h3>
              <span className="result-badge result-badge--success">Add Components</span>
            </div>
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
                        placeholder="Example: 🥩 Chicken breast"
                      />
                    </td>
                    <td>
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        value={ingredient.quantity}
                        onChange={(event) => updateIngredient(index, 'quantity', event.target.value)}
                        placeholder="Example: ⚖ 200"
                      />
                    </td>
                    <td>
                      <input
                        type="text"
                        value={ingredient.unit}
                        onChange={(event) => updateIngredient(index, 'unit', event.target.value)}
                        placeholder="Example: 🧪 gram"
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
            {isAnalyzing ? '⚙ Analyzing...' : '⚙ Analyze Nutrition'}
          </button>
        </section>

        {result && (
          <section className="nutrition-result-card">
            <div className="result-title-row">
              <div>
                <span className="result-kicker">AI Result</span>
                <h2>Meal Nutrition Result</h2>
              </div>
              <button type="button" className="export-pdf-btn" onClick={exportPdf}>
                Export PDF
              </button>
            </div>
            <p className="meal-summary">{result.mealSummary || 'Meal nutrition analysis'}</p>

            <div className="nutrition-grid">
              {nutritionStats.map((stat) => (
                <div key={stat.label} className="nutrition-item">
                  <div className="nutrition-item-head">
                    <span>{stat.label}</span>
                    <span className="stat-badge">{stat.badge}</span>
                  </div>
                  <strong>{stat.value}</strong>
                </div>
              ))}
            </div>

            <div className="result-section">
              <div className="result-section-head">
                <h3>Ingredient Conflict Notes</h3>
                <span className="result-badge result-badge--info">Safety Check</span>
              </div>
              <ul>
                {(result.ingredientConflicts || []).map((conflict, index) => (
                  <li key={`${index}-${conflict}`}>{conflict}</li>
                ))}
              </ul>
            </div>

            <div className="result-section">
              <div className="result-section-head">
                <h3>Best Consumption Advice</h3>
                <span className="result-badge result-badge--success">Actionable Advice</span>
              </div>
              <p>{cleanedAdvice || 'No advice provided.'}</p>
            </div>

            <div className="result-section">
              <div className="result-section-head">
                <h3>Overall Note</h3>
                <span className="result-badge result-badge--health">{overallHealthBadge}</span>
              </div>
              <p>{cleanedOverallNote || createOverallInsight(result)}</p>
            </div>
          </section>
        )}

        <AILoadingOverlay
          open={isAnalyzing}
          title="AI chef is preparing your nutrition scan..."
          description="Scanning ingredients, balancing macros, and validating compatibility."
          steps={['Vision Parsing', 'Macro Engine', 'Conflict Matrix']}
        />
      </div>
    </Container>
  );
};

export default NutrientCalculator;
