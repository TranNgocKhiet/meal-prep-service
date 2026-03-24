import { useEffect, useMemo, useRef, useState } from 'react';
import Chart from 'chart.js/auto';
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

  const ordersRevenueCanvasRef = useRef<HTMLCanvasElement | null>(null);
  const orderStatusCanvasRef = useRef<HTMLCanvasElement | null>(null);
  const aiMealPlanCanvasRef = useRef<HTMLCanvasElement | null>(null);
  const aiNutritionCanvasRef = useRef<HTMLCanvasElement | null>(null);

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

  useEffect(() => {
    if (!data) {
      return;
    }

    const charts: Chart[] = [];

    if (ordersRevenueCanvasRef.current) {
      charts.push(new Chart(ordersRevenueCanvasRef.current, {
        type: 'line',
        data: {
          labels: data.monthlyOrderRevenue.map((x) => x.label),
          datasets: [
            {
              label: 'Revenue',
              data: data.monthlyOrderRevenue.map((x) => x.revenue),
              borderColor: '#22B14C',
              backgroundColor: 'rgba(34,177,76,0.15)',
              yAxisID: 'y',
              tension: 0.35
            },
            {
              label: 'Orders',
              data: data.monthlyOrderRevenue.map((x) => x.orders),
              borderColor: '#0D6EFD',
              backgroundColor: 'rgba(13,110,253,0.15)',
              yAxisID: 'y1',
              tension: 0.35
            }
          ]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: {
              labels: {
                color: '#e2e8f0'
              }
            }
          },
          scales: {
            x: {
              ticks: { color: '#cbd5e1' },
              grid: { color: 'rgba(255,255,255,0.08)' }
            },
            y: {
              ticks: { color: '#cbd5e1' },
              grid: { color: 'rgba(255,255,255,0.08)' }
            },
            y1: {
              position: 'right',
              ticks: { color: '#cbd5e1' },
              grid: { drawOnChartArea: false }
            }
          }
        }
      }));
    }

    if (orderStatusCanvasRef.current) {
      charts.push(new Chart(orderStatusCanvasRef.current, {
        type: 'bar',
        data: {
          labels: data.monthlyOrderStatusCounts.map((x) => x.label),
          datasets: [
            {
              label: 'Failed',
              data: data.monthlyOrderStatusCounts.map((x) => x.failedCount),
              backgroundColor: 'rgba(220,53,69,0.7)',
              borderColor: 'rgba(220,53,69,1)',
              borderWidth: 1
            },
            {
              label: 'Canceled',
              data: data.monthlyOrderStatusCounts.map((x) => x.canceledCount),
              backgroundColor: 'rgba(255,193,7,0.7)',
              borderColor: 'rgba(255,193,7,1)',
              borderWidth: 1
            },
            {
              label: 'Customer Received',
              data: data.monthlyOrderStatusCounts.map((x) => x.customerReceivedCount),
              backgroundColor: 'rgba(34,177,76,0.7)',
              borderColor: 'rgba(34,177,76,1)',
              borderWidth: 1
            },
            {
              label: 'Customer Rejected',
              data: data.monthlyOrderStatusCounts.map((x) => x.customerRejectedCount),
              backgroundColor: 'rgba(13,110,253,0.7)',
              borderColor: 'rgba(13,110,253,1)',
              borderWidth: 1
            }
          ]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: {
              labels: {
                color: '#e2e8f0'
              }
            }
          },
          scales: {
            x: {
              ticks: { color: '#cbd5e1' },
              grid: { color: 'rgba(255,255,255,0.08)' }
            },
            y: {
              beginAtZero: true,
              ticks: { color: '#cbd5e1', precision: 0 },
              grid: { color: 'rgba(255,255,255,0.08)' }
            }
          }
        }
      }));
    }

    if (aiMealPlanCanvasRef.current) {
      charts.push(new Chart(aiMealPlanCanvasRef.current, {
        type: 'line',
        data: {
          labels: data.monthlyAiMealPlanUsage.map((x) => x.label),
          datasets: [
            {
              label: 'Meal Plan Uses',
              data: data.monthlyAiMealPlanUsage.map((x) => x.count),
              borderColor: '#f59e0b',
              backgroundColor: 'rgba(245,158,11,0.15)',
              tension: 0.35
            }
          ]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: {
              labels: {
                color: '#e2e8f0'
              }
            }
          },
          scales: {
            x: {
              ticks: { color: '#cbd5e1' },
              grid: { color: 'rgba(255,255,255,0.08)' }
            },
            y: {
              ticks: { color: '#cbd5e1' },
              grid: { color: 'rgba(255,255,255,0.08)' }
            }
          }
        }
      }));
    }

    if (aiNutritionCanvasRef.current) {
      charts.push(new Chart(aiNutritionCanvasRef.current, {
        type: 'line',
        data: {
          labels: data.monthlyAiNutritionUsage.map((x) => x.label),
          datasets: [
            {
              label: 'Nutrition Uses',
              data: data.monthlyAiNutritionUsage.map((x) => x.count),
              borderColor: '#8b5cf6',
              backgroundColor: 'rgba(139,92,246,0.15)',
              tension: 0.35
            }
          ]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: {
              labels: {
                color: '#e2e8f0'
              }
            }
          },
          scales: {
            x: {
              ticks: { color: '#cbd5e1' },
              grid: { color: 'rgba(255,255,255,0.08)' }
            },
            y: {
              ticks: { color: '#cbd5e1' },
              grid: { color: 'rgba(255,255,255,0.08)' }
            }
          }
        }
      }));
    }

    return () => {
      charts.forEach((chart) => chart.destroy());
    };
  }, [data]);

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
        <h3>Orders and revenue trend</h3>
        <div className="chart-wrap chart-wrap--lg">
          <canvas ref={ordersRevenueCanvasRef} />
        </div>
      </section>

      <section className="trend-grid">
        <article className="trend-card">
          <h3>Order status count</h3>
          <div className="chart-wrap">
            <canvas ref={orderStatusCanvasRef} />
          </div>
        </article>

        <article className="trend-card">
          <h3>AI meal plan usage trend</h3>
          <div className="chart-wrap">
            <canvas ref={aiMealPlanCanvasRef} />
          </div>
        </article>

        <article className="trend-card">
          <h3>AI nutrition usage trend</h3>
          <div className="chart-wrap">
            <canvas ref={aiNutritionCanvasRef} />
          </div>
        </article>
      </section>

      <section className="trend-card">
        <h3>Monthly data table</h3>
        <div className="trend-table-wrap">
          <table className="dashboard-table">
            <thead>
              <tr>
                <th>Month</th>
                <th>Revenue (VND)</th>
                <th>Orders</th>
                <th>Failed</th>
                <th>Canceled</th>
                <th>Received</th>
                <th>Rejected</th>
              </tr>
            </thead>
            <tbody>
              {data.monthlyOrderRevenue.map((item, index) => (
                <tr key={item.label}>
                  <td>{item.label}</td>
                  <td>{item.revenue.toLocaleString()}</td>
                  <td>{item.orders.toLocaleString()}</td>
                  <td>{data.monthlyOrderStatusCounts[index]?.failedCount ?? 0}</td>
                  <td>{data.monthlyOrderStatusCounts[index]?.canceledCount ?? 0}</td>
                  <td>{data.monthlyOrderStatusCounts[index]?.customerReceivedCount ?? 0}</td>
                  <td>{data.monthlyOrderStatusCounts[index]?.customerRejectedCount ?? 0}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
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
