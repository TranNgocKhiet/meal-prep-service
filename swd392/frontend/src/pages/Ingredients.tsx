import { useState, useEffect } from 'react';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import './Ingredients.css';

interface Allergy {
  id: string;
  name: string;
  description: string;
}

interface Ingredient {
  id: string;
  name: string;
  category: string;
  unit: string;
  pricePerUnit: number;
  imageUrl: string;
  isAvailableForPurchase: boolean;
  allergies?: Allergy[];
}

const Ingredients = () => {
  const [ingredients, setIngredients] = useState<Ingredient[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [selectedIngredient, setSelectedIngredient] = useState<Ingredient | null>(null);
  const [showDetailModal, setShowDetailModal] = useState(false);

  // Search and filter state
  const [searchTerm, setSearchTerm] = useState('');
  const [category, setCategory] = useState('');

  const categories = [
    'Vegetables',
    'Fruits',
    'Meat',
    'Seafood',
    'Dairy',
    'Grains',
    'Spices',
    'Condiments',
    'Beverages',
    'Other'
  ];

  useEffect(() => {
    const loadIngredients = async () => {
      try {
        setLoading(true);
        setError('');

        const searchDto = {
          searchTerm: undefined,
          category: undefined,
        };

        const response = await apiClient.post('/ingredients/search', searchDto);
        if (response.data.success) {
          setIngredients(response.data.data);
        }
      } catch (err) {
        const error = err as { response?: { data?: { message?: string } } };
        setError(error.response?.data?.message || 'Failed to search ingredients');
      } finally {
        setLoading(false);
      }
    };

    loadIngredients();
  }, []);

  const searchIngredients = async () => {
    try {
      setLoading(true);
      setError('');

      const searchDto = {
        searchTerm: searchTerm || undefined,
        category: category || undefined,
      };

      const response = await apiClient.post('/ingredients/search', searchDto);
      if (response.data.success) {
        setIngredients(response.data.data);
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to search ingredients');
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    searchIngredients();
  };

  const handleReset = () => {
    setSearchTerm('');
    setCategory('');
  };

  const viewIngredientDetail = async (ingredientId: string) => {
    try {
      const response = await apiClient.get(`/ingredients/${ingredientId}`);
      if (response.data.success) {
        setSelectedIngredient(response.data.data);
        setShowDetailModal(true);
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to load ingredient details');
    }
  };

  const highlightSearchTerm = (text: string) => {
    if (!searchTerm) return text;
    
    const regex = new RegExp(`(${searchTerm})`, 'gi');
    const parts = text.split(regex);
    
    return parts.map((part, index) => 
      regex.test(part) ? <mark key={index}>{part}</mark> : part
    );
  };

  return (
    <Container>
      <div className="ingredients-page">
        <div className="page-header">
          <h1>Ingredient Search</h1>
        </div>

        <div className="search-section">
          <form onSubmit={handleSearch} className="search-form">
            <div className="search-row">
              <div className="form-group flex-grow">
                <input
                  type="text"
                  placeholder="Search by ingredient name..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="search-input"
                />
              </div>
              <button type="submit" className="btn btn-primary" disabled={loading}>
                {loading ? 'Searching...' : 'Search'}
              </button>
            </div>

            <div className="filters-row">
              <div className="form-group">
                <label htmlFor="category">Category</label>
                <select
                  id="category"
                  value={category}
                  onChange={(e) => setCategory(e.target.value)}
                  className="filter-select"
                >
                  <option value="">All Categories</option>
                  {categories.map(cat => (
                    <option key={cat} value={cat}>{cat}</option>
                  ))}
                </select>
              </div>

              <button type="button" onClick={handleReset} className="btn btn-secondary">
                Reset
              </button>
            </div>
          </form>
        </div>

        {error && <div className="error-message">{error}</div>}

        {loading ? (
          <div className="loading-container">
            <div className="spinner"></div>
            <p>Searching ingredients...</p>
          </div>
        ) : ingredients.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">🥕</div>
            <h2>No Ingredients Found</h2>
            <p>Try adjusting your search filters</p>
          </div>
        ) : (
          <>
            <div className="results-summary">
              Found {ingredients.length} ingredient{ingredients.length !== 1 ? 's' : ''}
            </div>

            <div className="ingredients-grid">
              {ingredients.map((ingredient) => (
                <div key={ingredient.id} className="ingredient-card">
                  {ingredient.imageUrl && (
                    <div className="ingredient-image">
                      <img src={ingredient.imageUrl} alt={ingredient.name} />
                    </div>
                  )}
                  
                  <div className="ingredient-content">
                    <h3>{highlightSearchTerm(ingredient.name)}</h3>
                    
                    <div className="ingredient-meta">
                      <span className="meta-item">
                        <span className="meta-icon">📁</span>
                        {ingredient.category}
                      </span>
                      <span className="meta-item">
                        <span className="meta-icon">📏</span>
                        {ingredient.unit}
                      </span>
                    </div>

                    <div className="ingredient-price">
                      {ingredient.pricePerUnit.toLocaleString()} VND per {ingredient.unit}
                    </div>

                    {ingredient.allergies && ingredient.allergies.length > 0 && (
                      <div className="allergy-info">
                        ⚠️ Contains allergens
                      </div>
                    )}

                    {!ingredient.isAvailableForPurchase && (
                      <div className="unavailable-badge">
                        Currently Unavailable
                      </div>
                    )}

                    <button
                      className="btn btn-sm btn-primary"
                      onClick={() => viewIngredientDetail(ingredient.id)}
                    >
                      View Details
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </>
        )}

        {showDetailModal && selectedIngredient && (
          <div className="modal-overlay" onClick={() => setShowDetailModal(false)}>
            <div className="modal-content ingredient-detail-modal" onClick={(e) => e.stopPropagation()}>
              <div className="modal-header">
                <h2>{selectedIngredient.name}</h2>
                <button className="btn-close" onClick={() => setShowDetailModal(false)}>
                  ×
                </button>
              </div>

              <div className="modal-body">
                {selectedIngredient.imageUrl && (
                  <img 
                    src={selectedIngredient.imageUrl} 
                    alt={selectedIngredient.name} 
                    className="detail-image" 
                  />
                )}

                <div className="detail-info-grid">
                  <div className="info-item">
                    <strong>Category:</strong>
                    <span>{selectedIngredient.category}</span>
                  </div>
                  <div className="info-item">
                    <strong>Unit:</strong>
                    <span>{selectedIngredient.unit}</span>
                  </div>
                  <div className="info-item">
                    <strong>Price:</strong>
                    <span className="price-highlight">
                      {selectedIngredient.pricePerUnit.toLocaleString()} VND per {selectedIngredient.unit}
                    </span>
                  </div>
                  <div className="info-item">
                    <strong>Availability:</strong>
                    <span className={selectedIngredient.isAvailableForPurchase ? 'available' : 'unavailable'}>
                      {selectedIngredient.isAvailableForPurchase ? 'Available' : 'Unavailable'}
                    </span>
                  </div>
                </div>

                {selectedIngredient.allergies && selectedIngredient.allergies.length > 0 && (
                  <div className="allergy-section">
                    <h3>Allergy Information</h3>
                    <div className="allergy-warning-prominent">
                      <span className="warning-icon">⚠️</span>
                      <div>
                        <strong>This ingredient contains allergens</strong>
                        <ul className="allergens-list">
                          {selectedIngredient.allergies.map((allergy) => (
                            <li key={allergy.id}>
                              <strong>{allergy.name}</strong>
                              {allergy.description && <p>{allergy.description}</p>}
                            </li>
                          ))}
                        </ul>
                      </div>
                    </div>
                  </div>
                )}

                {(!selectedIngredient.allergies || selectedIngredient.allergies.length === 0) && (
                  <div className="no-allergens">
                    <span className="check-icon">✓</span>
                    <span>No known allergens</span>
                  </div>
                )}
              </div>

              <div className="modal-actions">
                <button className="btn btn-secondary" onClick={() => setShowDetailModal(false)}>
                  Close
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </Container>
  );
};

export default Ingredients;
