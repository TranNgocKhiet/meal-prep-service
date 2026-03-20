// Order Hub Client
(function () {
    let orderHub = null;
    let initialized = false;

    function initializeOrderHub() {
        if (initialized) {
            return;
        }

        if (typeof SignalRManager === 'undefined') {
            return;
        }

        orderHub = SignalRManager.getConnection('OrderHub');
        if (!orderHub) {
            setTimeout(initializeOrderHub, 250);
            return;
        }

        initialized = true;

        // Listen for order status updates
        orderHub.on('ReceiveOrderStatusUpdate', (orderId, status, message) => {
            console.log(`Order ${orderId} status updated to ${status}: ${message}`);
            handleOrderStatusUpdate(orderId, status, message);
        });

        // If this is Order Details, auto-join the specific order group.
        orderHub.onreconnected(() => {
            const detailOrderId = (window.__orderPageOrderId || '').toString();
            if (detailOrderId) {
                joinOrderGroup(detailOrderId);
            }
        });

        const detailOrderId = (window.__orderPageOrderId || '').toString();
        if (detailOrderId) {
            joinOrderGroup(detailOrderId);
            window.addEventListener('beforeunload', () => {
                leaveOrderGroup(detailOrderId);
            });
        }
    }

    function handleOrderStatusUpdate(orderId, status, message) {
        // Update UI elements
        const orderStatusBadge = document.querySelector(`[data-order-id="${orderId}"] .order-status-badge`);
        if (orderStatusBadge) {
            updateStatusBadge(orderStatusBadge, status);
        }

        const orderProgressIndicator = document.querySelector(`[data-order-id="${orderId}"] .order-progress`);
        if (orderProgressIndicator) {
            updateProgressIndicator(orderProgressIndicator, status);
        }

        // Show notification
        showNotification('Order Update', message, 'info');

        // Trigger custom event for page-specific handling
        const event = new CustomEvent('order-status-updated', {
            detail: { orderId, status, message }
        });
        document.dispatchEvent(event);
    }

    function updateStatusBadge(element, status) {
        const statusMap = {
            'pending': { class: 'bg-warning text-dark', icon: 'bi-clock', text: 'Pending' },
            'preparing': { class: 'bg-warning text-dark', icon: 'bi-fire', text: 'Preparing' },
            'confirmed': { class: 'bg-success', icon: 'bi-check-circle', text: 'Confirmed' },
            'prepared': { class: 'bg-success', icon: 'bi-check2-all', text: 'Prepared' },
            'customer_received': { class: 'bg-success', icon: 'bi-check2-circle', text: 'Completed' },
            'delivered': { class: 'bg-success', icon: 'bi-box-seam', text: 'Completed' },
            'pending_payment': { class: 'bg-primary', icon: 'bi-hourglass-split', text: 'Pending Payment' },
            'awaiting_online_payment': { class: 'bg-primary', icon: 'bi-credit-card', text: 'Awaiting VNPay' },
            'pending_confirmation': { class: 'bg-primary', icon: 'bi-telephone', text: 'Pending Confirmation' },
            'preparing_failed': { class: 'bg-danger', icon: 'bi-exclamation-triangle', text: 'Preparing Failed' },
            'delivering': { class: 'bg-primary', icon: 'bi-truck', text: 'On Scheduled' },
            'payment_failed': { class: 'bg-danger', icon: 'bi-x-circle', text: 'Payment Fail' },
            'cancelled': { class: 'bg-danger', icon: 'bi-slash-circle', text: 'Cancelled' },
            'failed': { class: 'bg-danger', icon: 'bi-exclamation-octagon', text: 'Delivery Fail' },
            'customer_reject': { class: 'bg-danger', icon: 'bi-x-octagon', text: 'Customer Reject' }
        };

        const statusInfo = statusMap[status.toLowerCase()] || statusMap['pending'];
        element.className = `badge ${statusInfo.class}`;
        element.innerHTML = `<i class="${statusInfo.icon}"></i> ${statusInfo.text}`;
    }

    function updateProgressIndicator(element, status) {
        // Reload the progress indicator or update it dynamically
        // This is a simplified version - you might want to fetch updated HTML
        element.classList.add('updating');
        setTimeout(() => {
            element.classList.remove('updating');
        }, 500);
    }

    function joinOrderGroup(orderId) {
        if (orderHub && orderHub.state === signalR.HubConnectionState.Connected) {
            orderHub.invoke('JoinOrderGroup', orderId)
                .then(() => console.log(`Joined order group ${orderId}`))
                .catch(err => console.error('Error joining order group:', err));
        }
    }

    function leaveOrderGroup(orderId) {
        if (orderHub && orderHub.state === signalR.HubConnectionState.Connected) {
            orderHub.invoke('LeaveOrderGroup', orderId)
                .then(() => console.log(`Left order group ${orderId}`))
                .catch(err => console.error('Error leaving order group:', err));
        }
    }

    function showNotification(title, message, type) {
        // Simple notification - you can enhance this with a toast library
        if ('Notification' in window && Notification.permission === 'granted') {
            new Notification(title, {
                body: message,
                icon: '/favicon.ico'
            });
        }
    }

    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', function () {
        if (document.body.dataset.authenticated === 'true') {
            setTimeout(initializeOrderHub, 500);
        }
    });

    // Expose functions globally
    window.OrderHubClient = {
        joinOrderGroup,
        leaveOrderGroup
    };
})();
