import { useState } from 'react';
import feedbackAPI from '../../services/feedbackService';
import type { CreateFeedbackDto } from '../../types/feedback';
import '../Feedback.css';

interface CreateFeedbackModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

const CreateFeedbackModal = ({ isOpen, onClose, onSuccess }: CreateFeedbackModalProps) => {
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!title.trim() || !content.trim()) {
      setError('Title and content are required');
      return;
    }

    try {
      setLoading(true);
      setError('');

      const feedbackData: CreateFeedbackDto = {
        title: title.trim(),
        content: content.trim()
      };

      await feedbackAPI.createFeedback(feedbackData);
      setTitle('');
      setContent('');
      onSuccess();
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to create feedback');
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="feedback-modal-overlay" onClick={onClose}>
      <div className="feedback-modal" onClick={(e) => e.stopPropagation()}>
        <div className="feedback-modal-header">
          <h2>Submit Feedback</h2>
          <button
            className="feedback-modal-close"
            onClick={onClose}
            disabled={loading}
          >
            ×
          </button>
        </div>

        <form onSubmit={handleSubmit} className="feedback-form">
          <div className="form-group">
            <label htmlFor="title">Title*</label>
            <input
              id="title"
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Enter feedback title"
              maxLength={200}
              disabled={loading}
              required
            />
            <small>{title.length}/200</small>
          </div>

          <div className="form-group">
            <label htmlFor="content">Content*</label>
            <textarea
              id="content"
              value={content}
              onChange={(e) => setContent(e.target.value)}
              placeholder="Please tell us your feedback, suggestions, or issues..."
              rows={6}
              maxLength={5000}
              disabled={loading}
              required
            />
            <small>{content.length}/5000</small>
          </div>

          {error && <div className="form-error">{error}</div>}

          <div className="feedback-modal-actions">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={onClose}
              disabled={loading}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={loading}
            >
              {loading ? 'Submitting...' : 'Submit Feedback'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default CreateFeedbackModal;
