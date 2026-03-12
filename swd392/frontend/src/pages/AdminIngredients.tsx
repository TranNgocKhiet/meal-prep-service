import { useState, useEffect } from 'react';
import apiClient from '../config/api';
import './AdminCrud.css';

interface Ingredient {
  id: string;
  name: string;
  unit: string;
  imageUrl: string;
}

const AdminIngredients = () => {
  const [ingredients, setIngredients] = useState<Ingredient[]>([]);
  const [filteredIngredients, setFilteredIngredients] = useState<Ingredient[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editingItem, setEditingItem] = useState<Ingredient | null>(null);
  const [formData, setFormData] = useState<Partial<Ingredient>>({
    name: '',
    unit: '',
    imageUrl: ''
  });

  useEffect(() => {
    fetchIngredients();
  }, []);

  useEffect(() => {
    const filtered = ingredients.filter(item =>
      item.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      item.unit.toLowerCase().includes(searchTerm.toLowerCase())
    );
    setFilteredIngredients(filtered);
  }, [searchTerm, ingredients]);

  const fetchIngredients = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get('/admin/ingredients');
      if (response.data.success) {
        setIngredients(response.data.data);
        setFilteredIngredients(response.data.data);
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load ingredients');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setEditingItem(null);
    setFormData({
      name: '',
      unit: '',
      imageUrl: ''
    });
    setShowModal(true);
  };

  const handleEdit = (item: Ingredient) => {
    setEditingItem(item);
    setFormData(item);
    setShowModal(true);
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this ingredient?')) return;

    try {
      await apiClient.delete(`/ingredients/${id}`);
      fetchIngredients();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to delete ingredient');
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      if (editingItem) {
        await apiClient.put(`/ingredients/${editingItem.id}`, formData);
      } else {
        await apiClient.post('/admin/ingredients', formData);
      }
      setShowModal(false);
      fetchIngredients();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to save ingredient');
    }
  };

  if (loading) {
    return <div className="container"><div className="loading">Loading...</div></div>;
  }

  return (
    <div className="container">
      <div className="crud-header">
        <h1>Ingredients Management</h1>
        <div className="crud-actions">
          <input
            type="text"
            placeholder="Search ingredients..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="search-input"
          />
          <button onClick={handleCreate} className="btn-primary">Add Ingredient</button>
        </div>
      </div>

      {error && <div className="error-message">{error}</div>}

      <div className="crud-table-container">
        <table className="crud-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Unit</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {filteredIngredients.map((item) => (
              <tr key={item.id}>
                <td>{item.name}</td>
                <td>{item.unit}</td>
                <td>
                  <button onClick={() => handleEdit(item)} className="btn-edit">Edit</button>
                  <button onClick={() => handleDelete(item.id)} className="btn-delete">Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showModal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <h2>{editingItem ? 'Edit Ingredient' : 'Add Ingredient'}</h2>
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label>Name *</label>
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  required
                />
              </div>
              <div className="form-group">
                <label>Unit *</label>
                <input
                  type="text"
                  value={formData.unit}
                  onChange={(e) => setFormData({ ...formData, unit: e.target.value })}
                  required
                />
              </div>
              <div className="form-group">
                <label>Image URL</label>
                <input
                  type="text"
                  value={formData.imageUrl}
                  onChange={(e) => setFormData({ ...formData, imageUrl: e.target.value })}
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

export default AdminIngredients;

