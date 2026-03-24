import { useEffect, useMemo, useState } from 'react';
import apiClient from '../config/api';
import './AdminDashboard.css';

interface MonthlyOrderRevenuePoint {
  label: string;
  revenue: number;
  orders: number;
}

interface MonthlyUsagePoint {
  label: string;
  count: number;
}

interface MonthlyOrderStatusCountPoint {
  label: string;
  failedCount: number;
  canceledCount: number;
  customerReceivedCount: number;
  customerRejectedCount: number;
}

interface MonthChangeOverview {
  currentMonthLabel: string;
  previousMonthLabel: string;
  currentValue: number;
  previousValue: number;
  difference: number;
  isIncrease: boolean;
}

interface TopCustomerUsage {
  customerName: string;
  email: string;
  usageCount: number;
}

interface TopCustomerSpending {
  customerName: string;
  email: string;
  totalSpent: number;
}

interface TopMealOrder {
  mealName: string;
  totalQuantity: number;
}

interface DashboardData {
  fromDate: string;
  toDate: string;
  lastUpdated: string;
  topMonth: number;
  topYear: number;
  availableTopYears: number[];

  monthlyOrderRevenue: MonthlyOrderRevenuePoint[];
  monthlyAiMealPlanUsage: MonthlyUsagePoint[];
  monthlyAiNutritionUsage: MonthlyUsagePoint[];
  monthlyOrderStatusCounts: MonthlyOrderStatusCountPoint[];

  revenueChangeOverview: MonthChangeOverview;
  ordersChangeOverview: MonthChangeOverview;

  topCustomersAiMealPlanUsage: TopCustomerUsage[];
  topCustomersAiNutritionUsage: TopCustomerUsage[];
  topCustomerOrderSpending: TopCustomerSpending[];
  topMealsOrdered: TopMealOrder[];

  mealPlanPage: number;
  nutritionPage: number;
  spendingPage: number;
  mealPage: number;

  mealPlanTotalPages: number;
  nutritionTotalPages: number;
  spendingTotalPages: number;
  mealTotalPages: number;
}

const monthNames = [
  'January',
  'February',
  'March',
  'April',
  'May',
  'June',
  'July',
  'August',
  'September',
  'October',
  'November',
  'December'
];

const AdminDashboard = () => {
  const today = new Date();
  const defaultFrom = new Date(today.getFullYear(), today.getMonth() - 5, 1).toISOString().split('T')[0];
  const defaultTo = today.toISOString().split('T')[0];

  const [fromDate, setFromDate] = useState(defaultFrom);
  const [toDate, setToDate] = useState(defaultTo);
  const [topMonth, setTopMonth] = useState(today.getMonth() + 1);
  const [topYear, setTopYear] = useState(today.getFullYear());
  const [mealPlanPage, setMealPlanPage] = useState(1);
  const [nutritionPage, setNutritionPage] = useState(1);
  const [spendingPage, setSpendingPage] = useState(1);
  const [mealPage, setMealPage] = useState(1);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [data, setData] = useState<DashboardData | null>(null);

  const topRevenueMonth = useMemo(() => {
    if (!data || data.monthlyOrderRevenue.length === 0) {
      return null;
    }

    return [...data.monthlyOrderRevenue].sort((a, b) => b.revenue - a.revenue)[0];
  }, [data]);

  const fetchDashboard = async () => {
    try {
      setLoading(true);
      setError('');

      const params = new URLSearchParams({
        fromDate,
        toDate,
        topMonth: String(topMonth),
        topYear: String(topYear),
        mealPlanPage: String(mealPlanPage),
        nutritionPage: String(nutritionPage),
        spendingPage: String(spendingPage),
        mealPage: String(mealPage)
      });

      const response = await apiClient.get(`/admin/dashboard?${params.toString()}`);

      if (!response.data?.success) {
        throw new Error(response.data?.message || 'Failed to load dashboard.');
      }

      setData(response.data.data as DashboardData);
    } catch (err: any) {
      setData(null);
      setError(err?.response?.data?.message || err?.message || 'Failed to load dashboard data.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDashboard();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [mealPlanPage, nutritionPage, spendingPage, mealPage]);

  const applyInterval = () => {
    setMealPlanPage(1);
    setNutritionPage(1);
    setSpendingPage(1);
    setMealPage(1);
    fetchDashboard();
  };

  const applyTopMonthYear = () => {
    setMealPlanPage(1);
    setNutritionPage(1);
    setSpendingPage(1);
    setMealPage(1);
    fetchDashboard();
  };

  if (loading) {
    return <div className="container"><div className="loading">Loading dashboard...</div></div>;
  }

  if (!data) {
    return (
      <div className="container">
        <div className="error-message">{error || 'No dashboard data available.'}</div>
        <button onClick={fetchDashboard} className="btn-primary" style={{ marginTop: '0.75rem' }}>
          Retry loading dashboard
        </button>
      </div>
    );
  }

  return (
    <div className="container admin-dashboard-page">
      <section className="dashboard-hero-card">
        <div>
          <h1>Admin Dashboard</h1>
          <p>Analytics center for revenue, orders, AI usage, and top dishes.</p>
        </div>
        <div className="hero-side">
          <span>Last updated: {new Date(data.lastUpdated).toLocaleString()}</span>
          <button onClick={fetchDashboard} className="btn-primary">Refresh</button>
        </div>
      </section>

      {error && <div className="error-message">{error}</div>}

      <section className="summary-grid">
        <article className="summary-card">
          <h4>Revenue vs last month</h4>
          <p className={data.revenueChangeOverview.isIncrease ? 'up' : 'down'}>
            {data.revenueChangeOverview.isIncrease ? '+' : ''}
            {data.revenueChangeOverview.difference.toLocaleString()} VND
          </p>
        </article>
        <article className="summary-card">
          <h4>Orders vs last month</h4>
          <p className={data.ordersChangeOverview.isIncrease ? 'up' : 'down'}>
            {data.ordersChangeOverview.isIncrease ? '+' : ''}
            {data.ordersChangeOverview.difference.toLocaleString()}
          </p>
        </article>
        <article className="summary-card">
          <h4>Top revenue month</h4>
          <p>{topRevenueMonth ? `${topRevenueMonth.label} (${topRevenueMonth.revenue.toLocaleString()} VND)` : 'N/A'}</p>
        </article>
      </section>

      <section className="filter-card">
        <h3>Date interval</h3>
        <div className="filter-grid">
          <div>
            <label>From date</label>
            <input type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
          </div>
          <div>
            <label>To date</label>
            <input type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
          </div>
          <button onClick={applyInterval} className="btn-success">Apply Interval</button>
        </div>
      </section>

      <section className="trend-card">
        <h3>Monthly orders and revenue</h3>
        <div className="trend-table-wrap">
          <table className="dashboard-table">
            <thead>
              <tr>
                <th>Month</th>
                <th>Revenue (VND)</th>
                <th>Orders</th>
              </tr>
            </thead>
            <tbody>
              {data.monthlyOrderRevenue.map((item) => (
                <tr key={item.label}>
                  <td>{item.label}</td>
                  <td>{item.revenue.toLocaleString()}</td>
                  <td>{item.orders.toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="trend-grid">
        <article className="trend-card">
          <h3>Order status count</h3>
          <div className="trend-table-wrap">
            <table className="dashboard-table">
              <thead>
                <tr>
                  <th>Month</th>
                  <th>Failed</th>
                  <th>Canceled</th>
                  <th>Received</th>
                  <th>Rejected</th>
                </tr>
              </thead>
              <tbody>
                {data.monthlyOrderStatusCounts.map((item) => (
                  <tr key={item.label}>
                    <td>{item.label}</td>
                    <td>{item.failedCount}</td>
                    <td>{item.canceledCount}</td>
                    <td>{item.customerReceivedCount}</td>
                    <td>{item.customerRejectedCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </article>

        <article className="trend-card">
          <h3>AI usage trend</h3>
          <div className="trend-table-wrap">
            <table className="dashboard-table">
              <thead>
                <tr>
                  <th>Month</th>
                  <th>AI Meal Plans</th>
                  <th>AI Nutrition</th>
                </tr>
              </thead>
              <tbody>
                {data.monthlyAiMealPlanUsage.map((item, index) => (
                  <tr key={item.label}>
                    <td>{item.label}</td>
                    <td>{item.count}</td>
                    <td>{data.monthlyAiNutritionUsage[index]?.count ?? 0}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </article>
      </section>

      <section className="filter-card">
        <h3>Top 100 month/year</h3>
        <div className="filter-grid">
          <div>
            <label>Month</label>
            <select value={topMonth} onChange={(e) => setTopMonth(Number(e.target.value))}>
              {monthNames.map((month, index) => (
                <option key={month} value={index + 1}>{month}</option>
              ))}
            </select>
          </div>
          <div>
            <label>Year</label>
            <select value={topYear} onChange={(e) => setTopYear(Number(e.target.value))}>
              {data.availableTopYears.map((year) => (
                <option key={year} value={year}>{year}</option>
              ))}
            </select>
          </div>
          <button onClick={applyTopMonthYear} className="btn-primary">Apply Top Month/Year</button>
        </div>
      </section>

      <section className="top-grid">
        <TopTable
          title="Top 100 Customers - AI Meal Plan Usage"
          headers={['Customer', 'Email', 'Uses']}
          rows={data.topCustomersAiMealPlanUsage.map((x) => [x.customerName, x.email, x.usageCount.toString()])}
          page={data.mealPlanPage}
          totalPages={data.mealPlanTotalPages}
          onPrev={() => setMealPlanPage((p) => Math.max(1, p - 1))}
          onNext={() => setMealPlanPage((p) => Math.min(data.mealPlanTotalPages, p + 1))}
        />

        <TopTable
          title="Top 100 Customers - AI Nutrition Usage"
          headers={['Customer', 'Email', 'Uses']}
          rows={data.topCustomersAiNutritionUsage.map((x) => [x.customerName, x.email, x.usageCount.toString()])}
          page={data.nutritionPage}
          totalPages={data.nutritionTotalPages}
          onPrev={() => setNutritionPage((p) => Math.max(1, p - 1))}
          onNext={() => setNutritionPage((p) => Math.min(data.nutritionTotalPages, p + 1))}
        />

        <TopTable
          title={`Top 100 Customers - Order Spending (${monthNames[topMonth - 1]} ${topYear})`}
          headers={['Customer', 'Email', 'Spent (VND)']}
          rows={data.topCustomerOrderSpending.map((x) => [x.customerName, x.email, x.totalSpent.toLocaleString()])}
          page={data.spendingPage}
          totalPages={data.spendingTotalPages}
          onPrev={() => setSpendingPage((p) => Math.max(1, p - 1))}
          onNext={() => setSpendingPage((p) => Math.min(data.spendingTotalPages, p + 1))}
        />

        <TopTable
          title={`Top 100 Meals Ordered (${monthNames[topMonth - 1]} ${topYear})`}
          headers={['Meal', 'Quantity']}
          rows={data.topMealsOrdered.map((x) => [x.mealName, x.totalQuantity.toString()])}
          page={data.mealPage}
          totalPages={data.mealTotalPages}
          onPrev={() => setMealPage((p) => Math.max(1, p - 1))}
          onNext={() => setMealPage((p) => Math.min(data.mealTotalPages, p + 1))}
        />
      </section>
    </div>
  );
};

interface TopTableProps {
  title: string;
  headers: string[];
  rows: string[][];
  page: number;
  totalPages: number;
  onPrev: () => void;
  onNext: () => void;
}

const TopTable = ({ title, headers, rows, page, totalPages, onPrev, onNext }: TopTableProps) => {
  return (
    <article className="top-table-card">
      <div className="top-table-header">
        <h4>{title}</h4>
      </div>
      <div className="trend-table-wrap">
        <table className="dashboard-table">
          <thead>
            <tr>
              {headers.map((header) => (
                <th key={header}>{header}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.length === 0 && (
              <tr>
                <td colSpan={headers.length}>No data available</td>
              </tr>
            )}
            {rows.map((row, index) => (
              <tr key={`${title}-${index}`}>
                {row.map((col, colIndex) => (
                  <td key={`${title}-${index}-${colIndex}`}>{col}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="pager-row">
        <button onClick={onPrev} disabled={page <= 1}>Previous</button>
        <span>{page} / {totalPages}</span>
        <button onClick={onNext} disabled={page >= totalPages}>Next</button>
      </div>
    </article>
  );
};

export default AdminDashboard;
