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
  address?: string;
}

interface Driver {
  id: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
}

interface DeliveryScheduleInfo {
  id: string;
  orderId: string;
  driverId: string;
  driverName: string;
  deliveryTime: string;
  address: string;
  driverContact: string;
}

interface StatusConfirmAction {
  title: string;
  message: string;
  execute: () => Promise<void>;
}

interface ActionFeedback {
  type: 'success' | 'error';
  message: string;
}

type FilterPeriod = 'all' | 'today' | 'week' | 'month';
type StatusTab = 'pending' | 'confirmed' | 'canceled' | 'preparing' | 'preparingFailed' | 'prepared' | 'onScheduled';

const ORDER_STATUS_IDS = {
  pending: 1,
  cancelled: 2,
  confirmed: 3,
  preparing: 5,
  preparingFailed: 6,
  prepared: 7,
} as const;

const StaffOrderList = () => {
  const [orders, setOrders] = useState<Order[]>([]);
  const [filteredOrders, setFilteredOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [filterPeriod, setFilterPeriod] = useState<FilterPeriod>('all');
  const [activeTab, setActiveTab] = useState<StatusTab>('confirmed');
  const [tabPages, setTabPages] = useState<Record<StatusTab, number>>({
    pending: 1,
    confirmed: 1,
    canceled: 1,
    preparing: 1,
    preparingFailed: 1,
    prepared: 1,
    onScheduled: 1
  });
  const ITEMS_PER_PAGE = 30;
  const [showScheduleModal, setShowScheduleModal] = useState(false);
  const [scheduleModalMode, setScheduleModalMode] = useState<'create' | 'edit'>('create');
  const [selectedOrderId, setSelectedOrderId] = useState('');
  const [selectedScheduleId, setSelectedScheduleId] = useState('');
  const [drivers, setDrivers] = useState<Driver[]>([]);
  const [filteredDrivers, setFilteredDrivers] = useState<Driver[]>([]);
  const [deliverySchedules, setDeliverySchedules] = useState<DeliveryScheduleInfo[]>([]);
  const [selectedDriver, setSelectedDriver] = useState('');
  const [driverSearchQuery, setDriverSearchQuery] = useState('');
  const [showDriverDropdown, setShowDriverDropdown] = useState(false);
  const [deliveryTime, setDeliveryTime] = useState('');
  const [scheduling, setScheduling] = useState(false);
  const [actionFeedback, setActionFeedback] = useState<ActionFeedback | null>(null);
  const [statusConfirmAction, setStatusConfirmAction] = useState<StatusConfirmAction | null>(null);
  const [confirmingStatusAction, setConfirmingStatusAction] = useState(false);
  const [showScheduleSaveConfirm, setShowScheduleSaveConfirm] = useState(false);
  const [confirmingScheduleSave, setConfirmingScheduleSave] = useState(false);
  const navigate = useNavigate();

  const getPaginatedOrders = (): Order[] => {
    const currentPage = tabPages[activeTab];
    const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
    const endIndex = startIndex + ITEMS_PER_PAGE;
    return filteredOrders.slice(startIndex, endIndex);
  };

  const getTotalPages = (): number => {
    return Math.ceil(filteredOrders.length / ITEMS_PER_PAGE);
  };

  const handlePreviousPage = () => {
    setTabPages(prev => ({
      ...prev,
      [activeTab]: Math.max(1, prev[activeTab] - 1)
    }));
  };

  const handleNextPage = () => {
    const maxPages = getTotalPages();
    setTabPages(prev => ({
      ...prev,
      [activeTab]: Math.min(maxPages, prev[activeTab] + 1)
    }));
  };

  const getCurrentDateTimeLocal = () => {
    const now = new Date();
    // Match datetime-local minute precision.
    now.setSeconds(0, 0);
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  };

  useEffect(() => {
    fetchOrders();
    fetchDrivers();
    fetchDeliverySchedules();
    setTabPages(prev => ({ ...prev, [activeTab]: 1 }));
    // eslint-disable-next-line react-hooks/exhaustive-deps
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

  const fetchDeliverySchedules = async () => {
    try {
      const response = await apiClient.get('/delivery-schedules');
      if (response.data.success) {
        setDeliverySchedules(response.data.data);
      }
    } catch (err) {
      console.error('Failed to load delivery schedules', err);
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
    try {
      await apiClient.post(`/orders/${orderId}/confirm`);
      await fetchOrders();
      setActionFeedback({ type: 'success', message: 'Order confirmed successfully.' });
    } catch (err: any) {
      setActionFeedback({ type: 'error', message: err.response?.data?.message || 'Failed to confirm order.' });
    }
  };

  const handleCancelOrder = async (orderId: string) => {
    try {
      await apiClient.post(`/orders/${orderId}/cancel`, { reason: 'Cancelled by staff' });
      await fetchOrders();
      setActionFeedback({ type: 'success', message: 'Order cancelled successfully.' });
    } catch (err: any) {
      setActionFeedback({ type: 'error', message: err.response?.data?.message || 'Failed to cancel order.' });
    }
  };

  const isCurrentStatus = (orderStatus: string, targetStatus: 'pending' | 'confirmed' | 'cancelled') => {
    const normalized = orderStatus.toLowerCase();

    if (targetStatus === 'cancelled') {
      return normalized.includes('cancel');
    }

    return normalized.includes(targetStatus);
  };

  const handleManualStatusChange = async (orderId: string, statusId: number, statusLabel: string) => {
    try {
      await apiClient.post(`/orders/${orderId}/update-status`, { statusId });
      await fetchOrders();
      setActionFeedback({ type: 'success', message: `Order status updated to ${statusLabel}.` });
    } catch (err: any) {
      setActionFeedback({ type: 'error', message: err.response?.data?.message || 'Failed to update order status.' });
    }
  };

  const handlePreparingSuccess = async (orderId: string) => {
    try {
      await apiClient.post(`/orders/${orderId}/update-status`, { statusId: 7 });
      await fetchOrders();
      setActionFeedback({ type: 'success', message: 'Order marked as Prepared.' });
    } catch (err: any) {
      setActionFeedback({ type: 'error', message: err.response?.data?.message || 'Failed to update order status.' });
    }
  };

  const handlePreparingFailed = async (orderId: string) => {
    try {
      await apiClient.post(`/orders/${orderId}/update-status`, { statusId: 6 });
      await fetchOrders();
      setActionFeedback({ type: 'success', message: 'Order marked as Preparing Failed.' });
    } catch (err: any) {
      setActionFeedback({ type: 'error', message: err.response?.data?.message || 'Failed to update order status.' });
    }
  };

  const handleOpenScheduleModal = (orderId: string) => {
    setScheduleModalMode('create');
    setSelectedOrderId(orderId);
    setSelectedScheduleId('');
    setShowScheduleModal(true);
    setDriverSearchQuery('');
    setSelectedDriver('');
    setDeliveryTime('');
  };

  const toDateTimeLocalValue = (value: string) => {
    const match = value.match(/^(\d{4}-\d{2}-\d{2})T(\d{2}:\d{2})/);
    if (match) return `${match[1]}T${match[2]}`;

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  };

  const toApiDateTimeValue = (dateTimeLocal: string) => `${dateTimeLocal}:00`;

  const handleOpenEditScheduleModal = (orderId: string) => {
    const schedule = deliverySchedules.find((s) => s.orderId === orderId);
    if (!schedule) {
      setActionFeedback({ type: 'error', message: 'Delivery schedule not found for this order.' });
      return;
    }

    setScheduleModalMode('edit');
    setSelectedOrderId(orderId);
    setSelectedScheduleId(schedule.id);
    setSelectedDriver(schedule.driverId);
    setDriverSearchQuery(schedule.driverName);
    setDeliveryTime(toDateTimeLocalValue(schedule.deliveryTime));
    setShowScheduleModal(true);
    setShowDriverDropdown(false);
  };

  const openStatusConfirm = (message: string, execute: () => Promise<void>, title = 'Confirm Status Change') => {
    setStatusConfirmAction({ title, message, execute });
  };

  const handleConfirmStatusAction = async () => {
    if (!statusConfirmAction) return;

    setConfirmingStatusAction(true);
    try {
      await statusConfirmAction.execute();
    } finally {
      setConfirmingStatusAction(false);
      setStatusConfirmAction(null);
    }
  };

  const handleScheduleDelivery = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!selectedDriver || !deliveryTime) {
      setActionFeedback({ type: 'error', message: 'Please select a driver and delivery time.' });
      return;
    }

    const selectedDeliveryDate = new Date(deliveryTime);
    const minAllowedDate = new Date();
    minAllowedDate.setSeconds(0, 0);

    if (Number.isNaN(selectedDeliveryDate.getTime()) || selectedDeliveryDate < minAllowedDate) {
      setActionFeedback({ type: 'error', message: 'Delivery time cannot be in the past.' });
      return;
    }

    if (scheduleModalMode === 'edit') {
      setShowScheduleSaveConfirm(true);
      return;
    }

    await executeScheduleSave();
  };

  const executeScheduleSave = async () => {

    setScheduling(true);
    try {
      const selectedDriverInfo = drivers.find(d => d.id === selectedDriver);
      const schedulePayload = {
        driverId: selectedDriver,
        deliveryTime: toApiDateTimeValue(deliveryTime),
        address: deliverySchedules.find(s => s.id === selectedScheduleId)?.address,
        driverContact: selectedDriverInfo?.phoneNumber || deliverySchedules.find(s => s.id === selectedScheduleId)?.driverContact || ''
      };

      if (scheduleModalMode === 'edit' && selectedScheduleId) {
        await apiClient.put(`/delivery-schedules/${selectedScheduleId}`, schedulePayload);
      } else {
        const order = orders.find(o => o.id === selectedOrderId);
        if (!order) {
          setActionFeedback({ type: 'error', message: 'Order not found for scheduling.' });
          return;
        }

        await apiClient.post('/delivery-schedules', {
          ...schedulePayload,
          orderId: selectedOrderId,
          address: order.address || 'N/A'
        });

        // Update order status to Delivering (9)
        await apiClient.post(`/orders/${selectedOrderId}/update-status`, { statusId: 9 });
      }

      await fetchOrders();
      await fetchDeliverySchedules();
      setShowScheduleModal(false);
      setShowScheduleSaveConfirm(false);
      setSelectedScheduleId('');
      setSelectedDriver('');
      setDeliveryTime('');
      setDriverSearchQuery('');
      setActionFeedback({
        type: 'success',
        message: scheduleModalMode === 'edit' ? 'Delivery schedule updated successfully.' : 'Delivery scheduled successfully.'
      });
    } catch (err: any) {
      setActionFeedback({ type: 'error', message: err.response?.data?.message || 'Failed to save delivery schedule.' });
    } finally {
      setScheduling(false);
    }
  };

  const handleConfirmScheduleSave = async () => {
    if (confirmingScheduleSave) return;

    setConfirmingScheduleSave(true);
    try {
      await executeScheduleSave();
    } finally {
      setConfirmingScheduleSave(false);
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

        {actionFeedback && (
          <div className={`action-feedback ${actionFeedback.type}`}>
            <span>{actionFeedback.message}</span>
            <button
              type="button"
              className="action-feedback-close"
              onClick={() => setActionFeedback(null)}
              aria-label="Dismiss message"
            >
              ×
            </button>
          </div>
        )}

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
                {getPaginatedOrders().map((order) => (
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
                              onClick={() => openStatusConfirm('Confirm this order?', () => handleConfirmOrder(order.id), 'Confirm Order')}
                            >
                              Confirm
                            </button>
                            <button 
                              className="btn btn-sm btn-danger"
                              onClick={() => openStatusConfirm('Are you sure you want to cancel this order?', () => handleCancelOrder(order.id), 'Cancel Order')}
                            >
                              Cancel
                            </button>
                          </>
                        )}
                        {activeTab === 'confirmed' && (
                          <>
                            <button
                              className="btn btn-sm btn-secondary"
                              onClick={() => openStatusConfirm('Change order status to Pending?', () => handleManualStatusChange(order.id, ORDER_STATUS_IDS.pending, 'Pending'))}
                              disabled={isCurrentStatus(order.status, 'pending')}
                            >
                              Back to Pending
                            </button>
                            <button
                              className="btn btn-sm btn-danger"
                              onClick={() => openStatusConfirm('Change order status to Cancelled?', () => handleManualStatusChange(order.id, ORDER_STATUS_IDS.cancelled, 'Cancelled'))}
                              disabled={isCurrentStatus(order.status, 'cancelled')}
                            >
                              Cancelled
                            </button>
                          </>
                        )}
                        {activeTab === 'confirmed' && (
                          <button
                            className="btn btn-sm btn-primary"
                            onClick={() => openStatusConfirm('Change order status to Preparing?', () => handleManualStatusChange(order.id, ORDER_STATUS_IDS.preparing, 'Preparing'))}
                          >
                            Preparing
                          </button>
                        )}
                        {activeTab === 'canceled' && (
                          <>
                            <button
                              className="btn btn-sm btn-secondary"
                              onClick={() => openStatusConfirm('Change order status to Pending?', () => handleManualStatusChange(order.id, ORDER_STATUS_IDS.pending, 'Pending'))}
                              disabled={isCurrentStatus(order.status, 'pending')}
                            >
                              Back to Pending
                            </button>
                            <button
                              className="btn btn-sm btn-success"
                              onClick={() => openStatusConfirm('Change order status to Confirmed?', () => handleManualStatusChange(order.id, ORDER_STATUS_IDS.confirmed, 'Confirmed'))}
                              disabled={isCurrentStatus(order.status, 'confirmed')}
                            >
                              Confirmed
                            </button>
                          </>
                        )}
                        {activeTab === 'preparing' && (
                          <>
                            <button
                              className="btn btn-sm btn-secondary"
                              onClick={() => openStatusConfirm('Change order status to Confirmed?', () => handleManualStatusChange(order.id, ORDER_STATUS_IDS.confirmed, 'Confirmed'))}
                            >
                              Back to Confirmed
                            </button>
                            <button 
                              className="btn btn-sm btn-success"
                              onClick={() => openStatusConfirm('Mark this order as prepared successfully?', () => handlePreparingSuccess(order.id), 'Mark as Prepared')}
                            >
                              Success
                            </button>
                            <button 
                              className="btn btn-sm btn-danger"
                              onClick={() => openStatusConfirm('Mark this order preparation as failed?', () => handlePreparingFailed(order.id), 'Mark as Failed')}
                            >
                              Failed
                            </button>
                          </>
                        )}
                        {activeTab === 'prepared' && (
                          <>
                            <button
                              className="btn btn-sm btn-secondary"
                              onClick={() => openStatusConfirm('Change order status to Preparing?', () => handleManualStatusChange(order.id, ORDER_STATUS_IDS.preparing, 'Preparing'))}
                            >
                              Back to Preparing
                            </button>
                            <button
                              className="btn btn-sm btn-danger"
                              onClick={() => openStatusConfirm('Change order status to Preparing Failed?', () => handleManualStatusChange(order.id, ORDER_STATUS_IDS.preparingFailed, 'Preparing Failed'))}
                            >
                              Preparing Failed
                            </button>
                            <button 
                              className="btn btn-sm btn-primary"
                              onClick={() => handleOpenScheduleModal(order.id)}
                            >
                              Schedule
                            </button>
                          </>
                        )}
                        {activeTab === 'preparingFailed' && (
                          <>
                            <button
                              className="btn btn-sm btn-secondary"
                              onClick={() => openStatusConfirm('Change order status to Preparing?', () => handleManualStatusChange(order.id, ORDER_STATUS_IDS.preparing, 'Preparing'))}
                            >
                              Back to Preparing
                            </button>
                            <button
                              className="btn btn-sm btn-success"
                              onClick={() => openStatusConfirm('Change order status to Prepared?', () => handleManualStatusChange(order.id, ORDER_STATUS_IDS.prepared, 'Prepared'))}
                            >
                              Prepared
                            </button>
                          </>
                        )}
                        {activeTab === 'onScheduled' && (
                          <button
                            className="btn btn-sm btn-primary"
                            onClick={() => handleOpenEditScheduleModal(order.id)}
                          >
                            Edit Schedule
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
            
            {/* Pagination Controls */}
            <div className="pagination-controls">
              <button
                className="btn btn-sm btn-secondary"
                onClick={handlePreviousPage}
                disabled={tabPages[activeTab] === 1}
              >
                Previous
              </button>
              <span className="pagination-info">
                Page {tabPages[activeTab]} of {getTotalPages()}
              </span>
              <button
                className="btn btn-sm btn-secondary"
                onClick={handleNextPage}
                disabled={tabPages[activeTab] >= getTotalPages()}
              >
                Next
              </button>
            </div>
          </div>
        )}

        {statusConfirmAction && (
          <div
            className="status-confirm-overlay"
            onClick={() => {
              if (!confirmingStatusAction) {
                setStatusConfirmAction(null);
              }
            }}
          >
            <div className="status-confirm-modal" onClick={(e) => e.stopPropagation()}>
              <h3>{statusConfirmAction.title}</h3>
              <p>{statusConfirmAction.message}</p>
              <div className="status-confirm-actions">
                <button
                  className="btn btn-secondary"
                  onClick={() => setStatusConfirmAction(null)}
                  disabled={confirmingStatusAction}
                >
                  Cancel
                </button>
                <button
                  className="btn btn-primary"
                  onClick={handleConfirmStatusAction}
                  disabled={confirmingStatusAction}
                >
                  {confirmingStatusAction ? 'Processing...' : 'Confirm'}
                </button>
              </div>
            </div>
          </div>
        )}

        {showScheduleSaveConfirm && (
          <div
            className="status-confirm-overlay"
            onClick={() => {
              if (!confirmingScheduleSave) {
                setShowScheduleSaveConfirm(false);
              }
            }}
          >
            <div className="status-confirm-modal" onClick={(e) => e.stopPropagation()}>
              <h3>Confirm Schedule Update</h3>
              <p>Are you sure you want to save changes to this delivery schedule?</p>
              <div className="status-confirm-actions">
                <button
                  className="btn btn-secondary"
                  onClick={() => setShowScheduleSaveConfirm(false)}
                  disabled={confirmingScheduleSave}
                >
                  Cancel
                </button>
                <button
                  className="btn"
                  onClick={handleConfirmScheduleSave}
                  disabled={confirmingScheduleSave}
                >
                  {confirmingScheduleSave ? 'Saving...' : 'Confirm'}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Schedule Delivery Modal */}
        {showScheduleModal && (
          <div className="modal-overlay" onClick={() => {
            setShowScheduleModal(false);
            setShowScheduleSaveConfirm(false);
            setScheduleModalMode('create');
            setSelectedScheduleId('');
            setDriverSearchQuery('');
            setSelectedDriver('');
            setShowDriverDropdown(false);
          }}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()}>
              <h2>{scheduleModalMode === 'edit' ? 'Update Delivery Schedule' : 'Schedule Delivery'}</h2>
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
                    min={getCurrentDateTimeLocal()}
                    required
                  />
                </div>

                <div className="modal-actions">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={() => {
                      setShowScheduleModal(false);
                      setShowScheduleSaveConfirm(false);
                      setScheduleModalMode('create');
                      setSelectedScheduleId('');
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
                    className="btn"
                    disabled={scheduling}
                  >
                    {scheduling ? 'Saving...' : scheduleModalMode === 'edit' ? 'Save Changes' : 'Schedule Delivery'}
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
