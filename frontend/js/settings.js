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
                this.notifications.error(this.i18n.t('accessDenied'));
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
            this.initializeModals();

            // Update translations
            this.i18n.updateUI();
        } catch (error) {
            console.error('Error initializing settings:', error);
            this.notifications.error(this.i18n.t('unexpectedError'));
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
                this.notifications.error(this.i18n.t('accessDenied'));
                setTimeout(() => (window.location.href = '../login.html'), 2000);
            } else if (error.message.includes('Network')) {
                this.notifications.error(this.i18n.t('networkError'));
            } else if (error.message.includes('timeout')) {
                this.notifications.error(this.i18n.t('timeoutMessage'));
            } else {
                this.notifications.error(this.i18n.t('operationFailed'));
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
        // Change Email button
        const emailBtn = document.querySelector('[data-modal="modalEmail"]');
        if (emailBtn) {
            emailBtn.addEventListener('click', (e) => {
                e.preventDefault();
                this.handleChangeEmail();
            });
        }

        // Change Username button
        const usernameBtn = document.querySelector('[data-modal="modalUsername"]');
        if (usernameBtn) {
            usernameBtn.addEventListener('click', (e) => {
                e.preventDefault();
                this.handleChangeUsername();
            });
        }

        // Change Password button
        const passwordBtn = document.querySelector('[data-modal="modalPassword"]');
        if (passwordBtn) {
            passwordBtn.addEventListener('click', (e) => {
                e.preventDefault();
                this.handleChangePassword();
            });
        }

        // Delete Account button
        const deleteBtn = document.querySelector('[data-modal="modalDelete"]');
        if (deleteBtn) {
            deleteBtn.addEventListener('click', (e) => {
                e.preventDefault();
                this.handleDeleteAccount();
            });
        }
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
            this.notifications.info(this.i18n.t('processing'));

            // TODO: Implement actual upgrade/downgrade logic
            setTimeout(() => {
                const action = targetPlanLevel > currentPlanLevel ? 'upgrade' : 'downgrade';
                this.notifications.info(this.i18n.t('featureNotImplemented'), 5000, null, {
                    featureName: `Plan ${action}`
                });
            }, 1000);
        }
    }

    initializeModals() {
        const overlay = document.getElementById('modalOverlay');

        // Close modal on overlay click
        if (overlay) {
            overlay.addEventListener('click', () => {
                this.closeAllModals();
            });
        }

        // Close buttons
        document.querySelectorAll('.modal-close').forEach((btn) => {
            btn.addEventListener('click', () => {
                this.closeAllModals();
            });
        });

        // Cancel buttons
        document.querySelectorAll('[data-action="cancel"]').forEach((btn) => {
            btn.addEventListener('click', () => {
                this.closeAllModals();
            });
        });

        // ESC key to close modals
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closeAllModals();
            }
        });

        document.querySelectorAll('.toggle-password').forEach(button => {
        button.addEventListener('click', () => {
            const targetId = button.getAttribute('data-target');
            const input = document.getElementById(targetId);
            const icon = button.querySelector('.material-symbols-outlined');
            
            if (input.type === 'password') {
                input.type = 'text';
                icon.textContent = 'visibility';
            } else {
                input.type = 'password';
                icon.textContent = 'visibility_off';
            }
        });
    });
    }

    /**
     * Open modal
     */
    openModal(modalId) {
        const modal = document.getElementById(modalId);
        const overlay = document.getElementById('modalOverlay');

        if (modal && overlay) {
            overlay.classList.add('show');
            modal.classList.add('show');
            document.body.style.overflow = 'hidden';

            // Auto-focus first input
            setTimeout(() => {
                const firstInput = modal.querySelector('input');
                if (firstInput) {
                    firstInput.focus();
                    if (firstInput.select) firstInput.select();
                }
            }, 100);
        }
    }

    /**
     * Close all modals
     */
    closeAllModals() {
        const overlay = document.getElementById('modalOverlay');

        document.querySelectorAll('.modal.show').forEach((modal) => {
            modal.classList.remove('show');

            // Clear input values
            modal.querySelectorAll('input').forEach((input) => {
                if (input.type !== 'email') {
                    input.value = '';
                }
            });
        });

        if (overlay) {
            overlay.classList.remove('show');
        }

        document.body.style.overflow = '';
    }

    /**
     * Handle Change Email
     */
    async handleChangeEmail() {
        this.openModal('modalEmail');

        const modal = document.getElementById('modalEmail');
        const saveBtn = modal.querySelector('[data-action="save"]');
        const emailInput = document.getElementById('newEmail');

        // Set current email as default
        emailInput.value = this.currentUser.email || '';

        // Remove old listeners
        const newSaveBtn = saveBtn.cloneNode(true);
        saveBtn.parentNode.replaceChild(newSaveBtn, saveBtn);

        newSaveBtn.addEventListener('click', async () => {
            const newEmail = emailInput.value.trim();

            if (!newEmail || !this.validateEmail(newEmail)) {
                this.notifications.error(this.i18n.t('invalidEmail'));
                return;
            }

            if (newEmail === this.currentUser.email) {
                this.notifications.info(this.i18n.t('sameEmail'));
                return;
            }

            try {
                // Disable button during request
                newSaveBtn.disabled = true;
                newSaveBtn.textContent = this.i18n.t('processing') || 'Processing...';

                const response = await this.api.requestEmailChange(this.currentUserId, newEmail);

                this.notifications.success(this.i18n.t('verificationEmailSent'));
                this.closeAllModals();

                // Note: Email will be updated after user confirms via email link
            } catch (error) {
                console.error('Error requesting email change:', error);

                if (error.errorCode === 'EMAIL_EXISTS') {
                    this.notifications.error(this.i18n.t('emailAlreadyExists'));
                } else if (error.errorCode === 'INVALID_EMAIL') {
                    this.notifications.error(this.i18n.t('invalidEmail'));
                } else {
                    this.notifications.error(this.i18n.t('failedToUpdateEmail'));
                }
            } finally {
                newSaveBtn.disabled = false;
                newSaveBtn.innerHTML = `<span data-i18n="save">${this.i18n.t('save')}</span>`;
            }
        });
    }

    /**
     * Handle Change Username
     */
    async handleChangeUsername() {
        this.openModal('modalUsername');

        const modal = document.getElementById('modalUsername');
        const saveBtn = modal.querySelector('[data-action="save"]');
        const usernameInput = document.getElementById('newUsername');

        // Set current username as default
        usernameInput.value = this.currentUser.username || '';

        // Remove old listeners
        const newSaveBtn = saveBtn.cloneNode(true);
        saveBtn.parentNode.replaceChild(newSaveBtn, saveBtn);

        newSaveBtn.addEventListener('click', async () => {
            const newUsername = usernameInput.value.trim();

            if (!newUsername || newUsername.length < 3 || newUsername.length > 20) {
                this.notifications.error(this.i18n.t('invalidUsername'));
                return;
            }

            if (newUsername === this.currentUser.username) {
                this.notifications.info(this.i18n.t('sameUsername'));
                return;
            }

            try {
                // Disable button during request
                newSaveBtn.disabled = true;
                newSaveBtn.textContent = this.i18n.t('processing') || 'Processing...';

                await this.api.changeUsername(this.currentUserId, newUsername);

                this.notifications.success(this.i18n.t('usernameUpdated'));
                this.closeAllModals();

                // Update display
                this.currentUser.username = newUsername;
                localStorage.setItem('cloudcore_user', JSON.stringify(this.currentUser));
                this.loadUserDataFromStorage();
            } catch (error) {
                console.error('Error updating username:', error);

                if (error.errorCode === 'USERNAME_EXISTS') {
                    this.notifications.error(this.i18n.t('usernameAlreadyExists'));
                } else {
                    this.notifications.error(this.i18n.t('failedToUpdateUsername'));
                }
            } finally {
                newSaveBtn.disabled = false;
                newSaveBtn.innerHTML = `<span data-i18n="save">${this.i18n.t('save')}</span>`;
            }
        });
    }

    /**
     * Handle Change Password
     */
    async handleChangePassword() {
        this.openModal('modalPassword');

        const modal = document.getElementById('modalPassword');
        const saveBtn = modal.querySelector('[data-action="save"]');
        const currentPasswordInput = document.getElementById('currentPassword');
        const newPasswordInput = document.getElementById('newPassword');
        const confirmPasswordInput = document.getElementById('confirmNewPassword');

        // Remove old listeners
        const newSaveBtn = saveBtn.cloneNode(true);
        saveBtn.parentNode.replaceChild(newSaveBtn, saveBtn);

        newSaveBtn.addEventListener('click', async () => {
            const currentPassword = currentPasswordInput.value;
            const newPassword = newPasswordInput.value;
            const confirmPassword = confirmPasswordInput.value;

            if (!currentPassword || !newPassword || !confirmPassword) {
                this.notifications.error(this.i18n.t('allFieldsRequired'));
                return;
            }

            if (newPassword.length < 6) {
                this.notifications.error(this.i18n.t('passwordTooShort'));
                return;
            }

            if (newPassword !== confirmPassword) {
                this.notifications.error(this.i18n.t('passwordsDoNotMatch'));
                return;
            }

            if (currentPassword === newPassword) {
                this.notifications.error(this.i18n.t('samePassword'));
                return;
            }

            try {
                // Disable button during request
                newSaveBtn.disabled = true;
                newSaveBtn.textContent = this.i18n.t('processing') || 'Processing...';

                await this.api.changePassword(this.currentUserId, currentPassword, newPassword, confirmPassword);

                this.notifications.success(this.i18n.t('passwordChanged'));
                this.closeAllModals();

                // Optional: Force logout after password change for security
                // setTimeout(() => this.handleLogout(), 2000);
            } catch (error) {
                console.error('Error changing password:', error);

                if (error.errorCode === 'INVALID_PASSWORD') {
                    this.notifications.error(this.i18n.t('invalidCurrentPassword'));
                } else {
                    this.notifications.error(this.i18n.t('failedToChangePassword'));
                }
            } finally {
                newSaveBtn.disabled = false;
                newSaveBtn.innerHTML = `<span data-i18n="save">${this.i18n.t('save')}</span>`;
            }
        });
    }

    /**
 * Handle Delete Account - Step 1
 */
// async handleDeleteAccount() {
//     this.openModal('modalDelete');
    
//     const modal = document.getElementById('modalDelete');
//     const continueBtn = modal.querySelector('[data-action="continue-delete"]');
    
//     // Remove old listeners
//     const newContinueBtn = continueBtn.cloneNode(true);
//     continueBtn.parentNode.replaceChild(newContinueBtn, continueBtn);
    
//     newContinueBtn.addEventListener('click', () => {
//         this.closeAllModals();
//         this.showDeleteConfirmation();
//     });
// }

/**
 * Show Delete Confirmation - Step 2
 */
// showDeleteConfirmation() {
//     this.openModal('modalDeleteConfirm');
    
//     const modal = document.getElementById('modalDeleteConfirm');
//     const confirmInput = document.getElementById('deleteConfirmInput');
//     const finalDeleteBtn = document.getElementById('finalDeleteBtn');
//     const instructionsEl = document.getElementById('deleteConfirmInstructions');
    
//     // Set confirmation text with username
//     const username = this.currentUser.username || 'user';
//     const confirmText = `delete ${username}-account`;
    
//     // Use i18n with parameter replacement
//     const instructionText = this.i18n.t('typeToConfirm', {
//         text: `<strong>${confirmText}</strong>`
//     });
    
//     instructionsEl.innerHTML = instructionText;
    
//     // Clear input
//     confirmInput.value = '';
//     finalDeleteBtn.disabled = true;
    
//     // Validate input
//     const validateInput = () => {
//         const inputValue = confirmInput.value.trim();
//         const isValid = inputValue === confirmText;
//         finalDeleteBtn.disabled = !isValid;
        
//         if (isValid) {
//             confirmInput.style.borderColor = 'var(--color-green)';
//         } else if (inputValue.length > 0) {
//             confirmInput.style.borderColor = 'var(--color-red)';
//         } else {
//             confirmInput.style.borderColor = 'var(--border-color)';
//         }
//     };
    
//     // Remove old event listeners by cloning
//     const newConfirmInput = confirmInput.cloneNode(true);
//     confirmInput.parentNode.replaceChild(newConfirmInput, confirmInput);
    
//     const newFinalDeleteBtn = finalDeleteBtn.cloneNode(true);
//     finalDeleteBtn.parentNode.replaceChild(newFinalDeleteBtn, finalDeleteBtn);
    
//     // Add new listeners
//     newConfirmInput.addEventListener('input', validateInput);
    
//     newFinalDeleteBtn.addEventListener('click', async () => {
//         if (newFinalDeleteBtn.disabled) return;
        
//         try {
//             newFinalDeleteBtn.disabled = true;
//             newFinalDeleteBtn.textContent = this.i18n.t('processing') || 'Processing...';
            
//             await this.api.deleteAccount(this.currentUserId);
            
//             this.notifications.success(this.i18n.t('accountDeleted'));
//             this.closeAllModals();
            
//             // Clear everything and redirect
//             localStorage.clear();
//             setTimeout(() => {
//                 window.location.href = '../login.html';
//             }, 2000);
//         } catch (error) {
//             console.error('Error deleting account:', error);
//             this.notifications.error(this.i18n.t('failedToDeleteAccount'));
            
//             newFinalDeleteBtn.disabled = false;
//             newFinalDeleteBtn.innerHTML = `<span data-i18n="deleteAccount">${this.i18n.t('deleteAccount')}</span>`;
//         }
//     });
    
//     // Focus input
//     setTimeout(() => newConfirmInput.focus(), 100);
// }


    /**
     * Handle Logout
     */
    handleLogout() {
        this.openModal('modalLogout');

        const modal = document.getElementById('modalLogout');
        const logoutBtn = modal.querySelector('[data-action="logout"]');

        // Remove old listeners
        const newLogoutBtn = logoutBtn.cloneNode(true);
        logoutBtn.parentNode.replaceChild(newLogoutBtn, logoutBtn);

        newLogoutBtn.addEventListener('click', () => {
            // Save theme preference
            const theme = localStorage.getItem('cloudcore-theme');
            const language = localStorage.getItem('cloudcore_language');

            // Clear all
            localStorage.clear();

            // Restore preferences
            if (theme) localStorage.setItem('cloudcore-theme', theme);
            if (language) localStorage.setItem('cloudcore_language', language);

            this.notifications.success('signedOut');
            this.closeAllModals();

            setTimeout(() => {
                window.location.href = '../login.html';
            }, 1000);
        });
    }

    /**
     * Validate email
     */
    validateEmail(email) {
        const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return re.test(email);
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
