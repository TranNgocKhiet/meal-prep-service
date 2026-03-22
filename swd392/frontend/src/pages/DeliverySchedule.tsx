import { useState, useEffect } from 'react';
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
  const [allOrders, setAllOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [creating, setCreating] = useState(false);
  
  // Search and filter state
  const [searchQuery, setSearchQuery] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  
  // View/Edit modal state
  const [showViewModal, setShowViewModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [selectedSchedule, setSelectedSchedule] = useState<DeliverySchedule | null>(null);
  const [editingSchedule, setEditingSchedule] = useState<DeliverySchedule | null>(null);
  const [editingDeliveryTime, setEditingDeliveryTime] = useState('');
  const [editingAddress, setEditingAddress] = useState('');
  const [editingDriverContact, setEditingDriverContact] = useState('');
  const [editingDriver, setEditingDriver] = useState('');
  const [updating, setUpdating] = useState(false);
  
  // Form state
  const [selectedDriver, setSelectedDriver] = useState('');
  const [selectedOrder, setSelectedOrder] = useState('');
  const [deliveryTime, setDeliveryTime] = useState('');
  const [address, setAddress] = useState('');
  const [driverContact, setDriverContact] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const ITEMS_PER_PAGE = 30;

  const getCurrentDateTimeLocal = () => {
    const now = new Date();
    // Align to minute precision used by datetime-local inputs.
    now.setSeconds(0, 0);
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  };

  useEffect(() => {
    fetchData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    setCurrentPage(1);
  }, [searchQuery, dateFrom, dateTo]);

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
        // Store all orders for address lookup
        const orders = response.data.data;
        setAllOrders(orders);
        
        // Filter only prepared orders (StatusId = 7) that don't have a delivery schedule yet
        const scheduledOrderIds = schedules.map(s => s.orderId);
        const prepared = orders.filter((order: { status: string; id: string }) => 
          order.status === 'Prepared' && 
          !scheduledOrderIds.includes(order.id)
        );
        setConfirmedOrders(prepared);
      }
    } catch (err) {
      console.error('Failed to load orders', err);
    }
  };

  // Helper function to get real address from order matching
  const getRealAddress = (schedule: DeliverySchedule): string => {
    // Check if address looks like a meal name (Dinner, Breakfast, Lunch, etc.)
    const mealNames = ['dinner', 'breakfast', 'lunch', 'meal', 'snack', 'brunch'];
    const isLikelyMealName = mealNames.some(name => 
      schedule.address.toLowerCase().includes(name)
    );

    if (isLikelyMealName) {
      // Look up order by orderId to get real address
      const order = allOrders.find(o => o.id === schedule.orderId);
      return order?.address || schedule.address;
    }

    return schedule.address;
  };

  // Convert ISO datetime to datetime-local format
  const toDateTimeLocalValue = (isoDate: string): string => {
    const date = new Date(isoDate);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  };

  // Filter schedules based on search and date range
  const getFilteredSchedules = (): DeliverySchedule[] => {
    return schedules.filter(schedule => {
      const lowerSearchQuery = searchQuery.toLowerCase();
      const matchesSearch = !searchQuery || 
        schedule.customerName.toLowerCase().includes(lowerSearchQuery) ||
        schedule.driverName.toLowerCase().includes(lowerSearchQuery) ||
        getRealAddress(schedule).toLowerCase().includes(lowerSearchQuery);

      const scheduleDate = new Date(schedule.deliveryTime).toISOString().split('T')[0];
      const matchesDateFrom = !dateFrom || scheduleDate >= dateFrom;
      const matchesDateTo = !dateTo || scheduleDate <= dateTo;

      return matchesSearch && matchesDateFrom && matchesDateTo;
    });
  };

  const getPaginatedSchedules = (): DeliverySchedule[] => {
    const filtered = getFilteredSchedules();
    const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
    const endIndex = startIndex + ITEMS_PER_PAGE;
    return filtered.slice(startIndex, endIndex);
  };

  const getTotalPages = (): number => {
    return Math.ceil(getFilteredSchedules().length / ITEMS_PER_PAGE);
  };

  const handlePreviousPage = () => {
    setCurrentPage(prev => Math.max(1, prev - 1));
  };

  const handleNextPage = () => {
    const maxPages = getTotalPages();
    setCurrentPage(prev => Math.min(maxPages, prev + 1));
  };

  const handleOpenViewModal = (schedule: DeliverySchedule) => {
    setSelectedSchedule(schedule);
    setShowViewModal(true);
  };

  const handleOpenEditModal = (schedule: DeliverySchedule) => {
    setEditingSchedule(schedule);
    setEditingDriver(schedule.driverId);
    setEditingDeliveryTime(toDateTimeLocalValue(schedule.deliveryTime));
    setEditingAddress(getRealAddress(schedule));
    setEditingDriverContact(schedule.driverContact);
    setShowViewModal(false);
    setShowEditModal(true);
  };

  const handleUpdateSchedule = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!editingSchedule || !editingDriver || !editingDeliveryTime || !editingAddress || !editingDriverContact) {
      alert('Please fill in all required fields');
      return;
    }

    const selectedDeliveryDate = new Date(editingDeliveryTime);
    const minAllowedDate = new Date();
    minAllowedDate.setSeconds(0, 0);

    if (Number.isNaN(selectedDeliveryDate.getTime()) || selectedDeliveryDate < minAllowedDate) {
      alert('Delivery time cannot be in the past');
      return;
    }

    setUpdating(true);
    try {
      const response = await apiClient.put(`/delivery-schedules/${editingSchedule.id}`, {
        driverId: editingDriver,
        deliveryTime: new Date(editingDeliveryTime).toISOString(),
        address: editingAddress,
        driverContact: editingDriverContact
      });

      if (response.data.success) {
        alert('Delivery schedule updated successfully');
        setShowEditModal(false);
        setEditingSchedule(null);
        await fetchData();
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to update delivery schedule');
    } finally {
      setUpdating(false);
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

    const selectedDeliveryDate = new Date(deliveryTime);
    const minAllowedDate = new Date();
    minAllowedDate.setSeconds(0, 0);

    if (Number.isNaN(selectedDeliveryDate.getTime()) || selectedDeliveryDate < minAllowedDate) {
      alert('Delivery time cannot be in the past');
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

        {/* Search and Filter Controls */}
        <div className="schedule-controls">
          <div className="search-box">
            <input
              type="text"
              placeholder="Search by customer name, driver name, or address..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="search-input"
            />
          </div>
          <div className="date-filters">
            <input
              type="date"
              placeholder="From"
              value={dateFrom}
              onChange={(e) => setDateFrom(e.target.value)}
              className="date-input"
            />
            <span className="date-separator">to</span>
            <input
              type="date"
              placeholder="To"
              value={dateTo}
              onChange={(e) => setDateTo(e.target.value)}
              className="date-input"
            />
            {(searchQuery || dateFrom || dateTo) && (
              <button
                className="btn btn-sm btn-secondary"
                onClick={() => {
                  setSearchQuery('');
                  setDateFrom('');
                  setDateTo('');
                }}
              >
                Clear Filters
              </button>
            )}
          </div>
        </div>

        {getFilteredSchedules().length === 0 ? (
          <div className="empty-state">
            <p>{schedules.length === 0 ? 'No delivery schedules yet' : 'No schedules match your filters'}</p>
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
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {getPaginatedSchedules().map((schedule) => (
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
                    <td className="address-cell">{getRealAddress(schedule)}</td>
                    <td>{schedule.driverContact}</td>
                    <td>
                      <div className="action-buttons">
                        <button
                          className="btn btn-sm btn-secondary"
                          onClick={() => handleOpenViewModal(schedule)}
                        >
                          View
                        </button>
                        <button
                          className="btn btn-sm btn-danger"
                          onClick={() => handleDeleteSchedule(schedule.id)}
                        >
                          Delete
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
                disabled={currentPage === 1}
              >
                Previous
              </button>
              <span className="pagination-info">
                Page {currentPage} of {getTotalPages()}
              </span>
              <button
                className="btn btn-sm btn-secondary"
                onClick={handleNextPage}
                disabled={currentPage >= getTotalPages()}
              >
                Next
              </button>
            </div>
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
                    min={getCurrentDateTimeLocal()}
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

        {/* View Modal */}
        {showViewModal && selectedSchedule && (
          <div className="modal-overlay" onClick={() => setShowViewModal(false)}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()}>
              <h2>Delivery Schedule Details</h2>
              <div className="view-details">
                <div className="detail-row">
                  <label>Order Number:</label>
                  <span>{selectedSchedule.orderNumber}</span>
                </div>
                <div className="detail-row">
                  <label>Customer Name:</label>
                  <span>{selectedSchedule.customerName}</span>
                </div>
                <div className="detail-row">
                  <label>Customer Phone:</label>
                  <span>{selectedSchedule.customerPhone}</span>
                </div>
                <div className="detail-row">
                  <label>Driver:</label>
                  <span>{selectedSchedule.driverName}</span>
                </div>
                <div className="detail-row">
                  <label>Driver Email:</label>
                  <span>{selectedSchedule.driverEmail}</span>
                </div>
                <div className="detail-row">
                  <label>Delivery Time:</label>
                  <span>
                    {new Date(selectedSchedule.deliveryTime).toLocaleString('en-US', {
                      month: 'short',
                      day: 'numeric',
                      year: 'numeric',
                      hour: '2-digit',
                      minute: '2-digit'
                    })}
                  </span>
                </div>
                <div className="detail-row">
                  <label>Address:</label>
                  <span>{getRealAddress(selectedSchedule)}</span>
                </div>
                <div className="detail-row">
                  <label>Status:</label>
                  <span>{selectedSchedule.orderStatus}</span>
                </div>
              </div>
              <div className="modal-actions">
                <button
                  className="btn btn-secondary"
                  onClick={() => setShowViewModal(false)}
                >
                  Close
                </button>
                <button
                  className="btn btn-primary"
                  onClick={() => handleOpenEditModal(selectedSchedule)}
                >
                  Edit
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Edit Modal */}
        {showEditModal && editingSchedule && (
          <div className="modal-overlay" onClick={() => setShowEditModal(false)}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()}>
              <h2>Edit Delivery Schedule</h2>
              <form onSubmit={handleUpdateSchedule}>
                <div className="form-group">
                  <label>Order Number (Read-only)</label>
                  <input
                    type="text"
                    value={editingSchedule.orderNumber}
                    disabled
                  />
                </div>

                <div className="form-group">
                  <label>Customer Name (Read-only)</label>
                  <input
                    type="text"
                    value={editingSchedule.customerName}
                    disabled
                  />
                </div>

                <div className="form-group">
                  <label>Select Driver *</label>
                  <select
                    value={editingDriver}
                    onChange={(e) => {
                      setEditingDriver(e.target.value);
                      const driver = drivers.find(d => d.id === e.target.value);
                      if (driver && driver.phoneNumber) {
                        setEditingDriverContact(driver.phoneNumber);
                      }
                    }}
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
                  <label>Delivery Time *</label>
                  <input
                    type="datetime-local"
                    value={editingDeliveryTime}
                    onChange={(e) => setEditingDeliveryTime(e.target.value)}
                    min={getCurrentDateTimeLocal()}
                    required
                  />
                </div>

                <div className="form-group">
                  <label>Delivery Address *</label>
                  <textarea
                    value={editingAddress}
                    onChange={(e) => setEditingAddress(e.target.value)}
                    rows={3}
                    required
                  />
                </div>

                <div className="form-group">
                  <label>Driver Contact *</label>
                  <input
                    type="tel"
                    value={editingDriverContact}
                    onChange={(e) => setEditingDriverContact(e.target.value)}
                    placeholder="0912345678"
                    required
                  />
                </div>

                <div className="modal-actions">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={() => {
                      setShowEditModal(false);
                      setEditingSchedule(null);
                    }}
                    disabled={updating}
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="btn btn-primary"
                    disabled={updating}
                  >
                    {updating ? 'Updating...' : 'Update Schedule'}
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
