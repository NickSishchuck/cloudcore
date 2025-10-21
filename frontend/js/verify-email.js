import { ApiClient } from './api.js';
import { I18n } from './translations.js';

const api = new ApiClient();
const i18n = new I18n();

const messageDiv = document.getElementById('message');
const loginBtn = document.getElementById('loginBtn');

async function verifyEmail() {
    const params = new URLSearchParams(window.location.search);
    const token = params.get('token');
    if (!token) {
        messageDiv.textContent = i18n.t('vereficationTokenMissing');
        showLoginButton();
        return;
    }

    try {
        const result = await api.verifyEmailToken(token);
        if (result.token) {
            localStorage.setItem('cloudcore_token', result.token);
        }
        messageDiv.textContent = i18n.t('verificationSuccess');
        showLoginButton();
    } catch (err) {
        messageDiv.textContent = i18n.t('verificationFailed');
        showLoginButton();
    }
}

function showLoginButton() {
    if (loginBtn) {
        loginBtn.style.display = 'block';
    }
}

document.addEventListener('DOMContentLoaded', verifyEmail);
