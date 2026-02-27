// SignalR Connection Manager
const SignalRManager = (function () {
    let connections = {};
    let reconnectAttempts = {};
    const maxReconnectAttempts = 5;
    const reconnectDelay = 3000;

    function createConnection(hubUrl, hubName) {
        if (connections[hubName]) {
            return connections[hubName];
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: retryContext => {
                    if (retryContext.elapsedMilliseconds < 60000) {
                        return Math.random() * 10000;
                    } else {
                        return null;
                    }
                }
            })
            .configureLogging(signalR.LogLevel.Information)
            .build();

        connection.onreconnecting(error => {
            console.log(`${hubName} connection lost. Reconnecting...`, error);
            updateConnectionStatus(hubName, 'reconnecting');
        });

        connection.onreconnected(connectionId => {
            console.log(`${hubName} reconnected. Connection ID: ${connectionId}`);
            updateConnectionStatus(hubName, 'connected');
            reconnectAttempts[hubName] = 0;
        });

        connection.onclose(error => {
            console.log(`${hubName} connection closed.`, error);
            updateConnectionStatus(hubName, 'disconnected');
            
            if (reconnectAttempts[hubName] < maxReconnectAttempts) {
                setTimeout(() => {
                    reconnectAttempts[hubName] = (reconnectAttempts[hubName] || 0) + 1;
                    startConnection(hubName);
                }, reconnectDelay);
            }
        });

        connections[hubName] = connection;
        reconnectAttempts[hubName] = 0;
        return connection;
    }

    function startConnection(hubName) {
        const connection = connections[hubName];
        if (!connection) {
            console.error(`No connection found for ${hubName}`);
            return Promise.reject();
        }

        return connection.start()
            .then(() => {
                console.log(`${hubName} connected successfully`);
                updateConnectionStatus(hubName, 'connected');
            })
            .catch(err => {
                console.error(`Error connecting to ${hubName}:`, err);
                updateConnectionStatus(hubName, 'error');
                throw err;
            });
    }

    function stopConnection(hubName) {
        const connection = connections[hubName];
        if (connection) {
            return connection.stop()
                .then(() => {
                    console.log(`${hubName} disconnected`);
                    updateConnectionStatus(hubName, 'disconnected');
                });
        }
        return Promise.resolve();
    }

    function getConnection(hubName) {
        return connections[hubName];
    }

    function updateConnectionStatus(hubName, status) {
        const event = new CustomEvent('signalr-status-changed', {
            detail: { hubName, status }
        });
        document.dispatchEvent(event);
    }

    return {
        createConnection,
        startConnection,
        stopConnection,
        getConnection
    };
})();

// Initialize connections when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    // Check if user is authenticated
    const isAuthenticated = document.body.dataset.authenticated === 'true';
    
    if (isAuthenticated) {
        // Initialize authenticated hubs
        initializeAuthenticatedHubs();
    }
    
    // Always initialize public hubs
    initializePublicHubs();
});

function initializeAuthenticatedHubs() {
    // Order Hub
    const orderHub = SignalRManager.createConnection('/hubs/order', 'OrderHub');
    SignalRManager.startConnection('OrderHub').catch(err => {
        console.error('Failed to start OrderHub:', err);
    });

    // Delivery Hub
    const deliveryHub = SignalRManager.createConnection('/hubs/delivery', 'DeliveryHub');
    SignalRManager.startConnection('DeliveryHub').catch(err => {
        console.error('Failed to start DeliveryHub:', err);
    });

    // Notification Hub
    const notificationHub = SignalRManager.createConnection('/hubs/notification', 'NotificationHub');
    SignalRManager.startConnection('NotificationHub').catch(err => {
        console.error('Failed to start NotificationHub:', err);
    });
}

function initializePublicHubs() {
    // Menu Hub (public)
    const menuHub = SignalRManager.createConnection('/hubs/menu', 'MenuHub');
    SignalRManager.startConnection('MenuHub').catch(err => {
        console.error('Failed to start MenuHub:', err);
    });
}

// Cleanup on page unload
window.addEventListener('beforeunload', function () {
    Object.keys(SignalRManager.getConnection).forEach(hubName => {
        SignalRManager.stopConnection(hubName);
    });
});
