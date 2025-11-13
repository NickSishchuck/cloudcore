import { writable } from 'svelte/store';

function createNotificationStore() {
    const { subscribe, update } = writable([]);

    return {
        subscribe,
        add: (notification) => {
            const id = Date.now() + Math.random();
            update(notifications => [...notifications, { ...notification, id }]);
            return id;
        },
        remove: (id) => {
            update(notifications => notifications.filter(n => n.id !== id));
        },
        success: (message, title = 'Success', duration = 5000) => {
            return createNotificationStore().add({ type: 'success', title, message, duration });
        },
        error: (message, title = 'Error', duration = 7000) => {
            return createNotificationStore().add({ type: 'error', title, message, duration });
        },
        warning: (message, title = 'Warning', duration = 6000) => {
            return createNotificationStore().add({ type: 'warning', title, message, duration });
        },
        info: (message, title = 'Info', duration = 5000) => {
            return createNotificationStore().add({ type: 'info', title, message, duration });
        }
    };
}

export const notifications = createNotificationStore();
