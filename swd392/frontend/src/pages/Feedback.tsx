import { useState, useEffect } from 'react';
import { useAuth } from '../hooks/useAuth';
import Container from '../components/layout/Container';
import feedbackAPI from '../services/feedbackService';
import type { FeedbackDto } from '../types/feedback';
import CreateFeedbackModal from './components/CreateFeedbackModal';
import FeedbackList from './components/FeedbackList';
import AdminFeedbackList from './components/AdminFeedbackList';
import './Feedback.css';

const Feedback = () => {
  const { user } = useAuth();
  const [feedbacks, setFeedbacks] = useState<FeedbackDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);

  const normalizedRole = (user?.roleName || '').trim().toLowerCase();
  const isManager = normalizedRole === 'manager' || normalizedRole === 'admin';
  const isCustomer = normalizedRole === 'customer';

  useEffect(() => {
    fetchFeedbacks();
  }, [page]);

  const fetchFeedbacks = async () => {
    try {
      setLoading(true);
      setError('');

      if (isManager) {
        const data = await feedbackAPI.getAllFeedbacks(page, pageSize);
        setFeedbacks(data.feedbacks);
      } else if (isCustomer) {
        const data = await feedbackAPI.getMyFeedbacks();
        setFeedbacks(data);
      }
    } catch (err) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Failed to load feedbacks');
    } finally {
      setLoading(false);
    }
  };

  const handleFeedbackCreated = async () => {
    setShowCreateModal(false);
    setPage(1);
    await fetchFeedbacks();
  };

  if (!user) {
    return (
      <Container>
        <div className="feedback-container">
          <div className="error-container">
            <p>Please log in to view feedbacks.</p>
          </div>
        </div>
      </Container>
    );
  }

  return (
    <Container>
      <div className="feedback-container">
        <div className="feedback-header">
          <h1>Feedback</h1>
          {isCustomer && (
            <button
              className="btn btn-primary"
              onClick={() => setShowCreateModal(true)}
            >
              Submit Feedback
            </button>
          )}
        </div>

        {error && (
          <div className="feedback-error">
            <p>{error}</p>
          </div>
        )}

        {loading ? (
          <div className="loading-container">
            <div className="spinner"></div>
            <p>Loading feedbacks...</p>
          </div>
        ) : (
          <>
            {isManager ? (
              <AdminFeedbackList 
                feedbacks={feedbacks} 
                onRefresh={fetchFeedbacks}
                page={page}
                pageSize={pageSize}
                onPageChange={setPage}
              />
            ) : (
              <FeedbackList feedbacks={feedbacks} />
            )}
          </>
        )}

        {isCustomer && (
          <CreateFeedbackModal
            isOpen={showCreateModal}
            onClose={() => setShowCreateModal(false)}
            onSuccess={handleFeedbackCreated}
          />
        )}
      </div>
    </Container>
  );
};

export default Feedback;
