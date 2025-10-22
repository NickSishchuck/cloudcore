import { I18n } from './translations.js';

// Initialize I18n
const i18n = new I18n();

// Tabs functionality
const tabs = document.querySelectorAll('.sidebar-item');
const tabContents = document.querySelectorAll('.tab-content');

tabs.forEach((tab) => {
    tab.addEventListener('click', () => {
        tabs.forEach((t) => {
            t.classList.remove('active');
            t.setAttribute('aria-selected', 'false');
        });
        tabContents.forEach((tc) => tc.classList.remove('active'));
        tab.classList.add('active');
        tab.setAttribute('aria-selected', 'true');
        document.getElementById(tab.getAttribute('aria-controls')).classList.add('active');
    });
});

// Back button
const backBtn = document.getElementById('backBtn');
if (backBtn) {
    backBtn.addEventListener('click', () => {
        window.location.href = '../index.html';
    });
}

// Plan buttons functionality
const planButtons = document.querySelectorAll('.plan-btn-upgrade');
planButtons.forEach((btn) => {
    btn.addEventListener('click', () => {
        const card = btn.closest('.plan-card');
        const plan = card.dataset.plan;
        alert(i18n.t('subscribe') + ': ' + plan);
    });
});

// Logout button
const logoutBtn = document.getElementById('logoutBtn');
if (logoutBtn) {
    logoutBtn.addEventListener('click', () => {
        if (confirm(i18n.t('signOutMessage'))) {
            alert(i18n.t('signOut'));
        }
    });
}

// Theme toggle functionality
const themeToggleBtn = document.getElementById('themeToggleBtn');

function updateThemeIcon() {
    if (!themeToggleBtn) return;
    const currentTheme = document.documentElement.getAttribute('data-theme');
    const icon = themeToggleBtn.querySelector('.material-symbols-outlined');
    if (icon) {
        icon.textContent = currentTheme === 'dark' ? 'dark_mode' : 'light_mode';
    }
}

// Initialize icon
updateThemeIcon();

// Toggle theme
if (themeToggleBtn) {
    themeToggleBtn.addEventListener('click', () => {
        const currentTheme = document.documentElement.getAttribute('data-theme');
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-theme', newTheme);
        localStorage.setItem('cloudcore-theme', newTheme);
        updateThemeIcon();
    });
}

// Language toggle functionality
const languageBtn = document.getElementById('languageBtn');
if (languageBtn) {
    languageBtn.addEventListener('click', () => {
        i18n.switchLanguage();
    });
}

// Initialize UI with translations
i18n.updateUI();
