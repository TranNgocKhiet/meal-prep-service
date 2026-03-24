import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import './VirtualFridge.css';

interface Ingredient {
  id: string;
  name: string;
  category: string;
  unit: string;
  imageUrl?: string;
}

interface FridgeItem {
  id: string;
  ingredient: Ingredient;
  quantity: number;
  unit: string;
  expiryDate: string;
  isExpired: boolean;
  daysUntilExpiry: number;
  addedAt: string;
}

type ItemStatusFilter = 'all' | 'expired' | 'today' | 'tomorrow' | 'soon' | 'fresh';

const VirtualFridge = () => {
  const navigate = useNavigate();
  const [fridgeItems, setFridgeItems] = useState<FridgeItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showAddForm, setShowAddForm] = useState(false);
  const [editingItem, setEditingItem] = useState<FridgeItem | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [ingredientSearch, setIngredientSearch] = useState('');
  const [searchResults, setSearchResults] = useState<Ingredient[]>([]);
  const [searchingIngredients, setSearchingIngredients] = useState(false);
  const [activeStatusFilter, setActiveStatusFilter] = useState<ItemStatusFilter>('all');
  const [showDeleteConfirmModal, setShowDeleteConfirmModal] = useState(false);
  const [pendingDeleteItemId, setPendingDeleteItemId] = useState<string | null>(null);
  const [deletingItem, setDeletingItem] = useState(false);
  const [showUpdateConfirmModal, setShowUpdateConfirmModal] = useState(false);
  const [updatingItem, setUpdatingItem] = useState(false);

  // Form state
  const [formData, setFormData] = useState({
    ingredientId: '',
    ingredientName: '',
    unit: '',
    quantity: '',
    expiryDate: '',
  });

  const getTodayDateString = () => new Date().toISOString().split('T')[0];

  useEffect(() => {
    fetchFridgeItems();
  }, []);

  const fetchFridgeItems = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get('/fridge');
      if (response.data.success) {
        setFridgeItems(response.data.data);
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load fridge items');
    } finally {
      setLoading(false);
    }
  };

  const searchIngredients = async (term: string) => {
    if (!term || term.length < 2) {
      setSearchResults([]);
      return;
    }

    try {
      setSearchingIngredients(true);
      const response = await apiClient.get(`/ingredients/search?term=${encodeURIComponent(term)}`);
      if (response.data.success) {
        setSearchResults(response.data.data);
      }
    } catch (err) {
      console.error('Failed to search ingredients:', err);
    } finally {
      setSearchingIngredients(false);
    }
  };

  useEffect(() => {
    const timer = setTimeout(() => {
      searchIngredients(ingredientSearch);
    }, 300);

    return () => clearTimeout(timer);
  }, [ingredientSearch]);

  const handleAddItem = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.ingredientId || !formData.quantity || !formData.expiryDate) {
      alert('Please fill in all required fields');
      return;
    }

    if (formData.expiryDate < getTodayDateString()) {
      alert('Expiry date cannot be before today');
      return;
    }

    try {
      const response = await apiClient.post('/fridge', {
        ingredientId: formData.ingredientId,
        quantity: parseFloat(formData.quantity),
        expiryDate: formData.expiryDate,
      });

      if (response.data.success) {
        await fetchFridgeItems();
        resetForm();
        setShowAddForm(false);
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to add item');
    }
  };

  const handleUpdateItem = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!editingItem || !formData.quantity || !formData.expiryDate) {
      alert('Please fill in all required fields');
      return;
    }

    if (formData.expiryDate < getTodayDateString()) {
      alert('Expiry date cannot be before today');
      return;
    }

    setShowUpdateConfirmModal(true);
  };

  const confirmUpdateItem = async () => {
    if (!editingItem || updatingItem) {
      return;
    }

    setUpdatingItem(true);
    try {
      const response = await apiClient.put(`/fridge/${editingItem.id}`, {
        quantity: parseFloat(formData.quantity),
        expiryDate: formData.expiryDate,
      });

      if (response.data.success) {
        await fetchFridgeItems();
        resetForm();
        setEditingItem(null);
        setShowUpdateConfirmModal(false);
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to update item');
    } finally {
      setUpdatingItem(false);
    }
  };

  const requestDeleteItem = (id: string) => {
    setPendingDeleteItemId(id);
    setShowDeleteConfirmModal(true);
  };

  const handleDeleteItem = async () => {
    if (!pendingDeleteItemId || deletingItem) {
      return;
    }

    setDeletingItem(true);
    try {
      await apiClient.delete(`/fridge/${pendingDeleteItemId}`);
      setFridgeItems(fridgeItems.filter(item => item.id !== pendingDeleteItemId));
      setShowDeleteConfirmModal(false);
      setPendingDeleteItemId(null);
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to delete item');
    } finally {
      setDeletingItem(false);
    }
  };

  const selectIngredient = (ingredient: Ingredient) => {
    setFormData({
      ...formData,
      ingredientId: ingredient.id,
      ingredientName: ingredient.name,
      unit: ingredient.unit,
    });
    // Keep selected ingredient only in "Selected" area and stop search dropdown from reopening.
    setIngredientSearch('');
    setSearchResults([]);
  };

  const startEdit = (item: FridgeItem) => {
    setEditingItem(item);
    setFormData({
      ingredientId: item.ingredient.id,
      ingredientName: item.ingredient.name,
      unit: item.ingredient.unit,
      quantity: item.quantity.toString(),
      expiryDate: item.expiryDate.split('T')[0],
    });
    setShowAddForm(false);
  };

  const resetForm = () => {
    setFormData({
      ingredientId: '',
      ingredientName: '',
      unit: '',
      quantity: '',
      expiryDate: '',
    });
    setIngredientSearch('');
    setSearchResults([]);
  };

  const cancelEdit = () => {
    setEditingItem(null);
    setShowUpdateConfirmModal(false);
    resetForm();
  };

  const canAddMoreItems = () => {
    // Allow unlimited fridge items for all users
    return true;
  };

  const getExpiryClass = (item: FridgeItem) => {
    if (item.isExpired) return 'expired';
    if (item.daysUntilExpiry <= 3) return 'expiring-soon';
    if (item.daysUntilExpiry <= 7) return 'expiring-warning';
    return 'fresh';
  };

  const getExpiryLabel = (item: FridgeItem) => {
    if (item.isExpired) return 'Expired';
    if (item.daysUntilExpiry === 0) return 'Expires today';
    if (item.daysUntilExpiry === 1) return 'Expires tomorrow';
    return `${item.daysUntilExpiry} days left`;
  };

  const getStatusFilterKey = (item: FridgeItem): Exclude<ItemStatusFilter, 'all'> => {
    if (item.isExpired) return 'expired';
    if (item.daysUntilExpiry === 0) return 'today';
    if (item.daysUntilExpiry === 1) return 'tomorrow';
    if (item.daysUntilExpiry <= 3) return 'soon';
    return 'fresh';
  };

  const filteredItems = fridgeItems.filter(item =>
    item.ingredient.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    item.ingredient.category.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const statusCounts = filteredItems.reduce(
    (acc, item) => {
      const status = getStatusFilterKey(item);
      acc[status] += 1;
      return acc;
    },
    {
      expired: 0,
      today: 0,
      tomorrow: 0,
      soon: 0,
      fresh: 0,
    }
  );

  const statusFilteredItems =
    activeStatusFilter === 'all'
      ? filteredItems
      : filteredItems.filter((item) => getStatusFilterKey(item) === activeStatusFilter);

  const sortedItems = [...statusFilteredItems].sort((a, b) => {
    const dateA = new Date(a.expiryDate).getTime();
    const dateB = new Date(b.expiryDate).getTime();
    return dateA - dateB;
  });

  const expiringItems = fridgeItems.filter(item => !item.isExpired && item.daysUntilExpiry <= 3);

  if (loading) {
    return (
      <Container>
        <div className="loading-container">
          <div className="spinner"></div>
          <p>Loading fridge items...</p>
        </div>
      </Container>
    );
  }

  return (
    <Container>
      <div className="virtual-fridge-page">
        <div className="page-header">
          <h1>Virtual Fridge</h1>
          <div className="header-actions">
            <button
              className="btn btn-warning"
              onClick={() => navigate('/grocery-list', { state: { refreshAt: Date.now() } })}
            >
              Create Grocery List
            </button>
            <button
              className="btn"
              onClick={() => {
                setShowAddForm(true);
                setEditingItem(null);
                resetForm();
              }}
              disabled={!canAddMoreItems()}
            >
              Add Item
            </button>
          </div>
        </div>

        {!canAddMoreItems() && (
          <div className="limit-warning">
            You've reached the limit of 100 items. Upgrade to premium for unlimited storage!
          </div>
        )}

        {expiringItems.length > 0 && (
          <div className="expiring-notification">
            <span className="notification-icon">⚠️</span>
            <span>
              {expiringItems.length} item{expiringItems.length > 1 ? 's' : ''} expiring soon!
            </span>
          </div>
        )}

        {error && <div className="error-message">{error}</div>}

        {(showAddForm || editingItem) && (
          <div className="form-card">
            <div className="form-header">
              <h2>{editingItem ? 'Edit Item' : 'Add New Item'}</h2>
              <button
                className="btn-close"
                onClick={() => {
                  setShowAddForm(false);
                  cancelEdit();
                }}
              >
                ×
              </button>
            </div>
            <form onSubmit={editingItem ? handleUpdateItem : handleAddItem}>
              {!editingItem && (
                <div className="form-group">
                  <label htmlFor="ingredient">Ingredient *</label>
                  <div className="ingredient-search">
                    <input
                      type="text"
                      id="ingredient"
                      value={ingredientSearch}
                      onChange={(e) => setIngredientSearch(e.target.value)}
                      placeholder="Search for an ingredient..."
                      required={!formData.ingredientId}
                    />
                    {ingredientSearch && (
                      <button
                        type="button"
                        className="btn-clear-search"
                        onClick={() => {
                          setIngredientSearch('');
                          setSearchResults([]);
                        }}
                      >
                        Clear
                      </button>
                    )}
                    {searchingIngredients && <div className="search-spinner"></div>}
                    {searchResults.length > 0 && (
                      <div className="search-results">
                        {searchResults.map((ingredient) => (
                          <div
                            key={ingredient.id}
                            className="search-result-item"
                            onClick={() => selectIngredient(ingredient)}
                          >
                            <span className="ingredient-name">{ingredient.name}</span>
                            <span className="ingredient-category">{ingredient.category}</span>
                          </div>
                        ))}
                      </div>
                    )}
                    {ingredientSearch.length >= 2 && !searchingIngredients && searchResults.length === 0 && !formData.ingredientId && (
                      <div className="search-results">
                        <div className="search-result-item no-results">
                          No ingredients found
                        </div>
                      </div>
                    )}
                  </div>
                  {formData.ingredientName && (
                    <div className="selected-ingredient">
                      Selected: <strong>{formData.ingredientName}</strong>
                    </div>
                  )}
                </div>
              )}

              {editingItem && (
                <div className="form-group">
                  <label>Ingredient</label>
                  <input
                    type="text"
                    value={formData.ingredientName}
                    disabled
                    className="input-disabled"
                  />
                </div>
              )}

              <div className="form-group">
                <label htmlFor="quantity">Quantity * {formData.unit && `(${formData.unit})`}</label>
                <input
                  type="number"
                  id="quantity"
                  value={formData.quantity}
                  onChange={(e) => setFormData({ ...formData, quantity: e.target.value })}
                  min="0.01"
                  step="0.01"
                  required
                />
              </div>

              <div className="form-group">
                <label htmlFor="expiryDate">Expiry Date *</label>
                <input
                  type="date"
                  id="expiryDate"
                  value={formData.expiryDate}
                  onChange={(e) => setFormData({ ...formData, expiryDate: e.target.value })}
                  min={getTodayDateString()}
                  required
                />
              </div>

              <div className="form-actions">
                <button type="submit" className="btn">
                  {editingItem ? 'Update Item' : 'Add Item'}
                </button>
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => {
                    setShowAddForm(false);
                    cancelEdit();
                  }}
                >
                  Cancel
                </button>
              </div>
            </form>
          </div>
        )}

        {showUpdateConfirmModal && (
          <div className="delete-modal-overlay" role="dialog" aria-modal="true" aria-labelledby="update-fridge-item-title">
            <div className="delete-modal-card">
              <div className="delete-modal-header update-modal-header">
                <h3 id="update-fridge-item-title">Confirm Update Item</h3>
              </div>
              <div className="delete-modal-body">
                <p>Are you sure you want to save changes to this fridge item?</p>
              </div>
              <div className="delete-modal-actions">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => {
                    if (updatingItem) return;
                    setShowUpdateConfirmModal(false);
                  }}
                  disabled={updatingItem}
                >
                  Cancel
                </button>
                <button
                  type="button"
                  className="btn"
                  onClick={confirmUpdateItem}
                  disabled={updatingItem}
                >
                  {updatingItem ? 'Updating...' : 'Confirm'}
                </button>
              </div>
            </div>
          </div>
        )}

        <div className="search-bar">
          <input
            type="text"
            placeholder="Search items by name or category..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="search-input"
          />
        </div>

        <div className="status-summary-grid">
          <button
            type="button"
            className={`status-summary-card ${activeStatusFilter === 'all' ? 'active' : ''}`}
            onClick={() => setActiveStatusFilter('all')}
          >
            <span className="status-summary-title">All</span>
            <span className="status-summary-value">{filteredItems.length}</span>
          </button>
          <button
            type="button"
            className={`status-summary-card expired ${activeStatusFilter === 'expired' ? 'active' : ''}`}
            onClick={() => setActiveStatusFilter('expired')}
          >
            <span className="status-summary-title">Expired</span>
            <span className="status-summary-value">{statusCounts.expired}</span>
          </button>
          <button
            type="button"
            className={`status-summary-card today ${activeStatusFilter === 'today' ? 'active' : ''}`}
            onClick={() => setActiveStatusFilter('today')}
          >
            <span className="status-summary-title">Today</span>
            <span className="status-summary-value">{statusCounts.today}</span>
          </button>
          <button
            type="button"
            className={`status-summary-card tomorrow ${activeStatusFilter === 'tomorrow' ? 'active' : ''}`}
            onClick={() => setActiveStatusFilter('tomorrow')}
          >
            <span className="status-summary-title">Tomorrow</span>
            <span className="status-summary-value">{statusCounts.tomorrow}</span>
          </button>
          <button
            type="button"
            className={`status-summary-card soon ${activeStatusFilter === 'soon' ? 'active' : ''}`}
            onClick={() => setActiveStatusFilter('soon')}
          >
            <span className="status-summary-title">Expiring Soon</span>
            <span className="status-summary-value">{statusCounts.soon}</span>
          </button>
          <button
            type="button"
            className={`status-summary-card fresh ${activeStatusFilter === 'fresh' ? 'active' : ''}`}
            onClick={() => setActiveStatusFilter('fresh')}
          >
            <span className="status-summary-title">Fresh</span>
            <span className="status-summary-value">{statusCounts.fresh}</span>
          </button>
        </div>

        {sortedItems.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">🥗</div>
            <h2>{fridgeItems.length === 0 ? 'Your Fridge is Empty' : 'No Items Match This Filter'}</h2>
            <p>
              {fridgeItems.length === 0
                ? 'Start adding ingredients to track your inventory'
                : 'Try another status card or clear your search keywords.'}
            </p>
            <button
              className="btn"
              onClick={() => {
                setShowAddForm(true);
                resetForm();
              }}
            >
              Add First Item
            </button>
          </div>
        ) : (
          <div className="fridge-items-list">
            {sortedItems.map((item) => (
              <div key={item.id} className={`fridge-item ${getExpiryClass(item)}`}>
                <div className="item-main">
                  <div className="item-info">
                    <h3>{item.ingredient.name}</h3>
                  </div>
                  <div className="item-quantity">
                    <span className="quantity-value">
                      {item.quantity} {item.unit}
                    </span>
                  </div>
                </div>
                <div className="item-footer">
                  <div className="expiry-info">
                    <span className={`expiry-label ${getExpiryClass(item)}`}>
                      {getExpiryLabel(item)}
                    </span>
                    <span className="expiry-date">
                      {new Date(item.expiryDate).toLocaleDateString()}
                    </span>
                  </div>
                  <div className="item-actions">
                    <button
                      className="btn-icon"
                      onClick={() => startEdit(item)}
                      title="Edit"
                    >
                      ✏️
                    </button>
                    <button
                      className="btn-icon"
                      onClick={() => requestDeleteItem(item.id)}
                      title="Delete"
                    >
                      🗑️
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        {showDeleteConfirmModal && (
          <div className="delete-modal-overlay" role="dialog" aria-modal="true" aria-labelledby="delete-fridge-item-title">
            <div className="delete-modal-card">
              <div className="delete-modal-header">
                <h3 id="delete-fridge-item-title">Confirm Delete Item</h3>
              </div>
              <div className="delete-modal-body">
                <p>Are you sure you want to delete this item from your fridge?</p>
                <p>This action cannot be undone.</p>
              </div>
              <div className="delete-modal-actions">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => {
                    if (deletingItem) return;
                    setShowDeleteConfirmModal(false);
                    setPendingDeleteItemId(null);
                  }}
                  disabled={deletingItem}
                >
                  Cancel
                </button>
                <button
                  type="button"
                  className="btn"
                  onClick={handleDeleteItem}
                  disabled={deletingItem}
                >
                  {deletingItem ? 'Deleting...' : 'Delete'}
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </Container>
  );
};

export default VirtualFridge;
