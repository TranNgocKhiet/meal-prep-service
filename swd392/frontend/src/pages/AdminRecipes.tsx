import { useState, useEffect } from 'react';
import apiClient from '../config/api';
import './AdminCrud.css';

interface Recipe {
  id: string;
  recipeName: string;
  instructions: string;
  createdAt?: string;
  updatedAt?: string;
}

interface Ingredient {
  id: string;
  name: string;
  unit: string;
}

interface RecipeIngredient {
  id: string;
  ingredientId: string;
  ingredientName: string;
  ingredientUnit: string;
  amount: string;
}

const AdminRecipes = () => {
  const [items, setItems] = useState<Recipe[]>([]);
  const [filtered, setFiltered] = useState<Recipe[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editingItem, setEditingItem] = useState<Recipe | null>(null);
  const [ingredients, setIngredients] = useState<Ingredient[]>([]);
  const [recipeIngredients, setRecipeIngredients] = useState<RecipeIngredient[]>([]);
  const [ingredientsLoading, setIngredientsLoading] = useState(false);
  const [ingredientsError, setIngredientsError] = useState('');
  const [newIngredientId, setNewIngredientId] = useState('');
  const [newIngredientQuery, setNewIngredientQuery] = useState('');
  const [newIngredientAmount, setNewIngredientAmount] = useState<string>('');
  const [ingredientToDeleteId, setIngredientToDeleteId] = useState<string | null>(null);
  const [ingredientDeleteLoading, setIngredientDeleteLoading] = useState(false);
  const [formData, setFormData] = useState<Partial<Recipe>>({
    recipeName: '',
    instructions: ''
  });

  useEffect(() => {
    fetchItems();
  }, []);

  useEffect(() => {
    const result = items.filter(item =>
      item.recipeName.toLowerCase().includes(searchTerm.toLowerCase())
    );
    setFiltered(result);
  }, [searchTerm, items]);

  const fetchItems = async () => {
    try {
      setLoading(true);
      setError('');
      const response = await apiClient.get('/admin/recipes');
      if (response.data.success) {
        setItems(response.data.data);
        setFiltered(response.data.data);
      } else {
        setError(response.data.message || 'Failed to load recipes');
      }
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load recipes');
      console.error('Error fetching recipes:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setEditingItem(null);
    setFormData({
      recipeName: '',
      instructions: ''
    });
    setRecipeIngredients([]);
    setNewIngredientId('');
    setNewIngredientQuery('');
    setNewIngredientAmount('');
    setShowModal(true);
  };

  const handleEdit = (item: Recipe) => {
    setEditingItem(item);
    setFormData({
      recipeName: item.recipeName,
      instructions: item.instructions
    });
    setNewIngredientId('');
    setNewIngredientQuery('');
    setNewIngredientAmount('');
    fetchIngredients();
    fetchRecipeIngredients(item.id);
    setShowModal(true);
  };

  const handleIngredientQueryChange = (value: string) => {
    setNewIngredientQuery(value);

    const matchedIngredient = ingredients
      .filter(i => !recipeIngredients.some(ri => ri.ingredientId === i.id))
      .find(i => i.name.toLowerCase() === value.trim().toLowerCase());

    setNewIngredientId(matchedIngredient?.id || '');
  };

  const fetchIngredients = async () => {
    try {
      setIngredientsLoading(true);
      setIngredientsError('');
      const response = await apiClient.get('/admin/ingredients');
      if (response.data.success) {
        setIngredients(response.data.data);
      } else {
        setIngredientsError(response.data.message || 'Failed to load ingredients');
      }
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setIngredientsError(error.response?.data?.message || 'Failed to load ingredients');
    } finally {
      setIngredientsLoading(false);
    }
  };

  const fetchRecipeIngredients = async (recipeId: string) => {
    try {
      setIngredientsLoading(true);
      setIngredientsError('');
      const response = await apiClient.get(`/admin/recipes/${recipeId}/ingredients`);
      if (response.data.success) {
        setRecipeIngredients(
          (response.data.data as RecipeIngredient[]).map(ri => ({
            ...ri,
            amount: ri.amount != null ? String(ri.amount) : ''
          }))
        );
      } else {
        setIngredientsError(response.data.message || 'Failed to load recipe ingredients');
      }
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setIngredientsError(error.response?.data?.message || 'Failed to load recipe ingredients');
    } finally {
      setIngredientsLoading(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this recipe?')) return;
    try {
      const response = await apiClient.delete(`/admin/recipes/${id}`);
      if (response.data.success) {
        fetchItems();
      } else {
        alert(response.data.message || 'Failed to delete recipe');
      }
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to delete recipe');
      console.error('Error deleting recipe:', err);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!formData.recipeName || !formData.instructions) {
      alert('Please fill in all required fields');
      return;
    }

    try {
      if (editingItem) {
        const response = await apiClient.put(`/admin/recipes/${editingItem.id}`, formData);
        if (response.data.success) {
          setShowModal(false);
          fetchItems();
        } else {
          alert(response.data.message || 'Failed to update recipe');
        }
      } else {
        const response = await apiClient.post('/admin/recipes', formData);
        if (response.data.success) {
          setShowModal(false);
          fetchItems();
        } else {
          alert(response.data.message || 'Failed to create recipe');
        }
      }
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to save recipe');
      console.error('Error saving recipe:', err);
    }
  };

  const handleIngredientAmountChange = (id: string, amount: string) => {
    const inputElement = document.getElementById(`ingredient-amount-${id}`) as HTMLInputElement | null;
    if (inputElement) {
      inputElement.setCustomValidity('');
    }

    setRecipeIngredients(prev =>
      prev.map(ri => ri.id === id ? { ...ri, amount } : ri)
    );
  };

  const handleSaveIngredient = async (ingredient: RecipeIngredient) => {
    if (!editingItem) return;

    const amountInput = document.getElementById(`ingredient-amount-${ingredient.id}`) as HTMLInputElement | null;
    const amountText = ingredient.amount.trim();

    if (!amountText) {
      if (amountInput) {
        amountInput.setCustomValidity('Please enter an amount');
        amountInput.reportValidity();
      }
      return;
    }

    const amountValue = parseFloat(ingredient.amount);
    if (isNaN(amountValue) || amountValue <= 0) {
      if (amountInput) {
        amountInput.setCustomValidity('Amount must be greater than 0');
        amountInput.reportValidity();
      }
      return;
    }

    if (amountInput) {
      amountInput.setCustomValidity('');
    }

    try {
      const response = await apiClient.put(`/admin/recipes/${editingItem.id}/ingredients/${ingredient.id}`, {
        amount: amountValue
      });
      if (!response.data.success) {
        alert(response.data.message || 'Failed to update ingredient');
        return;
      }
      fetchRecipeIngredients(editingItem.id);
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to update ingredient');
    }
  };

  const handleDeleteIngredient = async (id: string) => {
    // Open custom confirmation dialog instead of browser confirm
    setIngredientToDeleteId(id);
  };

  const confirmDeleteIngredient = async () => {
    if (!editingItem || !ingredientToDeleteId) return;

    try {
      setIngredientDeleteLoading(true);
      const response = await apiClient.delete(`/admin/recipes/${editingItem.id}/ingredients/${ingredientToDeleteId}`);
      if (!response.data.success) {
        alert(response.data.message || 'Failed to delete ingredient from recipe');
        return;
      }
      setIngredientToDeleteId(null);
      fetchRecipeIngredients(editingItem.id);
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to delete ingredient from recipe');
    } finally {
      setIngredientDeleteLoading(false);
    }
  };

  const cancelDeleteIngredient = () => {
    if (ingredientDeleteLoading) return;
    setIngredientToDeleteId(null);
  };

  const handleAddIngredient = async () => {
    if (!editingItem) return;
    if (!newIngredientId || !newIngredientAmount) {
      alert('Please select an ingredient and enter an amount');
      return;
    }

    const amountValue = parseFloat(newIngredientAmount);
    if (isNaN(amountValue) || amountValue <= 0) {
      alert('Amount must be greater than 0');
      return;
    }

    try {
      const response = await apiClient.post(`/admin/recipes/${editingItem.id}/ingredients`, {
        ingredientId: newIngredientId,
        amount: amountValue
      });
      if (!response.data.success) {
        alert(response.data.message || 'Failed to add ingredient to recipe');
        return;
      }
      setNewIngredientId('');
      setNewIngredientQuery('');
      setNewIngredientAmount('');
      fetchRecipeIngredients(editingItem.id);
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to add ingredient to recipe');
    }
  };

  if (loading) return <div className="container"><div className="loading">Loading recipes...</div></div>;

  return (
    <div className="container">
      <div className="crud-header">
        <h1 style={{ color: '#fff' }}>Recipes Management</h1>
        <div className="crud-actions">
          <input
            type="text"
            placeholder="Search recipes..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="search-input"
          />
          <button onClick={handleCreate} className="btn-primary">Add Recipe</button>
        </div>
      </div>

      {error && <div className="error-message">{error}</div>}

      <div className="crud-table-container">
        <table className="crud-table">
          <thead>
            <tr>
              <th>Recipe Name</th>
              <th>Instructions</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {filtered.length === 0 ? (
              <tr>
                <td colSpan={3} style={{ textAlign: 'center', padding: '20px' }}>
                  {searchTerm ? 'No recipes found matching your search' : 'No recipes available. Click "Add Recipe" to create one.'}
                </td>
              </tr>
            ) : (
              filtered.map((item) => (
                <tr key={item.id}>
                  <td>{item.recipeName}</td>
                  <td>{item.instructions.substring(0, 100)}{item.instructions.length > 100 ? '...' : ''}</td>
                  <td>
                    <button onClick={() => handleEdit(item)} className="btn-edit">Edit</button>
                    <button onClick={() => handleDelete(item.id)} className="btn-delete">Delete</button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {showModal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <h2>{editingItem ? 'Edit Recipe' : 'Add Recipe'}</h2>
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label>Recipe Name *</label>
                <input 
                  type="text" 
                  value={formData.recipeName || ''} 
                  onChange={(e) => setFormData({ ...formData, recipeName: e.target.value })} 
                  required 
                  minLength={3}
                  maxLength={200}
                  placeholder="Enter recipe name"
                />
              </div>
              <div className="form-group">
                <label>Instructions *</label>
                <textarea 
                  className="recipe-instructions-textarea"
                  value={formData.instructions || ''} 
                  onChange={(e) => setFormData({ ...formData, instructions: e.target.value })} 
                  required 
                  rows={10}
                  minLength={10}
                  maxLength={2000}
                  placeholder="Enter cooking instructions"
                />
              </div>
              {editingItem && (
                <div className="form-group">
                  <label>Recipe Ingredients</label>
                  {ingredientsError && <div className="error-message">{ingredientsError}</div>}
                  {ingredientsLoading && <div className="loading">Loading ingredients...</div>}
                  {!ingredientsLoading && (
                    <div className="crud-table-container">
                      <table className="crud-table">
                        <thead>
                          <tr>
                            <th>Ingredient</th>
                            <th>Unit</th>
                            <th>Amount</th>
                            <th>Actions</th>
                          </tr>
                        </thead>
                        <tbody>
                          {recipeIngredients.length === 0 && (
                            <tr>
                              <td colSpan={4} style={{ textAlign: 'center', padding: '10px' }}>
                                No ingredients for this recipe yet.
                              </td>
                            </tr>
                          )}
                          {recipeIngredients.map((ri) => (
                            <tr key={ri.id}>
                              <td>{ri.ingredientName}</td>
                              <td>{ri.ingredientUnit}</td>
                              <td>
                                <input
                                  id={`ingredient-amount-${ri.id}`}
                                  type="number"
                                  min={0.01}
                                  step={0.01}
                                  value={ri.amount}
                                  onChange={(e) => handleIngredientAmountChange(ri.id, e.target.value)}
                                  style={{ width: '100%' }}
                                />
                              </td>
                              <td>
                                <button
                                  type="button"
                                  className="btn-edit"
                                  onClick={() => handleSaveIngredient(ri)}
                                >
                                  Save
                                </button>
                                <button
                                  type="button"
                                  className="btn-delete"
                                  onClick={() => handleDeleteIngredient(ri.id)}
                                  style={{ marginLeft: '8px' }}
                                >
                                  Remove
                                </button>
                              </td>
                            </tr>
                          ))}
                          <tr>
                            <td>
                              <input
                                list="available-ingredients"
                                value={newIngredientQuery}
                                onChange={(e) => handleIngredientQueryChange(e.target.value)}
                                placeholder="Select or search ingredient..."
                                style={{ width: '100%' }}
                              />
                              <datalist id="available-ingredients">
                                {ingredients
                                  .filter(i => !recipeIngredients.some(ri => ri.ingredientId === i.id))
                                  .map(i => (
                                    <option key={i.id} value={i.name} />
                                  ))}
                              </datalist>
                            </td>
                            <td>
                              {newIngredientId && (
                                ingredients.find(i => i.id === newIngredientId)?.unit || ''
                              )}
                            </td>
                            <td>
                              <input
                                type="number"
                                min={0.01}
                                step={0.01}
                                value={newIngredientAmount}
                                onChange={(e) => setNewIngredientAmount(e.target.value)}
                                style={{ width: '100%' }}
                              />
                            </td>
                            <td>
                              <button
                                type="button"
                                className="btn-primary"
                                onClick={handleAddIngredient}
                              >
                                Add
                              </button>
                            </td>
                          </tr>
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>
              )}
              <div className="modal-actions">
                <button type="button" onClick={() => setShowModal(false)} className="btn-secondary">Cancel</button>
                <button type="submit" className="btn-primary">Save</button>
              </div>
            </form>
          </div>
        </div>
      )}
      {ingredientToDeleteId && (
        <div className="modal-overlay">
          <div className="modal-content confirm-modal">
            <h2>Confirm removal</h2>
            <p>Are you sure you want to remove this ingredient from the recipe?</p>
            <div className="modal-actions">
              <button
                type="button"
                className="btn-secondary"
                onClick={cancelDeleteIngredient}
                disabled={ingredientDeleteLoading}
              >
                Cancel
              </button>
              <button
                type="button"
                className="btn-delete"
                onClick={confirmDeleteIngredient}
                disabled={ingredientDeleteLoading}
              >
                {ingredientDeleteLoading ? 'Removing...' : 'Remove'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminRecipes;

