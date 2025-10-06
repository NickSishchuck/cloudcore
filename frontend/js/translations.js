export const translations = {
    en: {
        // Header
        signOut: 'Sign Out',
        
        // Sidebar
        new: 'New',
        uploadFiles: 'Upload Files',
        uploadFolder: 'Upload Folder',
        myDrive: 'My Drive',
        recent: 'Recent',
        shared: 'Shared with me',
        trash: 'Trash',
        
        // File operations
        name: 'Name',
        modified: 'Modified',
        created: 'Created',
        size: 'Size',
        downloadFile: 'Download',
        downloadFolder: 'Download folder',
        rename: 'Rename',
        delete: 'Delete',
        deleteFile: 'Delete file',
        deleteFolder: 'Delete folder',
        restore: 'Restore',
        deletePermanently: 'Delete permanently',
        
        // Messages
        loading: 'Loading...',
        calculating: 'Calculating...',
        emptyFolder: 'This folder is empty',
        uploadGetStarted: 'Upload files or create folders to get started',
        restoring: 'Restoring',
        restored: 'restored',
        
        // Auth
        username: 'Username',
        password: 'Password',
        emailAddress: 'Email Address',
        confirmPassword: 'Confirm Password',
        signIn: 'Sign In',
        createAccount: 'Create Account',
        noAccount: "Don't have an account?",
        alreadyAccount: 'Already have an account?',
        
        // Errors and notifications
        downloading: 'Downloading',
        downloaded: 'Downloaded',
        failedDownload: 'Failed to download',
        uploadFailed: 'Upload failed',
        invalidCredentials: 'Invalid username or password'
    },
    uk: {
        // Header
        signOut: 'Вийти',
        
        // Sidebar
        new: 'Створити',
        uploadFiles: 'Завантажити файли',
        uploadFolder: 'Завантажити папку',
        myDrive: 'Мій диск',
        recent: 'Останні',
        shared: 'Надані мені',
        trash: 'Кошик',
        
        // File operations
        name: 'Назва',
        modified: 'Змінено',
        created: 'Створено',
        size: 'Розмір',
        downloadFile: 'Завантажити',
        downloadFolder: 'Завантажити папку',
        rename: 'Перейменувати',
        delete: 'Видалити',
        deleteFile: 'Видалити файл',
        deleteFolder: 'Видалити папку',
        restore: 'Відновити',
        deletePermanently: 'Видалити назавжди',
        
        // Messages
        loading: 'Завантаження...',
        calculating: 'Обчислення...',
        emptyFolder: 'Ця папка порожня',
        uploadGetStarted: 'Завантажте файли або створіть папки',
        restoring: 'Відновлення',
        restored: 'відновлено',
        
        // Auth
        username: 'Ім\'я користувача',
        password: 'Пароль',
        emailAddress: 'Електронна адреса',
        confirmPassword: 'Підтвердити пароль',
        signIn: 'Увійти',
        createAccount: 'Створити обліковий запис',
        noAccount: 'Немає облікового запису?',
        alreadyAccount: 'Вже маєте обліковий запис?',
        
        // Errors and notifications
        downloading: 'Завантажується',
        downloaded: 'Завантажено',
        failedDownload: 'Не вдалося завантажити',
        uploadFailed: 'Помилка завантаження',
        invalidCredentials: 'Неправильне ім\'я або пароль'
    }
};

export class I18n {
    constructor() {
        this.currentLanguage = localStorage.getItem('cloudcore-language') || 'en';
    }

    t(key) {
        return translations[this.currentLanguage]?.[key] || key;
    }

    switchLanguage() {
        this.currentLanguage = this.currentLanguage === 'en' ? 'uk' : 'en';
        localStorage.setItem('cloudcore-language', this.currentLanguage);
        this.updateUI();
    }

    updateUI() {
        document.querySelectorAll('[data-i18n]').forEach(element => {
            const key = element.getAttribute('data-i18n');
            const translation = translations[this.currentLanguage]?.[key];
            if (translation) {
                element.textContent = translation;
            }
        });
        
        this.updateLanguageButton();
    }

    updateLanguageButton() {
        const btn = document.getElementById('languageBtn');
        if (!btn) return;
        
        const flag = btn.querySelector('.language-flag');
        const code = btn.querySelector('.language-code');
        
        if (this.currentLanguage === 'uk') {
            flag.textContent = '🇺🇦';
            code.textContent = 'UA';
            btn.title = 'Перемкнути мову';
        } else {
            flag.textContent = '🌐';
            code.textContent = 'EN';
            btn.title = 'Switch language';
        }
    }

    getCurrentLanguage() {
        return this.currentLanguage;
    }
}
