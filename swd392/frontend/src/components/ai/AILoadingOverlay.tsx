import './AILoadingOverlay.css';

interface AILoadingOverlayProps {
  open: boolean;
  title?: string;
  description?: string;
  steps?: string[];
}

const defaultSteps = ['Vision Parsing', 'Macro Engine', 'Conflict Matrix'];

const AILoadingOverlay = ({
  open,
  title = 'AI chef is preparing your nutrition scan...',
  description = 'Scanning ingredients, balancing macros, and validating compatibility.',
  steps = defaultSteps
}: AILoadingOverlayProps) => {
  if (!open) {
    return null;
  }

  return (
    <div className="ai-loading-overlay" role="status" aria-live="polite">
      <div className="ai-loading-content">
        <div className="ai-tech-grid" aria-hidden="true" />

        <div className="ai-robot-chef-module" aria-hidden="true">
          <div className="ai-orbital ai-orbital-one" />
          <div className="ai-orbital ai-orbital-two" />

          <div className="ai-robot-head">
            <span className="ai-robot-antenna" />
            <div className="ai-robot-eyes">
              <span className="ai-robot-eye" />
              <span className="ai-robot-eye" />
            </div>
            <div className="ai-robot-mouth" />
          </div>

          <div className="ai-cooking-bowl">
            <span className="ai-steam ai-steam-one" />
            <span className="ai-steam ai-steam-two" />
            <span className="ai-steam ai-steam-three" />
          </div>
        </div>

        <h3>{title}</h3>
        <p>{description}</p>

        <div className="ai-loading-steps" aria-hidden="true">
          {steps.map((step) => (
            <span key={step}>{step}</span>
          ))}
        </div>
      </div>
    </div>
  );
};

export default AILoadingOverlay;
