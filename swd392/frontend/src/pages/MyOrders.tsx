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
  const navigate = useNavigate();

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
          <button className="btn btn-primary" onClick={() => navigate('/weekly-menu')}>
            Browse Menu
          </button>
        </div>

        {orders.length === 0 ? (
          <div className="empty-orders">
            <p>You haven't placed any orders yet</p>
            <button className="btn btn-primary" onClick={() => navigate('/weekly-menu')}>
              Order Now
            </button>
          </div>
        ) : (
          <div className="orders-list">
            {orders.map((order) => (
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
        )}
      </div>
    </Container>
  );
};

export default MyOrders;
