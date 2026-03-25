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
  const [successMessage, setSuccessMessage] = useState('');
  const [updatingStatus, setUpdatingStatus] = useState<string | null>(null);
  const [showStatusConfirmModal, setShowStatusConfirmModal] = useState(false);
  const [pendingStatusChange, setPendingStatusChange] = useState<{ orderId: string; statusId: number } | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const ITEMS_PER_PAGE = 30;

  useEffect(() => {
    if (user?.id) {
      fetchMySchedules();
    }
  }, [user]);

  useEffect(() => {
    setCurrentPage(1);
  }, [searchQuery, dateFrom, dateTo]);

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
    setError('');
    setSuccessMessage('');
    setPendingStatusChange({ orderId, statusId: newStatusId });
    setShowStatusConfirmModal(true);
  };

  const confirmStatusChange = async () => {
    if (!pendingStatusChange) {
      return;
    }

    setUpdatingStatus(pendingStatusChange.orderId);
    try {
      await apiClient.post(`/orders/${pendingStatusChange.orderId}/update-status`, { statusId: pendingStatusChange.statusId });
      await fetchMySchedules();
      setSuccessMessage('Delivery status updated successfully');
      setShowStatusConfirmModal(false);
      setPendingStatusChange(null);
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to update delivery status');
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

  const getFilteredSchedules = (): DeliverySchedule[] => {
    return schedules.filter(schedule => {
      const lowerSearchQuery = searchQuery.toLowerCase();
      const matchesSearch = !searchQuery || 
        schedule.customerName.toLowerCase().includes(lowerSearchQuery) ||
        schedule.address.toLowerCase().includes(lowerSearchQuery);

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
        {successMessage && <div className="success-message">{successMessage}</div>}

        {/* Search and Filter Controls */}
        <div className="schedule-controls">
          <div className="search-box">
            <input
              type="text"
              placeholder="Search by customer name or address..."
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
            <p>{schedules.length === 0 ? 'No delivery schedules assigned to you yet' : 'No schedules match your filters'}</p>
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

        {showStatusConfirmModal && (
          <div className="modal-overlay" onClick={() => {
            if (updatingStatus) return;
            setShowStatusConfirmModal(false);
            setPendingStatusChange(null);
          }}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()}>
              <h2>Confirm Status Update</h2>
              <p className="confirm-update-message">Are you sure you want to update the delivery status?</p>
              <div className="modal-actions">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => {
                    setShowStatusConfirmModal(false);
                    setPendingStatusChange(null);
                  }}
                  disabled={!!updatingStatus}
                >
                  Cancel
                </button>
                <button
                  type="button"
                  className="btn"
                  onClick={confirmStatusChange}
                  disabled={!!updatingStatus}
                >
                  {updatingStatus ? 'Updating...' : 'Confirm'}
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </Container>
  );
};

export default MyDeliverySchedule;
