import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import apiClient from '../config/api';
import { useAuth } from '../hooks/useAuth';
import './AICredits.css';

interface AICreditPackage {
  id: string;
  packageName: string;
  price: number;
  creditAmount: number;
}

interface Transaction {
  id: string;
  packageName: string;
  creditAmount: number;
  price: number;
  createdAt: string;
}

const AICredits = () => {
  const { user, refreshUser } = useAuth();
  const [searchParams] = useSearchParams();
  const [packages, setPackages] = useState<AICreditPackage[]>([]);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [purchasing, setPurchasing] = useState(false);
  const [showSuccess, setShowSuccess] = useState(false);
  const [showError, setShowError] = useState(false);

  useEffect(() => {
    fetchPackages();
    fetchTransactions();
    refreshUser(); // Refresh user data to get updated credits

    // Check payment callback
    const success = searchParams.get('success');
    if (success === 'true') {
      setShowSuccess(true);
      setTimeout(() => setShowSuccess(false), 5000);
    } else if (success === 'false') {
      setShowError(true);
      setTimeout(() => setShowError(false), 5000);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchParams]);

  const fetchPackages = async () => {
    try {
      const response = await apiClient.get('/aicredit/packages');
      if (response.data.success) {
        setPackages(response.data.data);
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load packages');
    } finally {
      setLoading(false);
    }
  };

  const fetchTransactions = async () => {
    try {
      const response = await apiClient.get('/aicredit/transactions');
      if (response.data.success) {
        setTransactions(response.data.data);
      }
    } catch (err: any) {
      console.error('Failed to load transactions:', err);
    }
  };

  const handlePurchase = async (packageId: string) => {
    try {
      setPurchasing(true);
      const response = await apiClient.post('/aicredit/purchase', {
        aIcreditPackageId: packageId,
        paymentMethod: 'VNPay'
      });

      if (response.data.success && response.data.data.paymentUrl) {
        // Redirect to VNPay
        window.location.href = response.data.data.paymentUrl;
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to purchase package');
      setPurchasing(false);
    }
  };

  if (loading) {
    return <div className="container"><div className="loading">Loading...</div></div>;
  }

  return (
    <div className="container">
      <div className="ai-credits-header">
        <h1>AI Credits</h1>
        <div className="current-credits">
          <span className="credits-label">Your Credits:</span>
          <span className="credits-value">{user?.currentCredits || 0}</span>
        </div>
      </div>

      {showSuccess && (
        <div className="alert alert-success">
          Payment successful! Your credits have been added to your account.
        </div>
      )}

      {showError && (
        <div className="alert alert-error">
          Payment failed or was cancelled. Please try again.
        </div>
      )}

      {error && <div className="error-message">{error}</div>}

      <div className="packages-section">
        <h2>Purchase AI Credits</h2>
        <div className="packages-grid">
          {packages.map((pkg) => (
            <div key={pkg.id} className="package-card">
              <h3>{pkg.packageName}</h3>
              <div className="package-credits">
                <span className="credits-amount">{pkg.creditAmount}</span>
                <span className="credits-text">Credits</span>
              </div>
              <div className="package-price">
                {pkg.price.toLocaleString('vi-VN')} VND
              </div>
              <button
                className="btn-primary"
                onClick={() => handlePurchase(pkg.id)}
                disabled={purchasing}
              >
                {purchasing ? 'Processing...' : 'Purchase'}
              </button>
            </div>
          ))}
        </div>
      </div>

      {transactions.length > 0 && (
        <div className="transactions-section">
          <h2>Purchase History</h2>
          <div className="transactions-table">
            <table>
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Package</th>
                  <th>Credits</th>
                  <th>Amount</th>
                </tr>
              </thead>
              <tbody>
                {transactions.map((transaction) => (
                  <tr key={transaction.id}>
                    <td>{new Date(transaction.createdAt).toLocaleDateString()}</td>
                    <td>{transaction.packageName}</td>
                    <td>{transaction.creditAmount}</td>
                    <td>{transaction.price.toLocaleString('vi-VN')} VND</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
};

export default AICredits;
