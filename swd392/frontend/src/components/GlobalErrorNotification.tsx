import { useEffect, useState } from 'react';
import { APP_ERROR_EVENT } from '../types/errors';
import './GlobalErrorNotification.css';

const AUTO_HIDE_MS = 5000;

const GlobalErrorNotification = () => {
  const [message, setMessage] = useState('');

  useEffect(() => {
    let hideTimer: ReturnType<typeof setTimeout> | undefined;

    const onError = (event: Event) => {
      const customEvent = event as CustomEvent<{ message?: string }>;
      const nextMessage = customEvent.detail?.message?.trim();

      if (!nextMessage) {
        return;
      }

      setMessage(nextMessage);

      if (hideTimer) {
        clearTimeout(hideTimer);
      }

      hideTimer = setTimeout(() => {
        setMessage('');
      }, AUTO_HIDE_MS);
    };

    window.addEventListener(APP_ERROR_EVENT, onError as EventListener);

    return () => {
      if (hideTimer) {
        clearTimeout(hideTimer);
      }
      window.removeEventListener(APP_ERROR_EVENT, onError as EventListener);
    };
  }, []);

  if (!message) {
    return null;
  }

  return (
    <div className="global-error-notification" role="alert" aria-live="assertive">
      <div className="global-error-content">
        <strong>Error</strong>
        <p>{message}</p>
      </div>
      <button
        type="button"
        className="global-error-close"
        onClick={() => setMessage('')}
        aria-label="Close error notification"
      >
        x
      </button>
    </div>
  );
};

export default GlobalErrorNotification;
