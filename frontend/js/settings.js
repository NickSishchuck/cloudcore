import { I18n } from './translations.js';
import { ApiClient } from './api.js';
import { getNotificationManager } from './ui/notifications.js';

class SettingsManager {
    constructor() {
        this.i18n = new I18n();
        this.api = new ApiClient();
        this.notifications = getNotificationManager(this.i18n);

        // User state
        this.currentUserId = null;
        this.currentUser = null;
        this.currentPlan = 'free'; // default

        // DOM elements
        this.nicknameDisplay = document.getElementById('nicknameDisplay');
        this.emailDisplay = document.getElementById('emailDisplay');
        this.currentPlanElement = document.getElementById('currentPlan');
        this.storageBarFill = document.getElementById('storageBarFill');
        this.storageInfo = document.getElementById('storageInfo');
    }

    /**
     * Initialize settings page
     */
    async initialize() {
        try {
            // Get userId from token
            this.currentUserId = this.getUserIdFromToken();

            if (!this.currentUserId) {
                this.notifications.error('accessDenied');
                setTimeout(() => (window.location.href = '../login.html'), 2000);
                return;
            }

            // Load user data from localStorage
            this.loadUserDataFromStorage();

            // Load storage info from API
            await this.loadStorageInfo();

            // Initialize UI components
            this.initializeTabs();
            this.initializeEventHandlers();
            this.initializeTheme();

            // Update translations
            this.i18n.updateUI();
        } catch (error) {
            console.error('Error initializing settings:', error);
            this.notifications.error('unexpectedError');
        }
    }

    /**
     * Decode JWT token to get userId
     */
    getUserIdFromToken() {
        const token = localStorage.getItem('cloudcore_token');
        if (!token) return null;

        try {
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(
                atob(base64)
                    .split('')
                    .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
                    .join('')
            );

            const payload = JSON.parse(jsonPayload);

            return (
                payload.userId ||
                payload.sub ||
                payload.id ||
                payload.nameid ||
                payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
            );
        } catch (error) {
            console.error('Error decoding JWT token:', error);
            return null;
        }
    }

    /**
     * Determine plan type based on limitMb
     * free: 10240 MB (10 GB)
     * premium: 20480 MB (20 GB)
     * enterprise: 51200 MB (50 GB)
     */
    determinePlanByLimit(limitMb) {
        if (limitMb >= 51200) {
            return 'enterprise'; // 50 GB or more
        } else if (limitMb >= 20480) {
            return 'premium'; // 20 GB or more
        } else {
            return 'free'; // 10 GB (default)
        }
    }

    /**
     * Load user data from localStorage
     */
    loadUserDataFromStorage() {
        const userData = JSON.parse(localStorage.getItem('cloudcore_user') || '{}');
        this.currentUser = userData;

        // Update profile display
        const username = userData.username || userData.email?.split('@')[0] || 'User';
        if (this.nicknameDisplay) {
            const greeting = this.i18n.t('welcomeBack', { username });
            this.nicknameDisplay.textContent = greeting;
        }

        if (this.emailDisplay) {
            this.emailDisplay.textContent = userData.email || this.i18n.t('itemNotFound');
        }

        // Plan will be determined from API response
        const cachedPlan = localStorage.getItem('cloudcore_userPlan') || 'free';
        this.updateProfilePlanDisplay(cachedPlan);
    }

    updateProfilePlanDisplay(planType) {
        const normalizedPlan = planType.toLowerCase();

        if (this.currentPlanElement) {
            this.currentPlanElement.textContent = this.getPlanDisplayName(normalizedPlan);
        }

        // Update plan card status
        const planCardInfo = document.querySelector('.plan-card-info');
        if (planCardInfo) {
            planCardInfo.setAttribute('data-plan-status', normalizedPlan);
        }

        // Update plan badge text
        const planBadge = document.getElementById('planStatusBadge');
        if (planBadge) {
            const badgeTexts = {
                free: this.i18n.getCurrentLanguage() === 'uk' ? 'Безкоштовний' : 'Free',
                premium: 'Premium',
                enterprise: 'Enterprise'
            };
            planBadge.textContent = badgeTexts[normalizedPlan] || 'Free';
        }

        // Update subscription tab cards
        this.updateActivePlan(normalizedPlan);
    }

    /**
     * Load storage info from API
     */
    async loadStorageInfo() {
        try {
            const storage = await this.api.getPersonalStorage(this.currentUserId);

            console.log('Storage API response:', storage);

            // Determine plan based on limitMb
            this.currentPlan = this.determinePlanByLimit(storage.limitMb);

            console.log(`Determined plan: ${this.currentPlan} (based on limitMb: ${storage.limitMb})`);

            // Cache the plan
            localStorage.setItem('cloudcore_userPlan', this.currentPlan);

            // Update plan display
            this.updateProfilePlanDisplay(this.currentPlan);

            // Update progress bar
            const percentage = storage.percentageUsed || 0;

            if (this.storageBarFill) {
                const displayWidth = percentage === 0 ? 2 : percentage;
                this.storageBarFill.style.width = `${displayWidth}%`;

                // Color coding based on usage
                if (percentage >= 90) {
                    this.storageBarFill.style.backgroundColor = '#dc3545';
                } else if (percentage >= 75) {
                    this.storageBarFill.style.backgroundColor = '#ffc107';
                } else if (percentage === 0) {
                    this.storageBarFill.style.backgroundColor = '#e0e0e0';
                } else {
                    this.storageBarFill.style.backgroundColor = '#28a745';
                }
            }

            // Update storage text with "of" translation
            if (this.storageInfo) {
                const currentLang = this.i18n.getCurrentLanguage();
                const ofText = currentLang === 'uk' ? 'з' : 'of';
                this.storageInfo.textContent = `${storage.formattedUsed || '0 MB'} ${ofText} ${
                    storage.formattedLimit || '0 MB'
                }`;
            }

            console.log('Storage info loaded:', storage);
        } catch (error) {
            console.error('Error loading storage info:', error);

            if (this.storageInfo) {
                this.storageInfo.textContent = '0 MB of 0 MB';
            }

            if (this.storageBarFill) {
                this.storageBarFill.style.width = '2%';
                this.storageBarFill.style.backgroundColor = '#e0e0e0';
            }

            // Use translated error messages
            if (error.message === 'Unauthorized') {
                this.notifications.error('accessDenied');
                setTimeout(() => (window.location.href = '../login.html'), 2000);
            } else if (error.message.includes('Network')) {
                this.notifications.error('networkError');
            } else if (error.message.includes('timeout')) {
                this.notifications.error('timeoutMessage');
            } else {
                this.notifications.error('operationFailed');
            }
        }
    }

    /**
     * Get display name for plan
     */
    getPlanDisplayName(planType) {
        const planKey = planType.toLowerCase();

        // Map plan types to translation keys
        const planTranslations = {
            free: 'Free Plan',
            premium: 'Premium Plan',
            enterprise: 'Enterprise Plan'
        };

        return planTranslations[planKey] || 'Free Plan';
    }

    /**
     * Update active plan display
     */
    updateActivePlan(currentPlan) {
        const normalizedCurrentPlan = currentPlan.toLowerCase();

        const planHierarchy = {
            free: 0,
            premium: 1,
            enterprise: 2
        };

        const currentPlanLevel = planHierarchy[normalizedCurrentPlan] || 0;

        document.querySelectorAll('.plan-card').forEach((card) => {
            const planType = card.dataset.plan;
            const planLevel = planHierarchy[planType] || 0;
            let badge = card.querySelector('.plan-badge');
            const button = card.querySelector('.plan-btn');

            card.classList.remove('plan-card-current', 'plan-card-popular');

            if (planType === normalizedCurrentPlan) {
                card.classList.add('plan-card-current');
                card.setAttribute('aria-checked', 'true');

                if (!badge) {
                    badge = document.createElement('div');
                    badge.className = 'plan-badge plan-badge-current';
                    badge.setAttribute('data-i18n', 'currentPlanBadge');
                    card.insertBefore(badge, card.firstChild);
                }

                badge.textContent = this.i18n.t('currentPlanBadge');
                badge.className = 'plan-badge plan-badge-current';
                badge.style.display = 'block';

                if (button) {
                    button.disabled = true;
                    button.className = 'plan-btn plan-btn-current';
                    button.innerHTML = `
                    <span class="material-symbols-outlined">check_circle</span>
                    <span data-i18n="currentPlanBtn">${this.i18n.t('currentPlanBtn')}</span>
                `;
                }
            } else {
                card.setAttribute('aria-checked', 'false');

                if (planType === 'premium' && normalizedCurrentPlan !== 'premium') {
                    card.classList.add('plan-card-popular');

                    if (!badge) {
                        badge = document.createElement('div');
                        badge.className = 'plan-badge plan-badge-popular';
                        badge.setAttribute('data-i18n', 'popularBadge');
                        card.insertBefore(badge, card.firstChild);
                    }

                    badge.textContent = this.i18n.t('popularBadge');
                    badge.className = 'plan-badge plan-badge-popular';
                    badge.style.display = 'block';
                } else if (badge) {
                    badge.style.display = 'none';
                }

                if (button) {
                    button.disabled = false;

                    if (planLevel > currentPlanLevel) {
                        button.className = 'plan-btn plan-btn-upgrade';
                        button.innerHTML = `
                        <span data-i18n="getStarted">${this.i18n.t('getStarted')}</span>
                        <span class="material-symbols-outlined">arrow_forward</span>
                    `;
                    } else {
                        button.className = 'plan-btn plan-btn-downgrade';
                        button.innerHTML = `
                        <span data-i18n="downgrade">${this.i18n.t('downgrade')}</span>
                        <span class="material-symbols-outlined">arrow_downward</span>
                    `;
                    }
                }
            }
        });

        console.log(`Updated plan cards UI for: ${normalizedCurrentPlan}`);
    }

    /**
     * Initialize tabs
     */
    initializeTabs() {
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

                const targetId = tab.getAttribute('aria-controls');
                const targetContent = document.getElementById(targetId);
                if (targetContent) {
                    targetContent.classList.add('active');
                }
            });
        });
    }

    /**
     * Initialize event handlers
     */
    initializeEventHandlers() {
        // Back button
        const backBtn = document.getElementById('backBtn');
        if (backBtn) {
            backBtn.addEventListener('click', () => {
                window.location.href = '../index.html';
            });
        }

        // Plan upgrade buttons
        const plansContainer = document.querySelector('.plans-container');
        if (plansContainer) {
            plansContainer.addEventListener('click', (event) => {
                const button = event.target.closest('.plan-btn-upgrade, .plan-btn-downgrade');

                if (button && !button.disabled) {
                    const card = button.closest('.plan-card');
                    if (card) {
                        const plan = card.dataset.plan;
                        this.handlePlanUpgrade(plan);
                    }
                }
            });
        }

        // Logout button
        const logoutBtn = document.getElementById('logoutBtn');
        if (logoutBtn) {
            logoutBtn.addEventListener('click', () => {
                this.handleLogout();
            });
        }

        // Theme toggle
        const themeToggleBtn = document.getElementById('themeToggleBtn');
        if (themeToggleBtn) {
            themeToggleBtn.addEventListener('click', () => {
                this.toggleTheme();
            });
        }

        // Language toggle
        const languageBtn = document.getElementById('languageBtn');
        if (languageBtn) {
            languageBtn.addEventListener('click', () => {
                this.i18n.switchLanguage();
                this.loadUserDataFromStorage(); // Reload to update translations
                this.loadStorageInfo(); // Reload storage to update "of" text
            });
        }

        // Security section modal triggers (if needed)
        this.initializeSecurityModals();
    }

    /**
     * Initialize security modals
     */
    initializeSecurityModals() {
        const modalTriggers = document.querySelectorAll('.modal-trigger');

        modalTriggers.forEach((trigger) => {
            trigger.addEventListener('click', () => {
                const modalId = trigger.getAttribute('data-modal');
                // Use featureNotImplemented from translations
                this.notifications.info('featureNotImplemented', 5000, null, {
                    featureName: trigger.textContent
                });
            });
        });
    }

    /**
     * Handle plan upgrade
     */
    handlePlanUpgrade(planType) {
        const planHierarchy = {
            free: 0,
            premium: 1,
            enterprise: 2
        };

        const currentPlanLevel = planHierarchy[this.currentPlan.toLowerCase()] || 0;
        const targetPlanLevel = planHierarchy[planType.toLowerCase()] || 0;

        let confirmMessage;

        if (targetPlanLevel > currentPlanLevel) {
            // Upgrade
            confirmMessage = `${this.i18n.t('subscribe')}: ${planType}?`;
        } else {
            // Downgrade
            confirmMessage = this.i18n.t('confirmDowngrade');
        }

        if (confirm(confirmMessage)) {
            this.notifications.info('processing');

            // TODO: Implement actual upgrade/downgrade logic
            setTimeout(() => {
                const action = targetPlanLevel > currentPlanLevel ? 'upgrade' : 'downgrade';
                this.notifications.info('featureNotImplemented', 5000, null, {
                    featureName: `Plan ${action}`
                });
            }, 1000);
        }
    }

    /**
     * Handle logout
     */
    handleLogout() {
        const confirmMessage = this.i18n.t('signOutMessage');

        if (confirm(confirmMessage)) {
            this.api.clearAuthToken();
            localStorage.removeItem('cloudcore_userPlan'); // Clear cached plan
            this.notifications.success('deletedSuccessfully'); // Repurpose as "Signed out successfully"
            setTimeout(() => (window.location.href = '../login.html'), 1000);
        }
    }

    /**
     * Toggle theme
     */
    toggleTheme() {
        const currentTheme = document.documentElement.getAttribute('data-theme');
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';

        document.documentElement.setAttribute('data-theme', newTheme);
        localStorage.setItem('cloudcore-theme', newTheme);

        this.updateThemeIcon();
    }

    /**
     * Initialize and update theme
     */
    initializeTheme() {
        this.updateThemeIcon();
    }

    /**
     * Update theme icon
     */
    updateThemeIcon() {
        const themeToggleBtn = document.getElementById('themeToggleBtn');
        if (!themeToggleBtn) return;

        const currentTheme = document.documentElement.getAttribute('data-theme');
        const icon = themeToggleBtn.querySelector('.material-symbols-outlined');

        if (icon) {
            icon.textContent = currentTheme === 'dark' ? 'dark_mode' : 'light_mode';
        }
    }
}

// Initialize settings manager when DOM is loaded
let settingsManager;

document.addEventListener('DOMContentLoaded', async () => {
    // Check authentication
    const token = localStorage.getItem('cloudcore_token');
    if (!token) {
        window.location.href = '../login.html';
        return;
    }

    // Create and initialize settings manager
    settingsManager = new SettingsManager();
    await settingsManager.initialize();
});

// Export for external use
export { settingsManager, SettingsManager };
