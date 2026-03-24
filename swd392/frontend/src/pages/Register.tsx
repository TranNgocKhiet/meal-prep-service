import { useState, type FormEvent } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { GoogleLogin } from '@react-oauth/google';
import { useAuth } from '../hooks/useAuth';
import { getErrorMessage } from '../types/errors';
import './Register.css';

const Register = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    fullName: ''
  });
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const { register, registerWithGoogle } = useAuth();
  const navigate = useNavigate();

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const validateForm = () => {
    if (!formData.email || !formData.password || !formData.fullName) {
      setError('Please fill in all required fields');
      return false;
    }

    if (formData.password.length < 6) {
      setError('Password must be at least 6 characters long');
      return false;
    }

    if (formData.password !== formData.confirmPassword) {
      setError('Passwords do not match');
      return false;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(formData.email)) {
      setError('Please enter a valid email address');
      return false;
    }

    return true;
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');

    if (!validateForm()) {
      return;
    }

    setIsLoading(true);

    try {
      await register(formData.email, formData.password, formData.fullName);
      navigate('/');
    } catch (err: unknown) {
      setError(getErrorMessage(err) || 'Registration failed. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleGoogleSuccess = async (credentialResponse: any) => {
    setError('');
    setIsLoading(true);

    try {
      if (credentialResponse.credential) {
        await registerWithGoogle(credentialResponse.credential);
        navigate('/');
      } else {
        setError('Google signup failed: No credential received');
      }
    } catch (err: unknown) {
      const message = getErrorMessage(err) || 'Google signup failed';
      if (message.toLowerCase().includes('already') || message.toLowerCase().includes('exists')) {
        setError('This account already exists. Please sign in instead.');
      } else {
        setError(message);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleGoogleError = () => {
    setError('Google signup failed. Please try again.');
  };

  return (
    <div className="register-container">
      <div className="register-card">
        <h1>Create Account</h1>
        <p className="register-subtitle">Join us to start planning your meals</p>

        {error && <div className="error-message">{error}</div>}

        <form onSubmit={handleSubmit} className="register-form">
          <div className="form-group">
            <label htmlFor="fullName">Full Name</label>
            <input
              type="text"
              id="fullName"
              name="fullName"
              value={formData.fullName}
              onChange={handleChange}
              placeholder="Enter your full name"
              disabled={isLoading}
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              type="email"
              id="email"
              name="email"
              value={formData.email}
              onChange={handleChange}
              placeholder="Enter your email"
              disabled={isLoading}
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Password</label>
            <div className="password-input-wrapper">
              <input
                type={showPassword ? 'text' : 'password'}
                id="password"
                name="password"
                value={formData.password}
                onChange={handleChange}
                placeholder="Create a password (min. 6 characters)"
                disabled={isLoading}
                required
              />
              <button
                type="button"
                className="password-toggle"
                onClick={() => setShowPassword((prev) => !prev)}
                aria-label={showPassword ? 'Hide password' : 'Show password'}
                title={showPassword ? 'Hide password' : 'Show password'}
                disabled={isLoading}
              >
                {showPassword ? (
                  <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                    <path d="M3 3.9 4.3 2.6 21.4 19.7 20.1 21l-3.2-3.2c-1.5.7-3.1 1.1-4.9 1.1C6.6 18.9 2.2 12 2.2 12s1.7-2.7 4.7-4.9L3 3.9Zm8.9 8.9-1.8-1.8a2.5 2.5 0 0 0 3 3l-1.2-1.2Zm6.8 1.6-1.5-1.5c1.7-1.6 2.6-2.9 2.6-2.9s-4.4-6.9-9.8-6.9c-1.1 0-2.1.2-3.1.5L5.3 2.1c1.4-.5 2.9-.8 4.7-.8 5.4 0 9.8 6.9 9.8 6.9s-1.1 1.7-3.1 3.7Z" />
                  </svg>
                ) : (
                  <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                    <path d="M12 5.5c5.4 0 9.8 6.5 9.8 6.5s-4.4 6.5-9.8 6.5S2.2 12 2.2 12s4.4-6.5 9.8-6.5Zm0 11a4.5 4.5 0 1 0 0-9 4.5 4.5 0 0 0 0 9Zm0-2a2.5 2.5 0 1 1 0-5 2.5 2.5 0 0 1 0 5Z" />
                  </svg>
                )}
              </button>
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="confirmPassword">Confirm Password</label>
            <div className="password-input-wrapper">
              <input
                type={showConfirmPassword ? 'text' : 'password'}
                id="confirmPassword"
                name="confirmPassword"
                value={formData.confirmPassword}
                onChange={handleChange}
                placeholder="Confirm your password"
                disabled={isLoading}
                required
              />
              <button
                type="button"
                className="password-toggle"
                onClick={() => setShowConfirmPassword((prev) => !prev)}
                aria-label={showConfirmPassword ? 'Hide confirm password' : 'Show confirm password'}
                title={showConfirmPassword ? 'Hide confirm password' : 'Show confirm password'}
                disabled={isLoading}
              >
                {showConfirmPassword ? (
                  <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                    <path d="M3 3.9 4.3 2.6 21.4 19.7 20.1 21l-3.2-3.2c-1.5.7-3.1 1.1-4.9 1.1C6.6 18.9 2.2 12 2.2 12s1.7-2.7 4.7-4.9L3 3.9Zm8.9 8.9-1.8-1.8a2.5 2.5 0 0 0 3 3l-1.2-1.2Zm6.8 1.6-1.5-1.5c1.7-1.6 2.6-2.9 2.6-2.9s-4.4-6.9-9.8-6.9c-1.1 0-2.1.2-3.1.5L5.3 2.1c1.4-.5 2.9-.8 4.7-.8 5.4 0 9.8 6.9 9.8 6.9s-1.1 1.7-3.1 3.7Z" />
                  </svg>
                ) : (
                  <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                    <path d="M12 5.5c5.4 0 9.8 6.5 9.8 6.5s-4.4 6.5-9.8 6.5S2.2 12 2.2 12s4.4-6.5 9.8-6.5Zm0 11a4.5 4.5 0 1 0 0-9 4.5 4.5 0 0 0 0 9Zm0-2a2.5 2.5 0 1 1 0-5 2.5 2.5 0 0 1 0 5Z" />
                  </svg>
                )}
              </button>
            </div>
          </div>

          <button type="submit" className="btn-primary" disabled={isLoading}>
            {isLoading ? 'Creating account...' : 'Create Account'}
          </button>
        </form>

        <div className="divider">
          <span>OR</span>
        </div>

        <div className="google-login-container">
          <GoogleLogin
            onSuccess={handleGoogleSuccess}
            onError={handleGoogleError}
            theme="outline"
            size="large"
            text="signup_with"
            shape="rectangular"
            width="100%"
          />
        </div>

        <p className="register-footer">
          Already have an account? <Link to="/login">Sign in</Link>
        </p>
      </div>
    </div>
  );
};

export default Register;
