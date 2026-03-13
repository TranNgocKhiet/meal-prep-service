import { useState, useEffect } from 'react';
import apiClient from '../config/api';
import './AdminCrud.css';

interface SubscriptionPackage {
  id: string;
  packageName: string;
  price: number;
  creditAmount: number;
  durationDays: number;
  description: string;
}

const AdminSubscriptionPackages = () => {
  const [items, setItems] = useState<SubscriptionPackage[]>([]);
  const [filtered, setFiltered] = useState<SubscriptionPackage[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editingItem, setEditingItem] = useState<SubscriptionPackage | null>(null);
  const [formData, setFormData] = useState({
    packageName: '',
    price: 0,
    creditAmount: 0,
    durationDays: 30,
    description: ''
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
      const response = await apiClient.get('/subscriptionpackages');
      if (response.data.success) {
        setItems(response.data.data);
        setFiltered(response.data.data);
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load subscription packages');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setEditingItem(null);
    setFormData({
      packageName: '',
      price: 0,
      creditAmount: 0,
      durationDays: 30,
      description: ''
    });
    setShowModal(true);
  };

  const handleEdit = (item: SubscriptionPackage) => {
    setEditingItem(item);
    setFormData({
      packageName: item.packageName,
      price: item.price,
      creditAmount: item.creditAmount,
      durationDays: item.durationDays,
      description: item.description
    });
    setShowModal(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingItem) {
        await apiClient.put(`/subscriptionpackages/${editingItem.id}`, formData);
      } else {
        await apiClient.post('/subscriptionpackages', formData);
      }
      setShowModal(false);
      fetchItems();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to save subscription package');
    }
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm('Are you sure you want to delete this package?')) return;
    
    try {
      await apiClient.delete(`/subscriptionpackages/${id}`);
      fetchItems();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to delete subscription package');
    }
  };

  if (loading) return <div className="container"><div className="loading">Loading...</div></div>;

  return (
    <div className="container">
      <div className="crud-header">
        <h1 style={{ color: '#fff' }}>Subscription Packages</h1>
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
              <th>Duration (Days)</th>
              <th>Description</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {filtered.length === 0 ? (
              <tr>
                <td colSpan={6} style={{ textAlign: 'center', padding: '2rem' }}>
                  No subscription packages found
                </td>
              </tr>
            ) : (
              filtered.map((item) => (
                <tr key={item.id}>
                  <td>{item.packageName}</td>
                  <td>{item.price.toLocaleString()}</td>
                  <td>{item.creditAmount}</td>
                  <td>{item.durationDays}</td>
                  <td>{item.description}</td>
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
                  min="0"
                  required
                />
              </div>
              <div className="form-group">
                <label>Duration (Days) *</label>
                <input
                  type="number"
                  value={formData.durationDays}
                  onChange={(e) => setFormData({ ...formData, durationDays: parseInt(e.target.value) })}
                  min="1"
                  required
                />
              </div>
              <div className="form-group">
                <label>Description</label>
                <textarea
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  rows={3}
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

export default AdminSubscriptionPackages;
