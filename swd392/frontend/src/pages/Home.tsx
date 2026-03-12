import { Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import Container from '../components/layout/Container';
import './Home.css';

const Home = () => {
  const { isAuthenticated, user } = useAuth();

  return (
    <Container>
      <div className="home">
        <section className="hero">
          {isAuthenticated ? (
            <>
              <h1>Welcome back, {user?.fullName}!</h1>
              <p>Ready to plan your next meal?</p>
              <div className="hero-actions">
                <Link to="/todays-menu" className="btn-primary">Today's Menu</Link>
                <Link to="/weekly-menu" className="btn-secondary">Weekly Menu</Link>
              </div>
            </>
          ) : (
            <>
              <h1>Welcome to Meal Prep Service</h1>
              <p>Plan your meals, manage your fridge, and order ingredients with ease</p>
              <div className="hero-actions">
                <Link to="/register" className="btn-primary">Get Started</Link>
                <Link to="/login" className="btn-secondary">Sign In</Link>
              </div>
            </>
          )}
        </section>

        <section className="features">
          <h2>Features</h2>
          <div className="features-grid">
            <div className="feature-card">
              <h3>Meal Planning</h3>
              <p>Create custom meal plans or let AI generate them for you</p>
            </div>
            <div className="feature-card">
              <h3>Virtual Fridge</h3>
              <p>Track your ingredients and expiry dates</p>
            </div>
            <div className="feature-card">
              <h3>Smart Shopping</h3>
              <p>Generate grocery lists based on your meal plans</p>
            </div>
            <div className="feature-card">
              <h3>Nutrient Tracking</h3>
              <p>Calculate nutritional information for your meals</p>
            </div>
          </div>
        </section>
      </div>
    </Container>
  );
};

export default Home;
