import type { FeedbackDto } from '../../types/feedback';
import '../Feedback.css';

interface FeedbackListProps {
  feedbacks: FeedbackDto[];
}

const FeedbackList = ({ feedbacks }: FeedbackListProps) => {
  if (feedbacks.length === 0) {
    return (
      <div className="feedback-empty">
        <p>You haven't submitted any feedback yet.</p>
        <small>Your feedback helps us improve our service!</small>
      </div>
    );
  }

  return (
    <div className="feedback-list">
      {feedbacks.map((feedback) => (
        <div key={feedback.id} className="feedback-item">
          <div className="feedback-item-header">
            <h3>{feedback.title}</h3>
            <span className="feedback-date">
              {new Date(feedback.createdAt).toLocaleDateString('en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
              })}
            </span>
          </div>
          <p className="feedback-content">{feedback.content}</p>
          <div className="feedback-item-footer">
            <small>ID: {feedback.id.substring(0, 8)}...</small>
          </div>
        </div>
      ))}
    </div>
  );
};

export default FeedbackList;
