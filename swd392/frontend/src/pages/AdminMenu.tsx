import { useState, useEffect } from 'react';
import apiClient from '../config/api';
import './AdminCrud.css';

interface Recipe {
  id: string;
  recipeName: string;
  instructions: string;
}

interface MenuMeal {
  id: string;
  mealTypeId: number;
  price: number;
  availableQuantity: number;
  totalCalories: number;
  proteinG: number;
  fatG: number;
  carbsG: number;
  menuMealRecipes?: { recipe: Recipe }[];
}

interface DailyMenu {
  id: string;
  statusId: number;
  menuDate: string;
  menuMeals: MenuMeal[];
}

const AdminMenu = () => {
  const [items, setItems] = useState<DailyMenu[]>([]);
  const [filtered, setFiltered] = useState<DailyMenu[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showCreateMenuModal, setShowCreateMenuModal] = useState(false);
  const [showAddRecipesModal, setShowAddRecipesModal] = useState(false);
  const [showMealInfoModal, setShowMealInfoModal] = useState(false);
  const [showEditMealModal, setShowEditMealModal] = useState(false);
  const [selectedMenu, setSelectedMenu] = useState<DailyMenu | null>(null);
  const [selectedMeal, setSelectedMeal] = useState<MenuMeal | null>(null);
  const [recipes, setRecipes] = useState<Recipe[]>([]);
  const [recipeSearchTerm, setRecipeSearchTerm] = useState('');
  const [showAllRecipes, setShowAllRecipes] = useState(false);
  
  const [menuFormData, setMenuFormData] = useState({
    menuDate: new Date().toISOString().split('T')[0]
  });

  const [editMealFormData, setEditMealFormData] = useState({
    price: 0,
    availibleQuantity: 0
  });

  const [selectedRecipeIds, setSelectedRecipeIds] = useState<string[]>([]);

  useEffect(() => {
    fetchItems();
    fetchRecipes();
  }, []);

  useEffect(() => {
    const result = items.filter(item =>
      new Date(item.menuDate).toLocaleDateString().includes(searchTerm)
    );
    setFiltered(result);
  }, [searchTerm, items]);

  const getStatusName = (statusId: number) => {
    switch (statusId) {
      case 16: return 'Draft';
      case 17: return 'Active';
      case 18: return 'Inactive';
      default: return 'Unknown';
    }
  };

  const getStatusColor = (statusId: number) => {
    switch (statusId) {
      case 16: return '#ed8936'; // orange for draft
      case 17: return '#48bb78'; // green for active
      case 18: return '#e53e3e'; // red for inactive
      default: return '#999';
    }
  };

  const getMealTypeName = (mealTypeId: number) => {
    switch (mealTypeId) {
      case 1: return 'Breakfast';
      case 2: return 'Lunch';
      case 3: return 'Dinner';
      default: return 'Unknown';
    }
  };

  const fetchItems = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get('/dailymenus?includeAll=true');
      if (response.data.success) {
        setItems(response.data.data);
        setFiltered(response.data.data);
      }
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load daily menus');
    } finally {
      setLoading(false);
    }
  };

  const fetchRecipes = async () => {
    try {
      const response = await apiClient.get('/admin/recipes');
      if (response.data.success) {
        setRecipes(response.data.data);
      }
    } catch (err) {
      console.error('Failed to load recipes', err);
    }
  };

  // Step 1: Create empty menu
  const handleCreateMenu = () => {
    setMenuFormData({
      menuDate: new Date().toISOString().split('T')[0]
    });
    setShowCreateMenuModal(true);
  };

  const handleSubmitMenu = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const menuDate = new Date(menuFormData.menuDate).toISOString();
      const response = await apiClient.post('/dailymenus', {
        menuDate
      });
      
      if (response.data.success) {
        setShowCreateMenuModal(false);
        fetchItems();
        alert('Menu created successfully as Draft! Now you can add meals to it.');
      }
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to create menu');
    }
  };

  // Add recipes to meal
  const handleAddRecipes = (menu: DailyMenu, meal: MenuMeal) => {
    if (menu.statusId === 17) {
      alert('Cannot edit recipes while menu is active. Please deactivate the menu first.');
      return;
    }
    setSelectedMenu(menu);
    setSelectedMeal(meal);
    setSelectedRecipeIds(meal.menuMealRecipes?.map(mmr => mmr.recipe.id) || []);
    setRecipeSearchTerm('');
    setShowAllRecipes(false);
    setShowAddRecipesModal(true);
  };

  const handleSubmitRecipes = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedMenu || !selectedMeal) return;

    try {
      const response = await apiClient.post(
        `/dailymenus/${selectedMenu.id}/meals/${selectedMeal.id}/recipes`,
        { recipeIds: selectedRecipeIds }
      );
      
      if (response.data.success) {
        setShowAddRecipesModal(false);
        fetchItems();
        const message = selectedRecipeIds.length === 0 
          ? 'All recipes removed from meal' 
          : 'Recipes updated successfully!';
        alert(message);
      }
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to update recipes');
    }
  };

  // View meal info
  const handleViewMealInfo = (menu: DailyMenu, meal: MenuMeal) => {
    setSelectedMenu(menu);
    setSelectedMeal(meal);
    setShowMealInfoModal(true);
  };

  // Edit meal details
  const handleEditMeal = (menu: DailyMenu, meal: MenuMeal) => {
    if (menu.statusId === 17) {
      alert('Cannot edit meal details while menu is active. Please deactivate the menu first.');
      return;
    }
    setSelectedMenu(menu);
    setSelectedMeal(meal);
    setEditMealFormData({
      price: meal.price,
      availibleQuantity: meal.availableQuantity
    });
    setShowEditMealModal(true);
  };

  const handleSubmitEditMeal = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedMenu || !selectedMeal) return;

    console.log('Submitting meal update with price:', editMealFormData.price);

    try {
      const response = await apiClient.patch(
        `/dailymenus/${selectedMenu.id}/meals/${selectedMeal.id}`,
        editMealFormData
      );
      
      if (response.data.success) {
        setShowEditMealModal(false);
        fetchItems();
        alert('Meal details updated successfully!');
      }
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to update meal details');
    }
  };

  const handleUpdateStatus = async (menuId: string, newStatusId: number) => {
    try {
      const response = await apiClient.patch(`/dailymenus/${menuId}/status`, {
        statusId: newStatusId
      });
      
      if (response.data.success) {
        fetchItems();
        alert('Menu status updated successfully!');
      }
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to update status');
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this daily menu?')) return;
    try {
      await apiClient.delete(`/dailymenus/${id}`);
      fetchItems();
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to delete');
    }
  };

  const toggleRecipe = (recipeId: string) => {
    if (selectedRecipeIds.includes(recipeId)) {
      setSelectedRecipeIds(selectedRecipeIds.filter(id => id !== recipeId));
    } else {
      setSelectedRecipeIds([...selectedRecipeIds, recipeId]);
    }
  };

  const getFilteredRecipes = () => {
    if (!recipeSearchTerm.trim()) {
      return [];
    }
    return recipes.filter(recipe =>
      recipe.recipeName.toLowerCase().includes(recipeSearchTerm.toLowerCase())
    );
  };

  const getDisplayedRecipes = () => {
    if (showAllRecipes) {
      return recipes;
    }
    return getFilteredRecipes();
  };

  const toggleShowAll = () => {
    setShowAllRecipes(!showAllRecipes);
    if (!showAllRecipes) {
      setRecipeSearchTerm('');
    }
  };

  const clearRecipeSearch = () => {
    setRecipeSearchTerm('');
  };

  const getSelectedRecipes = () => {
    return recipes.filter(recipe => selectedRecipeIds.includes(recipe.id));
  };

  if (loading) return <div className="container"><div className="loading">Loading...</div></div>;

  return (
    <div className="container">
      <div className="crud-header">
        <h1>Daily Menu Management</h1>
        <div className="crud-actions">
          <input
            type="text"
            placeholder="Search by date..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="search-input"
          />
          <button onClick={handleCreateMenu} className="btn-primary">Create New Menu</button>
        </div>
      </div>

      {error && <div className="error-message">{error}</div>}

      <div className="crud-table-container">
        <table className="crud-table">
          <thead>
            <tr>
              <th>Date</th>
              <th>Status</th>
              <th>Meals</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((item) => (
              <tr key={item.id}>
                <td>{new Date(item.menuDate).toLocaleDateString()}</td>
                <td>
                  <span style={{ 
                    color: getStatusColor(item.statusId),
                    fontWeight: 'bold'
                  }}>
                    {getStatusName(item.statusId)}
                  </span>
                  {item.statusId === 16 && item.menuMeals.length > 0 && (
                    <button 
                      onClick={() => handleUpdateStatus(item.id, 17)} 
                      className="btn-success"
                      style={{ marginLeft: '0.5rem', padding: '0.25rem 0.5rem', fontSize: '0.75rem' }}
                      title="Activate menu"
                    >
                      Activate
                    </button>
                  )}
                  {item.statusId === 17 && (
                    <button 
                      onClick={() => handleUpdateStatus(item.id, 18)} 
                      className="btn-warning"
                      style={{ marginLeft: '0.5rem', padding: '0.25rem 0.5rem', fontSize: '0.75rem' }}
                      title="Deactivate menu"
                    >
                      Deactivate
                    </button>
                  )}
                  {item.statusId === 18 && (
                    <button 
                      onClick={() => handleUpdateStatus(item.id, 17)} 
                      className="btn-success"
                      style={{ marginLeft: '0.5rem', padding: '0.25rem 0.5rem', fontSize: '0.75rem' }}
                      title="Activate menu"
                    >
                      Activate
                    </button>
                  )}
                </td>
                <td>
                  {item.menuMeals.length === 0 ? (
                    <span style={{ color: '#999' }}>No meals added</span>
                  ) : (
                    item.menuMeals.map(meal => (
                      <div key={meal.id} style={{ marginBottom: '0.5rem', padding: '0.5rem', background: '#f7fafc', borderRadius: '6px' }}>
                        <div style={{ marginBottom: '0.25rem' }}>
                          <strong>{getMealTypeName(meal.mealTypeId)}</strong>: {meal.price.toLocaleString('vi-VN')} VND ({meal.availableQuantity} available)
                          {meal.menuMealRecipes && meal.menuMealRecipes.length > 0 && (
                            <span style={{ marginLeft: '0.5rem', color: '#48bb78' }}>
                              ✓ {meal.menuMealRecipes.length} recipes
                            </span>
                          )}
                        </div>
                        {meal.menuMealRecipes && meal.menuMealRecipes.length > 0 && (
                          <div style={{ fontSize: '0.75rem', color: '#666', marginBottom: '0.25rem' }}>
                            <span style={{ marginRight: '0.75rem' }}>🔥 {meal.totalCalories.toFixed(0)} cal</span>
                            <span style={{ marginRight: '0.75rem' }}>💪 {meal.proteinG.toFixed(1)}g protein</span>
                            <span style={{ marginRight: '0.75rem' }}>🥑 {meal.fatG.toFixed(1)}g fat</span>
                            <span>🍞 {meal.carbsG.toFixed(1)}g carbs</span>
                          </div>
                        )}
                        <button 
                          onClick={() => handleViewMealInfo(item, meal)} 
                          className="btn-edit"
                          style={{ marginLeft: '0rem', padding: '0.25rem 0.5rem', fontSize: '0.75rem' }}
                          title="View meal info"
                        >
                          Info
                        </button>
                        <button 
                          onClick={() => handleEditMeal(item, meal)} 
                          className="btn-edit"
                          style={{ marginLeft: '0.5rem', padding: '0.25rem 0.5rem', fontSize: '0.75rem' }}
                          title={item.statusId === 17 ? 'Deactivate menu to edit' : 'Edit price and quantity'}
                          disabled={item.statusId === 17}
                        >
                          Edit
                        </button>
                        <button 
                          onClick={() => handleAddRecipes(item, meal)} 
                          className="btn-edit"
                          style={{ marginLeft: '0.5rem', padding: '0.25rem 0.5rem', fontSize: '0.75rem' }}
                          title={item.statusId === 17 ? 'Deactivate menu to edit recipes' : (meal.menuMealRecipes && meal.menuMealRecipes.length > 0 ? 'Edit recipes' : 'Add recipes')}
                          disabled={item.statusId === 17}
                        >
                          {meal.menuMealRecipes && meal.menuMealRecipes.length > 0 ? 'Edit Recipes' : 'Add Recipes'}
                        </button>
                      </div>
                    ))
                  )}
                </td>
                <td>
                  <button onClick={() => handleDelete(item.id)} className="btn-delete">Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Step 1: Create Menu Modal */}
      {showCreateMenuModal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <h2>Create New Menu</h2>
            <form onSubmit={handleSubmitMenu}>
              <div className="form-group">
                <label>Menu Date *</label>
                <input
                  type="date"
                  value={menuFormData.menuDate}
                  onChange={(e) => setMenuFormData({ ...menuFormData, menuDate: e.target.value })}
                  required
                />
              </div>
              
              <p style={{ color: '#666', fontSize: '0.875rem', marginTop: '1rem' }}>
                Note: Menu will be created as <strong>Draft</strong> with three meals (Breakfast, Lunch, Dinner). You can add recipes and set prices for each meal.
              </p>

              <div className="modal-actions">
                <button type="button" onClick={() => setShowCreateMenuModal(false)} className="btn-secondary">Cancel</button>
                <button type="submit" className="btn-primary">Create Menu</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Add Recipes Modal */}
      {showAddRecipesModal && selectedMenu && selectedMeal && (
        <div className="modal-overlay">
          <div className="modal-content modal-large">
            <h2>Edit Recipes for {getMealTypeName(selectedMeal.mealTypeId)}</h2>
            <p style={{ color: '#666', marginBottom: '1rem' }}>
              Menu Date: {new Date(selectedMenu.menuDate).toLocaleDateString()} | 
              Current: {selectedMeal.menuMealRecipes?.length || 0} recipe(s)
            </p>
            <p style={{ color: '#999', fontSize: '0.875rem', marginBottom: '1rem' }}>
              Tip: Remove all recipes to clear the meal, or select new recipes to replace existing ones.
            </p>
            <form onSubmit={handleSubmitRecipes}>
              <div className="form-group">
                <label>Search Recipes</label>
                <div className="search-with-clear">
                  <input
                    type="text"
                    placeholder="Type to search recipes..."
                    value={recipeSearchTerm}
                    onChange={(e) => setRecipeSearchTerm(e.target.value)}
                    className="recipe-search-input"
                  />
                  {recipeSearchTerm && (
                    <button
                      type="button"
                      onClick={clearRecipeSearch}
                      className="clear-search-btn"
                      title="Clear search"
                    >
                      ✕
                    </button>
                  )}
                </div>
                <button
                  type="button"
                  onClick={toggleShowAll}
                  className="btn-show-all"
                >
                  {showAllRecipes ? 'Hide All' : 'Show All Recipes'}
                </button>
              </div>

              {/* Show search results or all recipes */}
              {(recipeSearchTerm || showAllRecipes) && getDisplayedRecipes().length > 0 && (
                <div className="form-group">
                  <label>
                    {showAllRecipes 
                      ? `All Recipes (${recipes.length})` 
                      : `Search Results (${getDisplayedRecipes().length})`}
                  </label>
                  <div className="recipe-search-results">
                    {getDisplayedRecipes().map(recipe => (
                      <label key={recipe.id} className="checkbox-label">
                        <input
                          type="checkbox"
                          checked={selectedRecipeIds.includes(recipe.id)}
                          onChange={() => toggleRecipe(recipe.id)}
                        />
                        {recipe.recipeName}
                      </label>
                    ))}
                  </div>
                </div>
              )}

              {/* Show selected recipes */}
              {getSelectedRecipes().length > 0 && (
                <div className="form-group">
                  <label>Selected Recipes ({getSelectedRecipes().length})</label>
                  <div className="selected-recipes">
                    {getSelectedRecipes().map(recipe => (
                      <div key={recipe.id} className="selected-recipe-item">
                        <span>{recipe.recipeName}</span>
                        <button
                          type="button"
                          onClick={() => toggleRecipe(recipe.id)}
                          className="remove-recipe-btn"
                          title="Remove recipe"
                        >
                          ✕
                        </button>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {recipeSearchTerm && !showAllRecipes && getDisplayedRecipes().length === 0 && (
                <div className="no-results">No recipes found matching "{recipeSearchTerm}"</div>
              )}

              <div className="modal-actions">
                <button type="button" onClick={() => setShowAddRecipesModal(false)} className="btn-secondary">Cancel</button>
                <button type="submit" className="btn-primary">Save Recipes</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Edit Meal Details Modal */}
      {showEditMealModal && selectedMenu && selectedMeal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <h2>Edit Meal Details</h2>
            <p style={{ color: '#666', marginBottom: '1rem' }}>
              Menu Date: {new Date(selectedMenu.menuDate).toLocaleDateString()} | 
              Meal: {getMealTypeName(selectedMeal.mealTypeId)}
            </p>
            <form onSubmit={handleSubmitEditMeal}>
              <div className="form-row">
                <div className="form-group">
                  <label>Price *</label>
                  <input
                    type="number"
                    step="1"
                    value={editMealFormData.price}
                    onChange={(e) => setEditMealFormData({ ...editMealFormData, price: parseFloat(e.target.value) || 0 })}
                    required
                    min="0"
                  />
                </div>
                <div className="form-group">
                  <label>Available Quantity *</label>
                  <input
                    type="number"
                    value={editMealFormData.availibleQuantity}
                    onChange={(e) => setEditMealFormData({ ...editMealFormData, availibleQuantity: parseInt(e.target.value) || 0 })}
                    required
                    min="0"
                  />
                </div>
              </div>

              <div className="modal-actions">
                <button type="button" onClick={() => setShowEditMealModal(false)} className="btn-secondary">Cancel</button>
                <button type="submit" className="btn-primary">Save Changes</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Meal Info Modal */}
      {showMealInfoModal && selectedMenu && selectedMeal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <h2>Meal Information</h2>
            <div style={{ marginBottom: '1.5rem' }}>
              <p><strong>Menu Date:</strong> {new Date(selectedMenu.menuDate).toLocaleDateString()}</p>
              <p><strong>Meal Type:</strong> {getMealTypeName(selectedMeal.mealTypeId)}</p>
              <p><strong>Price:</strong> {selectedMeal.price.toLocaleString('vi-VN')} VND</p>
              <p><strong>Available Quantity:</strong> {selectedMeal.availableQuantity}</p>
              
              {selectedMeal.menuMealRecipes && selectedMeal.menuMealRecipes.length > 0 && (
                <div style={{ 
                  marginTop: '1rem', 
                  padding: '0.75rem', 
                  background: '#f7fafc', 
                  borderRadius: '6px',
                  border: '1px solid #e2e8f0'
                }}>
                  <strong>Nutritional Information:</strong>
                  <div style={{ marginTop: '0.5rem', display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem' }}>
                    <div>🔥 Calories: <strong>{selectedMeal.totalCalories.toFixed(0)} cal</strong></div>
                    <div>💪 Protein: <strong>{selectedMeal.proteinG.toFixed(1)}g</strong></div>
                    <div>🥑 Fat: <strong>{selectedMeal.fatG.toFixed(1)}g</strong></div>
                    <div>🍞 Carbs: <strong>{selectedMeal.carbsG.toFixed(1)}g</strong></div>
                  </div>
                </div>
              )}
            </div>

            <div>
              <h3 style={{ marginBottom: '1rem' }}>Recipes ({selectedMeal.menuMealRecipes?.length || 0})</h3>
              {selectedMeal.menuMealRecipes && selectedMeal.menuMealRecipes.length > 0 ? (
                <div style={{ 
                  maxHeight: '300px', 
                  overflowY: 'auto',
                  border: '1px solid #e2e8f0',
                  borderRadius: '8px',
                  padding: '1rem'
                }}>
                  {selectedMeal.menuMealRecipes.map((mmr, index) => (
                    <div 
                      key={mmr.recipe.id} 
                      style={{ 
                        padding: '0.75rem',
                        marginBottom: '0.5rem',
                        background: '#f7fafc',
                        borderRadius: '6px',
                        borderLeft: '3px solid #4299e1'
                      }}
                    >
                      <div style={{ fontWeight: 'bold', marginBottom: '0.25rem' }}>
                        {index + 1}. {mmr.recipe.recipeName}
                      </div>
                      {mmr.recipe.instructions && (
                        <div style={{ 
                          fontSize: '0.875rem', 
                          color: '#666',
                          marginTop: '0.5rem',
                          whiteSpace: 'pre-wrap'
                        }}>
                          {mmr.recipe.instructions.length > 150 
                            ? mmr.recipe.instructions.substring(0, 150) + '...' 
                            : mmr.recipe.instructions}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              ) : (
                <div style={{ 
                  padding: '2rem',
                  textAlign: 'center',
                  color: '#999',
                  background: '#f7fafc',
                  borderRadius: '8px'
                }}>
                  No recipes added to this meal yet.
                </div>
              )}
            </div>

            <div className="modal-actions" style={{ marginTop: '1.5rem' }}>
              <button 
                type="button" 
                onClick={() => setShowMealInfoModal(false)} 
                className="btn-secondary"
              >
                Close
              </button>
              <button 
                type="button" 
                onClick={() => {
                  setShowMealInfoModal(false);
                  if (selectedMenu.statusId === 17) {
                    alert('Cannot edit recipes while menu is active. Please deactivate the menu first.');
                    return;
                  }
                  handleAddRecipes(selectedMenu, selectedMeal);
                }} 
                className="btn-primary"
                disabled={selectedMenu.statusId === 17}
                title={selectedMenu.statusId === 17 ? 'Deactivate menu to edit recipes' : ''}
              >
                {selectedMeal.menuMealRecipes && selectedMeal.menuMealRecipes.length > 0 ? 'Edit Recipes' : 'Add Recipes'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminMenu;
