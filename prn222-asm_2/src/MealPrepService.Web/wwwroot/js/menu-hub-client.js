// Menu Hub Client
(function () {
    let menuHub = null;

    function initializeMenuHub() {
        menuHub = SignalRManager.getConnection('MenuHub');
        if (!menuHub) {
            console.error('MenuHub not initialized');
            return;
        }

        // Listen for menu updates
        menuHub.on('ReceiveMenuUpdate', (menuDate, mealId, quantityRemaining) => {
            console.log(`Menu updated: Meal ${mealId} on ${menuDate} - ${quantityRemaining} remaining`);
            handleMenuUpdate(menuDate, mealId, quantityRemaining);
        });

        // Listen for availability alerts
        menuHub.on('ReceiveMenuAvailabilityAlert', (menuDate, mealId, mealName, isAvailable) => {
            console.log(`Menu availability: ${mealName} is ${isAvailable ? 'available' : 'sold out'}`);
            handleAvailabilityAlert(menuDate, mealId, mealName, isAvailable);
        });
    }

    function handleMenuUpdate(menuDate, mealId, quantityRemaining) {
        // Update quantity display
        const quantityElements = document.querySelectorAll(`[data-meal-id="${mealId}"][data-menu-date="${menuDate}"] .quantity-remaining`);
        quantityElements.forEach(element => {
            element.textContent = quantityRemaining;
            
            // Add visual feedback
            element.classList.add('quantity-updated');
            setTimeout(() => {
                element.classList.remove('quantity-updated');
            }, 1000);

            // Update availability status
            if (quantityRemaining === 0) {
                const mealCard = element.closest('.meal-card');
                if (mealCard) {
                    mealCard.classList.add('sold-out');
                    const orderButton = mealCard.querySelector('.order-button');
                    if (orderButton) {
                        orderButton.disabled = true;
                        orderButton.textContent = 'Sold Out';
                    }
                }
            }
        });

        // Trigger custom event
        const event = new CustomEvent('menu-updated', {
            detail: { menuDate, mealId, quantityRemaining }
        });
        document.dispatchEvent(event);
    }

    function handleAvailabilityAlert(menuDate, mealId, mealName, isAvailable) {
        const message = isAvailable 
            ? `${mealName} is now available!` 
            : `${mealName} is sold out`;
        
        showNotification('Menu Update', message, isAvailable ? 'success' : 'warning');

        // Update UI
        const mealCards = document.querySelectorAll(`[data-meal-id="${mealId}"][data-menu-date="${menuDate}"]`);
        mealCards.forEach(card => {
            if (isAvailable) {
                card.classList.remove('sold-out');
                const orderButton = card.querySelector('.order-button');
                if (orderButton) {
                    orderButton.disabled = false;
                    orderButton.textContent = 'Order Now';
                }
            } else {
                card.classList.add('sold-out');
                const orderButton = card.querySelector('.order-button');
                if (orderButton) {
                    orderButton.disabled = true;
                    orderButton.textContent = 'Sold Out';
                }
            }
        });

        // Trigger custom event
        const event = new CustomEvent('menu-availability-changed', {
            detail: { menuDate, mealId, mealName, isAvailable }
        });
        document.dispatchEvent(event);
    }

    function showNotification(title, message, type) {
        // Simple toast notification
        const toast = document.createElement('div');
        toast.className = `alert alert-${type === 'success' ? 'success' : type === 'warning' ? 'warning' : 'info'} alert-dismissible fade show position-fixed`;
        toast.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
        toast.innerHTML = `
            <strong>${title}</strong> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        document.body.appendChild(toast);

        setTimeout(() => {
            toast.remove();
        }, 5000);
    }

    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', function () {
        setTimeout(initializeMenuHub, 1000);
    });

    // Expose functions globally
    window.MenuHubClient = {
        // No specific methods needed for menu hub as it's broadcast only
    };
})();
