export const translations = {
    en: {
        // --- General UI ---
        signOut: 'Sign Out',
        new: 'New',
        uploadFiles: 'Upload Files',
        uploadFolder: 'Upload Folder',
        myDrive: 'My Drive',
        recent: 'Recent',
        shared: 'Shared with me',
        trash: 'Trash',
        searchPlaceholder: 'Search in Drive',
        featureNotImplemented: 'Feature not yet implemented: {featureName}',
        emptyTrash: 'Trash is empty',
        emptyTrashMessage: 'Deleted items will be stored here for 30 days',
        noSearchResults: 'No results found',
        noSearchResultsMessage: 'Try a different search term',
        newFolder: 'New folder',
        deleteAllForever: 'Empty the trash',
        download: 'Download',
        move: 'Move',

        // --- File List & Items ---
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
        cancel: 'Cancel',
        deleteItem: 'Delete item?',
        renameItem: 'Rename item',
        newName: 'New name',
        renameHint: 'Enter a new name',
        
        // --- General Messages ---
        loading: 'Loading...',
        calculating: 'Calculating...',
        emptyFolder: 'This folder is empty',
        uploadGetStarted: 'Upload files or create folders to get started',
        
        // --- Auth ---
        username: 'Username',
        password: 'Password',
        emailAddress: 'Email Address',
        confirmPassword: 'Confirm Password',
        signIn: 'Sign In',
        signingIn: 'Signing in...',
        createAccount: 'Create Account',
        creatingAccount: 'Creating account...',
        noAccount: "Don't have an account?",
        alreadyAccount: 'Already have an account?',
        welcomeBack: 'Welcome back, {username}!',
        accountCreated: 'Account created! Welcome, {username}!',
        usernameHint: '3-50 latin letters or numbers.',
        passwordHint: 'At least 6 characters. Use a strong, unique password.',

        // --- Notifications & Dialogs (Success) ---
        downloading: 'Downloading {filename}',
        downloaded: 'Downloaded {filename}',
        creatingArchive: 'Creating archive...',
        uploadingFile: 'Uploading {filename} ({current}/{total})',
        uploadSuccess: '{filename} uploaded successfully',
        uploadingFolder: 'Uploading folder with {count} files',
        uploadFolderSuccess: 'Successfully uploaded {count} files',
        uploadFolderPartial: 'Uploaded {successCount} files, {errorCount} failed',
        createdSuccessfully: 'Created successfully',
        deletedSuccessfully: 'Deleted successfully',
        restoredSuccessfully: 'Restored successfully',
        renaming: 'Renaming...',
        renamed: 'Renamed "{oldName}" to "{newName}"',
        deleting: 'Deleting {filename}...',
        deleted: '"{filename}" has been deleted',
        restoring: 'Restoring {filename}...',
        restored: '"{filename}" has been restored',

        // --- Dialogs (Prompts) ---
        confirmDelete: 'Are you sure you want to delete "{filename}"?',
        confirmDeletePermanent: 'Permanently delete "{filename}"? This action cannot be undone.',
        renamePrompt: 'Enter new name for "{filename}":',
        signOutMessage: 'Are you sure you want to sign out?',

        // Batch operations
        confirmDeleteMultiple: 'Delete {count} items?',
        deletedMultiple: 'Deleted {count} items',
        restoredMultiple: 'Restored {count} items',
        deletedPermanentlyMultiple: 'Permanently deleted {count} items',
        confirmDeletePermanentlyMultiple: 'Permanently delete {count} items? This action cannot be undone.',
        failedDeleteMultiple: 'Failed to delete items',
        failedRestoreMultiple: 'Failed to restore items',
        moveDialogNotImplemented: 'Move dialog not implemented yet. Use drag & drop instead.',
        creatingArchive: 'Creating archive...',
        downloadingMultiple: 'Downloading {count} items...',
        downloadedMultiple: 'Downloaded {count} items',
        failedDownloadMultiple: 'Failed to download items',

        // Context menu
        open: 'Open',
        
        // Toolbar selection count
        selectionCount: '{count} selected',
        
        // --- API & Client-side Error Codes ---
        invalidCredentials: 'Invalid username or password',
        signInFailed: 'Sign in failed',
        registrationFailed: 'Registration failed. Please try again.',
        passwordsNoMatch: 'Passwords do not match',
        invalidName: 'Invalid name provided.',
        nameTooLong: 'The name is too long.',
        nameAlreadyExists: 'An item with this name already exists in this location.',
        invalidCharacter: 'The name contains invalid characters.',
        reservedName: 'This name is reserved and cannot be used.',
        invalidNameFormat: 'The name format is invalid.',
        notAllowedSymbol: 'The name contains a symbol that is not allowed.',
        itemNotFound: 'The requested item was not found.',
        fileNotFound: 'The requested file was not found.',
        folderNotFound: 'The requested folder was not found.',
        unsupportedType: 'This item type is not supported for this operation.',
        noItems: 'There are no items to process.',
        parentFolderDeleted: 'The parent folder has been deleted.',
        nullOrEmpty: 'A required value was not provided.',
        archiveTooLarge: 'The folder is too large to be downloaded as an archive.',
        tooManyFiles: 'The folder contains too many files to be processed at once.',
        fileTooLarge: 'The file is too large.',
        invalidFileType: 'This file type is not allowed.',
        fileRequired: 'A file is required for this operation.',
        accessDenied: 'Access denied. You do not have permission to perform this action.',
        insufficientPermission: 'You have insufficient permissions.',
        invalidPermission: 'The specified permission is invalid.',
        teamspaceNotFound: 'Teamspace not found.',
        teamspaceAccessDenied: 'You do not have access to this teamspace.',
        teamspaceLimitReached: 'The limit of teamspaces has been reached.',
        teamspaceNameTaken: 'This teamspace name is already taken.',
        memberNotFound: 'Member not found in this teamspace.',
        memberAlreadyExists: 'This user is already a member of the teamspace.',
        memberLimitReached: 'The teamspace member limit has been reached.',
        cannotRemoveAdmin: 'The last administrator cannot be removed from a teamspace.',
        cannotLeaveAsAdmin: 'You cannot leave the teamspace as you are the only administrator.',
        userNotFound: 'User not found.',
        storageLimitExceeded: 'Storage limit exceeded. Cannot upload file.',
        badRequest: 'The request was invalid.',
        operationFailed: 'The operation failed. Please try again.',
        unexpectedError: 'An unexpected error occurred.',
        ioError: 'A file system error occurred on the server.',
        networkError: 'Network error. Please check your connection.',
        uploadFailedSingle: 'Failed to upload {filename}',
        failedDownload: 'Failed to download file.',
        failedUpload: 'Upload failed.',
        failedRename: 'Failed to rename.',
        failedDelete: 'Failed to delete.',
        failedRestore: 'Failed to restore.',
        folderUploadNotSupported: 'Folder upload is not supported by your browser.'
    },
    uk: {
        // --- General UI ---
        signOut: 'Вийти',
        new: 'Створити',
        uploadFiles: 'Завантажити файли',
        uploadFolder: 'Завантажити папку',
        myDrive: 'Мій диск',
        recent: 'Останні',
        shared: 'Надані мені',
        trash: 'Кошик',
        searchPlaceholder: 'Пошук на Диску',
        featureNotImplemented: 'Функція ще не реалізована: {featureName}',
        emptyTrash: 'Кошик порожній',
        emptyTrashMessage: 'Видалені файли зберігатимуться тут 30 днів',
        noSearchResults: 'Нічого не знайдено',
        noSearchResultsMessage: 'Спробуйте інший запит',
        newFolder:'Нова папка',
        deleteAllForever: 'Очистити кошик',
        download: 'Завантажити',
        move: 'Перемістити',
        selectionCount: '{count} вибрано',

        // --- File List & Items ---
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
        cancel: 'Скасувати',
        deleteItem: 'Видалити елемент?',
        renameItem: 'Перейменувати елемент',
        newName: 'Нова назва',
        renameHint: 'Введіть нову назву',
        
        // --- General Messages ---
        loading: 'Завантаження...',
        calculating: 'Обчислення...',
        emptyFolder: 'Ця папка порожня',
        uploadGetStarted: 'Завантажте файли або створіть папки, щоб почати',

        // --- Auth ---
        username: 'Ім\'я користувача',
        password: 'Пароль',
        emailAddress: 'Електронна адреса',
        confirmPassword: 'Підтвердити пароль',
        signIn: 'Увійти',
        signingIn: 'Вхід...',
        createAccount: 'Створити акаунт',
        creatingAccount: 'Створення акаунту...',
        noAccount: 'Немає акаунту?',
        alreadyAccount: 'Вже є акаунт?',
        welcomeBack: 'З поверненням, {username}!',
        accountCreated: 'Акаунт створено! Вітаємо, {username}!',
        usernameHint: '3-50 латинських букв або цифр.',
        passwordHint: 'Щонайменше 6 символів. Використовуйте надійний унікальний пароль.',

        // --- Notifications & Dialogs (Success) ---
        downloading: 'Завантаження {filename}',
        downloaded: 'Завантажено {filename}',
        creatingArchive: 'Створення архіву...',
        uploadingFile: 'Завантаження {filename} ({current}/{total})',
        uploadSuccess: '{filename} завантажено успішно',
        uploadingFolder: 'Завантаження папки з {count} файлами',
        uploadFolderSuccess: 'Успішно завантажено {count} файлів',
        uploadFolderPartial: 'Завантажено {successCount} файлів, помилок: {errorCount}',
        createdSuccessfully: 'Успішно створено.',
        deletedSuccessfully: 'Успішно видалено.',
        restoredSuccessfully: 'Успішно відновлено.',
        renaming: 'Перейменування...',
        renamed: 'Перейменовано "{oldName}" на "{newName}"',
        deleting: 'Видалення {filename}...',
        deleted: '"{filename}" видалено',
        restoring: 'Відновлення {filename}...',
        restored: '"{filename}" відновлено',
        
        // --- Dialogs (Prompts) ---
        confirmDelete: 'Ви впевнені, що хочете видалити "{filename}"?',
        confirmDeletePermanent: 'Назавжди видалити "{filename}"? Цю дію неможливо скасувати.',
        renamePrompt: 'Введіть нову назву для "{filename}":',
        signOutMessage: 'Ви впевнені, що хочете вийти?',

        // Batch operations
        confirmDeleteMultiple: 'Видалити {count} елементів?',
        deletedMultiple: 'Видалено {count} елементів',
        restoredMultiple: 'Відновлено {count} елементів',
        deletedPermanentlyMultiple: 'Остаточно видалено {count} елементів',
        confirmDeletePermanentlyMultiple: 'Остаточно видалити {count} елементів? Цю дію не можна скасувати.',
        failedDeleteMultiple: 'Не вдалося видалити елементи',
        failedRestoreMultiple: 'Не вдалося відновити елементи',
        moveDialogNotImplemented: 'Діалог переміщення ще не реалізовано. Використовуйте перетягування.',
        creatingArchive: 'Створення архіву...',
        downloadingMultiple: 'Завантаження {count} елементів...',
        downloadedMultiple: 'Завантажено {count} елементів',
        failedDownloadMultiple: 'Не вдалося завантажити елементи',
        
        // Context menu
        open: 'Відкрити',
        
        // Toolbar selection count
        selectionCount: '{count} вибрано',

        // --- API & Client-side Error Codes ---
        invalidCredentials: 'Неправильне ім\'я користувача або пароль.',
        signInFailed: 'Не вдалося увійти.',
        registrationFailed: 'Помилка реєстрації. Спробуйте ще раз.',
        passwordsNoMatch: 'Паролі не збігаються.',
        invalidName: 'Вказано неправильну назву.',
        nameTooLong: 'Назва занадто довга.',
        nameAlreadyExists: 'Елемент з такою назвою вже існує в цій папці.',
        invalidCharacter: 'Назва містить недопустимі символи.',
        reservedName: 'Ця назва зарезервована і не може бути використана.',
        invalidNameFormat: 'Неправильний формат назви.',
        notAllowedSymbol: 'Назва містить заборонений символ.',
        itemNotFound: 'Запитаний елемент не знайдено.',
        fileNotFound: 'Запитаний файл не знайдено.',
        folderNotFound: 'Запитану папку не знайдено.',
        unsupportedType: 'Цей тип елемента не підтримується для даної операції.',
        noItems: 'Немає елементів для обробки.',
        parentFolderDeleted: 'Батьківська папка була видалена.',
        nullOrEmpty: 'Не було надано обов\'язкове значення.',
        archiveTooLarge: 'Папка занадто велика для завантаження у вигляді архіву.',
        tooManyFiles: 'Папка містить занадто багато файлів для одночасної обробки.',
        fileTooLarge: 'Файл занадто великий.',
        invalidFileType: 'Цей тип файлу не дозволений.',
        fileRequired: 'Для цієї операції потрібен файл.',
        accessDenied: 'Доступ заборонено. У вас немає дозволу на виконання цієї дії.',
        insufficientPermission: 'У вас недостатньо прав.',
        invalidPermission: 'Вказано недійсний дозвіл.',
        teamspaceNotFound: 'Робочий простір не знайдено.',
        teamspaceAccessDenied: 'У вас немає доступу до цього робочого простору.',
        teamspaceLimitReached: 'Досягнуто ліміту на кількість робочих просторів.',
        teamspaceNameTaken: 'Ця назва робочого простору вже зайнята.',
        memberNotFound: 'Учасника не знайдено в цьому робочому просторі.',
        memberAlreadyExists: 'Цей користувач вже є учасником робочого простору.',
        memberLimitReached: 'Досягнуто ліміту на кількість учасників.',
        cannotRemoveAdmin: 'Неможливо видалити останнього адміністратора з робочого простору.',
        cannotLeaveAsAdmin: 'Ви не можете покинути простір, оскільки є єдиним адміністратором.',
        userNotFound: 'Користувача не знайдено.',
        storageLimitExceeded: 'Перевищено ліміт сховища. Неможливо завантажити файл.',
        badRequest: 'Неправильний запит.',
        operationFailed: 'Операція не вдалася. Спробуйте ще раз.',
        unexpectedError: 'Сталася неочікувана помилка.',
        ioError: 'На сервері сталася помилка файлової системи.',
        networkError: 'Помилка мережі. Перевірте з\'єднання.',
        uploadFailedSingle: 'Не вдалося завантажити {filename}',
        failedDownload: 'Не вдалося завантажити файл.',
        failedUpload: 'Помилка завантаження.',
        failedRename: 'Не вдалося перейменувати.',
        failedDelete: 'Не вдалося видалити.',
        failedRestore: 'Не вдалося відновити.',
        folderUploadNotSupported: 'Ваш браузер не підтримує завантаження папок.'
    }
};


export class I18n {
    constructor() {
        this.currentLanguage = localStorage.getItem('cloudcore-language') || 'en';
    }

    t(key, replacements = {}) {
        let translation = translations[this.currentLanguage]?.[key] || key;

        // Debug (оставь пока, если нужно)
        console.log('t() called:', { key, replacements, translation });

        for (const placeholder in replacements) {
        translation = translation.replace(
            new RegExp(`\\{${placeholder}\\}`, 'g'),
            String(replacements[placeholder])
        );
        }

        console.log('  final:', translation);
        return translation;
    }

    getTranslatedError(error, defaultKey = 'networkError') {
        if (error && error.errorCode) {

            const key = error.errorCode.charAt(0).toLowerCase() + error.errorCode.slice(1).replace(/_(\w)/g, (_, p1) => p1.toUpperCase());
            const translation = this.t(key);
            if (translation !== key) {
                return translation;
            }
        }
        return this.t(defaultKey);
    }

    switchLanguage() {
        this.currentLanguage = this.currentLanguage === 'en' ? 'uk' : 'en';
        localStorage.setItem('cloudcore-language', this.currentLanguage);
        this.updateUI();
    }

    updateUI() {
        document.querySelectorAll('[data-i18n]').forEach(element => {
            const key = element.getAttribute('data-i18n');
            const attr = element.getAttribute('data-i18n-attr');

            const translation = this.t(key); 

            if (attr) {
                element.setAttribute(attr, translation);
            } else {
                element.textContent = translation;
            }
        });
    
        this.updateLanguageButton();
    }

    updateLanguageButton() {
        const langBtn = document.getElementById('languageBtn');
        if (!langBtn) return;
        
        const currentLang = this.currentLanguage;
        const langCode = langBtn.querySelector('.language-code');
        

        if (langCode) {
            langCode.textContent = currentLang === 'uk' ? 'UA' : 'EN';
        }
        
        const langFlag = langBtn.querySelector('.language-flag');
        if (langFlag) {
            langFlag.textContent = currentLang === 'uk' ? '🇺🇦' : '🇬🇧';
        }
    }

    getCurrentLanguage() {
        return this.currentLanguage;
    }
}
