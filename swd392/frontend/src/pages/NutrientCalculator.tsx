import Container from '../components/layout/Container';
import './NutrientCalculator.css';

const NutrientCalculator = () => {
  return (
    <Container>
      <div className="coming-soon-page">
        <div className="coming-soon-content">
          <div className="coming-soon-icon">
            <svg 
              width="120" 
              height="120" 
              viewBox="0 0 24 24" 
              fill="none" 
              stroke="currentColor" 
              strokeWidth="2" 
              strokeLinecap="round" 
              strokeLinejoin="round"
            >
              <circle cx="12" cy="12" r="10"></circle>
              <polyline points="12 6 12 12 16 14"></polyline>
            </svg>
          </div>
          <h1>Nutrient Calculator</h1>
          <h2>Coming Soon</h2>
          <p>We're working hard to bring you an amazing nutrient calculator feature.</p>
          <p>Stay tuned for updates!</p>
        </div>
      </div>
    </Container>
  );
};

export default NutrientCalculator;
