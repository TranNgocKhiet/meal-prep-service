import { useState, useEffect } from 'react';
import apiClient from '../config/api';
import { useAuth } from '../hooks/useAuth';
import { formatVND } from '../utils/currency';
import './WeeklyMenu.css';

interface MenuMeal {
  id: string;
  mealTypeId: number;
  totalCalories: number;
  proteinG: number;
  fatG: number;
  carbsG: number;
  price: number;
  availableQuantity: number;
  menuMealRecipes?: {
    recipe?: {
      recipeName?: string;
      name?: string;
    };
  }[];
}

interface DailyMenu {
  id: string;
  menuDate: string;
  statusId: number;
  menuMeals: MenuMeal[];
}

const getLocalDateString = (date: Date) => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const getDatePart = (value: string) => {
  return value.split('T')[0];
};

const getRoleFromToken = () => {
  try {
    const token = localStorage.getItem('authToken');
    if (!token) return '';

    const tokenParts = token.split('.');
    if (tokenParts.length < 2) return '';

    const base64 = tokenParts[1].replace(/-/g, '+').replace(/_/g, '/');
    const payload = JSON.parse(atob(base64));

    const role =
      payload?.role ||
      payload?.roles?.[0] ||
      payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
      payload?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role'] ||
      '';

    return String(role);
  } catch {
    return '';
  }
};

const WeeklyMenu = () => {
  const [menus, setMenus] = useState<DailyMenu[]>([]);
  const [currentWeekStart, setCurrentWeekStart] = useState<Date>(getWeekStart(new Date()));
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [addingToCart, setAddingToCart] = useState<string | null>(null);
  const [showAddSuccessModal, setShowAddSuccessModal] = useState(false);
  const { user } = useAuth();
  const normalizedRole = (user?.roleName || getRoleFromToken()).trim().toLowerCase();
  const isCustomer = normalizedRole === 'customer';

  function getWeekStart(date: Date): Date {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1); // Adjust when day is Sunday
    return new Date(d.setDate(diff));
  }

  function getWeekEnd(weekStart: Date): Date {
    const end = new Date(weekStart);
    end.setDate(end.getDate() + 6);
    return end;
  }

  const getMealTypeName = (mealTypeId: number) => {
    switch (mealTypeId) {
      case 1: return 'Breakfast';
      case 2: return 'Lunch';
      case 3: return 'Dinner';
      default: return 'Unknown';
    }
  };

  const getRecipeNames = (meal: MenuMeal): string[] => {
    return (meal.menuMealRecipes || [])
      .map((item) => item.recipe?.recipeName || item.recipe?.name)
      .filter((name): name is string => Boolean(name && name.trim()));
  };

  useEffect(() => {
    fetchWeeklyMenu();
  }, [currentWeekStart]);

  const fetchWeeklyMenu = async () => {
    try {
      setLoading(true);
      const weekEnd = getWeekEnd(currentWeekStart);
      const startDate = getLocalDateString(currentWeekStart);
      const endDate = getLocalDateString(weekEnd);
      
      const response = await apiClient.get(`/dailymenus?startDate=${startDate}&endDate=${endDate}`);
      
      if (response.data.success) {
        setMenus(response.data.data);
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load weekly menu');
    } finally {
      setLoading(false);
    }
  };

  const handleAddToCart = async (menuMealId: string) => {
    try {
      setAddingToCart(menuMealId);
      await apiClient.post('/cart/items', {
        menuMealId,
        quantity: 1
      });
      setShowAddSuccessModal(true);
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to add to cart');
    } finally {
      setAddingToCart(null);
    }
  };

  const goToPreviousWeek = () => {
    const newStart = new Date(currentWeekStart);
    newStart.setDate(newStart.getDate() - 7);
    setCurrentWeekStart(newStart);
  };

  const goToNextWeek = () => {
    const newStart = new Date(currentWeekStart);
    newStart.setDate(newStart.getDate() + 7);
    setCurrentWeekStart(newStart);
  };

  const goToCurrentWeek = () => {
    setCurrentWeekStart(getWeekStart(new Date()));
  };

  const formatWeekRange = () => {
    const weekEnd = getWeekEnd(currentWeekStart);
    return `${currentWeekStart.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} - ${weekEnd.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`;
  };

  const weekDays = Array.from({ length: 7 }, (_, index) => {
    const date = new Date(currentWeekStart);
    date.setDate(currentWeekStart.getDate() + index);
    return date;
  });

  const getMenuForDate = (date: Date) => {
    const dateKey = getLocalDateString(date);
    return menus.find((menu) => getDatePart(menu.menuDate) === dateKey);
  };

  if (loading) {
    return <div className="container"><div className="loading">Loading weekly menu...</div></div>;
  }

  if (error) {
    return <div className="container"><div className="error-message">{error}</div></div>;
  }

  return (
    <div className="container">
      <div className="weekly-menu-header">
        <h1>Weekly Menu</h1>
        <div className="week-navigation">
          <button onClick={goToPreviousWeek} className="btn-nav">← Previous</button>
          <div className="week-range">
            <span>{formatWeekRange()}</span>
            <button onClick={goToCurrentWeek} className="btn-current-week">This Week</button>
          </div>
          <button onClick={goToNextWeek} className="btn-nav">Next →</button>
        </div>
      </div>

      <div className="weekly-menu-list">
        {weekDays.map((day) => {
          const menu = getMenuForDate(day);
          const sortedMeals = menu?.menuMeals
            ? [...menu.menuMeals].sort((a, b) => a.mealTypeId - b.mealTypeId)
            : [];
          const isPastDate = getLocalDateString(day) < getLocalDateString(new Date());

          return (
            <div key={getLocalDateString(day)} className="day-menu">
              <div className="day-header">
                <h3>{day.toLocaleDateString('en-US', { weekday: 'long' })}</h3>
                <span className="day-date">{day.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}</span>
              </div>

              {!menu || sortedMeals.length === 0 ? (
                <div className="no-day-menu">No menu for this date</div>
              ) : (
                <div className="meals-horizontal">
                  {sortedMeals.map((meal) => (
                    <div key={meal.id} className="meal-card-horizontal">
                      <div className="meal-info">
                        <h4 style={{ color: '#000' }}>{getMealTypeName(meal.mealTypeId)}</h4>
                        <div className="meal-recipes-compact">
                          {getRecipeNames(meal).length > 0 ? (
                            getRecipeNames(meal).map((recipeName, index) => (
                              <span key={`${meal.id}-recipe-${index}`} className="recipe-chip" title={recipeName}>
                                {recipeName}
                              </span>
                            ))
                          ) : (
                            <span className="no-recipe-chip">No recipes assigned</span>
                          )}
                        </div>
                        <div className="meal-nutrition-compact">
                          <span style={{ color: '#666' }}>{meal.totalCalories.toFixed(0)} kcal</span>
                          <span style={{ color: '#666' }}>P: {meal.proteinG.toFixed(0)}g</span>
                          <span style={{ color: '#666' }}>C: {meal.carbsG.toFixed(0)}g</span>
                          <span style={{ color: '#666' }}>F: {meal.fatG.toFixed(0)}g</span>
                        </div>
                      </div>
                      <div className="meal-actions">
                        <span className="meal-price" style={{ color: '#000', fontWeight: 'bold' }}>{formatVND(meal.price)}</span>
                        <span className={`availability-badge ${meal.availableQuantity > 0 ? 'available' : 'unavailable'}`} style={{ color: meal.availableQuantity > 0 ? '#48bb78' : '#e53e3e' }}>
                          {meal.availableQuantity > 0 ? `${meal.availableQuantity} available` : 'Sold Out'}
                        </span>
                        {isCustomer && (
                          <button
                            className={`btn-add-to-cart ${isPastDate ? 'past-date' : ''}`}
                            onClick={() => handleAddToCart(meal.id)}
                            disabled={isPastDate || meal.availableQuantity === 0 || addingToCart === meal.id}
                            title={isPastDate ? 'Cannot add to cart for past dates' : undefined}
                          >
                            {isPastDate ? 'Date Passed' : (addingToCart === meal.id ? 'Adding...' : 'Add to Cart')}
                          </button>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          );
        })}
      </div>

      {showAddSuccessModal && (
        <div className="cart-success-overlay" onClick={() => setShowAddSuccessModal(false)}>
          <div className="cart-success-modal" onClick={(e) => e.stopPropagation()}>
            <h3>Added to Cart</h3>
            <p>Meal added to your cart successfully.</p>
            <button className="btn" onClick={() => setShowAddSuccessModal(false)}>
              OK
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default WeeklyMenu;
