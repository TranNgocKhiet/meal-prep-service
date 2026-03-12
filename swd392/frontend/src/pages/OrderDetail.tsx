import { useState, useEffect } from 'react';
import { getErrorMessage } from '../types/errors';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import Container from '../components/layout/Container';
import { useAuth } from '../hooks/useAuth';
import apiClient from '../config/api';
import './OrderDetail.css';

interface OrderItem {
  ingredientId: string;
  ingredientName: string;
  ingredientCategory: string;
  quantity: number;
  unit: string;
  unitPrice: number;
  totalPrice: number;
}

interface Order {
  id: string;
  orderNumber: string;
  items: OrderItem[];
  paymentMethod: string;
  status: string;
  subTotal: number;
  deliveryFee: number;
  totalAmount: number;
  createdAt: string;
  updatedAt: string;
  confirmedAt?: string;
  cancellationReason?: string;
  address?: string;
  phoneNumber?: string;
}

const OrderDetail = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { user } = useAuth();
  const [order, setOrder] = useState<Order | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showCancelModal, setShowCancelModal] = useState(false);
  const [cancellationReason, setCancellationReason] = useState('');
  const [processing, setProcessing] = useState(false);

  const isNewOrder = searchParams.get('created') === 'true';
  const isStaff = user?.roleName === 'Admin' || user?.roleName === 'Staff';

  const getBackUrl = () => {
    return isStaff ? '/staff/order-list' : '/my-orders';
  };

  const getBackText = () => {
    return isStaff ? '← Back to Order List' : '← Back to My Orders';
  };

  useEffect(() => {
    if (id) {
      fetchOrder();
    }
  }, [id]);

  const fetchOrder = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get(`/orders/${id}`);
      if (response.data.success) {
        setOrder(response.data.data);
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err) || 'Failed to load order');
    } finally {
      setLoading(false);
    }
  };

  const handleConfirmOrder = async () => {
    if (!order) return;

    setProcessing(true);
    try {
      const response = await apiClient.post(`/orders/${order.id}/confirm`);
      if (response.data.success) {
        await fetchOrder();
        alert('Order confirmed successfully');
      }
    } catch (err: unknown) {
      alert(getErrorMessage(err) || 'Failed to confirm order');
    } finally {
      setProcessing(false);
    }
  };

  const handleCancelOrder = async () => {
    if (!order || !cancellationReason.trim()) {
      alert('Please provide a cancellation reason');
      return;
    }

    setProcessing(true);
    try {
      const response = await apiClient.post(`/orders/${order.id}/cancel`, {
        reason: cancellationReason,
      });
      if (response.data.success) {
        await fetchOrder();
        setShowCancelModal(false);
        setCancellationReason('');
        alert('Order cancelled successfully');
      }
    } catch (err: unknown) {
      alert(getErrorMessage(err) || 'Failed to cancel order');
    } finally {
      setProcessing(false);
    }
  };

  const getStatusColor = (status: string) => {
    const statusLower = status.toLowerCase();
    if (statusLower.includes('pending')) return 'status-pending';
    if (statusLower.includes('confirmed')) return 'status-confirmed';
    if (statusLower.includes('preparing')) return 'status-preparing';
    if (statusLower.includes('ready')) return 'status-ready';
    if (statusLower.includes('delivered')) return 'status-delivered';
    if (statusLower.includes('cancel')) return 'status-cancelled';
    return '';
  };

  if (loading) {
    return (
      <Container>
        <div className="loading-container">
          <div className="spinner"></div>
          <p>Loading order details...</p>
        </div>
      </Container>
    );
  }

  if (error || !order) {
    return (
      <Container>
        <div className="error-state">
          <div className="error-icon">❌</div>
          <h2>Error Loading Order</h2>
          <p>{error || 'Order not found'}</p>
          <button className="btn btn-primary" onClick={() => navigate(getBackUrl())}>
            {getBackText().replace('← ', '')}
          </button>
        </div>
      </Container>
    );
  }

  return (
    <Container>
      <div className="order-detail-page">
        <div className="page-header">
          <button className="btn-back" onClick={() => navigate(getBackUrl())}>
            {getBackText()}
          </button>
        </div>

        {isNewOrder && order.paymentMethod === 'Cash' && (
          <div className="success-banner">
            <div className="success-icon">✓</div>
            <div>
              <h3>Order Placed Successfully!</h3>
              <p>Your order has been received and is pending confirmation.</p>
            </div>
          </div>
        )}

        <div className="order-detail-header">
          <div>
            <h1>Order #{order.orderNumber}</h1>
            <p className="order-date">
              Placed on {new Date(order.createdAt).toLocaleDateString('en-US', { 
                month: 'long', 
                day: 'numeric', 
                year: 'numeric' 
              })} at{' '}
              {new Date(order.createdAt).toLocaleTimeString('en-US', { 
                hour: '2-digit', 
                minute: '2-digit' 
              })}
            </p>
          </div>
          <span className={`order-status ${getStatusColor(order.status)}`}>
            {order.status}
          </span>
        </div>

        <div className="order-content">
          <div className="order-main">
            <div className="info-section">
              <h2>Order Items</h2>
              <div className="order-items-list">
                {order.items.map((item, index) => (
                  <div key={index} className="order-item-row">
                    <div className="item-info">
                      <h4>{item.ingredientName}</h4>
                      <p className="item-category">{item.ingredientCategory}</p>
                    </div>
                    <div className="item-quantity">
                      Qty: {item.quantity}
                    </div>
                    <div className="item-unit-price">
                      {item.unitPrice.toLocaleString()} VND each
                    </div>
                    <div className="item-total-price">
                      {item.totalPrice.toLocaleString()} VND
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="info-section">
              <h2>Payment Information</h2>
              <div className="info-grid">
                <div className="info-item">
                  <span className="info-label">Payment Method:</span>
                  <span className="info-value">{order.paymentMethod}</span>
                </div>
                <div className="info-item">
                  <span className="info-label">Created At:</span>
                  <span className="info-value">
                    {new Date(order.createdAt).toLocaleString('en-US', {
                      month: 'short',
                      day: 'numeric',
                      year: 'numeric',
                      hour: '2-digit',
                      minute: '2-digit'
                    })}
                  </span>
                </div>
                <div className="info-item">
                  <span className="info-label">Updated At:</span>
                  <span className="info-value">
                    {new Date(order.updatedAt).toLocaleString('en-US', {
                      month: 'short',
                      day: 'numeric',
                      year: 'numeric',
                      hour: '2-digit',
                      minute: '2-digit'
                    })}
                  </span>
                </div>
                {order.confirmedAt && (
                  <div className="info-item">
                    <span className="info-label">Confirmed At:</span>
                    <span className="info-value">
                      {new Date(order.confirmedAt).toLocaleString('en-US', {
                        month: 'short',
                        day: 'numeric',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit'
                      })}
                    </span>
                  </div>
                )}
              </div>
            </div>

            <div className="info-section">
              <h2>Delivery Information</h2>
              <div className="info-grid">
                <div className="info-item">
                  <span className="info-label">Phone Number:</span>
                  <span className="info-value">{order.phoneNumber || 'N/A'}</span>
                </div>
                <div className="info-item full-width">
                  <span className="info-label">Delivery Address:</span>
                  <span className="info-value">{order.address || 'N/A'}</span>
                </div>
              </div>
            </div>

            {order.cancellationReason && (
              <div className="info-section cancellation-section">
                <h2>Cancellation Reason</h2>
                <p>{order.cancellationReason}</p>
              </div>
            )}
          </div>

          <div className="order-sidebar">
            <div className="summary-card">
              <h3>Order Summary</h3>
              <div className="summary-rows">
                <div className="summary-row">
                  <span>Total Items:</span>
                  <span>{order.items.reduce((sum, item) => sum + item.quantity, 0)}</span>
                </div>
                <div className="summary-row">
                  <span>Subtotal:</span>
                  <span>{order.subTotal.toLocaleString()} VND</span>
                </div>
                {order.deliveryFee > 0 && (
                  <div className="summary-row">
                    <span>Delivery Fee:</span>
                    <span>{order.deliveryFee.toLocaleString()} VND</span>
                  </div>
                )}
                <div className="summary-row summary-total">
                  <span>Total Amount:</span>
                  <span>{order.totalAmount.toLocaleString()} VND</span>
                </div>
              </div>

              {isStaff && order.status.toLowerCase().includes('pending') && (
                <div className="action-buttons">
                  <button
                    className="btn btn-primary btn-block"
                    onClick={handleConfirmOrder}
                    disabled={processing}
                  >
                    {processing ? 'Processing...' : 'Confirm Order'}
                  </button>
                  <button
                    className="btn btn-secondary btn-block"
                    onClick={() => setShowCancelModal(true)}
                    disabled={processing}
                  >
                    Cancel Order
                  </button>
                </div>
              )}

              {!isStaff && order.status.toLowerCase().includes('pending') && (
                <div className="action-buttons">
                  <button
                    className="btn btn-secondary btn-block"
                    onClick={() => setShowCancelModal(true)}
                    disabled={processing}
                  >
                    Cancel Order
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>

        {showCancelModal && (
          <div className="modal-overlay" onClick={() => setShowCancelModal(false)}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()}>
              <div className="modal-header">
                <h2>Cancel Order</h2>
                <button className="btn-close" onClick={() => setShowCancelModal(false)}>
                  ×
                </button>
              </div>
              <div className="modal-body">
                <p>Are you sure you want to cancel this order?</p>
                <div className="form-group">
                  <label htmlFor="cancellation-reason">Cancellation Reason *</label>
                  <textarea
                    id="cancellation-reason"
                    value={cancellationReason}
                    onChange={(e) => setCancellationReason(e.target.value)}
                    rows={4}
                    placeholder="Please provide a reason for cancellation..."
                    required
                  />
                </div>
              </div>
              <div className="modal-actions">
                <button
                  className="btn btn-primary"
                  onClick={handleCancelOrder}
                  disabled={processing || !cancellationReason.trim()}
                >
                  {processing ? 'Processing...' : 'Confirm Cancellation'}
                </button>
                <button
                  className="btn btn-secondary"
                  onClick={() => setShowCancelModal(false)}
                  disabled={processing}
                >
                  Keep Order
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </Container>
  );
};

export default OrderDetail;
