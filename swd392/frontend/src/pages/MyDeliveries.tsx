import { useState, useEffect } from 'react';
import { getErrorMessage } from '../types/errors';
import { useNavigate } from 'react-router-dom';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import './MyDeliveries.css';

interface Location {
  latitude: number;
  longitude: number;
  timestamp: string;
}

interface Order {
  id: string;
  orderNumber: string;
  deliveryAddress: string;
  contactPhone: string;
  contactName: string;
}

interface Delivery {
  id: string;
  orderId: string;
  order: Order;
  status: string;
  deliveryAddress: string;
  currentLocation: Location | null;
  assignedAt: string;
  deliveredAt: string | null;
  estimatedDeliveryTime: string | null;
}

const MyDeliveries = () => {
  const navigate = useNavigate();
  const [deliveries, setDeliveries] = useState<Delivery[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [filter, setFilter] = useState<'all' | 'assigned' | 'pickedup' | 'intransit' | 'delivered'>('all');

  useEffect(() => {
    fetchDeliveries();
  }, []);

  const fetchDeliveries = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get('/delivery/assigned');
      if (response.data.success) {
        setDeliveries(response.data.data);
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err) || 'Failed to load deliveries');
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'assigned':
        return 'status-assigned';
      case 'pickedup':
        return 'status-pickedup';
      case 'intransit':
        return 'status-intransit';
      case 'delivered':
        return 'status-delivered';
      case 'failed':
        return 'status-failed';
      default:
        return '';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case 'assigned':
        return '📋';
      case 'pickedup':
        return '📦';
      case 'intransit':
        return '🚚';
      case 'delivered':
        return '✅';
      case 'failed':
        return '❌';
      default:
        return '📦';
    }
  };

  const filteredDeliveries = deliveries.filter(delivery => {
    if (filter === 'all') return true;
    return delivery.status.toLowerCase() === filter;
  });

  const activeDeliveries = deliveries.filter(d => 
    ['assigned', 'pickedup', 'intransit'].includes(d.status.toLowerCase())
  );

  if (loading) {
    return (
      <Container>
        <div className="loading-container">
          <div className="spinner"></div>
          <p>Loading deliveries...</p>
        </div>
      </Container>
    );
  }

  return (
    <Container>
      <div className="my-deliveries-page">
        <div className="page-header">
          <h1>My Deliveries</h1>
          <div className="header-stats">
            <div className="stat-badge">
              <span className="stat-number">{activeDeliveries.length}</span>
              <span className="stat-label">Active</span>
            </div>
            <div className="stat-badge">
              <span className="stat-number">{deliveries.length}</span>
              <span className="stat-label">Total</span>
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
            className={`filter-tab ${filter === 'all' ? 'active' : ''}`}
            onClick={() => setFilter('all')}
          >
            All
          </button>
          <button
            className={`filter-tab ${filter === 'assigned' ? 'active' : ''}`}
            onClick={() => setFilter('assigned')}
          >
            Assigned
          </button>
          <button
            className={`filter-tab ${filter === 'pickedup' ? 'active' : ''}`}
            onClick={() => setFilter('pickedup')}
          >
            Picked Up
          </button>
          <button
            className={`filter-tab ${filter === 'intransit' ? 'active' : ''}`}
            onClick={() => setFilter('intransit')}
          >
            In Transit
          </button>
          <button
            className={`filter-tab ${filter === 'delivered' ? 'active' : ''}`}
            onClick={() => setFilter('delivered')}
          >
            Delivered
          </button>
        </div>

        {filteredDeliveries.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">📦</div>
            <h2>No Deliveries Found</h2>
            <p>
              {filter === 'all'
                ? "You don't have any assigned deliveries"
                : `No ${filter} deliveries`}
            </p>
          </div>
        ) : (
          <div className="deliveries-list">
            {filteredDeliveries.map((delivery) => (
              <div
                key={delivery.id}
                className="delivery-card"
                onClick={() => navigate(`/deliveries/${delivery.id}`)}
              >
                <div className="delivery-header">
                  <div className="delivery-icon">
                    {getStatusIcon(delivery.status)}
                  </div>
                  <div className="delivery-title">
                    <h3>Order #{delivery.order.orderNumber}</h3>
                    <p className="delivery-time">
                      Assigned {new Date(delivery.assignedAt).toLocaleString()}
                    </p>
                  </div>
                  <span className={`delivery-status ${getStatusColor(delivery.status)}`}>
                    {delivery.status}
                  </span>
                </div>

                <div className="delivery-details">
                  <div className="detail-item">
                    <span className="detail-icon">📍</span>
                    <div className="detail-content">
                      <span className="detail-label">Delivery Address</span>
                      <span className="detail-value">{delivery.deliveryAddress}</span>
                    </div>
                  </div>
                  <div className="detail-item">
                    <span className="detail-icon">👤</span>
                    <div className="detail-content">
                      <span className="detail-label">Customer</span>
                      <span className="detail-value">
                        {delivery.order.contactName} - {delivery.order.contactPhone}
                      </span>
                    </div>
                  </div>
                  {delivery.estimatedDeliveryTime && (
                    <div className="detail-item">
                      <span className="detail-icon">⏱️</span>
                      <div className="detail-content">
                        <span className="detail-label">Estimated Time</span>
                        <span className="detail-value">{delivery.estimatedDeliveryTime}</span>
                      </div>
                    </div>
                  )}
                </div>

                <div className="delivery-footer">
                  <button className="btn btn-sm btn-primary">
                    Manage Delivery →
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

export default MyDeliveries;
