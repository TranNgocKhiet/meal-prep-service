import { useState, useEffect } from 'react';
import { getErrorMessage } from '../types/errors';
import { useNavigate, useSearchParams } from 'react-router-dom';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import './CreateOrder.css';

interface Ingredient {
  id: string;
  name: string;
  category: string;
  unit: string;
  pricePerUnit: number;
  imageUrl: string;
  isAvailableForPurchase: boolean;
}

interface OrderItem {
  ingredientId: string;
  quantity: number;
  unit: string;
}

interface OrderItemDisplay extends OrderItem {
  ingredient: Ingredient;
  totalPrice: number;
}

const CreateOrder = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [ingredients, setIngredients] = useState<Ingredient[]>([]);
  const [orderItems, setOrderItems] = useState<OrderItemDisplay[]>([]);
  const [loading, setLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [deliveryAddress, setDeliveryAddress] = useState('');
  const [contactName, setContactName] = useState('');
  const [contactPhone, setContactPhone] = useState('');
  const [paymentMethod, setPaymentMethod] = useState<'VNPay' | 'Cash'>('Cash');
  const [deliveryFee, setDeliveryFee] = useState<number | null>(null);
  const [distanceError, setDistanceError] = useState('');
  const [validatingAddress, setValidatingAddress] = useState(false);

  const groceryListId = searchParams.get('groceryListId');

  useEffect(() => {
    if (groceryListId) {
      loadFromGroceryList(groceryListId);
    }
    fetchIngredients();
  }, [groceryListId]);

  const fetchIngredients = async () => {
    try {
      const response = await apiClient.get('/ingredients');
      if (response.data.success) {
        const availableIngredients = response.data.data.filter(
          (ing: Ingredient) => ing.isAvailableForPurchase
        );
        setIngredients(availableIngredients);
      }
    } catch (err) {
      console.error('Failed to fetch ingredients:', err);
    }
  };

  const loadFromGroceryList = async (listId: string) => {
    try {
      const response = await apiClient.get(`/grocerylists/${listId}`);
      if (response.data.success) {
        const groceryList = response.data.data;
        const items = groceryList.items
          .filter((item: { isPurchased: boolean }) => !item.isPurchased)
          .map((item: { ingredient: { id: string; pricePerUnit: number }; deficitQuantity: number; unit: string }) => ({
            ingredientId: item.ingredient.id,
            quantity: item.deficitQuantity,
            unit: item.unit,
            ingredient: item.ingredient,
            totalPrice: item.deficitQuantity * item.ingredient.pricePerUnit,
          }));
        setOrderItems(items);
      }
    } catch (err) {
      console.error('Failed to load grocery list:', err);
    }
  };

  const validateDeliveryAddress = async (address: string) => {
    if (!address.trim()) {
      setDeliveryFee(null);
      setDistanceError('');
      return;
    }

    setValidatingAddress(true);
    setDistanceError('');

    try {
      const response = await apiClient.get(`/orders/delivery-fee?address=${encodeURIComponent(address)}`);
      if (response.data.success) {
        setDeliveryFee(response.data.data.deliveryFee);
        setDistanceError('');
      }
    } catch (err: unknown) {
      setDeliveryFee(null);
      setDistanceError(getErrorMessage(err) || 'Address is too far from service center (max 10km)');
    } finally {
      setValidatingAddress(false);
    }
  };

  const handleAddressBlur = () => {
    validateDeliveryAddress(deliveryAddress);
  };

  const addIngredient = (ingredient: Ingredient) => {
    const existing = orderItems.find(item => item.ingredientId === ingredient.id);
    if (existing) {
      updateQuantity(ingredient.id, existing.quantity + 1);
    } else {
      setOrderItems([
        ...orderItems,
        {
          ingredientId: ingredient.id,
          quantity: 1,
          unit: ingredient.unit,
          ingredient,
          totalPrice: ingredient.pricePerUnit,
        },
      ]);
    }
  };

  const updateQuantity = (ingredientId: string, quantity: number) => {
    if (quantity <= 0) {
      removeItem(ingredientId);
      return;
    }

    setOrderItems(
      orderItems.map(item =>
        item.ingredientId === ingredientId
          ? {
              ...item,
              quantity,
              totalPrice: quantity * item.ingredient.pricePerUnit,
            }
          : item
      )
    );
  };

  const removeItem = (ingredientId: string) => {
    setOrderItems(orderItems.filter(item => item.ingredientId !== ingredientId));
  };

  const calculateSubTotal = () => {
    return orderItems.reduce((sum, item) => sum + item.totalPrice, 0);
  };

  const calculateTotal = () => {
    const subTotal = calculateSubTotal();
    return subTotal + (deliveryFee || 0);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (orderItems.length === 0) {
      alert('Please add at least one item to your order');
      return;
    }

    if (!deliveryAddress.trim() || !contactName.trim() || !contactPhone.trim()) {
      alert('Please fill in all required fields');
      return;
    }

    if (distanceError) {
      alert('Please provide a valid delivery address within 10km');
      return;
    }

    if (deliveryFee === null) {
      alert('Please wait for address validation to complete');
      return;
    }

    setLoading(true);

    try {
      const orderData = {
        items: orderItems.map(item => ({
          ingredientId: item.ingredientId,
          quantity: item.quantity,
          unit: item.unit,
        })),
        deliveryAddress,
        contactName,
        contactPhone,
        paymentMethod,
      };

      const response = await apiClient.post('/orders', orderData);

      if (response.data.success) {
        const order = response.data.data;

        if (paymentMethod === 'VNPay') {
          // Redirect to VNPay payment page
          const paymentResponse = await apiClient.post(`/payments/create-payment-url/${order.id}`);
          if (paymentResponse.data.success) {
            window.location.href = paymentResponse.data.data.paymentUrl;
          }
        } else {
          // Cash payment - go to order confirmation
          navigate(`/orders/${order.id}?created=true`);
        }
      }
    } catch (err: unknown) {
      alert(getErrorMessage(err) || 'Failed to create order');
    } finally {
      setLoading(false);
    }
  };

  const filteredIngredients = ingredients.filter(ing =>
    ing.name.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <Container>
      <div className="create-order-page">
        <div className="page-header">
          <button className="btn-back" onClick={() => navigate(-1)}>
            ← Back
          </button>
          <h1>Create Order</h1>
        </div>

        <div className="order-layout">
          <div className="order-form-section">
            <form onSubmit={handleSubmit}>
              <div className="form-section">
                <h2>Delivery Information</h2>
                
                <div className="form-group">
                  <label htmlFor="contactName">Contact Name *</label>
                  <input
                    type="text"
                    id="contactName"
                    value={contactName}
                    onChange={(e) => setContactName(e.target.value)}
                    required
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="contactPhone">Contact Phone *</label>
                  <input
                    type="tel"
                    id="contactPhone"
                    value={contactPhone}
                    onChange={(e) => setContactPhone(e.target.value)}
                    required
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="deliveryAddress">Delivery Address *</label>
                  <textarea
                    id="deliveryAddress"
                    value={deliveryAddress}
                    onChange={(e) => setDeliveryAddress(e.target.value)}
                    onBlur={handleAddressBlur}
                    rows={3}
                    required
                  />
                  {validatingAddress && (
                    <span className="input-hint">Validating address...</span>
                  )}
                  {distanceError && (
                    <span className="input-error">{distanceError}</span>
                  )}
                  {deliveryFee !== null && !distanceError && (
                    <span className="input-success">
                      ✓ Address validated. Delivery fee: {deliveryFee.toLocaleString()} VND
                    </span>
                  )}
                </div>
              </div>

              <div className="form-section">
                <h2>Payment Method</h2>
                <div className="payment-methods">
                  <label className="payment-option">
                    <input
                      type="radio"
                      name="paymentMethod"
                      value="Cash"
                      checked={paymentMethod === 'Cash'}
                      onChange={() => setPaymentMethod('Cash')}
                    />
                    <div className="payment-option-content">
                      <span className="payment-icon">💵</span>
                      <div>
                        <div className="payment-title">Cash on Delivery</div>
                        <div className="payment-description">Pay when you receive your order</div>
                      </div>
                    </div>
                  </label>

                  <label className="payment-option">
                    <input
                      type="radio"
                      name="paymentMethod"
                      value="VNPay"
                      checked={paymentMethod === 'VNPay'}
                      onChange={() => setPaymentMethod('VNPay')}
                    />
                    <div className="payment-option-content">
                      <span className="payment-icon">💳</span>
                      <div>
                        <div className="payment-title">VNPay Online Payment</div>
                        <div className="payment-description">Pay securely online now</div>
                      </div>
                    </div>
                  </label>
                </div>
              </div>

              <div className="form-section">
                <h2>Add Ingredients</h2>
                <div className="ingredient-search">
                  <input
                    type="text"
                    placeholder="Search ingredients..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                  />
                </div>

                <div className="ingredient-grid">
                  {filteredIngredients.slice(0, 12).map((ingredient) => (
                    <div key={ingredient.id} className="ingredient-card">
                      <div className="ingredient-info">
                        <h4>{ingredient.name}</h4>
                        <p className="ingredient-category">{ingredient.category}</p>
                        <p className="ingredient-price">
                          {ingredient.pricePerUnit.toLocaleString()} VND / {ingredient.unit}
                        </p>
                      </div>
                      <button
                        type="button"
                        className="btn btn-sm btn-primary"
                        onClick={() => addIngredient(ingredient)}
                      >
                        Add
                      </button>
                    </div>
                  ))}
                </div>
              </div>
            </form>
          </div>

          <div className="order-summary-section">
            <div className="order-summary-sticky">
              <h2>Order Summary</h2>

              {orderItems.length === 0 ? (
                <div className="empty-cart">
                  <p>No items in your order</p>
                </div>
              ) : (
                <>
                  <div className="order-items">
                    {orderItems.map((item) => (
                      <div key={item.ingredientId} className="order-item">
                        <div className="order-item-details">
                          <h4>{item.ingredient.name}</h4>
                          <p className="order-item-price">
                            {item.ingredient.pricePerUnit.toLocaleString()} VND / {item.unit}
                          </p>
                        </div>
                        <div className="order-item-quantity">
                          <button
                            type="button"
                            onClick={() => updateQuantity(item.ingredientId, item.quantity - 1)}
                          >
                            -
                          </button>
                          <input
                            type="number"
                            value={item.quantity}
                            onChange={(e) =>
                              updateQuantity(item.ingredientId, parseFloat(e.target.value) || 0)
                            }
                            min="0.01"
                            step="0.01"
                          />
                          <button
                            type="button"
                            onClick={() => updateQuantity(item.ingredientId, item.quantity + 1)}
                          >
                            +
                          </button>
                        </div>
                        <div className="order-item-total">
                          {item.totalPrice.toLocaleString()} VND
                        </div>
                        <button
                          type="button"
                          className="btn-remove"
                          onClick={() => removeItem(item.ingredientId)}
                        >
                          ×
                        </button>
                      </div>
                    ))}
                  </div>

                  <div className="order-totals">
                    <div className="total-row">
                      <span>Subtotal:</span>
                      <span>{calculateSubTotal().toLocaleString()} VND</span>
                    </div>
                    <div className="total-row">
                      <span>Delivery Fee:</span>
                      <span>
                        {deliveryFee !== null ? `${deliveryFee.toLocaleString()} VND` : 'TBD'}
                      </span>
                    </div>
                    <div className="total-row total-final">
                      <span>Total:</span>
                      <span>{calculateTotal().toLocaleString()} VND</span>
                    </div>
                  </div>

                  <button
                    type="submit"
                    className="btn btn-primary btn-block"
                    onClick={handleSubmit}
                    disabled={loading || orderItems.length === 0 || deliveryFee === null || !!distanceError}
                  >
                    {loading ? 'Processing...' : paymentMethod === 'VNPay' ? 'Proceed to Payment' : 'Place Order'}
                  </button>
                </>
              )}
            </div>
          </div>
        </div>
      </div>
    </Container>
  );
};

export default CreateOrder;
