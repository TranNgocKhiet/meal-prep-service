import { useState, useEffect } from 'react';
import apiClient from '../config/api';
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
}

interface DailyMenu {
  id: string;
  menuDate: string;
  statusId: number;
  menuMeals: MenuMeal[];
}

const WeeklyMenu = () => {
  const [menus, setMenus] = useState<DailyMenu[]>([]);
  const [currentWeekStart, setCurrentWeekStart] = useState<Date>(getWeekStart(new Date()));
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [addingToCart, setAddingToCart] = useState<string | null>(null);

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

  useEffect(() => {
    fetchWeeklyMenu();
  }, [currentWeekStart]);

  const fetchWeeklyMenu = async () => {
    try {
      setLoading(true);
      const weekEnd = getWeekEnd(currentWeekStart);
      const startDate = currentWeekStart.toISOString().split('T')[0];
      const endDate = weekEnd.toISOString().split('T')[0];
      
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
      alert('Added to cart successfully!');
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

      {menus.length === 0 ? (
        <div className="no-menu">No menu available for this week</div>
      ) : (
        <div className="weekly-menu-list">
          {menus.map((menu) => (
            <div key={menu.id} className="day-menu">
              <div className="day-header">
                <h3>{new Date(menu.menuDate).toLocaleDateString('en-US', { weekday: 'long' })}</h3>
                <span className="day-date">{new Date(menu.menuDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}</span>
              </div>
              <div className="meals-horizontal">
                {menu.menuMeals.map((meal) => (
                  <div key={meal.id} className="meal-card-horizontal">
                    <div className="meal-info">
                      <h4 style={{ color: '#000' }}>{getMealTypeName(meal.mealTypeId)}</h4>
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
                      <button 
                        className="btn-add-to-cart"
                        onClick={() => handleAddToCart(meal.id)}
                        disabled={meal.availableQuantity === 0 || addingToCart === meal.id}
                      >
                        {addingToCart === meal.id ? 'Adding...' : 'Add to Cart'}
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default WeeklyMenu;
