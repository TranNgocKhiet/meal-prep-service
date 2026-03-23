import { useState, useEffect, type ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { createPortal } from 'react-dom';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import { formatVND } from '../utils/currency';
import './MyCart.css';

interface CartItem {
  id: string;
  menuMealId: string;
  quantity: number;
  menuMeal: {
    id: string;
    mealTypeId: number;
    price: number;
    availableQuantity: number;
    menuMealRecipes: Array<{
      recipe: {
        id: string;
        recipeName: string;
      };
    }>;
  };
}

interface Cart {
  id: string;
  cartItems: CartItem[];
  updatedAt: string;
}

const MyCart = () => {
  const [cart, setCart] = useState<Cart | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showCheckoutModal, setShowCheckoutModal] = useState(false);
  const [showRemoveConfirmModal, setShowRemoveConfirmModal] = useState(false);
  const [showClearConfirmModal, setShowClearConfirmModal] = useState(false);
  const [pendingRemoveItemId, setPendingRemoveItemId] = useState<string | null>(null);
  const [paymentMethod, setPaymentMethod] = useState('Cash');
  const [address, setAddress] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [isCreatingOrder, setIsCreatingOrder] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    fetchCart();
  }, []);

  const getMealTypeName = (mealTypeId: number) => {
    switch (mealTypeId) {
      case 1: return 'Breakfast';
      case 2: return 'Lunch';
      case 3: return 'Dinner';
      default: return 'Unknown';
    }
  };

  const fetchCart = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get('/cart');
      if (response.data.success) {
        setCart(response.data.data);
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load cart');
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateQuantity = async (cartItemId: string, newQuantity: number) => {
    if (newQuantity < 1) return;

    // Store the previous state in case we need to rollback
    const previousCart = cart;

    // Optimistically update the local state
    if (cart) {
      const updatedCart = {
        ...cart,
        cartItems: cart.cartItems.map(item =>
          item.id === cartItemId ? { ...item, quantity: newQuantity } : item
        )
      };
      setCart(updatedCart);
    }

    try {
      await apiClient.put(`/cart/items/${cartItemId}`, { quantity: newQuantity });
      // If successful, no need to refetch - local state is already correct
    } catch (err: any) {
      // Rollback to previous state on error
      setCart(previousCart);
      alert(err.response?.data?.message || 'Failed to update quantity');
    }
  };

  const handleRemoveItem = (cartItemId: string) => {
    setPendingRemoveItemId(cartItemId);
    setShowRemoveConfirmModal(true);
  };

  const confirmRemoveItem = async () => {
    if (!pendingRemoveItemId) return;

    try {
      await apiClient.delete(`/cart/items/${pendingRemoveItemId}`);
      await fetchCart();
      setShowRemoveConfirmModal(false);
      setPendingRemoveItemId(null);
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to remove item');
    }
  };

  const handleClearCart = () => {
    setShowClearConfirmModal(true);
  };

  const confirmClearCart = async () => {

    try {
      await apiClient.delete('/cart');
      await fetchCart();
      setShowClearConfirmModal(false);
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to clear cart');
    }
  };

  const calculateTotal = () => {
    if (!cart) return 0;
    return cart.cartItems.reduce((total, item) => 
      total + (item.menuMeal.price * item.quantity), 0
    );
  };

  const handleCheckout = () => {
    setShowCheckoutModal(true);
  };

  const renderModal = (modal: ReactNode) => {
    if (typeof document === 'undefined') return null;
    return createPortal(modal, document.body);
  };

  const validatePhoneNumber = (phone: string) => {
    const phoneRegex = /^(\+84|0)[0-9]{9,10}$/;
    return phoneRegex.test(phone);
  };

  const handleConfirmOrder = async () => {
    // Validate address
    if (!address || address.trim().length < 10) {
      alert('Please enter a valid address (at least 10 characters)');
      return;
    }

    if (address.trim().length > 500) {
      alert('Address is too long (maximum 500 characters)');
      return;
    }

    // Validate phone number
    if (!phoneNumber || !validatePhoneNumber(phoneNumber)) {
      alert('Please enter a valid Vietnamese phone number (e.g., 0912345678 or +84912345678)');
      return;
    }

    try {
      setIsCreatingOrder(true);
      const response = await apiClient.post('/orders', {
        paymentMethod,
        address: address.trim(),
        phoneNumber: phoneNumber.trim()
      });
      
      if (response.data.success) {
        const data = response.data.data;
        
        // If VNPay payment, redirect to payment URL
        if (paymentMethod === 'VNPay' && data.paymentUrl) {
          window.location.href = data.paymentUrl;
        } else {
          // Cash payment - order created successfully
          alert('Order created successfully!');
          setShowCheckoutModal(false);
          navigate('/my-orders');
        }
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to create order');
      setIsCreatingOrder(false);
    }
  };

  if (loading) {
    return (
      <Container>
        <div className="loading-container">
          <div className="spinner"></div>
          <p>Loading cart...</p>
        </div>
      </Container>
    );
  }

  if (error) {
    return (
      <Container>
        <div className="error-container">
          <p>{error}</p>
          <button className="btn btn-primary" onClick={fetchCart}>Try Again</button>
        </div>
      </Container>
    );
  }

  return (
    <Container>
      <div className="cart-page">
        <div className="cart-header">
          <h1>My Cart</h1>
          {cart && cart.cartItems.length > 0 && (
            <button className="btn btn-danger" onClick={handleClearCart}>
              Clear Cart
            </button>
          )}
        </div>

        {!cart || cart.cartItems.length === 0 ? (
          <div className="empty-cart">
            <p>Your cart is empty</p>
            <button className="btn" onClick={() => navigate('/weekly-menu')}>
              Browse Menu
            </button>
          </div>
        ) : (
          <>
            <div className="cart-items">
              {cart.cartItems.map((item) => (
                <div key={item.id} className="cart-item">
                  <div className="item-info">
                    <h3>{getMealTypeName(item.menuMeal.mealTypeId)}</h3>
                    <div className="item-recipes">
                      {item.menuMeal.menuMealRecipes.map((mr, idx) => (
                        <span key={idx} className="recipe-name">
                          {mr.recipe.recipeName}
                        </span>
                      ))}
                    </div>
                    <p className="item-price">{formatVND(item.menuMeal.price)} each</p>
                  </div>
                  
                  <div className="item-actions">
                    <div className="quantity-controls">
                      <button 
                        onClick={() => handleUpdateQuantity(item.id, item.quantity - 1)}
                        disabled={item.quantity <= 1}
                      >
                        -
                      </button>
                      <span className="quantity">{item.quantity}</span>
                      <button 
                        onClick={() => handleUpdateQuantity(item.id, item.quantity + 1)}
                        disabled={item.quantity >= item.menuMeal.availableQuantity}
                      >
                        +
                      </button>
                    </div>
                    
                    <p className="item-total">
                      {formatVND(item.menuMeal.price * item.quantity)}
                    </p>
                    
                    <button 
                      className="btn btn-sm btn-danger"
                      onClick={() => handleRemoveItem(item.id)}
                    >
                      Remove
                    </button>
                  </div>
                </div>
              ))}
            </div>

            <div className="cart-summary">
              <div className="summary-row">
                <span>Subtotal:</span>
                <span>{formatVND(calculateTotal())}</span>
              </div>
              <div className="summary-row total">
                <span>Total:</span>
                <span>{formatVND(calculateTotal())}</span>
              </div>
              <button className="btn btn-checkout" onClick={handleCheckout}>
                Proceed to Checkout
              </button>
            </div>
          </>
        )}

        {/* Checkout Modal */}
        {showCheckoutModal && renderModal(
          <div className="modal-overlay checkout-modal-overlay" onClick={() => setShowCheckoutModal(false)}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()}>
              <h2>Checkout</h2>
              <div className="checkout-form">
                <div className="form-group">
                  <label>Phone Number *</label>
                  <input
                    type="tel"
                    value={phoneNumber}
                    onChange={(e) => setPhoneNumber(e.target.value)}
                    placeholder="0912345678 or +84912345678"
                    className="form-control"
                    required
                  />
                </div>

                <div className="form-group">
                  <label>Delivery Address *</label>
                  <textarea
                    value={address}
                    onChange={(e) => setAddress(e.target.value)}
                    placeholder="Enter your delivery address"
                    className="form-control"
                    rows={3}
                    required
                  />
                </div>

                <div className="form-group">
                  <label>Payment Method</label>
                  <select
                    value={paymentMethod}
                    onChange={(e) => setPaymentMethod(e.target.value)}
                    className="form-control"
                  >
                    <option value="Cash">Cash on Delivery</option>
                    <option value="VNPay">VNPay</option>
                  </select>
                </div>

                <div className="order-summary">
                  <h3>Order Summary</h3>
                  <p>{cart?.cartItems.length} items</p>
                  <p className="total">Total: {formatVND(calculateTotal())}</p>
                </div>

                <div className="modal-actions">
                  <button
                    className="btn btn-secondary"
                    onClick={() => setShowCheckoutModal(false)}
                    disabled={isCreatingOrder}
                  >
                    Cancel
                  </button>
                  <button
                    className="btn"
                    onClick={handleConfirmOrder}
                    disabled={isCreatingOrder}
                  >
                    {isCreatingOrder ? 'Creating Order...' : 'Confirm Order'}
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}

        {showRemoveConfirmModal && renderModal(
          <div
            className="modal-overlay"
            onClick={() => {
              setShowRemoveConfirmModal(false);
              setPendingRemoveItemId(null);
            }}
          >
            <div className="modal-content confirm-modal-content" onClick={(e) => e.stopPropagation()}>
              <h2>Remove Item</h2>
              <p className="confirm-modal-message">Are you sure you want to remove this item from your cart?</p>
              <div className="modal-actions">
                <button
                  className="btn btn-secondary"
                  onClick={() => {
                    setShowRemoveConfirmModal(false);
                    setPendingRemoveItemId(null);
                  }}
                >
                  Cancel
                </button>
                <button className="btn btn-danger" onClick={confirmRemoveItem}>
                  Remove
                </button>
              </div>
            </div>
          </div>
        )}

        {showClearConfirmModal && renderModal(
          <div className="modal-overlay" onClick={() => setShowClearConfirmModal(false)}>
            <div className="modal-content confirm-modal-content" onClick={(e) => e.stopPropagation()}>
              <h2>Clear Cart</h2>
              <p className="confirm-modal-message">Are you sure you want to remove all items from your cart?</p>
              <div className="modal-actions">
                <button className="btn btn-secondary" onClick={() => setShowClearConfirmModal(false)}>
                  Cancel
                </button>
                <button className="btn btn-danger" onClick={confirmClearCart}>
                  Clear Cart
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </Container>
  );
};

export default MyCart;
