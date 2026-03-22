import { useState, useEffect } from 'react';
import { getErrorMessage } from '../types/errors';
import { useNavigate } from 'react-router-dom';
import Container from '../components/layout/Container';
import { useAuth } from '../hooks/useAuth';
import apiClient from '../config/api';
import './Orders.css';

interface Order {
  id: string;
  orderNumber: string;
  deliveryAddress: string;
  contactPhone: string;
  contactName: string;
  paymentMethod: string;
  status: string;
  subTotal: number;
  deliveryFee: number;
  totalAmount: number;
  createdAt: string;
  confirmedAt?: string;
}

const Orders = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [filter, setFilter] = useState<'all' | 'pending' | 'confirmed' | 'cancelled'>('all');

  // Redirect admin/staff to staff orders page
  useEffect(() => {
    if (user?.roleName === 'Admin' || user?.roleName === 'Staff') {
      navigate('/staff/orders', { replace: true });
    }
  }, [user, navigate]);

  useEffect(() => {
    if (user?.roleName !== 'Admin' && user?.roleName !== 'Staff') {
      fetchOrders();
    }
  }, [user]);

  const fetchOrders = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get('/orders/my-orders');
      if (response.data.success) {
        setOrders(response.data.data);
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err) || 'Failed to load orders');
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'pending':
        return 'status-pending';
      case 'confirmed':
        return 'status-confirmed';
      case 'paid':
        return 'status-paid';
      case 'cancelled':
        return 'status-cancelled';
      case 'delivered':
        return 'status-delivered';
      case 'delivering':
        return 'status-delivering';
      default:
        return '';
    }
  };

  const filteredOrders = orders.filter(order => {
    if (filter === 'all') return true;
    return order.status.toLowerCase() === filter;
  });

  if (loading) {
    return (
      <Container>
        <div className="loading-container">
          <div className="spinner"></div>
          <p>Loading orders...</p>
        </div>
      </Container>
    );
  }

  return (
    <Container>
      <div className="orders-page">
        <div className="page-header">
          <h1>My Orders</h1>
          <button className="btn btn-primary" onClick={() => navigate('/orders/create')}>
            + New Order
          </button>
        </div>

        {error && (
          <div className="error-message">
            {error}
          </div>
        )}

        <div className="filter-tabs">
          <button
            className={`filter-tab ${filter === 'all' ? 'active' : ''}`}
            onClick={() => setFilter('all')}
          >
            All Orders
          </button>
          <button
            className={`filter-tab ${filter === 'pending' ? 'active' : ''}`}
            onClick={() => setFilter('pending')}
          >
            Pending
          </button>
          <button
            className={`filter-tab ${filter === 'confirmed' ? 'active' : ''}`}
            onClick={() => setFilter('confirmed')}
          >
            Confirmed
          </button>
          <button
            className={`filter-tab ${filter === 'cancelled' ? 'active' : ''}`}
            onClick={() => setFilter('cancelled')}
          >
            Cancelled
          </button>
        </div>

        {filteredOrders.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">📦</div>
            <h2>No Orders Found</h2>
            <p>
              {filter === 'all'
                ? "You haven't placed any orders yet"
                : `No ${filter} orders`}
            </p>
            {filter === 'all' && (
              <button className="btn btn-primary" onClick={() => navigate('/orders/create')}>
                Create Your First Order
              </button>
            )}
          </div>
        ) : (
          <div className="orders-list">
            {filteredOrders.map((order) => (
              <div
                key={order.id}
                className="order-card"
                onClick={() => navigate(`/orders/${order.id}`)}
              >
                <div className="order-header">
                  <div>
                    <h3>Order #{order.orderNumber}</h3>
                    <p className="order-date">
                      {new Date(order.createdAt).toLocaleDateString()} at{' '}
                      {new Date(order.createdAt).toLocaleTimeString()}
                    </p>
                  </div>
                  <span className={`order-status ${getStatusColor(order.status)}`}>
                    {order.status}
                  </span>
                </div>

                <div className="order-details">
                  <div className="order-detail-item">
                    <span className="detail-label">Delivery Address:</span>
                    <span className="detail-value">{order.deliveryAddress}</span>
                  </div>
                  <div className="order-detail-item">
                    <span className="detail-label">Contact:</span>
                    <span className="detail-value">
                      {order.contactName} - {order.contactPhone}
                    </span>
                  </div>
                  <div className="order-detail-item">
                    <span className="detail-label">Payment Method:</span>
                    <span className="detail-value">{order.paymentMethod}</span>
                  </div>
                </div>

                <div className="order-footer">
                  <div className="order-total">
                    <span>Total:</span>
                    <span className="total-amount">{order.totalAmount.toLocaleString()} VND</span>
                  </div>
                  <button className="btn btn-sm btn-secondary">
                    View Details →
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </Container>
  );
};

export default Orders;
