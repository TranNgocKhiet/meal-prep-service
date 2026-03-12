import { useState, useEffect } from 'react';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import './GroceryList.css';

interface GroceryListItem {
  ingredientId: string;
  ingredientName: string;
  unit: string;
  requiredQuantity: number;
  currentQuantity: number;
  missingQuantity: number;
  pricePerUnit: number;
  estimatedCost: number;
  isSelected: boolean;
  purchaseQuantity?: number;
  expiryDate?: string;
}

interface GroceryList {
  items: GroceryListItem[];
  totalEstimatedCost: number;
  totalItems: number;
}

const GroceryList = () => {
  const [groceryList, setGroceryList] = useState<GroceryList | null>(null);
  const [loading, setLoading] = useState(true);
  const [purchasing, setPurchasing] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    fetchGroceryList();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const fetchGroceryList = async () => {
    try {
      setLoading(true);
      setError('');
      const response = await apiClient.get('/fridge/grocery-list');
      if (response.data.success) {
        const list = response.data.data;
        // Initialize purchase quantities and expiry dates, but don't auto-select
        const itemsWithDefaults = list.items.map((item: GroceryListItem) => ({
          ...item,
          isSelected: false, // Don't auto-select items
          purchaseQuantity: item.missingQuantity,
          expiryDate: getDefaultExpiryDate()
        }));
        setGroceryList({
          ...list,
          items: itemsWithDefaults
        });
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load grocery list');
    } finally {
      setLoading(false);
    }
  };

  const getDefaultExpiryDate = () => {
    const date = new Date();
    date.setDate(date.getDate() + 7); // Default 7 days from now
    return date.toISOString().split('T')[0];
  };

  const toggleItemSelection = (ingredientId: string) => {
    if (!groceryList) return;
    
    setGroceryList({
      ...groceryList,
      items: groceryList.items.map(item =>
        item.ingredientId === ingredientId
          ? { ...item, isSelected: !item.isSelected }
          : item
      )
    });
  };

  const toggleSelectAll = () => {
    if (!groceryList) return;
    
    const allSelected = groceryList.items.every(item => item.isSelected);
    
    setGroceryList({
      ...groceryList,
      items: groceryList.items.map(item => ({
        ...item,
        isSelected: !allSelected
      }))
    });
  };

  const updateQuantity = (ingredientId: string, quantity: number) => {
    if (!groceryList) return;
    
    setGroceryList({
      ...groceryList,
      items: groceryList.items.map(item =>
        item.ingredientId === ingredientId
          ? { ...item, purchaseQuantity: quantity }
          : item
      )
    });
  };

  const updateExpiryDate = (ingredientId: string, date: string) => {
    if (!groceryList) return;
    
    setGroceryList({
      ...groceryList,
      items: groceryList.items.map(item =>
        item.ingredientId === ingredientId
          ? { ...item, expiryDate: date }
          : item
      )
    });
  };

  const handlePurchase = async () => {
    if (!groceryList) return;

    const selectedItems = groceryList.items.filter(item => item.isSelected);
    
    if (selectedItems.length === 0) {
      setError('Please select at least one item to purchase');
      return;
    }

    // Validate all selected items have quantity and expiry date
    const invalidItems = selectedItems.filter(
      item => !item.purchaseQuantity || item.purchaseQuantity <= 0 || !item.expiryDate
    );

    if (invalidItems.length > 0) {
      setError('Please ensure all selected items have valid quantity and expiry date');
      return;
    }

    try {
      setPurchasing(true);
      setError('');
      setSuccess('');

      const purchaseData = {
        items: selectedItems.map(item => ({
          ingredientId: item.ingredientId,
          quantity: item.purchaseQuantity!,
          expiryDate: item.expiryDate!
        }))
      };

      const response = await apiClient.post('/fridge/purchase', purchaseData);
      
      if (response.data.success) {
        setSuccess(`Successfully purchased ${selectedItems.length} items!`);
        // Refresh the grocery list
        setTimeout(() => {
          fetchGroceryList();
          setSuccess('');
        }, 2000);
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to purchase items');
    } finally {
      setPurchasing(false);
    }
  };

  if (loading) {
    return (
      <Container>
        <div className="loading-container">
          <div className="spinner"></div>
          <p>Loading grocery list...</p>
        </div>
      </Container>
    );
  }

  if (error && !groceryList) {
    return (
      <Container>
        <div className="error-container">
          <p className="error-message">{error}</p>
          <button className="btn btn-primary" onClick={fetchGroceryList}>
            Try Again
          </button>
        </div>
      </Container>
    );
  }

  const selectedCount = groceryList?.items.filter(item => item.isSelected).length || 0;
  const allSelected = !!(groceryList && groceryList.items.length > 0 && groceryList.items.every(item => item.isSelected));

  return (
    <Container>
      <div className="grocery-list-page">
        <div className="page-header">
          <h1>Grocery List</h1>
          <button className="btn btn-secondary" onClick={fetchGroceryList}>
            Refresh
          </button>
        </div>

        {error && <div className="error-message">{error}</div>}
        {success && <div className="success-message">{success}</div>}

        {!groceryList || groceryList.items.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">🛒</div>
            <h2>No Items Needed</h2>
            <p>You have all the ingredients for your active meal plan!</p>
            <p className="empty-hint">
              Make sure you have an active meal plan with unfinished meals to generate a grocery list.
            </p>
          </div>
        ) : (
          <>
            <div className="grocery-summary">
              <div className="summary-item">
                <span className="summary-label">Total Items:</span>
                <span className="summary-value">{groceryList.totalItems}</span>
              </div>
              <div className="summary-item">
                <span className="summary-label">Selected:</span>
                <span className="summary-value">{selectedCount}</span>
              </div>
            </div>

            <div className="select-all-container">
              <label className="checkbox-container">
                <input
                  type="checkbox"
                  checked={allSelected}
                  onChange={toggleSelectAll}
                />
                <span className="checkmark"></span>
                <span className="select-all-label">Select All</span>
              </label>
            </div>

            <div className="grocery-items-list">
              {groceryList.items.map((item) => (
                <div key={item.ingredientId} className={`grocery-item ${item.isSelected ? 'selected' : ''}`}>
                  <div className="item-header">
                    <label className="checkbox-container">
                      <input
                        type="checkbox"
                        checked={item.isSelected}
                        onChange={() => toggleItemSelection(item.ingredientId)}
                      />
                      <span className="checkmark"></span>
                    </label>
                    <h3>{item.ingredientName}</h3>
                  </div>

                  <div className="item-details">
                    <div className="detail-row">
                      <span className="detail-label">Required:</span>
                      <span className="detail-value">{item.requiredQuantity} {item.unit}</span>
                    </div>
                    <div className="detail-row">
                      <span className="detail-label">Current:</span>
                      <span className="detail-value">{item.currentQuantity} {item.unit}</span>
                    </div>
                    <div className="detail-row missing">
                      <span className="detail-label">Missing:</span>
                      <span className="detail-value">{item.missingQuantity} {item.unit}</span>
                    </div>
                  </div>

                  {item.isSelected && (
                    <div className="purchase-inputs">
                      <div className="input-group">
                        <label htmlFor={`quantity-${item.ingredientId}`}>
                          Purchase Quantity ({item.unit})
                        </label>
                        <input
                          type="number"
                          id={`quantity-${item.ingredientId}`}
                          value={item.purchaseQuantity || ''}
                          onChange={(e) => updateQuantity(item.ingredientId, parseFloat(e.target.value))}
                          min="0.01"
                          step="0.01"
                          required
                        />
                      </div>
                      <div className="input-group">
                        <label htmlFor={`expiry-${item.ingredientId}`}>
                          Expiry Date
                        </label>
                        <input
                          type="date"
                          id={`expiry-${item.ingredientId}`}
                          value={item.expiryDate || ''}
                          onChange={(e) => updateExpiryDate(item.ingredientId, e.target.value)}
                          required
                        />
                      </div>
                    </div>
                  )}
                </div>
              ))}
            </div>

            <div className="purchase-actions">
              <button
                className="btn btn-primary btn-large"
                onClick={handlePurchase}
                disabled={purchasing || selectedCount === 0}
              >
                {purchasing ? 'Purchasing...' : `Purchase ${selectedCount} Item${selectedCount !== 1 ? 's' : ''}`}
              </button>
            </div>
          </>
        )}
      </div>
    </Container>
  );
};

export default GroceryList;
