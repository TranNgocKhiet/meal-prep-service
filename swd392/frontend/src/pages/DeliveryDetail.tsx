import { useState, useEffect, useRef } from 'react';
import { getErrorMessage } from '../types/errors';
import { useParams, useNavigate } from 'react-router-dom';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import './DeliveryDetail.css';

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
  totalAmount: number;
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

const DeliveryDetail = () => {
  const { deliveryId } = useParams<{ deliveryId: string }>();
  const navigate = useNavigate();
  const [delivery, setDelivery] = useState<Delivery | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [updating, setUpdating] = useState(false);
  const [confirmationType, setConfirmationType] = useState<'Signature' | 'Photo'>('Photo');
  const [confirmationData, setConfirmationData] = useState('');
  const [failureReason, setFailureReason] = useState('');
  const [showConfirmModal, setShowConfirmModal] = useState(false);
  const [showFailModal, setShowFailModal] = useState(false);
  const locationIntervalRef = useRef<number | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (deliveryId) {
      fetchDelivery();
    }

    return () => {
      if (locationIntervalRef.current) {
        clearInterval(locationIntervalRef.current);
      }
    };
  }, [deliveryId]);

  useEffect(() => {
    // Start GPS tracking if delivery is in transit
    if (delivery && delivery.status.toLowerCase() === 'intransit') {
      startLocationTracking();
    } else {
      stopLocationTracking();
    }

    return () => {
      stopLocationTracking();
    };
  }, [delivery?.status]);

  const fetchDelivery = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get(`/delivery/${deliveryId}`);
      if (response.data.success) {
        setDelivery(response.data.data);
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err) || 'Failed to load delivery');
    } finally {
      setLoading(false);
    }
  };

  const startLocationTracking = () => {
    if (locationIntervalRef.current) return;

    // Send location immediately
    sendCurrentLocation();

    // Then send every 30 seconds
    locationIntervalRef.current = setInterval(() => {
      sendCurrentLocation();
    }, 30000);
  };

  const stopLocationTracking = () => {
    if (locationIntervalRef.current) {
      clearInterval(locationIntervalRef.current);
      locationIntervalRef.current = null;
    }
  };

  const sendCurrentLocation = () => {
    if (!navigator.geolocation || !deliveryId) return;

    navigator.geolocation.getCurrentPosition(
      async (position) => {
        try {
          await apiClient.post(`/delivery/${deliveryId}/location`, {
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
          });
        } catch (err) {
          console.error('Failed to send location:', err);
        }
      },
      (error) => {
        console.error('Geolocation error:', error);
      }
    );
  };

  const updateStatus = async (newStatus: string) => {
    try {
      setUpdating(true);
      const response = await apiClient.put(`/delivery/${deliveryId}/status`, {
        status: newStatus,
      });
      if (response.data.success) {
        setDelivery(response.data.data);
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err) || 'Failed to update status');
    } finally {
      setUpdating(false);
    }
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      const reader = new FileReader();
      reader.onloadend = () => {
        setConfirmationData(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const confirmDelivery = async () => {
    if (!confirmationData) {
      setError('Please provide confirmation data');
      return;
    }

    try {
      setUpdating(true);
      const response = await apiClient.post(`/delivery/${deliveryId}/confirm`, {
        confirmationType,
        confirmationData,
      });
      if (response.data.success) {
        setDelivery(response.data.data);
        setShowConfirmModal(false);
        setConfirmationData('');
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err) || 'Failed to confirm delivery');
    } finally {
      setUpdating(false);
    }
  };

  const markAsFailed = async () => {
    if (!failureReason.trim()) {
      setError('Please provide a failure reason');
      return;
    }

    try {
      setUpdating(true);
      const response = await apiClient.post(`/delivery/${deliveryId}/fail`, {
        reason: failureReason,
      });
      if (response.data.success) {
        setDelivery(response.data.data);
        setShowFailModal(false);
        setFailureReason('');
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err) || 'Failed to mark delivery as failed');
    } finally {
      setUpdating(false);
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

  const canUpdateToPickedUp = delivery?.status.toLowerCase() === 'assigned';
  const canUpdateToInTransit = delivery?.status.toLowerCase() === 'pickedup';
  const canConfirmDelivery = ['pickedup', 'intransit'].includes(delivery?.status.toLowerCase() || '');
  const canMarkFailed = ['assigned', 'pickedup', 'intransit'].includes(delivery?.status.toLowerCase() || '');

  if (loading) {
    return (
      <Container>
        <div className="loading-container">
          <div className="spinner"></div>
          <p>Loading delivery...</p>
        </div>
      </Container>
    );
  }

  if (error && !delivery) {
    return (
      <Container>
        <div className="error-container">
          <div className="error-icon">⚠️</div>
          <h2>Unable to Load Delivery</h2>
          <p>{error}</p>
          <button className="btn btn-primary" onClick={() => navigate('/deliveries')}>
            Back to Deliveries
          </button>
        </div>
      </Container>
    );
  }

  if (!delivery) {
    return (
      <Container>
        <div className="empty-state">
          <div className="empty-icon">📦</div>
          <h2>Delivery Not Found</h2>
          <button className="btn btn-primary" onClick={() => navigate('/deliveries')}>
            Back to Deliveries
          </button>
        </div>
      </Container>
    );
  }

  return (
    <Container>
      <div className="delivery-detail-page">
        <div className="page-header">
          <button className="btn-back" onClick={() => navigate('/deliveries')}>
            ← Back to Deliveries
          </button>
          <h1>Delivery Details</h1>
        </div>

        {error && (
          <div className="error-message">
            {error}
            <button className="error-close" onClick={() => setError('')}>×</button>
          </div>
        )}

        <div className="detail-container">
          <div className="detail-main">
            <div className="info-card">
              <div className="card-header">
                <h2>Order #{delivery.order.orderNumber}</h2>
                <span className={`status-badge ${getStatusColor(delivery.status)}`}>
                  {delivery.status}
                </span>
              </div>
              <div className="card-body">
                <div className="info-row">
                  <span className="info-label">Order Amount:</span>
                  <span className="info-value">{delivery.order.totalAmount.toLocaleString()} VND</span>
                </div>
                <div className="info-row">
                  <span className="info-label">Assigned At:</span>
                  <span className="info-value">
                    {new Date(delivery.assignedAt).toLocaleString()}
                  </span>
                </div>
                {delivery.deliveredAt && (
                  <div className="info-row">
                    <span className="info-label">Delivered At:</span>
                    <span className="info-value">
                      {new Date(delivery.deliveredAt).toLocaleString()}
                    </span>
                  </div>
                )}
              </div>
            </div>

            <div className="info-card">
              <div className="card-header">
                <h3>Customer Information</h3>
              </div>
              <div className="card-body">
                <div className="info-row">
                  <span className="info-label">Name:</span>
                  <span className="info-value">{delivery.order.contactName}</span>
                </div>
                <div className="info-row">
                  <span className="info-label">Phone:</span>
                  <span className="info-value">
                    <a href={`tel:${delivery.order.contactPhone}`}>
                      {delivery.order.contactPhone}
                    </a>
                  </span>
                </div>
                <div className="info-row">
                  <span className="info-label">Address:</span>
                  <span className="info-value">{delivery.deliveryAddress}</span>
                </div>
              </div>
            </div>

            {delivery.currentLocation && (
              <div className="info-card">
                <div className="card-header">
                  <h3>Location Tracking</h3>
                </div>
                <div className="card-body">
                  <div className="info-row">
                    <span className="info-label">Last Update:</span>
                    <span className="info-value">
                      {new Date(delivery.currentLocation.timestamp).toLocaleString()}
                    </span>
                  </div>
                  <div className="info-row">
                    <span className="info-label">Coordinates:</span>
                    <span className="info-value">
                      {delivery.currentLocation.latitude.toFixed(6)}, {delivery.currentLocation.longitude.toFixed(6)}
                    </span>
                  </div>
                  {delivery.status.toLowerCase() === 'intransit' && (
                    <div className="tracking-status">
                      <span className="tracking-indicator"></span>
                      <span>GPS tracking active (updates every 30s)</span>
                    </div>
                  )}
                </div>
              </div>
            )}
          </div>

          <div className="detail-actions">
            <div className="actions-card">
              <h3>Update Status</h3>
              
              {canUpdateToPickedUp && (
                <button
                  className="btn btn-primary btn-block"
                  onClick={() => updateStatus('PickedUp')}
                  disabled={updating}
                >
                  {updating ? 'Updating...' : '📦 Mark as Picked Up'}
                </button>
              )}

              {canUpdateToInTransit && (
                <button
                  className="btn btn-primary btn-block"
                  onClick={() => updateStatus('InTransit')}
                  disabled={updating}
                >
                  {updating ? 'Updating...' : '🚚 Start Transit'}
                </button>
              )}

              {canConfirmDelivery && (
                <button
                  className="btn btn-success btn-block"
                  onClick={() => setShowConfirmModal(true)}
                  disabled={updating}
                >
                  ✅ Confirm Delivery
                </button>
              )}

              {canMarkFailed && (
                <button
                  className="btn btn-danger btn-block"
                  onClick={() => setShowFailModal(true)}
                  disabled={updating}
                >
                  ❌ Mark as Failed
                </button>
              )}

              {delivery.status.toLowerCase() === 'delivered' && (
                <div className="success-message">
                  ✅ Delivery completed successfully
                </div>
              )}

              {delivery.status.toLowerCase() === 'failed' && (
                <div className="error-message">
                  ❌ Delivery marked as failed
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Confirm Delivery Modal */}
        {showConfirmModal && (
          <div className="modal-overlay" onClick={() => setShowConfirmModal(false)}>
            <div className="modal" onClick={(e) => e.stopPropagation()}>
              <div className="modal-header">
                <h2>Confirm Delivery</h2>
                <button className="modal-close" onClick={() => setShowConfirmModal(false)}>×</button>
              </div>
              <div className="modal-body">
                <div className="form-group">
                  <label>Confirmation Type</label>
                  <select
                    value={confirmationType}
                    onChange={(e) => setConfirmationType(e.target.value as 'Signature' | 'Photo')}
                    className="form-control"
                  >
                    <option value="Photo">Photo</option>
                    <option value="Signature">Signature</option>
                  </select>
                </div>
                <div className="form-group">
                  <label>Upload {confirmationType}</label>
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept="image/*"
                    capture="environment"
                    onChange={handleFileSelect}
                    className="form-control"
                  />
                  {confirmationData && (
                    <div className="preview">
                      <img src={confirmationData} alt="Preview" />
                    </div>
                  )}
                </div>
              </div>
              <div className="modal-footer">
                <button
                  className="btn btn-secondary"
                  onClick={() => setShowConfirmModal(false)}
                  disabled={updating}
                >
                  Cancel
                </button>
                <button
                  className="btn btn-primary"
                  onClick={confirmDelivery}
                  disabled={updating || !confirmationData}
                >
                  {updating ? 'Confirming...' : 'Confirm Delivery'}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Mark Failed Modal */}
        {showFailModal && (
          <div className="modal-overlay" onClick={() => setShowFailModal(false)}>
            <div className="modal" onClick={(e) => e.stopPropagation()}>
              <div className="modal-header">
                <h2>Mark Delivery as Failed</h2>
                <button className="modal-close" onClick={() => setShowFailModal(false)}>×</button>
              </div>
              <div className="modal-body">
                <div className="form-group">
                  <label>Failure Reason *</label>
                  <textarea
                    value={failureReason}
                    onChange={(e) => setFailureReason(e.target.value)}
                    className="form-control"
                    rows={4}
                    placeholder="Please describe why the delivery failed..."
                  />
                </div>
              </div>
              <div className="modal-footer">
                <button
                  className="btn btn-secondary"
                  onClick={() => setShowFailModal(false)}
                  disabled={updating}
                >
                  Cancel
                </button>
                <button
                  className="btn btn-danger"
                  onClick={markAsFailed}
                  disabled={updating || !failureReason.trim()}
                >
                  {updating ? 'Marking...' : 'Mark as Failed'}
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </Container>
  );
};

export default DeliveryDetail;
