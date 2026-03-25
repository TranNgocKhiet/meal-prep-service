import { useState, useEffect } from 'react';
import { getErrorMessage } from '../types/errors';
import { useAuth } from '../hooks/useAuth';
import apiClient from '../config/api';
import './Profile.css';

interface AccountDto {
  id?: string;
  email: string;
  password?: string;
  fullName: string;
  phoneNumber: string;
  roleId: number;
  currentCredits: number;
  isActive: boolean;
  lastLoginAt?: string;
  googleAuthId?: string;
}

const Profile = () => {
  const { user, refreshUser } = useAuth();
  const [isEditing, setIsEditing] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    fullName: '',
    phoneNumber: ''
  });

  useEffect(() => {
    if (user) {
      setFormData({
        email: user.email || '',
        password: '',
        confirmPassword: '',
        fullName: user.fullName || '',
        phoneNumber: user.phoneNumber || ''
      });
    }
  }, [user]);

  const handleEdit = () => {
    setIsEditing(true);
    setError('');
    setSuccessMessage('');
  };

  const handleCancel = () => {
    setIsEditing(false);
    if (user) {
      setFormData({
        email: user.email || '',
        password: '',
        confirmPassword: '',
        fullName: user.fullName || '',
        phoneNumber: user.phoneNumber || ''
      });
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setSuccessMessage('');

    if (formData.password && formData.password !== formData.confirmPassword) {
      setError('Password and confirm password do not match');
      return;
    }

    setIsLoading(true);

    try {
      const currentAccountResponse = await apiClient.get(`/admin/accounts/${user?.id}`);
      const currentAccount = currentAccountResponse.data?.data as AccountDto | undefined;

      if (!currentAccount) {
        throw new Error('Unable to load current profile data. Please try again.');
      }

      const updateData: AccountDto = {
        id: currentAccount.id,
        email: formData.email,
        fullName: formData.fullName,
        phoneNumber: formData.phoneNumber,
        roleId: currentAccount.roleId,
        currentCredits: currentAccount.currentCredits,
        isActive: currentAccount.isActive,
        lastLoginAt: currentAccount.lastLoginAt,
        googleAuthId: currentAccount.googleAuthId,
        ...(formData.password && { password: formData.password })
      };

      const response = await apiClient.put(`/admin/accounts/${user?.id}`, updateData);
      
      if (response.data.success) {
        setSuccessMessage('Profile updated successfully');
        setIsEditing(false);
        setFormData({ ...formData, password: '', confirmPassword: '' });
        await refreshUser();
        setTimeout(() => setSuccessMessage(''), 3000);
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err) || 'Failed to update profile');
      setTimeout(() => setError(''), 5000);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="profile-container">
      <div className="profile-header">
        <h1 style={{ color: '#fff' }}>My Profile</h1>
      </div>

      {error && <div className="error-message">{error}</div>}
      {successMessage && <div className="success-message">{successMessage}</div>}

      <div className="profile-content">
        <section className="profile-section">
          <div className="section-header">
            <h2>User Information</h2>
            {!isEditing && (
              <button onClick={handleEdit} className="btn-edit-profile">
                Edit Profile
              </button>
            )}
          </div>

          {isEditing ? (
            <form onSubmit={handleSubmit} className="profile-form">
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
                <label>Password</label>
                <div className="password-input-wrapper">
                  <input
                    type={showPassword ? "text" : "password"}
                    value={formData.password}
                    onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                    placeholder="Leave blank to keep current password"
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
                <label>Confirm Password</label>
                <div className="password-input-wrapper">
                  <input
                    type={showConfirmPassword ? 'text' : 'password'}
                    value={formData.confirmPassword}
                    onChange={(e) => setFormData({ ...formData, confirmPassword: e.target.value })}
                    placeholder="Re-enter new password"
                    disabled={isLoading || !formData.password}
                  />
                  <button
                    type="button"
                    className="password-toggle"
                    onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                    aria-label={showConfirmPassword ? 'Hide confirm password' : 'Show confirm password'}
                    disabled={isLoading || !formData.password}
                  >
                    {showConfirmPassword ? (
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

              <div className="form-actions">
                <button type="button" onClick={handleCancel} className="btn-cancel" disabled={isLoading}>
                  Cancel
                </button>
                <button type="submit" className="btn-save" disabled={isLoading}>
                  {isLoading ? 'Saving...' : 'Save Changes'}
                </button>
              </div>
            </form>
          ) : (
            <div className="info-grid">
              <div className="info-item">
                <label>Full Name</label>
                <p>{user?.fullName}</p>
              </div>
              <div className="info-item">
                <label>Email</label>
                <p>{user?.email}</p>
              </div>
              <div className="info-item">
                <label>Phone Number</label>
                <p>{user?.phoneNumber || 'Not provided'}</p>
              </div>
            </div>
          )}
        </section>
      </div>
    </div>
  );
};

export default Profile;
