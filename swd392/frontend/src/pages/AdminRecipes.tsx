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

const AdminRecipes = () => {
  const [items, setItems] = useState<Recipe[]>([]);
  const [filtered, setFiltered] = useState<Recipe[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editingItem, setEditingItem] = useState<Recipe | null>(null);
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
    setShowModal(true);
  };

  const handleEdit = (item: Recipe) => {
    setEditingItem(item);
    setFormData({
      recipeName: item.recipeName,
      instructions: item.instructions
    });
    setShowModal(true);
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

  if (loading) return <div className="container"><div className="loading">Loading recipes...</div></div>;

  return (
    <div className="container">
      <div className="crud-header">
        <h1>Recipes Management</h1>
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
                  value={formData.instructions || ''} 
                  onChange={(e) => setFormData({ ...formData, instructions: e.target.value })} 
                  required 
                  rows={10}
                  minLength={10}
                  maxLength={2000}
                  placeholder="Enter cooking instructions"
                />
              </div>
              <div className="modal-actions">
                <button type="button" onClick={() => setShowModal(false)} className="btn-secondary">Cancel</button>
                <button type="submit" className="btn-primary">Save</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminRecipes;

