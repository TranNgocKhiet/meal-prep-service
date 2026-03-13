import { useEffect, useState, useRef } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import Container from '../components/layout/Container';
import apiClient from '../config/api';
import { useAuth } from '../hooks/useAuth';
import './PaymentCallback.css';

const AICreditCallback = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { refreshUser } = useAuth();
  const [status, setStatus] = useState<'processing' | 'success' | 'failed'>('processing');
  const [message, setMessage] = useState('');
  const [creditsAdded, setCreditsAdded] = useState(0);
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

        const response = await apiClient.get('/aicredit/callback?' + searchParams.toString());

        if (response.data.success) {
          setStatus('success');
          setMessage('Payment completed successfully! Your AI credits have been added to your account.');
          // Set credits added from response if available
          if (response.data.data?.creditsAdded) {
            setCreditsAdded(response.data.data.creditsAdded);
          }
          // Refresh user data to get updated credits
          await refreshUser();
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
  }, [searchParams, refreshUser]);

  if (status === 'processing') {
    return (
      <Container>
        <div className="payment-callback-page">
          <div className="payment-status processing">
            <div className="spinner-large"></div>
            <h1>Processing Payment...</h1>
            <p>Please wait while we verify your payment and add credits to your account</p>
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
            <h1>Payment Successful!</h1>
            <p>{message}</p>
            {creditsAdded > 0 && (
              <div className="credits-info">
                <p className="credits-added">+{creditsAdded} AI Credits</p>
              </div>
            )}
            <div className="action-buttons">
              <button
                className="btn btn-primary"
                onClick={() => navigate('/ai-credits')}
              >
                View AI Credits
              </button>
              <button
                className="btn btn-secondary"
                onClick={() => navigate('/')}
              >
                Back to Home
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
            <button
              className="btn btn-primary"
              onClick={() => navigate('/ai-credits')}
            >
              Try Again
            </button>
            <button
              className="btn btn-secondary"
              onClick={() => navigate('/')}
            >
              Back to Home
            </button>
          </div>
        </div>
      </div>
    </Container>
  );
};

export default AICreditCallback;
