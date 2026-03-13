import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import { formatVND } from '../utils/currency';
import './StaffOrderList.css';

interface OrderItem {
  ingredientId: string;
  ingredientName: string;
  quantity: number;
  unit: string;
  unitPrice: number;
  totalPrice: number;
}

interface Order {
  id: string;
  orderNumber: string;
  status: string;
  totalAmount: number;
  createdAt: string;
  updatedAt: string;
  items: OrderItem[];
  paymentMethod: string;
}

type FilterPeriod = 'all' | 'today' | 'week' | 'month';
type StatusTab = 'pending' | 'confirmed' | 'canceled' | 'preparing' | 'preparingFailed' | 'prepared' | 'onScheduled';

const StaffOrderList = () => {
  const [orders, setOrders] = useState<Order[]>([]);
  const [filteredOrders, setFilteredOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [filterPeriod, setFilterPeriod] = useState<FilterPeriod>('all');
  const [activeTab, setActiveTab] = useState<StatusTab>('confirmed');
  const [showScheduleModal, setShowScheduleModal] = useState(false);
  const [selectedOrderId, setSelectedOrderId] = useState('');
  const [drivers, setDrivers] = useState<any[]>([]);
  const [filteredDrivers, setFilteredDrivers] = useState<any[]>([]);
  const [selectedDriver, setSelectedDriver] = useState('');
  const [driverSearchQuery, setDriverSearchQuery] = useState('');
  const [showDriverDropdown, setShowDriverDropdown] = useState(false);
  const [deliveryTime, setDeliveryTime] = useState('');
  const [scheduling, setScheduling] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    fetchOrders();
    fetchDrivers();
  }, [activeTab]);

  useEffect(() => {
    applyFilter();
  }, [orders, filterPeriod]);

  useEffect(() => {
    // Filter drivers based on search query
    if (driverSearchQuery.trim() === '') {
      setFilteredDrivers(drivers);
    } else {
      const query = driverSearchQuery.toLowerCase();
      const filtered = drivers.filter(driver => 
        driver.fullName.toLowerCase().includes(query) ||
        driver.email.toLowerCase().includes(query) ||
        (driver.phoneNumber && driver.phoneNumber.includes(query))
      );
      setFilteredDrivers(filtered);
    }
  }, [driverSearchQuery, drivers]);

  const fetchOrders = async () => {
    try {
      setLoading(true);
      setError('');
      
      // Fetch all orders for staff
      const response = await apiClient.get('/orders/all');
      if (response.data.success) {
        setOrders(response.data.data);
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load orders');
    } finally {
      setLoading(false);
    }
  };

  const fetchDrivers = async () => {
    try {
      const response = await apiClient.get('/delivery-schedules/drivers/available');
      if (response.data.success) {
        setDrivers(response.data.data);
        setFilteredDrivers(response.data.data);
      }
    } catch (err) {
      console.error('Failed to load drivers', err);
    }
  };

  const applyFilter = () => {
    let filtered = orders;

    // Filter by status tab
    filtered = filtered.filter(order => {
      const statusLower = order.status.toLowerCase();
      if (activeTab === 'pending') {
        return statusLower.includes('pending');
      } else if (activeTab === 'confirmed') {
        return statusLower.includes('confirmed');
      } else if (activeTab === 'canceled') {
        return statusLower.includes('cancel');
      } else if (activeTab === 'preparing') {
        return statusLower.includes('preparing') && !statusLower.includes('failed');
      } else if (activeTab === 'preparingFailed') {
        return statusLower.includes('preparingfailed');
      } else if (activeTab === 'prepared') {
        return statusLower.includes('prepared') && !statusLower.includes('preparing');
      } else if (activeTab === 'onScheduled') {
        return statusLower.includes('delivering');
      }
      return true;
    });

    // Filter by time period
    if (filterPeriod !== 'all') {
      const now = new Date();
      filtered = filtered.filter(order => {
        const orderDate = new Date(order.createdAt);
        
        if (filterPeriod === 'today') {
          return orderDate.toDateString() === now.toDateString();
        }
        
        if (filterPeriod === 'week') {
          const weekAgo = new Date(now);
          weekAgo.setDate(now.getDate() - 7);
          return orderDate >= weekAgo;
        }
        
        if (filterPeriod === 'month') {
          const monthAgo = new Date(now);
          monthAgo.setMonth(now.getMonth() - 1);
          return orderDate >= monthAgo;
        }
        
        return true;
      });
    }

    setFilteredOrders(filtered);
  };

  const getStatusBadgeClass = (status: string) => {
    const statusLower = status.toLowerCase();
    if (statusLower.includes('pending')) return 'status-badge status-pending';
    if (statusLower.includes('confirmed')) return 'status-badge status-confirmed';
    if (statusLower.includes('preparing')) return 'status-badge status-preparing';
    if (statusLower.includes('ready')) return 'status-badge status-ready';
    if (statusLower.includes('delivered')) return 'status-badge status-delivered';
    if (statusLower.includes('cancelled')) return 'status-badge status-cancelled';
    return 'status-badge';
  };

  const handleConfirmOrder = async (orderId: string) => {
    if (!window.confirm('Confirm this order?')) return;

    try {
      await apiClient.post(`/orders/${orderId}/confirm`);
      await fetchOrders();
      alert('Order confirmed successfully');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to confirm order');
    }
  };

  const handleCancelOrder = async (orderId: string) => {
    if (!window.confirm('Are you sure you want to cancel this order?')) return;

    try {
      await apiClient.post(`/orders/${orderId}/cancel`, { reason: 'Cancelled by staff' });
      await fetchOrders();
      alert('Order cancelled successfully');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to cancel order');
    }
  };

  const handlePrepareOrder = async (orderId: string) => {
    if (!window.confirm('Start preparing this order?')) return;

    try {
      await apiClient.post(`/orders/${orderId}/update-status`, { statusId: 5 });
      await fetchOrders();
      alert('Order status updated to Preparing');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to update order status');
    }
  };

  const handlePreparingSuccess = async (orderId: string) => {
    if (!window.confirm('Mark this order as prepared successfully?')) return;

    try {
      await apiClient.post(`/orders/${orderId}/update-status`, { statusId: 7 });
      await fetchOrders();
      alert('Order marked as Prepared');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to update order status');
    }
  };

  const handlePreparingFailed = async (orderId: string) => {
    if (!window.confirm('Mark this order preparation as failed?')) return;

    try {
      await apiClient.post(`/orders/${orderId}/update-status`, { statusId: 6 });
      await fetchOrders();
      alert('Order marked as Preparing Failed');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to update order status');
    }
  };

  const handleOpenScheduleModal = (orderId: string) => {
    setSelectedOrderId(orderId);
    setShowScheduleModal(true);
    setDriverSearchQuery('');
    setSelectedDriver('');
  };

  const handleScheduleDelivery = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!selectedDriver || !deliveryTime) {
      alert('Please select a driver and delivery time');
      return;
    }

    setScheduling(true);
    try {
      const order = orders.find(o => o.id === selectedOrderId);
      if (!order) return;

      // Create delivery schedule
      await apiClient.post('/delivery-schedules', {
        driverId: selectedDriver,
        orderId: selectedOrderId,
        deliveryTime: new Date(deliveryTime).toISOString(),
        address: order.items[0]?.ingredientName || 'N/A', // You may need to get actual address
        driverContact: drivers.find(d => d.id === selectedDriver)?.phoneNumber || ''
      });

      // Update order status to Delivering (9)
      await apiClient.post(`/orders/${selectedOrderId}/update-status`, { statusId: 9 });
      
      await fetchOrders();
      setShowScheduleModal(false);
      setSelectedDriver('');
      setDeliveryTime('');
      alert('Delivery scheduled successfully');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to schedule delivery');
    } finally {
      setScheduling(false);
    }
  };

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

  if (error) {
    return (
      <Container>
        <div className="error-container">
          <p>{error}</p>
          <button className="btn btn-primary" onClick={fetchOrders}>Try Again</button>
        </div>
      </Container>
    );
  }

  return (
    <Container>
      <div className="staff-orders-page">
        <div className="orders-header">
          <h1>Order List</h1>
        </div>

        {/* Status Tabs */}
        <div className="status-tabs">
          <button 
            className={`tab-btn ${activeTab === 'pending' ? 'active' : ''}`}
            onClick={() => setActiveTab('pending')}
          >
            Pending
            {orders.filter(o => o.status.toLowerCase().includes('pending')).length > 0 && (
              <span className="tab-badge">
                {orders.filter(o => o.status.toLowerCase().includes('pending')).length}
              </span>
            )}
          </button>
          <button 
            className={`tab-btn ${activeTab === 'confirmed' ? 'active' : ''}`}
            onClick={() => setActiveTab('confirmed')}
          >
            Confirmed
            {orders.filter(o => o.status.toLowerCase().includes('confirmed')).length > 0 && (
              <span className="tab-badge">
                {orders.filter(o => o.status.toLowerCase().includes('confirmed')).length}
              </span>
            )}
          </button>
          <button 
            className={`tab-btn ${activeTab === 'preparing' ? 'active' : ''}`}
            onClick={() => setActiveTab('preparing')}
          >
            Preparing
            {orders.filter(o => o.status.toLowerCase().includes('preparing') && !o.status.toLowerCase().includes('failed')).length > 0 && (
              <span className="tab-badge">
                {orders.filter(o => o.status.toLowerCase().includes('preparing') && !o.status.toLowerCase().includes('failed')).length}
              </span>
            )}
          </button>
          <button 
            className={`tab-btn ${activeTab === 'preparingFailed' ? 'active' : ''}`}
            onClick={() => setActiveTab('preparingFailed')}
          >
            Preparing Failed
            {orders.filter(o => o.status.toLowerCase().includes('preparingfailed')).length > 0 && (
              <span className="tab-badge">
                {orders.filter(o => o.status.toLowerCase().includes('preparingfailed')).length}
              </span>
            )}
          </button>
          <button 
            className={`tab-btn ${activeTab === 'prepared' ? 'active' : ''}`}
            onClick={() => setActiveTab('prepared')}
          >
            Prepared
            {orders.filter(o => o.status.toLowerCase().includes('prepared')).length > 0 && (
              <span className="tab-badge">
                {orders.filter(o => o.status.toLowerCase().includes('prepared')).length}
              </span>
            )}
          </button>
          <button 
            className={`tab-btn ${activeTab === 'onScheduled' ? 'active' : ''}`}
            onClick={() => setActiveTab('onScheduled')}
          >
            On Scheduled
            {orders.filter(o => o.status.toLowerCase().includes('delivering')).length > 0 && (
              <span className="tab-badge">
                {orders.filter(o => o.status.toLowerCase().includes('delivering')).length}
              </span>
            )}
          </button>
          <button 
            className={`tab-btn ${activeTab === 'canceled' ? 'active' : ''}`}
            onClick={() => setActiveTab('canceled')}
          >
            Canceled
            {orders.filter(o => o.status.toLowerCase().includes('cancel')).length > 0 && (
              <span className="tab-badge">
                {orders.filter(o => o.status.toLowerCase().includes('cancel')).length}
              </span>
            )}
          </button>
        </div>

        {/* Time Period Filter */}
        <div className="filter-buttons">
          <button 
            className={`filter-btn ${filterPeriod === 'all' ? 'active' : ''}`}
            onClick={() => setFilterPeriod('all')}
          >
            All
          </button>
          <button 
            className={`filter-btn ${filterPeriod === 'today' ? 'active' : ''}`}
            onClick={() => setFilterPeriod('today')}
          >
            Today
          </button>
          <button 
            className={`filter-btn ${filterPeriod === 'week' ? 'active' : ''}`}
            onClick={() => setFilterPeriod('week')}
          >
            This Week
          </button>
          <button 
            className={`filter-btn ${filterPeriod === 'month' ? 'active' : ''}`}
            onClick={() => setFilterPeriod('month')}
          >
            This Month
          </button>
        </div>

        {filteredOrders.length === 0 ? (
          <div className="empty-orders">
            <p>No orders found for the selected period</p>
          </div>
        ) : (
          <div className="orders-table-container">
            <table className="orders-table">
              <thead>
                <tr>
                  <th>Order #</th>
                  <th>Created</th>
                  <th>Updated</th>
                  <th>Items</th>
                  <th>Payment</th>
                  <th>Total</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredOrders.map((order) => (
                  <tr key={order.id}>
                    <td>
                      <button 
                        className="order-number-link"
                        onClick={() => navigate(`/orders/${order.id}`)}
                      >
                        {order.orderNumber}
                      </button>
                    </td>
                    <td>
                      {new Date(order.createdAt).toLocaleDateString('en-US', {
                        month: 'short',
                        day: 'numeric',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit'
                      })}
                    </td>
                    <td>
                      {new Date(order.updatedAt).toLocaleDateString('en-US', {
                        month: 'short',
                        day: 'numeric',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit'
                      })}
                    </td>
                    <td>{order.items.length} items</td>
                    <td>{order.paymentMethod}</td>
                    <td className="amount">{formatVND(order.totalAmount)}</td>
                    <td>
                      <span className={getStatusBadgeClass(order.status)}>
                        {order.status}
                      </span>
                    </td>
                    <td>
                      <div className="action-buttons">
                        {activeTab === 'pending' && (
                          <>
                            <button 
                              className="btn btn-sm btn-success"
                              onClick={() => handleConfirmOrder(order.id)}
                            >
                              Confirm
                            </button>
                            <button 
                              className="btn btn-sm btn-danger"
                              onClick={() => handleCancelOrder(order.id)}
                            >
                              Cancel
                            </button>
                          </>
                        )}
                        {activeTab === 'confirmed' && (
                          <button 
                            className="btn btn-sm btn-primary"
                            onClick={() => handlePrepareOrder(order.id)}
                          >
                            Prepare
                          </button>
                        )}
                        {activeTab === 'preparing' && (
                          <>
                            <button 
                              className="btn btn-sm btn-success"
                              onClick={() => handlePreparingSuccess(order.id)}
                            >
                              Success
                            </button>
                            <button 
                              className="btn btn-sm btn-danger"
                              onClick={() => handlePreparingFailed(order.id)}
                            >
                              Failed
                            </button>
                          </>
                        )}
                        {activeTab === 'prepared' && (
                          <button 
                            className="btn btn-sm btn-primary"
                            onClick={() => handleOpenScheduleModal(order.id)}
                          >
                            Schedule
                          </button>
                        )}
                        <button 
                          className="btn btn-sm btn-secondary"
                          onClick={() => navigate(`/orders/${order.id}`)}
                        >
                          View
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Schedule Delivery Modal */}
        {showScheduleModal && (
          <div className="modal-overlay" onClick={() => {
            setShowScheduleModal(false);
            setDriverSearchQuery('');
            setSelectedDriver('');
            setShowDriverDropdown(false);
          }}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()}>
              <h2>Schedule Delivery</h2>
              <form onSubmit={handleScheduleDelivery}>
                <div className="form-group">
                  <label>Select Driver *</label>
                  <div className="driver-search-container">
                    <input
                      type="text"
                      className="driver-search-input"
                      placeholder="Search driver by name, email, or phone..."
                      value={driverSearchQuery}
                      onChange={(e) => setDriverSearchQuery(e.target.value)}
                      onFocus={() => setShowDriverDropdown(true)}
                      onBlur={() => {
                        // Delay to allow click on dropdown item
                        setTimeout(() => setShowDriverDropdown(false), 200);
                      }}
                      required={!selectedDriver}
                    />
                    {showDriverDropdown && filteredDrivers.length > 0 && (
                      <div className="driver-dropdown">
                        {filteredDrivers.map((driver) => (
                          <div
                            key={driver.id}
                            className={`driver-option ${selectedDriver === driver.id ? 'selected' : ''}`}
                            onMouseDown={(e) => {
                              e.preventDefault(); // Prevent blur event
                              setSelectedDriver(driver.id);
                              setDriverSearchQuery(`${driver.fullName} (${driver.email})`);
                              setShowDriverDropdown(false);
                            }}
                          >
                            <div className="driver-name">{driver.fullName}</div>
                            <div className="driver-details">
                              {driver.email}
                              {driver.phoneNumber && ` • ${driver.phoneNumber}`}
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                    {showDriverDropdown && filteredDrivers.length === 0 && driverSearchQuery && (
                      <div className="driver-dropdown">
                        <div className="driver-option no-results">
                          No drivers found
                        </div>
                      </div>
                    )}
                  </div>
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

                <div className="modal-actions">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={() => {
                      setShowScheduleModal(false);
                      setSelectedDriver('');
                      setDeliveryTime('');
                      setDriverSearchQuery('');
                      setShowDriverDropdown(false);
                    }}
                    disabled={scheduling}
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="btn btn-primary"
                    disabled={scheduling}
                  >
                    {scheduling ? 'Scheduling...' : 'Schedule Delivery'}
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

export default StaffOrderList;
