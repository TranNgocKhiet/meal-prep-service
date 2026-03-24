import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import './Header.css';

interface HeaderProps {
  onMenuToggle?: () => void;
}

const Header = ({ onMenuToggle }: HeaderProps) => {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [isManagementDropdownOpen, setIsManagementDropdownOpen] = useState(false);
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);
  const managementDropdownRef = useRef<HTMLLIElement | null>(null);
  const { isAuthenticated, user, logout } = useAuth();
  const navigate = useNavigate();

  const handleMenuToggle = () => {
    setIsMobileMenuOpen(!isMobileMenuOpen);
    onMenuToggle?.();
  };

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  const toggleManagementDropdown = () => {
    setIsManagementDropdownOpen(!isManagementDropdownOpen);
  };

  const closeManagementDropdown = () => {
    setIsManagementDropdownOpen(false);
  };

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (!managementDropdownRef.current) {
        return;
      }

      if (!managementDropdownRef.current.contains(event.target as Node)) {
        setIsManagementDropdownOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);

  const toggleUserMenu = () => {
    setIsUserMenuOpen(!isUserMenuOpen);
  };

  const normalizedRole = (user?.roleName || '').trim().toLowerCase();
  const isAdmin = normalizedRole === 'admin';
  const isManager = normalizedRole === 'manager';
  const isStaff = normalizedRole === 'staff';
  const isDeliveryman = normalizedRole === 'deliveryman' || normalizedRole === 'delivery_personnel';
  const isCustomer = normalizedRole === 'customer';

  // Debug logging
  console.log('User role:', user?.roleName);
  console.log('Normalized role:', normalizedRole);
  console.log('Is Customer:', isCustomer);

  return (
    <header className="header">
      <div className="container">
        <div className="header-content">
          <Link to="/" className="header-logo">
            <span className="logo-text">Meal Prep Service</span>
          </Link>

          <button
            className="mobile-menu-toggle"
            onClick={handleMenuToggle}
            aria-label="Toggle navigation menu"
            aria-expanded={isMobileMenuOpen}
          >
            <span className="hamburger-icon">
              <span></span>
              <span></span>
              <span></span>
            </span>
          </button>

          <nav className={`header-nav ${isMobileMenuOpen ? 'open' : ''}`}>
            {isAuthenticated ? (
              <>
                <ul className="nav-list">
                  {/* Common menu items for all roles */}
                  <li><Link to="/">Home</Link></li>
                  <li><Link to="/todays-menu">Today's Menu</Link></li>
                  <li><Link to="/weekly-menu">Weekly's Menu</Link></li>
                  
                  {/* Customer-only menu items */}
                  {isCustomer && (
                    <>
                      <li><Link to="/cart">My Cart</Link></li>
                      <li><Link to="/my-orders">My Orders</Link></li>
                      <li><Link to="/meal-plans">My Meal Plans</Link></li>
                      <li><Link to="/nutrient-calculator">Nutrient Calculator</Link></li>
                      <li><Link to="/fridge">My Fridge</Link></li>
                    </>
                  )}
                  
                  {/* Manager menu items */}
                  {isManager && (
                    <>
                      <li><Link to="/admin/menu">Menu</Link></li>
                      <li className="dropdown" ref={managementDropdownRef}>
                        <button 
                          className="dropdown-toggle"
                          onClick={toggleManagementDropdown}
                          aria-expanded={isManagementDropdownOpen}
                        >
                          Management
                          <span className={`dropdown-arrow ${isManagementDropdownOpen ? 'open' : ''}`}>▼</span>
                        </button>
                        <ul className={`dropdown-menu ${isManagementDropdownOpen ? 'open' : ''}`}>
                          <li><Link to="/admin/ingredients" onClick={closeManagementDropdown}>Ingredients</Link></li>
                          <li><Link to="/admin/allergies" onClick={closeManagementDropdown}>Allergies</Link></li>
                          <li><Link to="/admin/recipes" onClick={closeManagementDropdown}>Recipes</Link></li>
                          <li><Link to="/admin/nutrients" onClick={closeManagementDropdown}>Nutrients</Link></li>
                          <li><Link to="/feedback" onClick={closeManagementDropdown}>Feedback</Link></li>
                        </ul>
                      </li>
                    </>
                  )}
                  
                  {/* Staff menu items */}
                  {isStaff && (
                    <>
                      <li><Link to="/staff/order-list">Order List</Link></li>
                      <li><Link to="/staff/delivery-schedule">Delivery Schedule</Link></li>
                    </>
                  )}
                  
                  {/* Deliveryman menu items */}
                  {isDeliveryman && (
                    <>
                      <li><Link to="/deliveryman/my-schedule">My Delivery Schedule</Link></li>
                    </>
                  )}
                  
                  {/* Admin menu items */}
                  {isAdmin && (
                    <>
                      <li><Link to="/admin/menu">Menu</Link></li>
                      <li><Link to="/admin/accounts">Accounts</Link></li>
                      <li className="dropdown" ref={managementDropdownRef}>
                        <button 
                          className="dropdown-toggle"
                          onClick={toggleManagementDropdown}
                          aria-expanded={isManagementDropdownOpen}
                        >
                          Management
                          <span className={`dropdown-arrow ${isManagementDropdownOpen ? 'open' : ''}`}>▼</span>
                        </button>
                        <ul className={`dropdown-menu ${isManagementDropdownOpen ? 'open' : ''}`}>
                          <li><Link to="/admin/ingredients" onClick={closeManagementDropdown}>Ingredients</Link></li>
                          <li><Link to="/admin/allergies" onClick={closeManagementDropdown}>Allergies</Link></li>
                          <li><Link to="/admin/recipes" onClick={closeManagementDropdown}>Recipes</Link></li>
                          <li><Link to="/admin/nutrients" onClick={closeManagementDropdown}>Nutrients</Link></li>
                          <li><Link to="/feedback" onClick={closeManagementDropdown}>Feedback</Link></li>
                          <li><Link to="/admin/ai-credit-packages" onClick={closeManagementDropdown}>AI Credit Packages</Link></li>
                          <li><Link to="/admin/subscription-packages" onClick={closeManagementDropdown}>Subscription Packages</Link></li>
                        </ul>
                      </li>
                      <li><Link to="/admin/system-config">System Configuration</Link></li>
                      <li><Link to="/admin/revenue-report">Revenue Report</Link></li>
                    </>
                  )}
                </ul>
                <div className="header-actions">
                  <div className="user-menu-container">
                    <button 
                      className="user-icon-button"
                      onClick={toggleUserMenu}
                      aria-expanded={isUserMenuOpen}
                      aria-label="User menu"
                    >
                      <div className="user-icon">
                        {user?.fullName?.charAt(0).toUpperCase() || 'U'}
                      </div>
                    </button>
                    <div className={`user-dropdown-menu ${isUserMenuOpen ? 'open' : ''}`}>
                      <div className="user-dropdown-header">
                        <div className="user-dropdown-name">{user?.fullName}</div>
                        <div className="user-dropdown-email">{user?.email}</div>
                        {isCustomer && (
                          <div className="user-dropdown-credits">
                            <span className="credits-icon">⚡</span>
                            <span className="credits-text">AI Credits: </span>
                            <span className="credits-amount">{user?.currentCredits || 0}</span>
                          </div>
                        )}
                      </div>
                      <div className="user-dropdown-divider"></div>
                      <Link to="/profile" className="user-dropdown-item" onClick={() => setIsUserMenuOpen(false)}>
                        Profile Settings
                      </Link>
                      {isCustomer && (
                        <Link to="/feedback" className="user-dropdown-item" onClick={() => setIsUserMenuOpen(false)}>
                          Feedback
                        </Link>
                      )}
                      {isCustomer && (
                        <Link to="/ai-credits" className="user-dropdown-item" onClick={() => setIsUserMenuOpen(false)}>
                          AI Credits
                        </Link>
                      )}
                      <button onClick={handleLogout} className="user-dropdown-item user-dropdown-logout">
                        Logout
                      </button>
                    </div>
                  </div>
                </div>
              </>
            ) : (
              <div className="header-actions">
                <Link to="/login" className="btn-login">Login</Link>
                <Link to="/register" className="btn-register">Sign Up</Link>
              </div>
            )}
          </nav>
        </div>
      </div>
    </header>
  );
};

export default Header;
