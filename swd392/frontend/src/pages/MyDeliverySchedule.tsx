import { useState, useEffect } from 'react';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import { formatVND } from '../utils/currency';
import { useAuth } from '../hooks/useAuth';
import './DeliverySchedule.css';

interface DeliverySchedule {
  id: string;
  driverId: string;
  driverName: string;
  driverEmail: string;
  driverContact: string;
  orderId: string;
  orderNumber: string;
  orderTotal: number;
  orderStatus: string;
  address: string;
  customerName: string;
  customerPhone: string;
  deliveryTime: string;
  createdAt: string;
}

const MyDeliverySchedule = () => {
  const { user } = useAuth();
  const [schedules, setSchedules] = useState<DeliverySchedule[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [updatingStatus, setUpdatingStatus] = useState<string | null>(null);

  useEffect(() => {
    if (user?.id) {
      fetchMySchedules();
    }
  }, [user]);

  const fetchMySchedules = async () => {
    if (!user?.id) return;
    
    try {
      setLoading(true);
      setError('');
      
      // Fetch schedules for this driver
      const response = await apiClient.get(`/delivery-schedules/driver/${user.id}`);
      if (response.data.success) {
        setSchedules(response.data.data);
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load delivery schedules');
    } finally {
      setLoading(false);
    }
  };

  const handleStatusChange = async (orderId: string, newStatusId: number) => {
    if (!window.confirm('Are you sure you want to update the delivery status?')) {
      return;
    }

    setUpdatingStatus(orderId);
    try {
      await apiClient.post(`/orders/${orderId}/update-status`, { statusId: newStatusId });
      await fetchMySchedules();
      alert('Delivery status updated successfully');
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to update delivery status');
    } finally {
      setUpdatingStatus(null);
    }
  };

  const getStatusOptions = () => {
    // Return available status transitions
    const options = [
      { value: 9, label: 'Delivering' },
      { value: 10, label: 'Delivery Failed' },
      { value: 11, label: 'Customer Received' },
      { value: 12, label: 'Customer Rejected' }
    ];
    
    return options;
  };

  const getCurrentStatusId = (statusName: string): number => {
    const statusMap: { [key: string]: number } = {
      'Delivering': 9,
      'DeliveryFailed': 10,
      'CustomerReceived': 11,
      'CustomerRejected': 12
    };
    return statusMap[statusName] || 9;
  };

  if (loading) {
    return (
      <Container>
        <div className="loading-container">
          <div className="spinner"></div>
          <p>Loading delivery schedules...</p>
        </div>
      </Container>
    );
  }

  return (
    <Container>
      <div className="delivery-schedule-page">
        <div className="page-header">
          <h1>My Delivery Schedule</h1>
        </div>

        {error && <div className="error-message">{error}</div>}

        {schedules.length === 0 ? (
          <div className="empty-state">
            <p>No delivery schedules assigned to you yet</p>
          </div>
        ) : (
          <div className="schedules-table">
            <table>
              <thead>
                <tr>
                  <th>Order #</th>
                  <th>Status</th>
                  <th>Customer</th>
                  <th>Delivery Time</th>
                  <th>Address</th>
                  <th>Contact</th>
                  <th>Total</th>
                  <th>Update Status</th>
                </tr>
              </thead>
              <tbody>
                {schedules.map((schedule) => (
                  <tr key={schedule.id}>
                    <td>{schedule.orderNumber}</td>
                    <td>
                      <span className={`status-badge status-${schedule.orderStatus.toLowerCase()}`}>
                        {schedule.orderStatus}
                      </span>
                    </td>
                    <td>
                      <div>{schedule.customerName}</div>
                      <div className="text-secondary">{schedule.customerPhone}</div>
                    </td>
                    <td>
                      {new Date(schedule.deliveryTime).toLocaleString('en-US', {
                        month: 'short',
                        day: 'numeric',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit'
                      })}
                    </td>
                    <td className="address-cell">{schedule.address}</td>
                    <td>{schedule.driverContact}</td>
                    <td>{formatVND(schedule.orderTotal)}</td>
                    <td>
                      <select
                        className="status-dropdown"
                        value={getCurrentStatusId(schedule.orderStatus)}
                        onChange={(e) => handleStatusChange(schedule.orderId, parseInt(e.target.value))}
                        disabled={updatingStatus === schedule.orderId}
                      >
                        {getStatusOptions().map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </Container>
  );
};

export default MyDeliverySchedule;
