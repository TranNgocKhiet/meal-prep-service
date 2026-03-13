import { useState, useEffect } from 'react';
import apiClient from '../config/api';
import { useAuth } from '../hooks/useAuth';
import './AdminCrud.css';

interface Role {
  id: number;
  name: string;
}

interface Account {
  id: string;
  email: string;
  fullName: string;
  phoneNumber: string;
  roleId: number;
  role?: Role;
  currentCredits: number;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
  googleId?: string;
  updatedAt?: string;
}

interface AccountFormData {
  email: string;
  password?: string;
  fullName: string;
  phoneNumber: string;
  roleId: number;
}

const AdminAccounts = () => {
  const [items, setItems] = useState<Account[]>([]);
  const [filtered, setFiltered] = useState<Account[]>([]);
  const [roles, setRoles] = useState<Role[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedRoleFilter, setSelectedRoleFilter] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editingItem, setEditingItem] = useState<Account | null>(null);
  const [showPassword, setShowPassword] = useState(false);
  const { user } = useAuth();
  const [formData, setFormData] = useState<AccountFormData>({
    email: '',
    password: '',
    fullName: '',
    phoneNumber: '',
    roleId: 4 // Default to Customer role
  });

  useEffect(() => {
    fetchItems();
    fetchRoles();
  }, []);

  useEffect(() => {
    let result = items.filter(item =>
      item.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
      item.fullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      item.phoneNumber.includes(searchTerm)
    );

    // Filter by selected role
    if (selectedRoleFilter !== null) {
      result = result.filter(item => item.roleId === selectedRoleFilter);
    }

    // Exclude current user
    if (user?.id) {
      result = result.filter(item => item.id !== user.id);
    }

    setFiltered(result);
  }, [searchTerm, items, selectedRoleFilter, user]);

  const fetchItems = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get('/admin/accounts');
      if (response.data.success) {
        setItems(response.data.data);
        setFiltered(response.data.data);
      }
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load accounts');
    } finally {
      setLoading(false);
    }
  };

  const fetchRoles = async () => {
    try {
      const response = await apiClient.get('/roles');
      if (response.data.success) {
        setRoles(response.data.data);
      }
    } catch (err) {
      console.error('Failed to load roles', err);
    }
  };

  const handleCreate = () => {
    setEditingItem(null);
    setShowPassword(false);
    // Use the selected role filter if available, otherwise default to first non-Admin role
    const defaultRoleId = selectedRoleFilter !== null 
      ? selectedRoleFilter 
      : roles.find(r => r.name !== 'Admin')?.id || 4; // Default to Customer (ID 4)
    
    setFormData({
      email: '',
      password: '',
      fullName: '',
      phoneNumber: '',
      roleId: defaultRoleId
    });
    setShowModal(true);
  };

  const handleEdit = (item: Account) => {
    setEditingItem(item);
    setShowPassword(false);
    setFormData({
      email: item.email,
      password: '',
      fullName: item.fullName,
      phoneNumber: item.phoneNumber,
      roleId: item.roleId
    });
    setShowModal(true);
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this account?')) return;
    try {
      await apiClient.delete(`/admin/accounts/${id}`);
      fetchItems();
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || 'Failed to delete');
    }
  };

  const handleToggleStatus = async (id: string, currentStatus: boolean) => {
    const action = currentStatus ? 'deactivate' : 'activate';
    if (!confirm(`Are you sure you want to ${action} this account?`)) return;
    
    try {
      const response = await apiClient.patch(`/admin/accounts/${id}/toggle-status`);
      if (response.data.success) {
        fetchItems();
      } else {
        alert(response.data.message || `Failed to ${action} account`);
      }
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      alert(error.response?.data?.message || `Failed to ${action} account`);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    // Validate role is selected
    if (!formData.roleId || formData.roleId === 0) {
      alert('Please select a role');
      return;
    }
    
    try {
      if (editingItem) {
        await apiClient.put(`/admin/accounts/${editingItem.id}`, formData);
      } else {
        await apiClient.post('/admin/accounts', formData);
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
        <h1 style={{ color: '#fff' }}>Account Management</h1>
        <div className="crud-actions">
          <input
            type="text"
            placeholder="Search accounts..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="search-input"
          />
          <button onClick={handleCreate} className="btn-primary">Add Account</button>
        </div>
      </div>

      {error && <div className="error-message">{error}</div>}

      {/* Role Filter Tabs */}
      <div className="role-tabs">
        <button 
          className={`role-tab ${selectedRoleFilter === null ? 'active' : ''}`}
          onClick={() => setSelectedRoleFilter(null)}
        >
          All Roles
        </button>
        {roles.filter(role => role.name !== 'Admin').map(role => (
          <button 
            key={role.id}
            className={`role-tab ${selectedRoleFilter === role.id ? 'active' : ''}`}
            onClick={() => setSelectedRoleFilter(role.id)}
          >
            {role.name}
          </button>
        ))}
      </div>

      <div className="crud-table-container">
        <table className="crud-table">
          <thead>
            <tr>
              <th>Email</th>
              <th>Full Name</th>
              <th>Phone</th>
              <th>Role</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((item) => (
              <tr key={item.id}>
                <td>{item.email}</td>
                <td>{item.fullName}</td>
                <td>{item.phoneNumber}</td>
                <td>{item.role?.name || 'N/A'}</td>
                <td>
                  <span className={`badge ${item.isActive ? 'badge-success' : 'badge-danger'}`}>
                    {item.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td>
                  <button 
                    onClick={() => handleToggleStatus(item.id, item.isActive)} 
                    className={item.isActive ? 'btn-warning' : 'btn-success'}
                    title={item.isActive ? 'Deactivate account' : 'Activate account'}
                  >
                    {item.isActive ? 'Deactivate' : 'Activate'}
                  </button>
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
            <h2>{editingItem ? 'Edit Account' : 'Add Account'}</h2>
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label>Email *</label>
                <input
                  type="email"
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  required
                />
              </div>
              <div className="form-group">
                <label>Password {!editingItem && '*'}</label>
                <div className="password-input-wrapper">
                  <input
                    type={showPassword ? "text" : "password"}
                    value={formData.password}
                    onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                    required={!editingItem}
                    placeholder={editingItem ? 'Leave blank to keep current password' : ''}
                  />
                  <button
                    type="button"
                    className="password-toggle"
                    onClick={() => setShowPassword(!showPassword)}
                    aria-label={showPassword ? "Hide password" : "Show password"}
                  >
                    {showPassword ? (
                      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24" />
                        <line x1="1" y1="1" x2="23" y2="23" />
                      </svg>
                    ) : (
                      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                        <circle cx="12" cy="12" r="3" />
                      </svg>
                    )}
                  </button>
                </div>
              </div>
              <div className="form-group">
                <label>Full Name *</label>
                <input
                  type="text"
                  value={formData.fullName}
                  onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                  required
                />
              </div>
              <div className="form-group">
                <label>Phone Number</label>
                <input
                  type="tel"
                  value={formData.phoneNumber}
                  onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
                />
              </div>
              <div className="form-group">
                <label>Role *</label>
                <select
                  value={formData.roleId}
                  onChange={(e) => setFormData({ ...formData, roleId: Number(e.target.value) })}
                  required
                >
                  <option value="">Select a role</option>
                  {roles.filter(role => role.name !== 'Admin').map(role => (
                    <option key={role.id} value={role.id}>
                      {role.name}
                    </option>
                  ))}
                </select>
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

export default AdminAccounts;
