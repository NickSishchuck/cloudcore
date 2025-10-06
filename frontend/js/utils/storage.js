export class StorageManager {
    constructor(prefix = 'cloudcore_') {
        this.prefix = prefix;
    }

    set(key, value) {
        try {
            const prefixedKey = this.prefix + key;
            const serialized = JSON.stringify(value);
            localStorage.setItem(prefixedKey, serialized);
            return true;
        } catch (error) {
            console.error('Storage set error:', error);
            return false;
        }
    }

    get(key, defaultValue = null) {
        try {
            const prefixedKey = this.prefix + key;
            const item = localStorage.getItem(prefixedKey);
            
            if (item === null) {
                return defaultValue;
            }
            
            return JSON.parse(item);
        } catch (error) {
            console.error('Storage get error:', error);
            return defaultValue;
        }
    }

    remove(key) {
        try {
            const prefixedKey = this.prefix + key;
            localStorage.removeItem(prefixedKey);
            return true;
        } catch (error) {
            console.error('Storage remove error:', error);
            return false;
        }
    }

    clear() {
        try {
            const keys = Object.keys(localStorage);
            keys.forEach(key => {
                if (key.startsWith(this.prefix)) {
                    localStorage.removeItem(key);
                }
            });
            return true;
        } catch (error) {
            console.error('Storage clear error:', error);
            return false;
        }
    }

    has(key) {
        const prefixedKey = this.prefix + key;
        return localStorage.getItem(prefixedKey) !== null;
    }

    // Auth-specific helpers
    getAuthToken() {
        return this.get('token');
    }

    setAuthToken(token) {
        return this.set('token', token);
    }

    getUser() {
        return this.get('user');
    }

    setUser(user) {
        return this.set('user', user);
    }

    clearAuth() {
        this.remove('token');
        this.remove('user');
    }

    // Language preference
    getLanguage() {
        return localStorage.getItem('cloudcore-language') || 'en';
    }

    setLanguage(lang) {
        localStorage.setItem('cloudcore-language', lang);
    }

    // Settings
    getSettings() {
        return this.get('settings', {});
    }

    setSettings(settings) {
        return this.set('settings', settings);
    }

    getSetting(key, defaultValue = null) {
        const settings = this.getSettings();
        return settings[key] !== undefined ? settings[key] : defaultValue;
    }

    setSetting(key, value) {
        const settings = this.getSettings();
        settings[key] = value;
        return this.setSettings(settings);
    }
}

// Singleton instance
let storageManager = null;

export function getStorageManager() {
    if (!storageManager) {
        storageManager = new StorageManager();
    }
    return storageManager;
}
