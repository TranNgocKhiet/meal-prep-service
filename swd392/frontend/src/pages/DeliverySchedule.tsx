import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import { formatVND } from '../utils/currency';
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

interface Driver {
  id: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
}

interface Order {
  id: string;
  orderNumber: string;
  amount: number;
  address: string;
  phoneNumber: string;
}

const DeliverySchedule = () => {
  const [schedules, setSchedules] = useState<DeliverySchedule[]>([]);
  const [drivers, setDrivers] = useState<Driver[]>([]);
  const [confirmedOrders, setConfirmedOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [creating, setCreating] = useState(false);
  
  // Form state
  const [selectedDriver, setSelectedDriver] = useState('');
  const [selectedOrder, setSelectedOrder] = useState('');
  const [deliveryTime, setDeliveryTime] = useState('');
  const [address, setAddress] = useState('');
  const [driverContact, setDriverContact] = useState('');

  useEffect(() => {
    fetchData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      await Promise.all([
        fetchSchedules(),
        fetchDrivers(),
        fetchConfirmedOrders()
      ]);
    } catch (err) {
      console.error('Error fetching data:', err);
    } finally {
      setLoading(false);
    }
  };

  const fetchSchedules = async () => {
    try {
      const response = await apiClient.get('/delivery-schedules');
      if (response.data.success) {
        setSchedules(response.data.data);
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load delivery schedules');
    }
  };

  const fetchDrivers = async () => {
    try {
      const response = await apiClient.get('/delivery-schedules/drivers/available');
      if (response.data.success) {
        setDrivers(response.data.data);
      }
    } catch (err) {
      console.error('Failed to load drivers', err);
    }
  };

  const fetchConfirmedOrders = async () => {
    try {
      const response = await apiClient.get('/orders/all');
      if (response.data.success) {
        // Filter only prepared orders (StatusId = 7) that don't have a delivery schedule yet
        const allOrders = response.data.data;
        const scheduledOrderIds = schedules.map(s => s.orderId);
        const prepared = allOrders.filter((order: { status: string; id: string }) => 
          order.status === 'Prepared' && 
          !scheduledOrderIds.includes(order.id)
        );
        setConfirmedOrders(prepared);
      }
    } catch (err) {
      console.error('Failed to load orders', err);
    }
  };

  const handleDriverChange = (driverId: string) => {
    setSelectedDriver(driverId);
    const driver = drivers.find(d => d.id === driverId);
    if (driver && driver.phoneNumber) {
      setDriverContact(driver.phoneNumber);
    }
  };

  const handleOrderChange = (orderId: string) => {
    setSelectedOrder(orderId);
    const order = confirmedOrders.find(o => o.id === orderId);
    if (order) {
      setAddress(order.address || '');
    }
  };

  const handleCreateSchedule = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!selectedDriver || !selectedOrder || !deliveryTime || !address || !driverContact) {
      alert('Please fill in all required fields');
      return;
    }

    setCreating(true);
    try {
      const response = await apiClient.post('/delivery-schedules', {
        driverId: selectedDriver,
        orderId: selectedOrder,
        deliveryTime: new Date(deliveryTime).toISOString(),
        address,
        driverContact
      });

      if (response.data.success) {
        alert('Delivery schedule created successfully');
        setShowCreateModal(false);
        resetForm();
        await fetchData();
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to create delivery schedule');
    } finally {
      setCreating(false);
    }
  };

  const handleDeleteSchedule = async (scheduleId: string) => {
    if (!window.confirm('Are you sure you want to delete this delivery schedule?')) {
      return;
    }

    try {
      await apiClient.delete(`/delivery-schedules/${scheduleId}`);
      await fetchData();
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to delete delivery schedule');
    }
  };

  const resetForm = () => {
    setSelectedDriver('');
    setSelectedOrder('');
    setDeliveryTime('');
    setAddress('');
    setDriverContact('');
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
          <h1>Delivery Schedule</h1>
        </div>

        {error && <div className="error-message">{error}</div>}

        {schedules.length === 0 ? (
          <div className="empty-state">
            <p>No delivery schedules yet</p>
          </div>
        ) : (
          <div className="schedules-table">
            <table>
              <thead>
                <tr>
                  <th>Order #</th>
                  <th>Status</th>
                  <th>Customer</th>
                  <th>Driver</th>
                  <th>Delivery Time</th>
                  <th>Address</th>
                  <th>Contact</th>
                  <th>Total</th>
                  <th>Actions</th>
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
                      <div>{schedule.driverName}</div>
                      <div className="text-secondary">{schedule.driverEmail}</div>
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
                      <button
                        className="btn btn-sm btn-danger"
                        onClick={() => handleDeleteSchedule(schedule.id)}
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {showCreateModal && (
          <div className="modal-overlay" onClick={() => setShowCreateModal(false)}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()}>
              <h2>Create Delivery Shift</h2>
              <form onSubmit={handleCreateSchedule}>
                <div className="form-group">
                  <label>Select Driver *</label>
                  <select
                    value={selectedDriver}
                    onChange={(e) => handleDriverChange(e.target.value)}
                    required
                  >
                    <option value="">-- Select Driver --</option>
                    {drivers.map((driver) => (
                      <option key={driver.id} value={driver.id}>
                        {driver.fullName} ({driver.email})
                      </option>
                    ))}
                  </select>
                </div>

                <div className="form-group">
                  <label>Select Order *</label>
                  <select
                    value={selectedOrder}
                    onChange={(e) => handleOrderChange(e.target.value)}
                    required
                  >
                    <option value="">-- Select Order --</option>
                    {confirmedOrders.map((order) => (
                      <option key={order.id} value={order.id}>
                        {order.orderNumber} - {formatVND(order.amount)}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="form-group">
                  <label>Delivery Time *</label>
                  <input
                    type="datetime-local"
                    value={deliveryTime}
                    onChange={(e) => setDeliveryTime(e.target.value)}
                    required
                  />
                </div>

                <div className="form-group">
                  <label>Delivery Address *</label>
                  <textarea
                    value={address}
                    onChange={(e) => setAddress(e.target.value)}
                    rows={3}
                    required
                  />
                </div>

                <div className="form-group">
                  <label>Driver Contact *</label>
                  <input
                    type="tel"
                    value={driverContact}
                    onChange={(e) => setDriverContact(e.target.value)}
                    placeholder="0912345678"
                    required
                  />
                </div>

                <div className="modal-actions">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={() => {
                      setShowCreateModal(false);
                      resetForm();
                    }}
                    disabled={creating}
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="btn btn-primary"
                    disabled={creating}
                  >
                    {creating ? 'Creating...' : 'Create Schedule'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
      </div>
    </Container>
  );
};

export default DeliverySchedule;
