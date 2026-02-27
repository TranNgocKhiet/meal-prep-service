// Delivery Hub Client
(function () {
    let deliveryHub = null;

    function initializeDeliveryHub() {
        deliveryHub = SignalRManager.getConnection('DeliveryHub');
        if (!deliveryHub) {
            console.error('DeliveryHub not initialized');
            return;
        }

        // Listen for delivery updates
        deliveryHub.on('ReceiveDeliveryUpdate', (deliveryId, status, location, message) => {
            console.log(`Delivery ${deliveryId} updated: ${status} at ${location}`);
            handleDeliveryUpdate(deliveryId, status, location, message);
        });
    }

    function handleDeliveryUpdate(deliveryId, status, location, message) {
        // Update delivery status in UI
        const deliveryElement = document.querySelector(`[data-delivery-id="${deliveryId}"]`);
        if (deliveryElement) {
            const statusElement = deliveryElement.querySelector('.delivery-status');
            if (statusElement) {
                statusElement.textContent = status;
                statusElement.className = `delivery-status badge ${getStatusClass(status)}`;
            }

            const locationElement = deliveryElement.querySelector('.delivery-location');
            if (locationElement && location) {
                locationElement.textContent = location;
            }
        }

        // Show notification
        showNotification('Delivery Update', message, 'info');

        // Trigger custom event
        const event = new CustomEvent('delivery-updated', {
            detail: { deliveryId, status, location, message }
        });
        document.dispatchEvent(event);
    }

    function getStatusClass(status) {
        const statusMap = {
            'pending': 'bg-warning text-dark',
            'assigned': 'bg-info',
            'in_transit': 'bg-primary',
            'delivered': 'bg-success',
            'failed': 'bg-danger'
        };
        return statusMap[status.toLowerCase()] || 'bg-secondary';
    }

    function joinDeliveryGroup(deliveryId) {
        if (deliveryHub && deliveryHub.state === signalR.HubConnectionState.Connected) {
            deliveryHub.invoke('JoinDeliveryGroup', deliveryId)
                .then(() => console.log(`Joined delivery group ${deliveryId}`))
                .catch(err => console.error('Error joining delivery group:', err));
        }
    }

    function leaveDeliveryGroup(deliveryId) {
        if (deliveryHub && deliveryHub.state === signalR.HubConnectionState.Connected) {
            deliveryHub.invoke('LeaveDeliveryGroup', deliveryId)
                .then(() => console.log(`Left delivery group ${deliveryId}`))
                .catch(err => console.error('Error leaving delivery group:', err));
        }
    }

    function showNotification(title, message, type) {
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
            setTimeout(initializeDeliveryHub, 1000);
        }
    });

    // Expose functions globally
    window.DeliveryHubClient = {
        joinDeliveryGroup,
        leaveDeliveryGroup
    };
})();
