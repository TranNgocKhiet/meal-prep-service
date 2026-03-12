import { useState, useEffect } from 'react';
import { getErrorMessage } from '../types/errors';
import { useNavigate } from 'react-router-dom';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import './StaffOrders.css';

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

const StaffOrders = () => {
  const navigate = useNavigate();
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [filter, setFilter] = useState<'pending' | 'all'>('pending');

  useEffect(() => {
    fetchOrders();
  }, [filter]);

  const fetchOrders = async () => {
    try {
      setLoading(true);
      const endpoint = filter === 'pending' ? '/orders/pending' : '/orders';
      const response = await apiClient.get(endpoint);
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
      default:
        return '';
    }
  };

  const getStatusPriority = (status: string) => {
    switch (status.toLowerCase()) {
      case 'pending':
        return 1;
      case 'confirmed':
        return 2;
      case 'paid':
        return 3;
      default:
        return 4;
    }
  };

  const sortedOrders = [...orders].sort((a, b) => {
    const priorityDiff = getStatusPriority(a.status) - getStatusPriority(b.status);
    if (priorityDiff !== 0) return priorityDiff;
    return new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
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
      <div className="staff-orders-page">
        <div className="page-header">
          <h1>Order Management</h1>
          <div className="header-stats">
            <div className="stat-card">
              <span className="stat-value">{orders.filter(o => o.status.toLowerCase() === 'pending').length}</span>
              <span className="stat-label">Pending</span>
            </div>
            <div className="stat-card">
              <span className="stat-value">{orders.filter(o => o.status.toLowerCase() === 'confirmed').length}</span>
              <span className="stat-label">Confirmed</span>
            </div>
          </div>
        </div>

        {error && (
          <div className="error-message">
            {error}
          </div>
        )}

        <div className="filter-tabs">
          <button
            className={`filter-tab ${filter === 'pending' ? 'active' : ''}`}
            onClick={() => setFilter('pending')}
          >
            Pending Orders
          </button>
          <button
            className={`filter-tab ${filter === 'all' ? 'active' : ''}`}
            onClick={() => setFilter('all')}
          >
            All Orders
          </button>
        </div>

        {sortedOrders.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">📦</div>
            <h2>No Orders Found</h2>
            <p>
              {filter === 'pending'
                ? 'No pending orders at the moment'
                : 'No orders in the system yet'}
            </p>
          </div>
        ) : (
          <div className="orders-grid">
            {sortedOrders.map((order) => (
              <div
                key={order.id}
                className={`order-card ${order.status.toLowerCase() === 'pending' ? 'priority' : ''}`}
                onClick={() => navigate(`/orders/${order.id}`)}
              >
                <div className="order-card-header">
                  <div>
                    <h3>#{order.orderNumber}</h3>
                    <p className="order-time">
                      {new Date(order.createdAt).toLocaleString()}
                    </p>
                  </div>
                  <span className={`order-status ${getStatusColor(order.status)}`}>
                    {order.status}
                  </span>
                </div>

                <div className="order-card-body">
                  <div className="info-row">
                    <span className="info-icon">👤</span>
                    <div className="info-content">
                      <span className="info-label">Customer</span>
                      <span className="info-value">{order.contactName}</span>
                    </div>
                  </div>

                  <div className="info-row">
                    <span className="info-icon">📞</span>
                    <div className="info-content">
                      <span className="info-label">Phone</span>
                      <span className="info-value">{order.contactPhone}</span>
                    </div>
                  </div>

                  <div className="info-row">
                    <span className="info-icon">📍</span>
                    <div className="info-content">
                      <span className="info-label">Address</span>
                      <span className="info-value">{order.deliveryAddress}</span>
                    </div>
                  </div>

                  <div className="info-row">
                    <span className="info-icon">💳</span>
                    <div className="info-content">
                      <span className="info-label">Payment</span>
                      <span className="info-value">{order.paymentMethod}</span>
                    </div>
                  </div>
                </div>

                <div className="order-card-footer">
                  <div className="order-total">
                    <span>Total:</span>
                    <span className="total-amount">{order.totalAmount.toLocaleString()} VND</span>
                  </div>
                  {order.status.toLowerCase() === 'pending' && (
                    <button
                      className="btn btn-sm btn-primary"
                      onClick={(e) => {
                        e.stopPropagation();
                        navigate(`/orders/${order.id}`);
                      }}
                    >
                      Process Order
                    </button>
                  )}
                  {order.status.toLowerCase() !== 'pending' && (
                    <button className="btn btn-sm btn-secondary">
                      View Details
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </Container>
  );
};

export default StaffOrders;
