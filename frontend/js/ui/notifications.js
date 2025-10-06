export class NotificationManager {
    constructor() {
        this.notificationElement = document.getElementById('notification');
        this.autoHideTimeout = null;
    }

    show(message, type = 'info', duration = 3000) {
        if (!this.notificationElement) {
            console.warn('Notification element not found');
            return;
        }

        // Clear any existing timeout
        if (this.autoHideTimeout) {
            clearTimeout(this.autoHideTimeout);
        }

        this.notificationElement.textContent = message;
        this.notificationElement.className = `notification ${type}`;
        this.notificationElement.style.display = 'block';

        // Auto-hide after duration
        if (duration > 0) {
            this.autoHideTimeout = setTimeout(() => {
                this.hide();
            }, duration);
        }
    }

    hide() {
        if (this.notificationElement) {
            this.notificationElement.style.display = 'none';
        }
    }

    success(message, duration = 3000) {
        this.show(`✅ ${message}`, 'success', duration);
    }

    error(message, duration = 5000) {
        this.show(`❌ ${message}`, 'error', duration);
    }

    info(message, duration = 3000) {
        this.show(message, 'info', duration);
    }

    warning(message, duration = 4000) {
        this.show(`⚠️ ${message}`, 'warning', duration);
    }
}

// Singleton instance
let notificationManager = null;

export function getNotificationManager() {
    if (!notificationManager) {
        notificationManager = new NotificationManager();
    }
    return notificationManager;
}
