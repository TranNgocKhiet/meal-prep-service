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

const getLocalDateString = (date: Date = new Date()) => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const getDatePart = (value: string) => {
  return value.split('T')[0];
};

const formatApiDateForDisplay = (value: string) => {
  const datePart = getDatePart(value);
  const [year, month, day] = datePart.split('-').map(Number);
  const localDate = new Date(year, (month || 1) - 1, day || 1);
  return localDate.toLocaleDateString('en-US', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  });
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

const TodaysMenu = () => {
  const [menu, setMenu] = useState<DailyMenu | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [addingToCart, setAddingToCart] = useState<string | null>(null);
  const [showAddSuccessModal, setShowAddSuccessModal] = useState(false);
  const { user } = useAuth();
  const normalizedRole = (user?.roleName || getRoleFromToken()).trim().toLowerCase();
  const isCustomer = normalizedRole === 'customer';

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

  const getRecipeNames = (meal: MenuMeal): string[] => {
    return (meal.menuMealRecipes || [])
      .map((item) => item.recipe?.recipeName || item.recipe?.name)
      .filter((name): name is string => Boolean(name && name.trim()));
  };

  const fetchTodaysMenu = async () => {
    try {
      setLoading(true);
      const today = getLocalDateString();
      const response = await apiClient.get(`/dailymenus?date=${today}`);
      
      if (response.data.success && response.data.data.length > 0) {
        // Merge all menus for the same day into a single menu with all meals
        const menus = response.data.data as DailyMenu[];
        const mergedMenu: DailyMenu = {
          id: menus[0].id,
          menuDate: menus[0].menuDate,
          statusId: menus[0].statusId,
          menuMeals: menus.flatMap(m => m.menuMeals)
        };
        setMenu(mergedMenu);
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
        setShowAddSuccessModal(true);
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

  const visibleMeals = menu
    ? menu.menuMeals
      .filter((meal) => getRecipeNames(meal).length > 0)
      .sort((a, b) => a.mealTypeId - b.mealTypeId)
    : [];

  if (!menu || visibleMeals.length === 0) {
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
        <p className="menu-date">{formatApiDateForDisplay(menu.menuDate)}</p>
      </div>

      <div className="meals-grid">
        {visibleMeals.map((meal) => {
          const recipeNames = getRecipeNames(meal);

          return (
          <div key={meal.id} className="meal-card">
            <div className="meal-header">
              <h3 style={{ color: '#000' }}>{getMealTypeName(meal.mealTypeId)}</h3>
              <span className="meal-price" style={{ color: '#000' }}>{formatVND(meal.price)}</span>
            </div>
            <div className="meal-recipes-list">
              {recipeNames.length > 0 ? (
                recipeNames.map((recipeName, index) => (
                  <span key={`${meal.id}-recipe-${index}`} className="recipe-chip" title={recipeName}>
                    {recipeName}
                  </span>
                ))
              ) : (
                <span className="no-recipe-chip">No recipes assigned</span>
              )}
            </div>
            <div className="meal-nutrition">
              <div className="nutrition-item">
                <span className="nutrition-label" style={{ color: '#ffffff' }}>Calories</span>
                <span className="nutrition-value" style={{ color: '#ffffff' }}>{meal.totalCalories.toFixed(0)} kcal</span>
              </div>
              <div className="nutrition-item">
                <span className="nutrition-label" style={{ color: '#ffffff' }}>Protein</span>
                <span className="nutrition-value" style={{ color: '#ffffff' }}>{meal.proteinG.toFixed(1)}g</span>
              </div>
              <div className="nutrition-item">
                <span className="nutrition-label" style={{ color: '#ffffff' }}>Carbs</span>
                <span className="nutrition-value" style={{ color: '#ffffff' }}>{meal.carbsG.toFixed(1)}g</span>
              </div>
              <div className="nutrition-item">
                <span className="nutrition-label" style={{ color: '#ffffff' }}>Fat</span>
                <span className="nutrition-value" style={{ color: '#ffffff' }}>{meal.fatG.toFixed(1)}g</span>
              </div>
            </div>
            <div className="meal-footer">
              <span className={`availability ${meal.availableQuantity > 0 ? 'in-stock' : 'out-of-stock'}`} style={{ color: meal.availableQuantity > 0 ? '#48bb78' : '#e53e3e' }}>
                {meal.availableQuantity > 0 ? `${meal.availableQuantity} available` : 'Out of stock'}
              </span>
              {isCustomer && (
                <button 
                  className="btn" 
                  disabled={meal.availableQuantity === 0 || addingToCart === meal.id}
                  onClick={() => handleAddToCart(meal.id)}
                >
                  {addingToCart === meal.id ? 'Adding...' : 'Add to Cart'}
                </button>
              )}
            </div>
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

export default TodaysMenu;
