// Notification Hub Client
(function () {
    let notificationHub = null;

    function initializeNotificationHub() {
        notificationHub = SignalRManager.getConnection('NotificationHub');
        if (!notificationHub) {
            console.error('NotificationHub not initialized');
            return;
        }

        // Listen for notifications
        notificationHub.on('ReceiveNotification', (message, type) => {
            console.log(`Notification received: ${type} - ${message}`);
            handleNotification(message, type);
        });

        // Join user-specific group if user ID is available
        const userId = document.body.dataset.userId;
        if (userId) {
            joinUserGroup(parseInt(userId));
        }
    }

    function handleNotification(message, type) {
        // Show toast notification
        showToast(message, type);

        // Show browser notification if permitted
        if ('Notification' in window && Notification.permission === 'granted') {
            new Notification('Meal Prep Service', {
                body: message,
                icon: '/favicon.ico',
                badge: '/favicon.ico'
            });
        }

        // Trigger custom event
        const event = new CustomEvent('notification-received', {
            detail: { message, type }
        });
        document.dispatchEvent(event);
    }

    function showToast(message, type) {
        const alertClass = getAlertClass(type);
        const icon = getIcon(type);
        
        const toast = document.createElement('div');
        toast.className = `alert ${alertClass} alert-dismissible fade show position-fixed`;
        toast.style.cssText = 'top: 80px; right: 20px; z-index: 9999; min-width: 350px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);';
        toast.innerHTML = `
            <div class="d-flex align-items-center">
                <i class="${icon} me-2" style="font-size: 1.5rem;"></i>
                <div class="flex-grow-1">${message}</div>
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;
        document.body.appendChild(toast);

        // Auto-dismiss after 5 seconds
        setTimeout(() => {
            if (toast.parentNode) {
                toast.classList.remove('show');
                setTimeout(() => toast.remove(), 150);
            }
        }, 5000);
    }

    function getAlertClass(type) {
        const typeMap = {
            'success': 'alert-success',
            'info': 'alert-info',
            'warning': 'alert-warning',
            'error': 'alert-danger',
            'danger': 'alert-danger'
        };
        return typeMap[type.toLowerCase()] || 'alert-info';
    }

    function getIcon(type) {
        const iconMap = {
            'success': 'bi bi-check-circle-fill',
            'info': 'bi bi-info-circle-fill',
            'warning': 'bi bi-exclamation-triangle-fill',
            'error': 'bi bi-x-circle-fill',
            'danger': 'bi bi-x-circle-fill'
        };
        return iconMap[type.toLowerCase()] || 'bi bi-bell-fill';
    }

    function joinUserGroup(userId) {
        if (notificationHub && notificationHub.state === signalR.HubConnectionState.Connected) {
            notificationHub.invoke('JoinUserGroup', userId)
                .then(() => console.log(`Joined user notification group ${userId}`))
                .catch(err => console.error('Error joining user group:', err));
        }
    }

    function leaveUserGroup(userId) {
        if (notificationHub && notificationHub.state === signalR.HubConnectionState.Connected) {
            notificationHub.invoke('LeaveUserGroup', userId)
                .then(() => console.log(`Left user notification group ${userId}`))
                .catch(err => console.error('Error leaving user group:', err));
        }
    }

    function requestNotificationPermission() {
        if ('Notification' in window && Notification.permission === 'default') {
            Notification.requestPermission().then(permission => {
                console.log('Notification permission:', permission);
            });
        }
    }

    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', function () {
        if (document.body.dataset.authenticated === 'true') {
            setTimeout(initializeNotificationHub, 1000);
            requestNotificationPermission();
        }
    });

    // Expose functions globally
    window.NotificationHubClient = {
        joinUserGroup,
        leaveUserGroup,
        requestNotificationPermission
    };
})();
