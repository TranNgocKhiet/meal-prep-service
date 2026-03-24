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

interface TopMealSharePoint {
  mealName: string;
  totalQuantity: number;
  totalRevenue: number;
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
  monthlyOrderStatusCounts: Array<{
    label: string;
    failedCount: number;
    canceledCount: number;
    customerReceivedCount: number;
    customerRejectedCount: number;
  }>;

  revenueChangeOverview: MonthChangeOverview;
  ordersChangeOverview: MonthChangeOverview;

  topCustomersAiMealPlanUsage: TopCustomerUsage[];
  topCustomersAiNutritionUsage: TopCustomerUsage[];
  topCustomerOrderSpending: TopCustomerSpending[];
  topMealsOrdered: TopMealOrder[];
  topMealsByQuantityInRange: TopMealSharePoint[];
  topMealsByRevenueInRange: TopMealSharePoint[];

  mealPlanPage: number;
  nutritionPage: number;
  spendingPage: number;
  mealPage: number;

  mealPlanTotalPages: number;
  nutritionTotalPages: number;
  spendingTotalPages: number;
  mealTotalPages: number;
}

interface OverviewAggregate {
  revenue: number;
  orders: number;
  aiMealPlanUses: number;
  aiNutritionUses: number;
}

type ComparisonChartKey =
  | 'revenue'
  | 'orders'
  | 'aiMealPlan'
  | 'aiNutrition'
  | 'topMealsQuantityPie'
  | 'topMealsRevenuePie';

type ComparisonChartMode = Record<ComparisonChartKey, boolean>;

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

const DAY_IN_MS = 24 * 60 * 60 * 1000;

const formatDateInput = (date: Date) => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const parseDateInput = (value: string) => new Date(`${value}T00:00:00`);

const isValidDateRange = (from: string, to: string) => {
  const fromDate = parseDateInput(from);
  const toDate = parseDateInput(to);
  return !Number.isNaN(fromDate.getTime()) && !Number.isNaN(toDate.getTime()) && fromDate <= toDate;
};

const buildPreviousInterval = (from: string, to: string) => {
  const fromDate = parseDateInput(from);
  const toDate = parseDateInput(to);
  const dayCount = Math.max(1, Math.floor((toDate.getTime() - fromDate.getTime()) / DAY_IN_MS) + 1);

  const compareToDate = new Date(fromDate);
  compareToDate.setDate(compareToDate.getDate() - 1);

  const compareFromDate = new Date(compareToDate);
  compareFromDate.setDate(compareFromDate.getDate() - (dayCount - 1));

  return {
    from: formatDateInput(compareFromDate),
    to: formatDateInput(compareToDate)
  };
};

const aggregateOverview = (dashboard: DashboardData): OverviewAggregate => ({
  revenue: dashboard.monthlyOrderRevenue.reduce((sum, item) => sum + item.revenue, 0),
  orders: dashboard.monthlyOrderRevenue.reduce((sum, item) => sum + item.orders, 0),
  aiMealPlanUses: dashboard.monthlyAiMealPlanUsage.reduce((sum, item) => sum + item.count, 0),
  aiNutritionUses: dashboard.monthlyAiNutritionUsage.reduce((sum, item) => sum + item.count, 0)
});

const defaultComparisonMode: ComparisonChartMode = {
  revenue: false,
  orders: false,
  aiMealPlan: false,
  aiNutrition: false,
  topMealsQuantityPie: false,
  topMealsRevenuePie: false
};

const piePalette = [
  '#22b14c',
  '#0d6efd',
  '#f59e0b',
  '#8b5cf6',
  '#ef4444',
  '#14b8a6',
  '#f97316',
  '#64748b',
  '#eab308',
  '#06b6d4'
];

const formatRangeLabel = (from: string, to: string) => {
  const fromDate = parseDateInput(from);
  const toDate = parseDateInput(to);
  return `${fromDate.toLocaleDateString()} - ${toDate.toLocaleDateString()}`;
};

const AdminDashboard = () => {
  const today = new Date();
  const defaultFrom = new Date(today.getFullYear(), today.getMonth() - 5, 1).toISOString().split('T')[0];
  const defaultTo = today.toISOString().split('T')[0];

  const [fromDate, setFromDate] = useState(defaultFrom);
  const [toDate, setToDate] = useState(defaultTo);
  const [topMealsQuantityCount, setTopMealsQuantityCount] = useState(5);
  const [topMealsRevenueCount, setTopMealsRevenueCount] = useState(5);
  const [topMonth, setTopMonth] = useState(today.getMonth() + 1);
  const [topYear, setTopYear] = useState(today.getFullYear());
  const [mealPlanPage, setMealPlanPage] = useState(1);
  const [nutritionPage, setNutritionPage] = useState(1);
  const [spendingPage, setSpendingPage] = useState(1);
  const [mealPage, setMealPage] = useState(1);
  const [activeTab, setActiveTab] = useState<'overview' | 'top100'>('overview');

  const initialCompare = buildPreviousInterval(defaultFrom, defaultTo);
  const [compareFromDate, setCompareFromDate] = useState(initialCompare.from);
  const [compareToDate, setCompareToDate] = useState(initialCompare.to);
  const [compareLoading, setCompareLoading] = useState(true);
  const [compareError, setCompareError] = useState('');
  const [compareData, setCompareData] = useState<DashboardData | null>(null);
  const [comparisonMode, setComparisonMode] = useState<ComparisonChartMode>(defaultComparisonMode);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [data, setData] = useState<DashboardData | null>(null);

  const revenueCanvasRef = useRef<HTMLCanvasElement | null>(null);
  const ordersCanvasRef = useRef<HTMLCanvasElement | null>(null);
  const aiMealPlanCanvasRef = useRef<HTMLCanvasElement | null>(null);
  const aiNutritionCanvasRef = useRef<HTMLCanvasElement | null>(null);
  const topMealsQuantityPieCanvasRef = useRef<HTMLCanvasElement | null>(null);
  const topMealsRevenuePieCanvasRef = useRef<HTMLCanvasElement | null>(null);
  const topMealsQuantityPieCompareCanvasRef = useRef<HTMLCanvasElement | null>(null);
  const topMealsRevenuePieCompareCanvasRef = useRef<HTMLCanvasElement | null>(null);

  const currentOverview = useMemo(() => (data ? aggregateOverview(data) : null), [data]);

  const fetchDashboard = async () => {
    try {
      setLoading(true);
      setError('');

      const params = new URLSearchParams({
        fromDate,
        toDate,
        topMealsCount: '20',
        topMealsQuantityCount: '20',
        topMealsRevenueCount: '20',
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

  const fetchComparisonDashboard = async (overrideFromDate?: string, overrideToDate?: string) => {
    const targetFromDate = overrideFromDate ?? compareFromDate;
    const targetToDate = overrideToDate ?? compareToDate;

    if (!isValidDateRange(targetFromDate, targetToDate)) {
      setCompareError('Compare interval is invalid. Please ensure Compare from date is not after Compare to date.');
      setCompareLoading(false);
      setCompareData(null);
      return;
    }

    try {
      setCompareLoading(true);
      setCompareError('');

      const params = new URLSearchParams({
        fromDate: targetFromDate,
        toDate: targetToDate,
        topMealsCount: '20',
        topMealsQuantityCount: '20',
        topMealsRevenueCount: '20',
        topMonth: String(topMonth),
        topYear: String(topYear),
        mealPlanPage: '1',
        nutritionPage: '1',
        spendingPage: '1',
        mealPage: '1'
      });

      const response = await apiClient.get(`/admin/dashboard?${params.toString()}`);

      if (!response.data?.success) {
        throw new Error(response.data?.message || 'Failed to load comparison data.');
      }

      setCompareData(response.data.data as DashboardData);
    } catch (err: any) {
      setCompareData(null);
      setCompareError(err?.response?.data?.message || err?.message || 'Failed to load comparison data.');
    } finally {
      setCompareLoading(false);
    }
  };

  useEffect(() => {
    fetchDashboard();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [mealPlanPage, nutritionPage, spendingPage, mealPage]);

  useEffect(() => {
    fetchComparisonDashboard();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (activeTab === 'overview') {
      setComparisonMode(defaultComparisonMode);
    }
  }, [activeTab]);

  useEffect(() => {
    const hasComparisonChartEnabled = Object.values(comparisonMode).some(Boolean);
    if (!hasComparisonChartEnabled) {
      return;
    }

    fetchComparisonDashboard();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [compareFromDate, compareToDate]);

  useEffect(() => {
    if (!data || activeTab !== 'overview') {
      return;
    }

    const charts: Chart[] = [];

    if (revenueCanvasRef.current) {
      const useComparison = comparisonMode.revenue && !!compareData;
      const currentRange = formatRangeLabel(fromDate, toDate);
      const compareRange = formatRangeLabel(compareFromDate, compareToDate);
      const currentTotals = aggregateOverview(data);
      const compareTotals = compareData ? aggregateOverview(compareData) : null;

      charts.push(new Chart(revenueCanvasRef.current, {
        type: useComparison ? 'bar' : 'line',
        data: useComparison
          ? {
              labels: ['Revenue (VND)'],
              datasets: [
                {
                  label: currentRange,
                  data: [currentTotals.revenue],
                  backgroundColor: 'rgba(37,99,235,0.7)',
                  borderColor: 'rgba(37,99,235,1)',
                  borderWidth: 1
                },
                {
                  label: compareRange,
                  data: [compareTotals?.revenue ?? 0],
                  backgroundColor: 'rgba(245,158,11,0.7)',
                  borderColor: 'rgba(245,158,11,1)',
                  borderWidth: 1
                }
              ]
            }
          : {
              labels: data.monthlyOrderRevenue.map((x) => x.label),
              datasets: [
                {
                  label: 'Revenue',
                  data: data.monthlyOrderRevenue.map((x) => x.revenue),
                  borderColor: '#22B14C',
                  backgroundColor: 'rgba(34,177,76,0.15)',
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
          scales: useComparison
            ? {
                x: {
                  ticks: { color: '#cbd5e1' },
                  grid: { color: 'rgba(255,255,255,0.08)' }
                },
                y: {
                  beginAtZero: true,
                  ticks: { color: '#cbd5e1' },
                  grid: { color: 'rgba(255,255,255,0.08)' }
                }
              }
            : {
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

    if (ordersCanvasRef.current) {
      const useComparison = comparisonMode.orders && !!compareData;
      const currentRange = formatRangeLabel(fromDate, toDate);
      const compareRange = formatRangeLabel(compareFromDate, compareToDate);
      const currentTotals = aggregateOverview(data);
      const compareTotals = compareData ? aggregateOverview(compareData) : null;

      charts.push(new Chart(ordersCanvasRef.current, {
        type: useComparison ? 'bar' : 'line',
        data: useComparison
          ? {
              labels: ['Orders'],
              datasets: [
                {
                  label: currentRange,
                  data: [currentTotals.orders],
                  backgroundColor: 'rgba(37,99,235,0.7)',
                  borderColor: 'rgba(37,99,235,1)',
                  borderWidth: 1
                },
                {
                  label: compareRange,
                  data: [compareTotals?.orders ?? 0],
                  backgroundColor: 'rgba(245,158,11,0.7)',
                  borderColor: 'rgba(245,158,11,1)',
                  borderWidth: 1
                }
              ]
            }
          : {
              labels: data.monthlyOrderRevenue.map((x) => x.label),
              datasets: [
                {
                  label: 'Orders',
                  data: data.monthlyOrderRevenue.map((x) => x.orders),
                  borderColor: '#0D6EFD',
                  backgroundColor: 'rgba(13,110,253,0.15)',
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
              beginAtZero: true,
              ticks: { color: '#cbd5e1' },
              grid: { color: 'rgba(255,255,255,0.08)' }
            }
          }
        }
      }));
    }

    if (aiMealPlanCanvasRef.current) {
      const useComparison = comparisonMode.aiMealPlan && !!compareData;
      const currentRange = formatRangeLabel(fromDate, toDate);
      const compareRange = formatRangeLabel(compareFromDate, compareToDate);
      const currentTotal = data.monthlyAiMealPlanUsage.reduce((sum, item) => sum + item.count, 0);
      const compareTotal = compareData
        ? compareData.monthlyAiMealPlanUsage.reduce((sum, item) => sum + item.count, 0)
        : 0;

      charts.push(new Chart(aiMealPlanCanvasRef.current, {
        type: useComparison ? 'bar' : 'line',
        data: useComparison
          ? {
              labels: ['AI Meal Plan Uses'],
              datasets: [
                {
                  label: currentRange,
                  data: [currentTotal],
                  backgroundColor: 'rgba(37,99,235,0.7)',
                  borderColor: 'rgba(37,99,235,1)',
                  borderWidth: 1
                },
                {
                  label: compareRange,
                  data: [compareTotal],
                  backgroundColor: 'rgba(245,158,11,0.7)',
                  borderColor: 'rgba(245,158,11,1)',
                  borderWidth: 1
                }
              ]
            }
          : {
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
              beginAtZero: true,
              ticks: { color: '#cbd5e1' },
              grid: { color: 'rgba(255,255,255,0.08)' }
            }
          }
        }
      }));
    }

    if (aiNutritionCanvasRef.current) {
      const useComparison = comparisonMode.aiNutrition && !!compareData;
      const currentRange = formatRangeLabel(fromDate, toDate);
      const compareRange = formatRangeLabel(compareFromDate, compareToDate);
      const currentTotal = data.monthlyAiNutritionUsage.reduce((sum, item) => sum + item.count, 0);
      const compareTotal = compareData
        ? compareData.monthlyAiNutritionUsage.reduce((sum, item) => sum + item.count, 0)
        : 0;

      charts.push(new Chart(aiNutritionCanvasRef.current, {
        type: useComparison ? 'bar' : 'line',
        data: useComparison
          ? {
              labels: ['AI Nutrition Uses'],
              datasets: [
                {
                  label: currentRange,
                  data: [currentTotal],
                  backgroundColor: 'rgba(37,99,235,0.7)',
                  borderColor: 'rgba(37,99,235,1)',
                  borderWidth: 1
                },
                {
                  label: compareRange,
                  data: [compareTotal],
                  backgroundColor: 'rgba(245,158,11,0.7)',
                  borderColor: 'rgba(245,158,11,1)',
                  borderWidth: 1
                }
              ]
            }
          : {
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
              beginAtZero: true,
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
  }, [data, activeTab, compareData, comparisonMode, fromDate, toDate, compareFromDate, compareToDate]);

  useEffect(() => {
    if (!data || activeTab !== 'overview') {
      return;
    }

    const pieCharts: Chart[] = [];

    const getTopMeals = (
      source: DashboardData,
      mode: 'quantity' | 'revenue',
      topCount: number
    ): TopMealSharePoint[] => {
      if (mode === 'quantity') {
        return (source.topMealsByQuantityInRange?.length
          ? source.topMealsByQuantityInRange
          : (source.topMealsOrdered || []).map((item) => ({
              mealName: item.mealName,
              totalQuantity: item.totalQuantity,
              totalRevenue: 0
            })))
          .slice(0, topCount);
      }

      return (source.topMealsByRevenueInRange?.length
        ? source.topMealsByRevenueInRange
        : (source.topMealsOrdered || []).map((item) => ({
            mealName: item.mealName,
            totalQuantity: item.totalQuantity,
            totalRevenue: item.totalQuantity
          })))
        .slice(0, topCount);
    };

    if (topMealsQuantityPieCanvasRef.current) {
      const quantityTopMeals = getTopMeals(data, 'quantity', topMealsQuantityCount);

      if (quantityTopMeals.length > 0) {
        pieCharts.push(new Chart(topMealsQuantityPieCanvasRef.current, {
          type: 'pie',
          data: {
            labels: quantityTopMeals.map((x) => x.mealName),
            datasets: [
              {
                data: quantityTopMeals.map((x) => x.totalQuantity),
                backgroundColor: quantityTopMeals.map((_, idx) => piePalette[idx % piePalette.length]),
                borderColor: '#0f172a',
                borderWidth: 1
              }
            ]
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
              legend: {
                position: 'bottom',
                labels: {
                  color: '#e2e8f0',
                  boxWidth: 14,
                  padding: 12
                }
              },
              tooltip: {
                callbacks: {
                  label(context) {
                    const dataset = context.dataset.data as number[];
                    const total = dataset.reduce((sum, value) => sum + value, 0);
                    const value = Number(context.raw || 0);
                    const percentage = total > 0 ? (value / total) * 100 : 0;
                    return `${context.label}: ${value.toLocaleString()} (${percentage.toFixed(1)}%)`;
                  }
                }
              }
            }
          }
        }));
      }
    }

    if (topMealsQuantityPieCompareCanvasRef.current && comparisonMode.topMealsQuantityPie && compareData) {
      const compareQuantityTopMeals = getTopMeals(compareData, 'quantity', topMealsQuantityCount);

      if (compareQuantityTopMeals.length > 0) {
        pieCharts.push(new Chart(topMealsQuantityPieCompareCanvasRef.current, {
          type: 'pie',
          data: {
            labels: compareQuantityTopMeals.map((x) => x.mealName),
            datasets: [
              {
                data: compareQuantityTopMeals.map((x) => x.totalQuantity),
                backgroundColor: compareQuantityTopMeals.map((_, idx) => piePalette[idx % piePalette.length]),
                borderColor: '#0f172a',
                borderWidth: 1
              }
            ]
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
              legend: {
                position: 'bottom',
                labels: {
                  color: '#e2e8f0',
                  boxWidth: 14,
                  padding: 12
                }
              },
              tooltip: {
                callbacks: {
                  label(context) {
                    const dataset = context.dataset.data as number[];
                    const total = dataset.reduce((sum, value) => sum + value, 0);
                    const value = Number(context.raw || 0);
                    const percentage = total > 0 ? (value / total) * 100 : 0;
                    return `${context.label}: ${value.toLocaleString()} (${percentage.toFixed(1)}%)`;
                  }
                }
              }
            }
          }
        }));
      }
    }

    if (topMealsRevenuePieCanvasRef.current) {
      const revenueTopMeals = getTopMeals(data, 'revenue', topMealsRevenueCount);

      if (revenueTopMeals.length > 0) {
        pieCharts.push(new Chart(topMealsRevenuePieCanvasRef.current, {
          type: 'pie',
          data: {
            labels: revenueTopMeals.map((x) => x.mealName),
            datasets: [
              {
                data: revenueTopMeals.map((x) => x.totalRevenue),
                backgroundColor: revenueTopMeals.map((_, idx) => piePalette[idx % piePalette.length]),
                borderColor: '#0f172a',
                borderWidth: 1
              }
            ]
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
              legend: {
                position: 'bottom',
                labels: {
                  color: '#e2e8f0',
                  boxWidth: 14,
                  padding: 12
                }
              },
              tooltip: {
                callbacks: {
                  label(context) {
                    const dataset = context.dataset.data as number[];
                    const total = dataset.reduce((sum, value) => sum + value, 0);
                    const value = Number(context.raw || 0);
                    const percentage = total > 0 ? (value / total) * 100 : 0;
                    return `${context.label}: ${value.toLocaleString()} VND (${percentage.toFixed(1)}%)`;
                  }
                }
              }
            }
          }
        }));
      }
    }

    if (topMealsRevenuePieCompareCanvasRef.current && comparisonMode.topMealsRevenuePie && compareData) {
      const compareRevenueTopMeals = getTopMeals(compareData, 'revenue', topMealsRevenueCount);

      if (compareRevenueTopMeals.length > 0) {
        pieCharts.push(new Chart(topMealsRevenuePieCompareCanvasRef.current, {
          type: 'pie',
          data: {
            labels: compareRevenueTopMeals.map((x) => x.mealName),
            datasets: [
              {
                data: compareRevenueTopMeals.map((x) => x.totalRevenue),
                backgroundColor: compareRevenueTopMeals.map((_, idx) => piePalette[idx % piePalette.length]),
                borderColor: '#0f172a',
                borderWidth: 1
              }
            ]
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
              legend: {
                position: 'bottom',
                labels: {
                  color: '#e2e8f0',
                  boxWidth: 14,
                  padding: 12
                }
              },
              tooltip: {
                callbacks: {
                  label(context) {
                    const dataset = context.dataset.data as number[];
                    const total = dataset.reduce((sum, value) => sum + value, 0);
                    const value = Number(context.raw || 0);
                    const percentage = total > 0 ? (value / total) * 100 : 0;
                    return `${context.label}: ${value.toLocaleString()} VND (${percentage.toFixed(1)}%)`;
                  }
                }
              }
            }
          }
        }));
      }
    }

    return () => {
      pieCharts.forEach((chart) => chart.destroy());
    };
  }, [data, activeTab, topMealsQuantityCount, topMealsRevenueCount, compareData, comparisonMode]);

  const switchChartComparison = async (key: ComparisonChartKey, enabled: boolean) => {
    if (enabled) {
      await fetchComparisonDashboard();
    }

    setComparisonMode((prev) => ({
      ...prev,
      [key]: enabled
    }));
  };

  const applyInterval = async () => {
    setMealPlanPage(1);
    setNutritionPage(1);
    setSpendingPage(1);
    setMealPage(1);
    await fetchDashboard();
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

      <section className="dashboard-tab-switch" aria-label="Dashboard sections">
        <button
          type="button"
          className={`dashboard-tab-btn ${activeTab === 'overview' ? 'active' : ''}`}
          onClick={() => setActiveTab('overview')}
        >
          Overview
        </button>
        <button
          type="button"
          className={`dashboard-tab-btn ${activeTab === 'top100' ? 'active' : ''}`}
          onClick={() => setActiveTab('top100')}
        >
          Top 100
        </button>
      </section>

      {error && <div className="error-message">{error}</div>}

      {activeTab === 'overview' && (
        <>
          <section className="summary-grid">
            <article className="summary-card">
              <h4>Revenue</h4>
              <p>{(currentOverview?.revenue ?? 0).toLocaleString()} VND</p>
            </article>
            <article className="summary-card">
              <h4>Orders</h4>
              <p>{(currentOverview?.orders ?? 0).toLocaleString()}</p>
            </article>
            <article className="summary-card">
              <h4>AI Meal Plan Uses</h4>
              <p>{(currentOverview?.aiMealPlanUses ?? 0).toLocaleString()}</p>
            </article>
            <article className="summary-card">
              <h4>AI Nutrition Uses</h4>
              <p>{(currentOverview?.aiNutritionUses ?? 0).toLocaleString()}</p>
            </article>
          </section>

          <section className="interval-row">
            <article className="filter-card">
              <div className="trend-card-header">
                <h3>Date interval</h3>
                <button onClick={applyInterval} className="btn-success btn-compact">Apply Interval</button>
              </div>
              <div className="interval-filter-grid">
                <div>
                  <label>From date</label>
                  <input type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
                </div>
                <div>
                  <label>To date</label>
                  <input type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
                </div>
              </div>
            </article>

            <article className="filter-card">
              <h3>Compare interval</h3>
              <div className="interval-filter-grid">
                <div>
                  <label>From date</label>
                  <input type="date" value={compareFromDate} onChange={(e) => setCompareFromDate(e.target.value)} />
                </div>
                <div>
                  <label>To date</label>
                  <input type="date" value={compareToDate} onChange={(e) => setCompareToDate(e.target.value)} />
                </div>
              </div>
              {compareError && <div className="error-message" style={{ marginTop: '0.75rem' }}>{compareError}</div>}
            </article>
          </section>

          <section className="trend-grid">
            <article className="trend-card">
              <div className="trend-card-header">
                <h3>Revenue trend</h3>
                <button
                  type="button"
                  className="btn-primary btn-compact"
                  disabled={compareLoading}
                  onClick={() => switchChartComparison('revenue', !comparisonMode.revenue)}
                >
                  {comparisonMode.revenue ? 'Back to Normal' : 'Apply Comparison'}
                </button>
              </div>
              {comparisonMode.revenue && (
                <p className="compare-caption">
                  Comparing {formatRangeLabel(fromDate, toDate)} vs {formatRangeLabel(compareFromDate, compareToDate)}
                </p>
              )}
              <div className="chart-wrap">
                <canvas ref={revenueCanvasRef} />
              </div>
            </article>

            <article className="trend-card">
              <div className="trend-card-header">
                <h3>Orders trend</h3>
                <button
                  type="button"
                  className="btn-primary btn-compact"
                  disabled={compareLoading}
                  onClick={() => switchChartComparison('orders', !comparisonMode.orders)}
                >
                  {comparisonMode.orders ? 'Back to Normal' : 'Apply Comparison'}
                </button>
              </div>
              {comparisonMode.orders && (
                <p className="compare-caption">
                  Comparing {formatRangeLabel(fromDate, toDate)} vs {formatRangeLabel(compareFromDate, compareToDate)}
                </p>
              )}
              <div className="chart-wrap">
                <canvas ref={ordersCanvasRef} />
              </div>
            </article>

            <article className="trend-card">
              <div className="trend-card-header">
                <h3>AI meal plan usage trend</h3>
                <button
                  type="button"
                  className="btn-primary btn-compact"
                  disabled={compareLoading}
                  onClick={() => switchChartComparison('aiMealPlan', !comparisonMode.aiMealPlan)}
                >
                  {comparisonMode.aiMealPlan ? 'Back to Normal' : 'Apply Comparison'}
                </button>
              </div>
              {comparisonMode.aiMealPlan && (
                <p className="compare-caption">
                  Comparing {formatRangeLabel(fromDate, toDate)} vs {formatRangeLabel(compareFromDate, compareToDate)}
                </p>
              )}
              <div className="chart-wrap">
                <canvas ref={aiMealPlanCanvasRef} />
              </div>
            </article>

            <article className="trend-card">
              <div className="trend-card-header">
                <h3>AI nutrition usage trend</h3>
                <button
                  type="button"
                  className="btn-primary btn-compact"
                  disabled={compareLoading}
                  onClick={() => switchChartComparison('aiNutrition', !comparisonMode.aiNutrition)}
                >
                  {comparisonMode.aiNutrition ? 'Back to Normal' : 'Apply Comparison'}
                </button>
              </div>
              {comparisonMode.aiNutrition && (
                <p className="compare-caption">
                  Comparing {formatRangeLabel(fromDate, toDate)} vs {formatRangeLabel(compareFromDate, compareToDate)}
                </p>
              )}
              <div className="chart-wrap">
                <canvas ref={aiNutritionCanvasRef} />
              </div>
            </article>

            <article className="trend-card">
              <div className="trend-card-header">
                <h3>Top {topMealsQuantityCount} meals by quantity (%)</h3>
                <div className="pie-chart-header-actions">
                  <button
                    type="button"
                    className="btn-primary btn-compact"
                    disabled={compareLoading}
                    onClick={() => switchChartComparison('topMealsQuantityPie', !comparisonMode.topMealsQuantityPie)}
                  >
                    {comparisonMode.topMealsQuantityPie ? 'Hide Comparison' : 'Apply Comparison'}
                  </button>
                  <div className="pie-top-count-control">
                    <label htmlFor="topMealsQuantityCount">Top</label>
                    <input
                      id="topMealsQuantityCount"
                      type="number"
                      min={2}
                      max={20}
                      value={topMealsQuantityCount}
                      onChange={(e) => {
                        const parsed = Number(e.target.value);
                        if (!Number.isNaN(parsed)) {
                          setTopMealsQuantityCount(Math.max(2, Math.min(20, parsed)));
                        }
                      }}
                    />
                  </div>
                </div>
              </div>
              <p className="compare-caption">Based on selected date interval.</p>
              {(data.topMealsByQuantityInRange?.length ?? 0) > 0 || (data.topMealsOrdered?.length ?? 0) > 0 ? (
                <div className="chart-wrap chart-wrap--md">
                  <canvas ref={topMealsQuantityPieCanvasRef} />
                </div>
              ) : (
                <p className="compare-caption">No meal data found in this date interval.</p>
              )}
              {comparisonMode.topMealsQuantityPie && (
                <>
                  <p className="compare-caption">
                    Comparison from past interval: {formatRangeLabel(compareFromDate, compareToDate)}
                  </p>
                  {(compareData?.topMealsByQuantityInRange?.length ?? 0) > 0 || (compareData?.topMealsOrdered?.length ?? 0) > 0 ? (
                    <div className="chart-wrap chart-wrap--md">
                      <canvas ref={topMealsQuantityPieCompareCanvasRef} />
                    </div>
                  ) : (
                    <p className="compare-caption">No meal data found for the comparison interval.</p>
                  )}
                </>
              )}
            </article>

            <article className="trend-card">
              <div className="trend-card-header">
                <h3>Top {topMealsRevenueCount} meals by revenue (%)</h3>
                <div className="pie-chart-header-actions">
                  <button
                    type="button"
                    className="btn-primary btn-compact"
                    disabled={compareLoading}
                    onClick={() => switchChartComparison('topMealsRevenuePie', !comparisonMode.topMealsRevenuePie)}
                  >
                    {comparisonMode.topMealsRevenuePie ? 'Hide Comparison' : 'Apply Comparison'}
                  </button>
                  <div className="pie-top-count-control">
                    <label htmlFor="topMealsRevenueCount">Top</label>
                    <input
                      id="topMealsRevenueCount"
                      type="number"
                      min={2}
                      max={20}
                      value={topMealsRevenueCount}
                      onChange={(e) => {
                        const parsed = Number(e.target.value);
                        if (!Number.isNaN(parsed)) {
                          setTopMealsRevenueCount(Math.max(2, Math.min(20, parsed)));
                        }
                      }}
                    />
                  </div>
                </div>
              </div>
              <p className="compare-caption">Based on selected date interval.</p>
              {(data.topMealsByRevenueInRange?.length ?? 0) > 0 || (data.topMealsOrdered?.length ?? 0) > 0 ? (
                <div className="chart-wrap chart-wrap--md">
                  <canvas ref={topMealsRevenuePieCanvasRef} />
                </div>
              ) : (
                <p className="compare-caption">No meal data found in this date interval.</p>
              )}
              {comparisonMode.topMealsRevenuePie && (
                <>
                  <p className="compare-caption">
                    Comparison from past interval: {formatRangeLabel(compareFromDate, compareToDate)}
                  </p>
                  {(compareData?.topMealsByRevenueInRange?.length ?? 0) > 0 || (compareData?.topMealsOrdered?.length ?? 0) > 0 ? (
                    <div className="chart-wrap chart-wrap--md">
                      <canvas ref={topMealsRevenuePieCompareCanvasRef} />
                    </div>
                  ) : (
                    <p className="compare-caption">No meal data found for the comparison interval.</p>
                  )}
                </>
              )}
            </article>
          </section>
        </>
      )}

      {activeTab === 'top100' && (
        <>
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
        </>
      )}
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
