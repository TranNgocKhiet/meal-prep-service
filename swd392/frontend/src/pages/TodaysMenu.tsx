import { useState, useEffect } from 'react';
import apiClient from '../config/api';
import { useAuth } from '../hooks/useAuth';
import { formatVND } from '../utils/currency';
import './TodaysMenu.css';

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

const TodaysMenu = () => {
  const [menu, setMenu] = useState<DailyMenu | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [addingToCart, setAddingToCart] = useState<string | null>(null);
  const { user } = useAuth();
  const isCustomer = user?.roleName === 'Customer';

  useEffect(() => {
    fetchTodaysMenu();
  }, []);

  const getMealTypeName = (mealTypeId: number) => {
    switch (mealTypeId) {
      case 1: return 'Breakfast';
      case 2: return 'Lunch';
      case 3: return 'Dinner';
      default: return 'Unknown';
    }
  };

  const fetchTodaysMenu = async () => {
    try {
      setLoading(true);
      const today = new Date().toISOString().split('T')[0];
      const response = await apiClient.get(`/dailymenus?date=${today}`);
      
      if (response.data.success && response.data.data.length > 0) {
        setMenu(response.data.data[0]);
      } else {
        setMenu(null);
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load today\'s menu');
    } finally {
      setLoading(false);
    }
  };

  const handleAddToCart = async (menuMealId: string) => {
    try {
      setAddingToCart(menuMealId);
      const response = await apiClient.post('/cart/items', {
        menuMealId: menuMealId,
        quantity: 1
      });

      if (response.data.success) {
        alert('Added to cart successfully!');
        // Refresh menu to update available quantity
        await fetchTodaysMenu();
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to add to cart');
    } finally {
      setAddingToCart(null);
    }
  };

  if (loading) {
    return <div className="container"><div className="loading">Loading today's menu...</div></div>;
  }

  if (error) {
    return <div className="container"><div className="error-message">{error}</div></div>;
  }

  if (!menu || menu.menuMeals.length === 0) {
    return (
      <div className="container">
        <div className="todays-menu-header">
          <h1>Today's Menu</h1>
          <p className="menu-date">{new Date().toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}</p>
        </div>
        <div className="no-menu">No menu available for today</div>
      </div>
    );
  }

  return (
    <div className="container">
      <div className="todays-menu-header">
        <h1>Today's Menu</h1>
        <p className="menu-date">{new Date(menu.menuDate).toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}</p>
      </div>

      <div className="meals-grid">
        {menu.menuMeals.map((meal) => (
          <div key={meal.id} className="meal-card">
            <div className="meal-header">
              <h3 style={{ color: '#000' }}>{getMealTypeName(meal.mealTypeId)}</h3>
              <span className="meal-price" style={{ color: '#000' }}>{formatVND(meal.price)}</span>
            </div>
            <div className="meal-nutrition">
              <div className="nutrition-item">
                <span className="nutrition-label" style={{ color: '#666' }}>Calories</span>
                <span className="nutrition-value" style={{ color: '#000' }}>{meal.totalCalories.toFixed(0)} kcal</span>
              </div>
              <div className="nutrition-item">
                <span className="nutrition-label" style={{ color: '#666' }}>Protein</span>
                <span className="nutrition-value" style={{ color: '#000' }}>{meal.proteinG.toFixed(1)}g</span>
              </div>
              <div className="nutrition-item">
                <span className="nutrition-label" style={{ color: '#666' }}>Carbs</span>
                <span className="nutrition-value" style={{ color: '#000' }}>{meal.carbsG.toFixed(1)}g</span>
              </div>
              <div className="nutrition-item">
                <span className="nutrition-label" style={{ color: '#666' }}>Fat</span>
                <span className="nutrition-value" style={{ color: '#000' }}>{meal.fatG.toFixed(1)}g</span>
              </div>
            </div>
            <div className="meal-footer">
              <span className={`availability ${meal.availableQuantity > 0 ? 'in-stock' : 'out-of-stock'}`} style={{ color: meal.availableQuantity > 0 ? '#48bb78' : '#e53e3e' }}>
                {meal.availableQuantity > 0 ? `${meal.availableQuantity} available` : 'Out of stock'}
              </span>
              {isCustomer && (
                <button 
                  className="btn-primary" 
                  disabled={meal.availableQuantity === 0 || addingToCart === meal.id}
                  onClick={() => handleAddToCart(meal.id)}
                >
                  {addingToCart === meal.id ? 'Adding...' : 'Add to Cart'}
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default TodaysMenu;
