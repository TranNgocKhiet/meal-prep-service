import { useState, useEffect } from 'react';
import apiClient from '../config/api';
import './AdminCrud.css';

interface Nutrient {
  id: string;
  name: string;
}

const AdminNutrients = () => {
  const [items, setItems] = useState<Nutrient[]>([]);
  const [filtered, setFiltered] = useState<Nutrient[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editingItem, setEditingItem] = useState<Nutrient | null>(null);
  const [formData, setFormData] = useState<Partial<Nutrient>>({
    name: ''
  });

  useEffect(() => {
    fetchItems();
  }, []);

  useEffect(() => {
    const result = items.filter(item =>
      item.name.toLowerCase().includes(searchTerm.toLowerCase())
    );
    setFiltered(result);
  }, [searchTerm, items]);

  const fetchItems = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get('/admin/nutrients');
      if (response.data.success) {
        setItems(response.data.data);
        setFiltered(response.data.data);
      }
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load nutrients');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setEditingItem(null);
    setFormData({ name: '' });
    setShowModal(true);
  };

  const handleEdit = (item: Nutrient) => {
    setEditingItem(item);
    setFormData(item);
    setShowModal(true);
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure?')) return;
    try {
      await apiClient.delete(`/nutrients/${id}`);
      fetchItems();
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to delete');
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingItem) {
        await apiClient.put(`/nutrients/${editingItem.id}`, formData);
      } else {
        await apiClient.post('/admin/nutrients', formData);
      }
      setShowModal(false);
      fetchItems();
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to save');
    }
  };

  if (loading) return <div className="container"><div className="loading">Loading...</div></div>;

  return (
    <div className="container">
      <div className="crud-header">
        <h1 style={{ color: '#fff' }}>Nutrients Management</h1>
        <div className="crud-actions">
          <input
            type="text"
            placeholder="Search nutrients..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="search-input"
          />
          <button onClick={handleCreate} className="btn-primary">Add Nutrient</button>
        </div>
      </div>

      {error && <div className="error-message">{error}</div>}

      <div className="crud-table-container">
        <table className="crud-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((item) => (
              <tr key={item.id}>
                <td>{item.name}</td>
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
            <h2>{editingItem ? 'Edit Nutrient' : 'Add Nutrient'}</h2>
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label>Name *</label>
                <input type="text" value={formData.name} onChange={(e) => setFormData({ ...formData, name: e.target.value })} required />
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

export default AdminNutrients;

