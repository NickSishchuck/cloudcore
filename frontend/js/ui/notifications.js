// notifications.js

class NotificationManager {
    constructor(i18n = null) {
        this.i18n = i18n;
        this.container = document.getElementById('notificationContainer');
        if (!this.container) {
            this.container = document.createElement('div');
            this.container.id = 'notificationContainer';
            this.container.className = 'notification-container';
            document.body.appendChild(this.container);
        }
        this.notifications = new Map();
    }


    setI18n(i18n) {
        this.i18n = i18n;
    }


    show(message, type = 'info', duration = 5000, title = null, params = {}) {
        const id = `notification-${Date.now()}-${Math.random()}`;
        

        const translatedMessage = this.i18n ? this.i18n.t(message, params) : message;
        

        const notification = document.createElement('div');
        notification.className = `notification ${type}`;
        notification.id = id;
        notification.setAttribute('role', 'alert');
        notification.setAttribute('aria-live', 'polite');

        const config = this._getTypeConfig(type, title);
        
        const translatedTitle = config.title && this.i18n 
            ? this.i18n.t(config.title) 
            : config.title;

        notification.innerHTML = `
            <div class="notification-icon-wrapper">
                <span class="notification-icon material-symbols-outlined">${config.icon}</span>
            </div>
            <div class="notification-content">
                ${translatedTitle ? `<div class="notification-title">${translatedTitle}</div>` : ''}
                <div class="notification-message">${translatedMessage}</div>
            </div>
            <button class="notification-close" aria-label="${this.i18n ? this.i18n.t('close') : 'Close'}">
                <span class="material-symbols-outlined">close</span>
            </button>
            ${duration > 0 ? '<div class="notification-progress"></div>' : ''}
        `;


        const closeBtn = notification.querySelector('.notification-close');
        closeBtn.addEventListener('click', () => this.hide(id));


        this.container.appendChild(notification);
        this.notifications.set(id, notification);


        requestAnimationFrame(() => {
            notification.classList.add('show');
        });

        if (duration > 0) {
            setTimeout(() => this.hide(id), duration);
        }

        return id;
    }


    hide(id) {
        const notification = this.notifications.get(id);
        if (!notification) return;

        notification.classList.remove('show');
        notification.classList.add('hide');

        setTimeout(() => {
            notification.remove();
            this.notifications.delete(id);
        }, 300);
    }


    hideAll() {
        this.notifications.forEach((_, id) => this.hide(id));
    }


    _getTypeConfig(type, customTitle) {
        const configs = {
            success: {
                icon: 'check_circle',
                title: customTitle || 'notificationSuccess'
            },
            error: {
                icon: 'error',
                title: customTitle || 'notificationError'
            },
            warning: {
                icon: 'warning',
                title: customTitle || 'notificationWarning'
            },
            info: {
                icon: 'info',
                title: customTitle || 'notificationInfo'
            }
        };

        return configs[type] || configs.info;
    }

    success(message, duration = 5000, title = null, params = {}) {
        return this.show(message, 'success', duration, title, params);
    }

    error(message, duration = 7000, title = null, params = {}) {
        return this.show(message, 'error', duration, title, params);
    }

    warning(message, duration = 6000, title = null, params = {}) {
        return this.show(message, 'warning', duration, title, params);
    }

    info(message, duration = 5000, title = null, params = {}) {
        return this.show(message, 'info', duration, title, params);
    }
}

// Singleton instance
let notificationManagerInstance = null;

export function getNotificationManager(i18n = null) {
    if (!notificationManagerInstance) {
        notificationManagerInstance = new NotificationManager(i18n);
    } else if (i18n && !notificationManagerInstance.i18n) {
        notificationManagerInstance.setI18n(i18n);
    }
    return notificationManagerInstance;
}
