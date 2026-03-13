import { useState, useEffect } from 'react';
import apiClient from '../config/api';
import './AdminCrud.css';

interface AICreditPackage {
  id: string;
  packageName: string;
  price: number;
  creditAmount: number;
}

const AdminAICreditPackages = () => {
  const [items, setItems] = useState<AICreditPackage[]>([]);
  const [filtered, setFiltered] = useState<AICreditPackage[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editingItem, setEditingItem] = useState<AICreditPackage | null>(null);
  const [formData, setFormData] = useState({
    packageName: '',
    price: 0,
    creditAmount: 0
  });

  useEffect(() => {
    fetchItems();
  }, []);

  useEffect(() => {
    const result = items.filter(item =>
      item.packageName.toLowerCase().includes(searchTerm.toLowerCase())
    );
    setFiltered(result);
  }, [searchTerm, items]);

  const fetchItems = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get('/aicreditpackages');
      if (response.data.success) {
        setItems(response.data.data);
        setFiltered(response.data.data);
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load AI credit packages');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setEditingItem(null);
    setFormData({
      packageName: '',
      price: 0,
      creditAmount: 0
    });
    setShowModal(true);
  };

  const handleEdit = (item: AICreditPackage) => {
    setEditingItem(item);
    setFormData({
      packageName: item.packageName,
      price: item.price,
      creditAmount: item.creditAmount
    });
    setShowModal(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingItem) {
        await apiClient.put(`/aicreditpackages/${editingItem.id}`, formData);
      } else {
        await apiClient.post('/aicreditpackages', formData);
      }
      setShowModal(false);
      fetchItems();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to save AI credit package');
    }
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm('Are you sure you want to delete this package?')) return;
    
    try {
      await apiClient.delete(`/aicreditpackages/${id}`);
      fetchItems();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to delete AI credit package');
    }
  };

  if (loading) return <div className="container"><div className="loading">Loading...</div></div>;

  return (
    <div className="container">
      <div className="crud-header">
        <h1 style={{ color: '#fff' }}>AI Credit Packages</h1>
        <div className="crud-actions">
          <input
            type="text"
            placeholder="Search packages..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="search-input"
          />
          <button onClick={handleCreate} className="btn-primary">Add Package</button>
        </div>
      </div>

      {error && <div className="error-message">{error}</div>}

      <div className="crud-table-container">
        <table className="crud-table">
          <thead>
            <tr>
              <th>Package Name</th>
              <th>Price (VND)</th>
              <th>Credit Amount</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {filtered.length === 0 ? (
              <tr>
                <td colSpan={4} style={{ textAlign: 'center', padding: '2rem' }}>
                  No AI credit packages found
                </td>
              </tr>
            ) : (
              filtered.map((item) => (
                <tr key={item.id}>
                  <td>{item.packageName}</td>
                  <td>{item.price.toLocaleString()}</td>
                  <td>{item.creditAmount}</td>
                  <td>
                    <div className="action-buttons">
                      <button onClick={() => handleEdit(item)} className="btn-edit">Edit</button>
                      <button onClick={() => handleDelete(item.id)} className="btn-delete">Delete</button>
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {showModal && (
        <div className="modal-overlay" onClick={() => setShowModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>{editingItem ? 'Edit Package' : 'Add Package'}</h2>
              <button className="btn-close" onClick={() => setShowModal(false)}>×</button>
            </div>
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label>Package Name *</label>
                <input
                  type="text"
                  value={formData.packageName}
                  onChange={(e) => setFormData({ ...formData, packageName: e.target.value })}
                  required
                />
              </div>
              <div className="form-group">
                <label>Price (VND) *</label>
                <input
                  type="number"
                  value={formData.price}
                  onChange={(e) => setFormData({ ...formData, price: parseFloat(e.target.value) })}
                  min="0"
                  step="1000"
                  required
                />
              </div>
              <div className="form-group">
                <label>Credit Amount *</label>
                <input
                  type="number"
                  value={formData.creditAmount}
                  onChange={(e) => setFormData({ ...formData, creditAmount: parseInt(e.target.value) })}
                  min="1"
                  required
                />
              </div>
              <div className="modal-footer">
                <button type="button" onClick={() => setShowModal(false)} className="btn-secondary">
                  Cancel
                </button>
                <button type="submit" className="btn-primary">
                  {editingItem ? 'Update' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminAICreditPackages;
