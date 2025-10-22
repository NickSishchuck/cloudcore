import { ApiClient } from './api.js';
import { I18n } from './translations.js';

const api = new ApiClient();
const i18n = new I18n();
const messageDiv = document.getElementById('message');
const loginBtn = document.getElementById('loginBtn');

async function verifyEmail() {
    const params = new URLSearchParams(window.location.search);
    const token = params.get('token');
    const type = params.get('type'); // 'verify' or 'change'

    if (!token) {
        messageDiv.textContent = i18n.t('vereficationTokenMissing');
        showLoginButton();
        return;
    }

    try {
        let result;
        console.log('Type:', type);
        console.log('Token:', token);
        if (type === 'change') {
            console.log('Calling confirmEmailChange');
            result = await api.confirmEmailChange(token);
            messageDiv.textContent = i18n.t('emailChangeSuccess') || 'Email successfully changed!';
        } else {
            console.log('Calling verifyEmailToken');
            result = await api.verifyEmailToken(token);
            if (result.token) {
                localStorage.setItem('cloudcore_token', result.token);
            }
            messageDiv.textContent = i18n.t('verificationSuccess');
        }

        showLoginButton();
    } catch (err) {
        messageDiv.textContent =
            type === 'change' ? i18n.t('emailChangeFailed') || 'Email change failed' : i18n.t('verificationFailed');
        showLoginButton();
    }
}

function showLoginButton() {
    if (loginBtn) {
        loginBtn.style.display = 'block';
    }
}

document.addEventListener('DOMContentLoaded', verifyEmail);
