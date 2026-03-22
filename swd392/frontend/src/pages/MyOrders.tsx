import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import { formatVND } from '../utils/currency';
import './MyOrders.css';

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
  items: OrderItem[];
}

const MyOrders = () => {
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const navigate = useNavigate();
  const ITEMS_PER_PAGE = 30;

  useEffect(() => {
    fetchOrders();
  }, []);

  const fetchOrders = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get('/orders/user/orders');
      if (response.data.success) {
        setOrders(response.data.data);
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load orders');
    } finally {
      setLoading(false);
    }
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

  const filteredOrders = orders.filter((order) => {
    const createdDate = new Date(order.createdAt);
    const orderDateOnly = new Date(createdDate.getFullYear(), createdDate.getMonth(), createdDate.getDate());

    if (dateFrom) {
      const fromDate = new Date(dateFrom);
      fromDate.setHours(0, 0, 0, 0);
      if (orderDateOnly < fromDate) {
        return false;
      }
    }

    if (dateTo) {
      const toDate = new Date(dateTo);
      toDate.setHours(0, 0, 0, 0);
      if (orderDateOnly > toDate) {
        return false;
      }
    }

    return true;
  });

  const totalPages = Math.max(1, Math.ceil(filteredOrders.length / ITEMS_PER_PAGE));
  const paginatedOrders = filteredOrders.slice((currentPage - 1) * ITEMS_PER_PAGE, currentPage * ITEMS_PER_PAGE);

  const handleApplyDateFilter = () => {
    if (dateFrom && dateTo && new Date(dateFrom) > new Date(dateTo)) {
      setError('From date cannot be later than To date.');
      return;
    }

    setError('');
    setCurrentPage(1);
  };

  const handleClearDateFilter = () => {
    setDateFrom('');
    setDateTo('');
    setError('');
    setCurrentPage(1);
  };

  const handlePreviousPage = () => {
    setCurrentPage((prev) => Math.max(1, prev - 1));
  };

  const handleNextPage = () => {
    setCurrentPage((prev) => Math.min(totalPages, prev + 1));
  };

  useEffect(() => {
    if (currentPage > totalPages) {
      setCurrentPage(totalPages);
    }
  }, [currentPage, totalPages]);

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
      <div className="orders-page">
        <div className="orders-header">
          <h1>My Orders</h1>
          <button className="btn" onClick={() => navigate('/weekly-menu')}>
            Browse Menu
          </button>
        </div>

        <div className="orders-filters">
          <div className="date-filter-group">
            <label htmlFor="orders-from-date">From:</label>
            <input
              id="orders-from-date"
              type="date"
              value={dateFrom}
              onChange={(e) => setDateFrom(e.target.value)}
            />
          </div>
          <div className="date-filter-group">
            <label htmlFor="orders-to-date">To:</label>
            <input
              id="orders-to-date"
              type="date"
              value={dateTo}
              onChange={(e) => setDateTo(e.target.value)}
            />
          </div>
          <button className="btn btn-primary" onClick={handleApplyDateFilter}>Apply Filter</button>
          <button className="btn btn-secondary" onClick={handleClearDateFilter}>Clear</button>
        </div>

        {orders.length === 0 ? (
          <div className="empty-orders">
            <p>You haven't placed any orders yet</p>
            <button className="btn btn-primary" onClick={() => navigate('/weekly-menu')}>
              Order Now
            </button>
          </div>
        ) : filteredOrders.length === 0 ? (
          <div className="empty-orders">
            <p>No orders found in the selected date range</p>
            <button className="btn btn-secondary" onClick={handleClearDateFilter}>
              Clear Filter
            </button>
          </div>
        ) : (
          <>
            <div className="orders-list">
            {paginatedOrders.map((order) => (
              <div key={order.id} className="order-card" onClick={() => navigate(`/orders/${order.id}`)}>
                <div className="order-header">
                  <div>
                    <h3>Order #{order.orderNumber}</h3>
                    <p className="order-date">
                      {new Date(order.createdAt).toLocaleDateString('en-US', {
                        year: 'numeric',
                        month: 'long',
                        day: 'numeric'
                      })} at {new Date(order.createdAt).toLocaleTimeString('en-US', {
                        hour: '2-digit',
                        minute: '2-digit'
                      })}
                    </p>
                  </div>
                  <span className={getStatusBadgeClass(order.status)}>
                    {order.status}
                  </span>
                </div>
                
                <div className="order-details">
                  <p className="order-items">
                    {order.items.length} item{order.items.length > 1 ? 's' : ''}
                  </p>
                  <p className="order-total">
                    Total: {formatVND(order.totalAmount)}
                  </p>
                </div>
              </div>
            ))}
            </div>
            <div className="pagination-controls">
              <button
                className="btn btn-secondary"
                onClick={handlePreviousPage}
                disabled={currentPage === 1}
              >
                Previous
              </button>
              <span className="pagination-info">Page {currentPage} of {totalPages}</span>
              <button
                className="btn btn-secondary"
                onClick={handleNextPage}
                disabled={currentPage === totalPages}
              >
                Next
              </button>
            </div>
          </>
        )}
      </div>
    </Container>
  );
};

export default MyOrders;
