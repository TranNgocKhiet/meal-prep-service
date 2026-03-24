import { useEffect, useState, useRef } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import './PaymentCallback.css';

const PaymentCallback = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [status, setStatus] = useState<'processing' | 'success' | 'failed'>('processing');
  const [message, setMessage] = useState('');
  const [orderId, setOrderId] = useState<string | null>(null);
  const hasProcessed = useRef(false);

  useEffect(() => {
    if (hasProcessed.current) return;
    hasProcessed.current = true;

    const processPaymentCallback = async () => {
      try {
        // Convert URLSearchParams to object
        const params: Record<string, string> = {};
        searchParams.forEach((value, key) => {
          params[key] = value;
        });

        const response = await apiClient.get('/payments/callback?' + searchParams.toString());

        if (response.data.success) {
          const result = response.data.data;
          setOrderId(result.orderId);
          
          if (result.success) {
            setStatus('success');
            setMessage('Payment completed successfully!');
          } else {
            setStatus('failed');
            setMessage(result.message || 'Payment failed. Please try again.');
          }
        } else {
          setStatus('failed');
          setMessage('Payment verification failed');
        }
      } catch (err: unknown) {
        const error = err as { response?: { data?: { message?: string } } };
        setStatus('failed');
        setMessage(error.response?.data?.message || 'An error occurred while processing your payment');
      }
    };

    processPaymentCallback();
  }, [searchParams]);

  if (status === 'processing') {
    return (
      <Container>
        <div className="payment-callback-page">
          <div className="payment-status processing">
            <div className="spinner-large"></div>
            <h1>Processing Payment...</h1>
            <p>Please wait while we verify your payment</p>
          </div>
        </div>
      </Container>
    );
  }

  if (status === 'success') {
    return (
      <Container>
        <div className="payment-callback-page">
          <div className="payment-status success">
            <div className="status-icon success-icon">✓</div>
            <h1 style={{color: '#000000'}}>Payment Successful!</h1>
            <p>{message}</p>
            <div className="action-buttons">
              <button
                className="btn btn-primary"
                onClick={() => navigate('/my-orders')}
              >
                View My Orders
              </button>
              <button
                className="btn btn-secondary"
                onClick={() => navigate('/cart')}
              >
                Back to Cart
              </button>
            </div>
          </div>
        </div>
      </Container>
    );
  }

  return (
    <Container>
      <div className="payment-callback-page">
        <div className="payment-status failed">
          <div className="status-icon failed-icon">✕</div>
          <h1>Payment Failed</h1>
          <p>{message}</p>
          <div className="action-buttons">
            {orderId && (
              <button
                className="btn btn-primary"
                onClick={() => navigate('/my-orders')}
              >
                View My Orders
              </button>
            )}
            <button
              className="btn btn-secondary"
              onClick={() => navigate('/cart')}
            >
              Back to Cart
            </button>
          </div>
        </div>
      </div>
    </Container>
  );
};

export default PaymentCallback;
