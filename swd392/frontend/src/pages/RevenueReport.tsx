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
  const [filterMonth, setFilterMonth] = useState('');
  const [filterYear, setFilterYear] = useState(new Date().getFullYear().toString());
  const [showCalculateModal, setShowCalculateModal] = useState(false);
  const [calculateMonth, setCalculateMonth] = useState(new Date().getMonth() + 1);
  const [calculateYear, setCalculateYear] = useState(new Date().getFullYear());
  const [calculating, setCalculating] = useState(false);

  const monthNames = ['January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December'];

  useEffect(() => {
    fetchReports(undefined, new Date().getFullYear());
  }, []);

  const fetchReports = async (month?: number, year?: number) => {
    try {
      setLoading(true);
      setError('');

      const params = new URLSearchParams();
      if (typeof year === 'number') {
        params.append('year', year.toString());
      }
      if (typeof month === 'number') {
        params.append('month', month.toString());
      }

      const queryString = params.toString();
      const response = await apiClient.get(`/revenuereports${queryString ? `?${queryString}` : ''}`);
      
      if (response.data.success) {
        setReports(response.data.data.sort((a: RevenueReport, b: RevenueReport) => a.month - b.month));
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load revenue reports');
    } finally {
      setLoading(false);
    }
  };

  const handleApplyFilter = async () => {
    const parsedYear = parseInt(filterYear, 10);
    const parsedMonth = filterMonth ? parseInt(filterMonth, 10) : undefined;

    if (Number.isNaN(parsedYear) || parsedYear < 2000 || parsedYear > 2100) {
      setError('Please enter a valid year between 2000 and 2100.');
      return;
    }

    if (parsedMonth !== undefined && (Number.isNaN(parsedMonth) || parsedMonth < 1 || parsedMonth > 12)) {
      setError('Please enter a valid month from 1 to 12.');
      return;
    }

    await fetchReports(parsedMonth, parsedYear);
  };

  const handleClearFilter = async () => {
    const currentYear = new Date().getFullYear();
    setFilterMonth('');
    setFilterYear(currentYear.toString());
    await fetchReports(undefined, currentYear);
  };

  const handleCalculateRevenue = async () => {
    try {
      setCalculating(true);
      setError('');
      
      const response = await apiClient.post(`/revenuereports/calculate?month=${calculateMonth}&year=${calculateYear}`);
      
      if (response.data.success) {
        setShowCalculateModal(false);
        const parsedYear = parseInt(filterYear, 10);
        const parsedMonth = filterMonth ? parseInt(filterMonth, 10) : undefined;
        if (!Number.isNaN(parsedYear)) {
          await fetchReports(parsedMonth, parsedYear);
        }
        alert('Revenue calculated and saved successfully!');
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to calculate revenue');
    } finally {
      setCalculating(false);
    }
  };

  const handleDeleteReport = async (id: string, month: number, year: number) => {
    if (!window.confirm(`Are you sure you want to delete the revenue report for ${monthNames[month - 1]} ${year}?`)) {
      return;
    }

    try {
      const response = await apiClient.delete(`/revenuereports/${id}`);
      
      if (response.data.success) {
        await fetchReports();
        alert('Revenue report deleted successfully!');
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to delete revenue report');
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

  if (loading) {
    return <div className="container"><div className="loading">Loading...</div></div>;
  }

  return (
    <div className="container">
      <div className="revenue-header">
        <h1 style={{ color: '#fff' }}>Revenue Report</h1>
        <div className="header-controls">
          <div className="filter-group">
            <label>Month:</label>
            <select
              value={filterMonth}
              onChange={(e) => setFilterMonth(e.target.value)}
              className="filter-input"
            >
              <option value="">All</option>
              {monthNames.map((name, index) => (
                <option key={index + 1} value={index + 1}>{name}</option>
              ))}
            </select>
          </div>
          <div className="filter-group">
            <label>Year:</label>
            <input
              type="number"
              min="2000"
              max="2100"
              value={filterYear}
              onChange={(e) => setFilterYear(e.target.value)}
              className="filter-input"
            />
          </div>
          <button className="btn btn-sm" onClick={handleApplyFilter}>Apply Filter</button>
          <button className="btn btn-secondary" onClick={handleClearFilter}>Clear</button>
          <button 
            className="btn btn-sm"
            onClick={() => setShowCalculateModal(true)}
          >
            Calculate Revenue
          </button>
        </div>
      </div>

      {error && <div className="error-message">{error}</div>}

      {/* Calculate Revenue Modal */}
      {showCalculateModal && (
        <div className="modal-overlay" onClick={() => setShowCalculateModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Calculate Revenue</h2>
              <button className="modal-close" onClick={() => setShowCalculateModal(false)}>×</button>
            </div>
            <div className="modal-body">
              <p>Select the month and year to calculate revenue:</p>
              <div className="form-group">
                <label>Month:</label>
                <select 
                  value={calculateMonth} 
                  onChange={(e) => setCalculateMonth(parseInt(e.target.value))}
                  className="form-control"
                >
                  {monthNames.map((name, index) => (
                    <option key={index + 1} value={index + 1}>{name}</option>
                  ))}
                </select>
              </div>
              <div className="form-group">
                <label>Year:</label>
                <input
                  type="number"
                  min="2000"
                  max="2100"
                  value={calculateYear} 
                  onChange={(e) => setCalculateYear(parseInt(e.target.value, 10) || new Date().getFullYear())}
                  className="form-control"
                />
              </div>
            </div>
            <div className="modal-footer">
              <button 
                className="btn btn-secondary" 
                onClick={() => setShowCalculateModal(false)}
                disabled={calculating}
              >
                Cancel
              </button>
              <button 
                className="btn btn-sm" 
                onClick={handleCalculateRevenue}
                disabled={calculating}
              >
                {calculating ? 'Calculating...' : 'Calculate'}
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="revenue-summary">
        <div className="summary-card">
          <h3 style={{ color: '#000' }}>Total Revenue</h3>
          <p className="summary-value" style={{ color: '#000' }}>{grandTotal.toLocaleString()} VND</p>
        </div>
        <div className="summary-card">
          <h3 style={{ color: '#000' }}>Subscription Revenue</h3>
          <p className="summary-value" style={{ color: '#000' }}>{totals.subscription.toLocaleString()} VND</p>
        </div>
        <div className="summary-card">
          <h3 style={{ color: '#000' }}>Order Revenue</h3>
          <p className="summary-value" style={{ color: '#000' }}>{totals.order.toLocaleString()} VND</p>
        </div>
        <div className="summary-card">
          <h3 style={{ color: '#000' }}>AI Credit Revenue</h3>
          <p className="summary-value" style={{ color: '#000' }}>{totals.aiCredit.toLocaleString()} VND</p>
        </div>
        <div className="summary-card">
          <h3 style={{ color: '#000' }}>Total Orders</h3>
          <p className="summary-value" style={{ color: '#000' }}>{totals.orders}</p>
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
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {reports.length === 0 ? (
              <tr>
                <td colSpan={7} style={{ textAlign: 'center', padding: '2rem', color: '#718096' }}>
                  No revenue data available for selected filter
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
                    <td>
                      <button
                        className="btn-delete"
                        onClick={() => handleDeleteReport(report.id, report.month, report.year)}
                        title="Delete this report"
                      >
                        Delete
                      </button>
                    </td>
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
                <td></td>
              </tr>
            </tfoot>
          )}
        </table>
      </div>
    </div>
  );
};

export default RevenueReport;
