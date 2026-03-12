import { useState, useEffect } from 'react';
import apiClient from '../config/api';
import './RevenueReport.css';

interface RevenueReport {
  id: string;
  month: number;
  year: number;
  totalSubscriptionRev: number;
  totalOrderRev: number;
  totalAiCreditRev: number;
  totalOrdersCount: number;
  createdAt: string;
  updatedAt: string;
}

const RevenueReport = () => {
  const [reports, setReports] = useState<RevenueReport[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [selectedYear, setSelectedYear] = useState(new Date().getFullYear());

  const monthNames = ['January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December'];

  useEffect(() => {
    fetchReports();
  }, [selectedYear]);

  const fetchReports = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get(`/revenuereports?year=${selectedYear}`);
      
      if (response.data.success) {
        setReports(response.data.data.sort((a: RevenueReport, b: RevenueReport) => a.month - b.month));
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load revenue reports');
    } finally {
      setLoading(false);
    }
  };

  const calculateTotals = () => {
    return reports.reduce((acc, report) => ({
      subscription: acc.subscription + report.totalSubscriptionRev,
      order: acc.order + report.totalOrderRev,
      aiCredit: acc.aiCredit + report.totalAiCreditRev,
      orders: acc.orders + report.totalOrdersCount
    }), { subscription: 0, order: 0, aiCredit: 0, orders: 0 });
  };

  const totals = calculateTotals();
  const grandTotal = totals.subscription + totals.order + totals.aiCredit;

  const years = Array.from({ length: 5 }, (_, i) => new Date().getFullYear() - i);

  if (loading) {
    return <div className="container"><div className="loading">Loading...</div></div>;
  }

  return (
    <div className="container">
      <div className="revenue-header">
        <h1>Revenue Report</h1>
        <div className="year-selector">
          <label>Year:</label>
          <select value={selectedYear} onChange={(e) => setSelectedYear(parseInt(e.target.value))}>
            {years.map(year => (
              <option key={year} value={year}>{year}</option>
            ))}
          </select>
        </div>
      </div>

      {error && <div className="error-message">{error}</div>}

      <div className="revenue-summary">
        <div className="summary-card">
          <h3>Total Revenue</h3>
          <p className="summary-value">{grandTotal.toLocaleString()} VND</p>
        </div>
        <div className="summary-card">
          <h3>Subscription Revenue</h3>
          <p className="summary-value">{totals.subscription.toLocaleString()} VND</p>
        </div>
        <div className="summary-card">
          <h3>Order Revenue</h3>
          <p className="summary-value">{totals.order.toLocaleString()} VND</p>
        </div>
        <div className="summary-card">
          <h3>AI Credit Revenue</h3>
          <p className="summary-value">{totals.aiCredit.toLocaleString()} VND</p>
        </div>
        <div className="summary-card">
          <h3>Total Orders</h3>
          <p className="summary-value">{totals.orders}</p>
        </div>
      </div>

      <div className="crud-table-container">
        <table className="crud-table">
          <thead>
            <tr>
              <th>Month</th>
              <th>Subscription Revenue</th>
              <th>Order Revenue</th>
              <th>AI Credit Revenue</th>
              <th>Total Revenue</th>
              <th>Orders Count</th>
            </tr>
          </thead>
          <tbody>
            {reports.length === 0 ? (
              <tr>
                <td colSpan={6} style={{ textAlign: 'center', padding: '2rem', color: '#718096' }}>
                  No revenue data available for {selectedYear}
                </td>
              </tr>
            ) : (
              reports.map((report) => {
                const total = report.totalSubscriptionRev + report.totalOrderRev + report.totalAiCreditRev;
                return (
                  <tr key={report.id}>
                    <td><strong>{monthNames[report.month - 1]}</strong></td>
                    <td>{report.totalSubscriptionRev.toLocaleString()} VND</td>
                    <td>{report.totalOrderRev.toLocaleString()} VND</td>
                    <td>{report.totalAiCreditRev.toLocaleString()} VND</td>
                    <td><strong>{total.toLocaleString()} VND</strong></td>
                    <td>{report.totalOrdersCount}</td>
                  </tr>
                );
              })
            )}
          </tbody>
          {reports.length > 0 && (
            <tfoot>
              <tr style={{ fontWeight: 'bold', background: '#f7fafc' }}>
                <td>TOTAL</td>
                <td>{totals.subscription.toLocaleString()} VND</td>
                <td>{totals.order.toLocaleString()} VND</td>
                <td>{totals.aiCredit.toLocaleString()} VND</td>
                <td>{grandTotal.toLocaleString()} VND</td>
                <td>{totals.orders}</td>
              </tr>
            </tfoot>
          )}
        </table>
      </div>
    </div>
  );
};

export default RevenueReport;
