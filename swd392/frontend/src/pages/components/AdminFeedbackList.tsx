import { useState } from 'react';
import type { FeedbackDto } from '../../types/feedback';
import '../Feedback.css';

interface AdminFeedbackListProps {
  feedbacks: FeedbackDto[];
  onRefresh: () => void;
  page: number;
  pageSize: number;
  onPageChange: (page: number) => void;
}

const AdminFeedbackList = ({
  feedbacks,
  page,
  pageSize,
  onPageChange
}: AdminFeedbackListProps) => {
  const [selectedFeedback, setSelectedFeedback] = useState<FeedbackDto | null>(null);

  if (feedbacks.length === 0) {
    return (
      <div className="feedback-empty">
        <p>No feedbacks yet.</p>
      </div>
    );
  }

  return (
    <div className="admin-feedback-list">
      <table className="feedback-table">
        <thead>
          <tr>
            <th>Customer</th>
            <th>Title</th>
            <th>Content</th>
            <th>Date</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {feedbacks.map((feedback) => (
            <tr key={feedback.id}>
              <td>{feedback.customerName}</td>
              <td className="feedback-title">{feedback.title}</td>
              <td className="feedback-content-preview">
                {feedback.content.substring(0, 100)}
                {feedback.content.length > 100 ? '...' : ''}
              </td>
              <td className="feedback-date-column">
                {new Date(feedback.createdAt).toLocaleDateString()}
              </td>
              <td>
                <button
                  className="btn btn-sm btn-info"
                  onClick={() => setSelectedFeedback(feedback)}
                >
                  View
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      <div className="pagination">
        <button
          className="btn btn-sm"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
        >
          Previous
        </button>
        <span className="page-info">Page {page}</span>
        <button
          className="btn btn-sm"
          onClick={() => onPageChange(page + 1)}
          disabled={feedbacks.length < pageSize}
        >
          Next
        </button>
      </div>

      {selectedFeedback && (
        <div
          className="feedback-modal-overlay"
          onClick={() => setSelectedFeedback(null)}
        >
          <div className="feedback-modal" onClick={(e) => e.stopPropagation()}>
            <div className="feedback-modal-header">
              <h2>Feedback Details</h2>
              <button
                className="feedback-modal-close"
                onClick={() => setSelectedFeedback(null)}
                aria-label="Close feedback details"
              >
                ×
              </button>
            </div>

            <div className="feedback-view-body">
              <p><strong>Customer:</strong> {selectedFeedback.customerName}</p>
              <p><strong>Title:</strong> {selectedFeedback.title}</p>
              <p>
                <strong>Date:</strong>{' '}
                {new Date(selectedFeedback.createdAt).toLocaleString()}
              </p>
              <div className="feedback-view-content">
                {selectedFeedback.content}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminFeedbackList;
