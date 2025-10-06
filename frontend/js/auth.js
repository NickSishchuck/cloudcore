import { I18n } from './translations.js';
import { ApiClient } from './api.js';

class AuthManager {
    constructor() {
        this.i18n = new I18n();
        this.api = new ApiClient();
        this.checkExistingAuth();
    }

    checkExistingAuth() {
        const token = localStorage.getItem('cloudcore_token');
        if (token) {
            console.log('User already logged in, redirecting...');
            window.location.href = 'index.html';
            return true;
        }
        return false;
    }

    showError(message) {
        const errorDiv = document.getElementById('error-message');
        if (errorDiv) {
            errorDiv.textContent = message;
            errorDiv.style.display = 'block';
        }
        this.hideSuccess();
    }

    showSuccess(message) {
        const successDiv = document.getElementById('success-message');
        if (successDiv) {
            successDiv.textContent = message;
            successDiv.style.display = 'block';
        }
        this.hideError();
    }

    hideError() {
        const errorDiv = document.getElementById('error-message');
        if (errorDiv) {
            errorDiv.style.display = 'none';
        }
    }

    hideSuccess() {
        const successDiv = document.getElementById('success-message');
        if (successDiv) {
            successDiv.style.display = 'none';
        }
    }

    hideMessages() {
        this.hideError();
        this.hideSuccess();
    }

    async handleLogin(username, password, button) {
        button.disabled = true;
        button.textContent = this.i18n.t('signingIn');
        this.hideMessages();

        try {
            const data = await this.api.login(username, password);
            
            // Store authentication data
            this.api.setAuthToken(data.token);
            localStorage.setItem('cloudcore_user', JSON.stringify({
                id: data.userId,
                username: data.username,
                email: data.email
            }));

            console.log('🎉 Login successful!');
            
            const welcomeMsg = this.i18n.t('welcomeBack')
                .replace('{username}', data.username);
            this.showSuccess(welcomeMsg);

            // Redirect after 1 second
            setTimeout(() => {
                window.location.href = 'index.html';
            }, 1000);

        } catch (error) {
            console.error('Login error:', error);
            
            let errorMessage = this.i18n.t('signInFailed');
            if (error.message.toLowerCase().includes('invalid')) {
                errorMessage = this.i18n.t('invalidCredentials');
            } else if (error.message) {
                errorMessage = error.message;
            }
            
            this.showError(errorMessage);
        } finally {
            button.disabled = false;
            button.textContent = this.i18n.t('signIn');
        }
    }

    async handleRegister(username, email, password, confirmPassword, button) {
        // Validate passwords match
        if (password !== confirmPassword) {
            this.showError(this.i18n.t('passwordsNoMatch'));
            return;
        }

        button.disabled = true;
        button.textContent = this.i18n.t('creatingAccount');
        this.hideMessages();

        try {
            const data = await this.api.register(username, email, password);
            
            // Store authentication data
            this.api.setAuthToken(data.token);
            localStorage.setItem('cloudcore_user', JSON.stringify({
                id: data.userId,
                username: data.username,
                email: data.email
            }));

            console.log('🎉 Registration successful!');
            
            const successMsg = this.i18n.t('accountCreated')
                .replace('{username}', data.username);
            this.showSuccess(successMsg);

            // Redirect after 2 seconds
            setTimeout(() => {
                window.location.href = 'index.html';
            }, 2000);

        } catch (error) {
            console.error('Registration error:', error);
            
            let errorMessage = this.i18n.t('registrationFailed');
            if (error.message) {
                errorMessage = error.message;
            }
            
            this.showError(errorMessage);
        } finally {
            button.disabled = false;
            button.textContent = this.i18n.t('createAccount');
        }
    }

    setupLanguageSwitcher() {
        const languageBtn = document.getElementById('languageBtn');
        if (languageBtn) {
            languageBtn.addEventListener('click', () => {
                console.log('Language switch clicked');
                this.i18n.switchLanguage();
                location.reload();
            });
        }
    }

    initializeLoginPage() {
        this.i18n.updateUI();
        this.setupLanguageSwitcher();

        document.getElementById('loginForm')?.addEventListener('submit', async (e) => {
            e.preventDefault();
            
            const username = document.getElementById('username').value;
            const password = document.getElementById('password').value;
            const button = document.getElementById('loginBtn');
            
            await this.handleLogin(username, password, button);
        });
    }

    initializeRegisterPage() {
        this.i18n.updateUI();
        this.setupLanguageSwitcher();

        // Real-time password confirmation validation
        document.getElementById('confirmPassword')?.addEventListener('input', function() {
            const password = document.getElementById('password').value;
            const confirmPassword = this.value;

            if (confirmPassword && password !== confirmPassword) {
                this.style.borderColor = '#d93025';
            } else {
                this.style.borderColor = '#dadce0';
            }
        });

        document.getElementById('registerForm')?.addEventListener('submit', async (e) => {
            e.preventDefault();
            
            const username = document.getElementById('username').value;
            const email = document.getElementById('email').value;
            const password = document.getElementById('password').value;
            const confirmPassword = document.getElementById('confirmPassword').value;
            const button = document.getElementById('registerBtn');
            
            await this.handleRegister(username, email, password, confirmPassword, button);
        });
    }
}

// Export initialization functions
export function initLogin() {
    const authManager = new AuthManager();
    authManager.initializeLoginPage();
}

export function initRegister() {
    const authManager = new AuthManager();
    authManager.initializeRegisterPage();
}

// Auto-initialize based on current page
document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('loginForm')) {
        initLogin();
    } else if (document.getElementById('registerForm')) {
        initRegister();
    }
});
