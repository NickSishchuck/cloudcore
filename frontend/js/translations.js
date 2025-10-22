export const translations = {
    en: {
        // ═══════════════════════════════════════════════════════════════
        // GENERAL UI
        // ═══════════════════════════════════════════════════════════════
        signOut: 'Sign Out',
        new: 'New',
        uploadFiles: 'Upload Files',
        uploadFolder: 'Upload Folder',
        myDrive: 'My Drive',
        recent: 'Recent',
        shared: 'Shared with me',
        trash: 'Trash',
        searchPlaceholder: 'Search in Drive',
        open: 'Open',
        download: 'Download',
        move: 'Move',
        cancel: 'Cancel',
        create: 'Create',

        // ═══════════════════════════════════════════════════════════════
        // MOVE TO MODAL
        // ═══════════════════════════════════════════════════════════════
        moveToModalTitle: 'Move to',
        moveToModalDescription: 'Select a destination folder',
        moveItem: 'Move item',
        moveItems: 'Move {{count}} items',
        moveTo: 'Move to',
        movingTo: 'Moving to',
        selectDestination: 'Select destination',
        currentLocation: 'Current location',
        movingItems: 'Moving {{count}} items...',
        movedSuccessfully: 'Successfully moved {{count}} items',
        moveConfirm: 'Move here',
        cannotMoveHere: 'Cannot move items here',
        cannotMoveIntoItself: 'Cannot move folder into itself',
        folderSelection: 'Folder selection',
        items: 'items',
        folders: 'folders',
        files: 'files',
        movedItem: 'Successfully moved "{{filename}}"',
        movedPartial: 'Moved {{succeeded}} out of {{total}} items',
        failedToMove: 'Failed to move items',
        loadingFolders: 'Loading folders...',
        noSubfolders: 'No subfolders',
        failedToLoadFolders: 'Failed to load folders',
        movedItems: ' Successfully moved {{count}} items',

        // ═══════════════════════════════════════════════════════════════
        // FILE & FOLDER OPERATIONS
        // ═══════════════════════════════════════════════════════════════
        // Table Headers
        name: 'Name',
        modified: 'Modified',
        created: 'Created',
        size: 'Size',

        // Folder Operations
        newFolder: 'New Folder',
        folderName: 'Folder name:',
        untitledFolder: 'Untitled folder',
        creatingFolder: 'Creating folder...',
        folderCreated: "Folder '{foldername}' created",
        failedCreateFolder: "Failed to create folder '{foldername}'",
        folderNameRequired: 'Folder name is required',
        folderNameConflict: 'A folder with this name already exists',
        parentFolderNotFound: 'Parent folder not found',

        // File Operations
        downloadFile: 'Download',
        downloadFolder: 'Download folder',
        rename: 'Rename',
        renameItem: 'Rename item',
        newName: 'New name',
        renameHint: 'Enter a new name',
        renaming: 'Renaming...',
        renamed: 'Renamed "{oldName}" to "{newName}"',
        failedRename: 'Failed to rename',

        // Delete Operations
        delete: 'Delete',
        deleteFile: 'Delete file',
        deleteFolder: 'Delete folder',
        deleteItem: 'Delete item?',
        deleting: 'Deleting {filename}...',
        deleted: '"{filename}" has been deleted',
        deletedMultiple: 'Deleted {count} items',
        deletedPartial: 'Deleted {succeeded} items. Failed: {failed}',
        failedDelete: 'Failed to delete',
        failedDeleteMultiple: 'Failed to delete items',

        // Restore Operations
        restore: 'Restore',
        restoring: 'Restoring {filename}...',
        restored: '"{filename}" has been restored',
        restoredItems: 'Restored {count} items',
        restoredMultiple: 'Restored {count} items',
        restoredPartial: 'Restored {succeeded} items. Failed: {failed}',
        failedRestore: 'Failed to restore',
        failedRestoreMultiple: 'Failed to restore items',

        // ═══════════════════════════════════════════════════════════════
        // TRASH OPERATIONS
        // ═══════════════════════════════════════════════════════════════
        deletePermanently: 'Delete Permanently',
        deletedPermanently: '"{filename}" deleted permanently',
        deletedPermanentlyMultiple: '{count} items deleted permanently',
        failedDeletePermanently: 'Failed to delete permanently',
        emptyTrash: 'Trash is empty',
        emptyTheTrash: 'Empty Trash',
        emptyTrashMessage: 'Deleted items will be stored here for 30 days',
        deleteAllForever: 'Empty the trash',
        confirmEmptyTrash: 'Delete all items in trash permanently? This action cannot be undone.',
        trashAlreadyEmpty: 'Trash is already empty',
        trashEmptiedCount: '{count} items deleted permanently',
        failedEmptyTrash: 'Failed to empty trash',
        loadingTrashItems: 'Loading trash items...',
        deletingItems: 'Deleting items...',
        deletingItem: 'Deleting "{filename}"...',
        deletedPermanentlyPartial: '{succeeded} of {total} items deleted permanently',
        failedDeletePermanentlySingle: 'Failed to delete 1 item',
        failedDeletePermanentlyMultiple: 'Failed to delete {count} items',
        failedEmptyTrashPartial: 'Failed to delete {count} items',

        // ═══════════════════════════════════════════════════════════════
        // CONFIRMATION DIALOGS
        // ═══════════════════════════════════════════════════════════════
        confirmDelete: 'Are you sure you want to delete "{filename}"?',
        confirmDeletePermanent: 'Delete "{filename}" permanently? This action cannot be undone.',
        confirmDeletePermanentMultiple: 'Delete {count} items permanently? This action cannot be undone.',
        confirmDeleteMultiple: 'Delete {count} items?',
        renamePrompt: 'Enter new name for "{filename}":',
        signOutMessage: 'Are you sure you want to sign out?',
        continue: 'Continue',
        finalConfirmation: 'Final Confirmation',

        // Delete permanently - second confirmation
        confirmDeletePermanentFinal:
            'Are you absolutely sure? "{filename}" will be permanently deleted and cannot be recovered.',
        confirmDeletePermanentFinalMultiple:
            'Are you absolutely sure? {count} items will be permanently deleted and cannot be recovered.',

        // Empty trash - second confirmation
        confirmEmptyTrashFinal:
            'Are you absolutely sure? This will permanently delete ALL items in trash and cannot be undone.',

        // ═══════════════════════════════════════════════════════════════
        // UPLOAD & DOWNLOAD
        // ═══════════════════════════════════════════════════════════════
        downloading: 'Downloading {filename}',
        downloaded: 'Downloaded {filename}',
        downloadingMultiple: 'Downloading {count} items...',
        downloadedMultiple: 'Downloaded {count} items',
        failedDownload: 'Failed to download file',
        failedDownloadMultiple: 'Failed to download items',

        // Folder upload errors
        uploadSkippedParentFailed: 'Skipped - parent folder failed',
        uploadSkippedFolderExists: 'Skipped - folder already exists',
        uploadFailedFolderError: 'Failed - folder creation error',
        folderAlreadyExistsSkipped: 'Folder "{foldername}" already exists, files skipped',
        failedCreateFolderPath: 'Failed to create folder "{foldername}"',
        uploadFolderSkipped: 'Uploaded {successCount} files, {skippedCount} skipped (folder exists)',
        uploadFolderPartialComplete: '{successCount} uploaded, {errorCount} failed, {skippedCount} skipped',
        invalidFolderStructure: 'Invalid folder structure',
        uploadingFolder: 'Uploading folder: {count} files',
        folderAlreadyExistsCancelled: 'Folder "{foldername}" already exists. Upload cancelled',
        uploadBlockedFolderExists: 'Blocked: folder already exists',
        uploadCancelled: 'Cancelled',
        uploadFolderSuccess: 'Folder uploaded successfully: {count} files',
        uploadFolderPartial: 'Upload partially completed: {successCount} success, {errorCount} failed',
        uploadFolderFailed: 'Upload failed: {count} files',
        uploadFailed: 'Upload failed',

        uploadingFiles: 'Uploading files',
        uploadComplete: 'Upload complete',
        cancelUpload: 'Cancel upload',
        uploadingFile: 'Uploading {filename} ({current}/{total})',
        uploadSuccess: '{filename} uploaded successfully',
        failedUpload: 'Upload failed',
        uploadFailedSingle: 'Failed to upload {filename}',
        folderUploadNotSupported: 'Folder upload is not supported by your browser',

        creatingArchive: 'Creating archive...',

        // ═══════════════════════════════════════════════════════════════
        // SELECTION & TOOLBAR
        // ═══════════════════════════════════════════════════════════════
        selectionCount: '{count} selected',
        selectedAllItems: 'Selected {count} items',
        moveDialogNotImplemented: 'Move dialog not implemented yet. Use drag & drop instead.',

        // ═══════════════════════════════════════════════════════════════
        // EMPTY STATES
        // ═══════════════════════════════════════════════════════════════
        emptyFolder: 'This folder is empty',
        uploadGetStarted: 'Upload files or create folders to get started',
        noSearchResults: 'No results found',
        noSearchResultsMessage: 'Try a different search term',

        // ═══════════════════════════════════════════════════════════════
        // GENERAL MESSAGES
        // ═══════════════════════════════════════════════════════════════
        loading: 'Loading...',
        processing: 'Processing...',
        calculating: 'Calculating...',
        featureNotImplemented: 'Feature not yet implemented: {featureName}',
        createdSuccessfully: 'Created successfully',
        deletedSuccessfully: 'Deleted successfully',
        restoredSuccessfully: 'Restored successfully',

        // ═══════════════════════════════════════════════════════════════
        // NOTIFICATIONS
        // ═══════════════════════════════════════════════════════════════
        notificationSuccess: 'Success',
        notificationError: 'Error',
        notificationWarning: 'Warning',
        notificationInfo: 'Information',

        // ═══════════════════════════════════════════════════════════════
        // AUTHENTICATION
        // ═══════════════════════════════════════════════════════════════
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
        usernameHint: '3-50 latin letters or numbers',
        passwordHint: 'At least 6 characters. Use a strong, unique password',
        emailVerificationRequired: 'Email verification required',
        checkYourEmail: 'Please check your email and follow the link to activate your account.',
        emailVerification: 'Email Verification',
        checkingVerification: 'Checking verification...',
        verificationSuccess: 'Your email has been successfully verified! You can now log in.',
        emailChangeSuccess: 'Email successfully changed',
        vereficationTokenMissing: 'Verification token missing.',
        verificationFailed: 'Email verification failed or token expired.',
        goToLogin: 'Go to Login',

        // ═══════════════════════════════════════════════════════════════
        // VALIDATION ERRORS
        // ═══════════════════════════════════════════════════════════════
        invalidCharacters: 'Invalid characters: < > : " / \\ | ? *',
        nameTooLong: 'Name is too long (max 250 characters)',
        invalidName: 'Invalid name provided',
        nameAlreadyExists: 'An item with this name already exists in this location',
        invalidCharacter: 'The name contains invalid characters',
        reservedName: 'This name is reserved and cannot be used',
        invalidNameFormat: 'The name format is invalid',
        notAllowedSymbol: 'The name contains a symbol that is not allowed',

        // ═══════════════════════════════════════════════════════════════
        // API ERROR CODES
        // ═══════════════════════════════════════════════════════════════
        // Authentication Errors
        invalidCredentials: 'Invalid username or password',
        signInFailed: 'Sign in failed',
        registrationFailed: 'Registration failed. Please try again',
        passwordsNoMatch: 'Passwords do not match',

        // Item Errors
        itemNotFound: 'The requested item was not found',
        fileNotFound: 'The requested file was not found',
        folderNotFound: 'The requested folder was not found',
        unsupportedType: 'This item type is not supported for this operation',
        noItems: 'There are no items to process',
        parentFolderDeleted: 'The parent folder has been deleted',
        nullOrEmpty: 'A required value was not provided',

        // File Size & Type Errors
        fileTooLarge: 'The file is too large',
        archiveTooLarge: 'The folder is too large to be downloaded as an archive',
        tooManyFiles: 'The folder contains too many files to be processed at once',
        invalidFileType: 'This file type is not allowed',
        fileRequired: 'A file is required for this operation',

        // Permission Errors
        accessDenied: 'Access denied. You do not have permission to perform this action',
        insufficientPermission: 'You have insufficient permissions',
        invalidPermission: 'The specified permission is invalid',

        // Teamspace Errors
        teamspaceNotFound: 'Teamspace not found',
        teamspaceAccessDenied: 'You do not have access to this teamspace',
        teamspaceLimitReached: 'The limit of teamspaces has been reached',
        teamspaceNameTaken: 'This teamspace name is already taken',
        memberNotFound: 'Member not found in this teamspace',
        memberAlreadyExists: 'This user is already a member of the teamspace',
        memberLimitReached: 'The teamspace member limit has been reached',
        cannotRemoveAdmin: 'The last administrator cannot be removed from a teamspace',
        cannotLeaveAsAdmin: 'You cannot leave the teamspace as you are the only administrator',
        userNotFound: 'User not found',

        // Storage & System Errors
        storageLimitExceeded: 'Storage limit exceeded. Cannot upload file',
        badRequest: 'The request was invalid',
        operationFailed: 'The operation failed. Please try again',
        unexpectedError: 'An unexpected error occurred',
        ioError: 'A file system error occurred on the server',
        networkError: 'Network error. Please check your connection',
        connectionTimeout: 'Connection timed out',
        timeoutMessage: 'The server took too long to respond. Please try again.',
        serverError: 'Server error',
        serverErrorMessage: 'Something went wrong on the server. Please try again later.',
        noConnection: 'No internet connection',
        noConnectionMessage: 'Please check your internet connection and try again.',
        unableToConnect: 'Unable to connect',
        connectionErrorMessage: 'Please check your connection and try again.',
        serviceUnavailable: 'Service unavailable',

        // ══════════════════════════════════════════════════════════════
        // Error Pages
        // ══════════════════════════════════════════════════════════════
        error404Title: '404 - Page Not Found',
        error404Heading: 'Page Not Found',
        error404Message: "The page you're looking for doesn't exist or has been moved. Please check the URL or return to the home page.",
        error50xTitle: 'Server Error',
        error50xHeading: 'Server Error',
        error50xMessage: "Something went wrong on our end. We're working to fix the issue. Please try again in a few moments.",
        refreshPage: 'Refresh Page',
        goHome: 'Go to Home',
        contactSupport: 'If the problem persists, please contact support.',

        // ═══════════════════════════════════════════════════════════════
        // Settings
        // ═══════════════════════════════════════════════════════════════

        settings: 'Settings',
        profile: 'Profile',
        subscription: 'Subscription',
        security: 'Security',
        signOut: 'Sign Out',
        email: 'Email',
        currentPlan: 'Current plan',
        storageUsed: 'Storage used',
        plan_free_1: '10GB personal',
        plan_free_2: '5GB teamspace',
        plan_free_3: '2 teamspaces',
        plan_free_4: '5 members',
        plan_premium_1: '20GB personal',
        plan_premium_2: '50GB teamspace',
        plan_premium_3: '10 teamspaces',
        plan_premium_4: '25 members',
        plan_enterprise_1: '50GB personal',
        plan_enterprise_2: '500GB teamspace',
        plan_enterprise_3: 'Unlimited teamspaces',
        plan_enterprise_4: '100 members',
        subscribe: 'Subscribe',
        changeEmail: 'Change Email',
        changeUsername: 'Change Username',
        changePassword: 'Change Password',
        currentPlanBadge: 'Current',
        popularBadge: 'Popular',
        perMonth: '/month',
        currentPlanBtn: 'Current Plan',
        getStarted: 'Get Started',
    },

    uk: {
        // ═══════════════════════════════════════════════════════════════
        // GENERAL UI
        // ═══════════════════════════════════════════════════════════════
        signOut: 'Вийти',
        new: 'Створити',
        uploadFiles: 'Завантажити файли',
        uploadFolder: 'Завантажити папку',
        myDrive: 'Мій диск',
        recent: 'Останні',
        shared: 'Надані мені',
        trash: 'Кошик',
        searchPlaceholder: 'Пошук на Диску',
        open: 'Відкрити',
        download: 'Завантажити',
        move: 'Перемістити',
        cancel: 'Скасувати',
        create: 'Створити',

        // ═══════════════════════════════════════════════════════════════
        // MOVE TO MODAL
        // ═══════════════════════════════════════════════════════════════
        moveToModalTitle: 'Перемістити до',
        moveToModalDescription: 'Виберіть папку призначення',
        moveItem: 'Перемістити елемент',
        moveItems: 'Перемістити {{count}} елементів',
        moveTo: 'Перемістити до',
        movingTo: 'Переміщення до',
        selectDestination: 'Виберіть місце призначення',
        currentLocation: 'Поточне розташування',
        movingItems: 'Переміщення {{count}} елементів...',
        movedSuccessfully: 'Успішно переміщено {{count}} елементів',
        moveConfirm: 'Перемістити сюди',
        cannotMoveHere: 'Неможливо перемістити сюди',
        cannotMoveIntoItself: 'Неможливо перемістити папку в саму себе',
        folderSelection: 'Вибір папки',
        items: 'елементів',
        folders: 'папок',
        files: 'файлів',
        movedItem: 'Елемент "{{filename}}" успішно переміщено',
        movedPartial: 'Переміщено {{succeeded}} з {{total}} елементів',
        failedToMove: 'Не вдалося перемістити елементи',
        loadingFolders: 'Завантаження папок...',
        noSubfolders: 'Немає підпапок',
        failedToLoadFolders: 'Не вдалося завантажити папки',
        movedItems: ' Успішно переміщено {{count}} елементів',

        // ═══════════════════════════════════════════════════════════════
        // FILE & FOLDER OPERATIONS
        // ═══════════════════════════════════════════════════════════════
        // Table Headers
        name: 'Назва',
        modified: 'Змінено',
        created: 'Створено',
        size: 'Розмір',

        // Folder Operations
        newFolder: 'Нова папка',
        folderName: 'Назва папки:',
        untitledFolder: 'Папка без назви',
        creatingFolder: 'Створення папки...',
        folderCreated: "Папка '{foldername}' створена",
        failedCreateFolder: "Не вдалося створити папку '{foldername}'",
        folderNameRequired: 'Введіть назву папки',
        folderNameConflict: 'Папка з такою назвою вже існує',
        parentFolderNotFound: 'Батьківська папка не знайдена',

        // File Operations
        downloadFile: 'Завантажити',
        downloadFolder: 'Завантажити папку',
        rename: 'Перейменувати',
        renameItem: 'Перейменувати елемент',
        newName: 'Нова назва',
        renameHint: 'Введіть нову назву',
        renaming: 'Перейменування...',
        renamed: 'Перейменовано "{oldName}" на "{newName}"',
        failedRename: 'Не вдалося перейменувати',

        // Delete Operations
        delete: 'Видалити',
        deleteFile: 'Видалити файл',
        deleteFolder: 'Видалити папку',
        deleteItem: 'Видалити елемент?',
        deleting: 'Видалення {filename}...',
        deleted: '"{filename}" видалено',
        deletedMultiple: 'Видалено {count} елементів',
        deletedPartial: 'Видалено {succeeded} елементів. Помилок: {failed}',
        failedDelete: 'Не вдалося видалити',
        failedDeleteMultiple: 'Не вдалося видалити елементи',

        // Restore Operations
        restore: 'Відновити',
        restoring: 'Відновлення {filename}...',
        restored: '"{filename}" відновлено',
        restoredItems: 'Відновлено {count} елементів',
        restoredMultiple: 'Відновлено {count} елементів',
        restoredPartial: 'Відновлено {succeeded} елементів. Помилок: {failed}',
        failedRestore: 'Не вдалося відновити',
        failedRestoreMultiple: 'Не вдалося відновити елементи',

        // ═══════════════════════════════════════════════════════════════
        // TRASH OPERATIONS
        // ═══════════════════════════════════════════════════════════════
        deletePermanently: 'Видалити назавжди',
        deletedPermanently: '"{filename}" видалено назавжди',
        deletedPermanentlyMultiple: '{count} елементів видалено назавжди',
        failedDeletePermanently: 'Не вдалося видалити назавжди',
        emptyTrash: 'Кошик порожній',
        emptyTheTrash: 'Очистити кошик',
        emptyTrashMessage: 'Видалені елементи зберігатимуться тут 30 днів',
        deleteAllForever: 'Очистити кошик',
        confirmEmptyTrash: 'Видалити всі елементи з кошика назавжди? Цю дію неможливо скасувати.',
        trashAlreadyEmpty: 'Кошик вже порожній',
        trashEmptiedCount: '{count} елементів видалено назавжди',
        failedEmptyTrash: 'Не вдалося очистити кошик',
        loadingTrashItems: 'Завантаження елементів...',
        deletingItems: 'Видалення елементів...',
        deletingItem: 'Видалення "{filename}"...',
        deletedPermanentlyPartial: '{succeeded} з {total} елементів видалено назавжди',
        failedDeletePermanentlySingle: 'Не вдалося видалити 1 елемент',
        failedDeletePermanentlyMultiple: 'Не вдалося видалити {count} елементів',
        failedEmptyTrashPartial: 'Не вдалося видалити {count} елементів',

        // ═══════════════════════════════════════════════════════════════
        // CONFIRMATION DIALOGS
        // ═══════════════════════════════════════════════════════════════
        confirmDelete: 'Ви впевнені, що хочете видалити "{filename}"?',
        confirmDeleteMultiple: 'Видалити {count} елементів?',
        confirmDeletePermanent: 'Видалити "{filename}" назавжди? Цю дію неможливо скасувати.',
        confirmDeletePermanentMultiple: 'Видалити {count} елементів назавжди? Цю дію неможливо скасувати.',
        renamePrompt: 'Введіть нову назву для "{filename}":',
        signOutMessage: 'Ви впевнені, що хочете вийти?',
        continue: 'Продовжити',
        finalConfirmation: 'Остаточне підтвердження',

        // Delete permanently - second confirmation
        confirmDeletePermanentFinal: 'Ви абсолютно впевнені? "{filename}" буде безповоротно видалено.',
        confirmDeletePermanentFinalMultiple: 'Ви абсолютно впевнені? {count} елементів будуть безповоротно видалені.',

        // Empty trash - second confirmation
        confirmEmptyTrashFinal: 'Ви абсолютно впевнені? Це безповоротно видалить УСІ елементи з кошика.',

        // ═══════════════════════════════════════════════════════════════
        // UPLOAD & DOWNLOAD
        // ═══════════════════════════════════════════════════════════════
        downloading: 'Завантаження {filename}',
        downloaded: 'Завантажено {filename}',
        downloadingMultiple: 'Завантаження {count} елементів...',
        downloadedMultiple: 'Завантажено {count} елементів',
        failedDownload: 'Не вдалося завантажити файл',
        failedDownloadMultiple: 'Не вдалося завантажити елементи',

        // Folder upload errors
        uploadSkippedParentFailed: 'Пропущено - батьківська папка не створена',
        uploadSkippedFolderExists: 'Пропущено - папка вже існує',
        uploadFailedFolderError: 'Помилка - не вдалося створити папку',
        folderAlreadyExistsSkipped: 'Папка "{foldername}" вже існує, файли пропущено',
        failedCreateFolderPath: 'Не вдалося створити папку "{foldername}"',
        uploadFolderSkipped: 'Завантажено {successCount} файлів, {skippedCount} пропущено (папка існує)',
        uploadFolderPartialComplete: 'Завантажено {successCount}, помилок {errorCount}, пропущено {skippedCount}',
        invalidFolderStructure: 'Невірна структура папки',
        uploadingFolder: 'Завантаження папки: {count} файлів',
        folderAlreadyExistsCancelled: 'Папка "{foldername}" вже існує. Завантаження скасовано',
        uploadBlockedFolderExists: 'Заблоковано: папка вже існує',
        uploadCancelled: 'Скасовано',
        uploadFolderSuccess: 'Папка успішно завантажена: {count} файлів',
        uploadFolderPartial: 'Завантаження завершено частково: успішно {successCount}, помилок {errorCount}',
        uploadFolderFailed: 'Помилка завантаження: {count} файлів',
        uploadFailed: 'Помилка завантаження',

        uploadingFiles: 'Завантаження файлів',
        uploadComplete: 'Завантаження завершено',
        cancelUpload: 'Скасувати завантаження',
        uploadingFile: 'Завантаження {filename} ({current}/{total})',
        uploadSuccess: '{filename} завантажено успішно',
        failedUpload: 'Помилка завантаження',
        uploadFailedSingle: 'Не вдалося завантажити {filename}',
        folderUploadNotSupported: 'Ваш браузер не підтримує завантаження папок',

        creatingArchive: 'Створення архіву...',

        // ═══════════════════════════════════════════════════════════════
        // SELECTION & TOOLBAR
        // ═══════════════════════════════════════════════════════════════
        selectionCount: '{count} вибрано',
        selectedAllItems: 'Вибрано {count} елементів',
        moveDialogNotImplemented: 'Діалог переміщення ще не реалізований. Використовуйте перетягування.',

        // ═══════════════════════════════════════════════════════════════
        // EMPTY STATES
        // ═══════════════════════════════════════════════════════════════
        emptyFolder: 'Ця папка порожня',
        uploadGetStarted: 'Завантажте файли або створіть папки, щоб почати',
        noSearchResults: 'Нічого не знайдено',
        noSearchResultsMessage: 'Спробуйте інший запит',

        // ═══════════════════════════════════════════════════════════════
        // GENERAL MESSAGES
        // ═══════════════════════════════════════════════════════════════
        loading: 'Завантаження...',
        processing: 'Обробка...',
        calculating: 'Обчислення...',
        featureNotImplemented: 'Функція ще не реалізована: {featureName}',
        createdSuccessfully: 'Успішно створено',
        deletedSuccessfully: 'Успішно видалено',
        restoredSuccessfully: 'Успішно відновлено',

        // ═══════════════════════════════════════════════════════════════
        // NOTIFICATIONS
        // ═══════════════════════════════════════════════════════════════
        notificationSuccess: 'Успішно',
        notificationError: 'Помилка',
        notificationWarning: 'Попередження',
        notificationInfo: 'Інформація',

        // ═══════════════════════════════════════════════════════════════
        // AUTHENTICATION
        // ═══════════════════════════════════════════════════════════════
        username: "Ім'я користувача",
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
        usernameHint: '3-50 латинських букв або цифр',
        passwordHint: 'Щонайменше 6 символів. Використовуйте надійний унікальний пароль',
        emailVerificationRequired: 'Потрібна перевірка електронної пошти',
        checkYourEmail: 'Будь ласка, перевірте свою пошту та перейдіть за посиланням для активації вашого акаунту.',
        emailVerification: 'Перевірка електронної пошти',
        emailChangeSuccess: 'Електронну пошту успішно змінено',
        checkingVerification: 'Перевірка...',
        verificationSuccess: 'Ваша електронна пошта успішно підтверджена! Тепер ви можете увійти.',
        vereficationTokenMissing: 'Відсутній токен підтвердження.',
        verificationFailed: 'Перевірка електронної пошти не вдалася або термін дії токена минув.',
        goToLogin: 'Перейти до входу',

        // ═══════════════════════════════════════════════════════════════
        // VALIDATION ERRORS
        // ═══════════════════════════════════════════════════════════════
        invalidCharacters: 'Недопустимі символи: < > : " / \\ | ? *',
        nameTooLong: 'Назва занадто довга (максимум 250 символів)',
        invalidName: 'Вказано неправильну назву',
        nameAlreadyExists: 'Елемент з такою назвою вже існує в цій папці',
        invalidCharacter: 'Назва містить недопустимі символи',
        reservedName: 'Ця назва зарезервована і не може бути використана',
        invalidNameFormat: 'Неправильний формат назви',
        notAllowedSymbol: 'Назва містить заборонений символ',

        // ═══════════════════════════════════════════════════════════════
        // API ERROR CODES
        // ═══════════════════════════════════════════════════════════════
        // Authentication Errors
        invalidCredentials: "Неправильне ім'я користувача або пароль",
        signInFailed: 'Не вдалося увійти',
        registrationFailed: 'Помилка реєстрації. Спробуйте ще раз',
        passwordsNoMatch: 'Паролі не збігаються',

        // Item Errors
        itemNotFound: 'Запитаний елемент не знайдено',
        fileNotFound: 'Запитаний файл не знайдено',
        folderNotFound: 'Запитану папку не знайдено',
        unsupportedType: 'Цей тип елемента не підтримується для даної операції',
        noItems: 'Немає елементів для обробки',
        parentFolderDeleted: 'Батьківська папка була видалена',
        nullOrEmpty: "Не було надано обов'язкове значення",

        // File Size & Type Errors
        fileTooLarge: 'Файл занадто великий',
        archiveTooLarge: 'Папка занадто велика для завантаження у вигляді архіву',
        tooManyFiles: 'Папка містить занадто багато файлів для одночасної обробки',
        invalidFileType: 'Цей тип файлу не дозволений',
        fileRequired: 'Для цієї операції потрібен файл',

        // Permission Errors
        accessDenied: 'Доступ заборонено. У вас немає дозволу на виконання цієї дії',
        insufficientPermission: 'У вас недостатньо прав',
        invalidPermission: 'Вказано недійсний дозвіл',

        // Teamspace Errors
        teamspaceNotFound: 'Робочий простір не знайдено',
        teamspaceAccessDenied: 'У вас немає доступу до цього робочого простору',
        teamspaceLimitReached: 'Досягнуто ліміту на кількість робочих просторів',
        teamspaceNameTaken: 'Ця назва робочого простору вже зайнята',
        memberNotFound: 'Учасника не знайдено в цьому робочому просторі',
        memberAlreadyExists: 'Цей користувач вже є учасником робочого простору',
        memberLimitReached: 'Досягнуто ліміту на кількість учасників',
        cannotRemoveAdmin: 'Неможливо видалити останнього адміністратора з робочого простору',
        cannotLeaveAsAdmin: 'Ви не можете покинути простір, оскільки є єдиним адміністратором',
        userNotFound: 'Користувача не знайдено',

        // Storage & System Errors
        storageLimitExceeded: 'Перевищено ліміт сховища. Неможливо завантажити файл',
        badRequest: 'Неправильний запит',
        operationFailed: 'Операція не вдалася. Спробуйте ще раз',
        unexpectedError: 'Сталася неочікувана помилка',
        ioError: 'На сервері сталася помилка файлової системи',
        networkError: "Помилка мережі. Перевірте з'єднання",
        connectionTimeout: 'Час очікування минув',
        timeoutMessage: 'Сервер занадто довго відповідає. Спробуйте ще раз.',
        serverError: 'Помилка сервера',
        serverErrorMessage: 'Щось пішло не так на сервері. Спробуйте пізніше.',
        noConnection: 'Немає з\'єднання з інтернетом',
        noConnectionMessage: 'Перевірте підключення до інтернету та спробуйте ще раз.',
        unableToConnect: 'Не вдається підключитися',
        connectionErrorMessage: 'Перевірте з\'єднання та спробуйте ще раз.',
        serviceUnavailable: 'Сервіс недоступний',

        // ══════════════════════════════════════════════════════════════
        // Error Pages
        // ══════════════════════════════════════════════════════════════
        error404Title: '404 - Сторінку не знайдено',
        error404Heading: 'Сторінку не знайдено',
        error404Message: 'Сторінка, яку ви шукаєте, не існує або була переміщена. Будь ласка, перевірте URL або поверніться на головну сторінку.',
        error50xTitle: 'Помилка сервера',
        error50xHeading: 'Помилка сервера',
        error50xMessage: 'Щось пішло не так на нашому боці. Ми працюємо над вирішенням проблеми. Будь ласка, спробуйте ще раз через кілька хвилин.',
        refreshPage: 'Оновити сторінку',
        goHome: 'На головну',
        contactSupport: 'Якщо проблема не зникає, зверніться до служби підтримки.',

        // ═══════════════════════════════════════════════════════════════
        // Settings
        // ═══════════════════════════════════════════════════════════════

        settings: 'Налаштування',
        profile: 'Профіль',
        subscription: 'Підписка',
        security: 'Безпека',
        signOut: 'Вийти',
        email: 'Пошта',
        currentPlan: 'Поточний план',
        storageUsed: 'Використано місця',
        plan_free_1: '10ГБ особисте',
        plan_free_2: '5ГБ teamspace',
        plan_free_3: '2 teamspaces',
        plan_free_4: '5 учасників',
        plan_premium_1: '20ГБ особисте',
        plan_premium_2: '50ГБ teamspace',
        plan_premium_3: '10 teamspaces',
        plan_premium_4: '25 учасників',
        plan_enterprise_1: '50ГБ особисте',
        plan_enterprise_2: '500ГБ teamspace',
        plan_enterprise_3: 'Безліміт teamspaces',
        plan_enterprise_4: '100 учасників',
        subscribe: 'Підписатися',
        changeEmail: 'Змінити пошту',
        changeUsername: 'Змінити ім\'я користувача',
        changePassword: 'Змінити пароль',
        currentPlanBadge: 'Поточний',
        popularBadge: 'Популярний',
        perMonth: '/місяць',
        currentPlanBtn: 'Поточний план',
        getStarted: 'Почати',
    }
};

export class I18n {
    constructor() {
        this.currentLanguage = localStorage.getItem('cloudcore-language') || 'en';
        this.translations = translations;
    }

    t(key, replacements = {}) {
        let translation = translations[this.currentLanguage]?.[key] || key;

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
            const key =
                error.errorCode.charAt(0).toLowerCase() +
                error.errorCode.slice(1).replace(/_(\w)/g, (_, p1) => p1.toUpperCase());
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
        document.querySelectorAll('[data-i18n]').forEach((element) => {
            const key = element.getAttribute('data-i18n');
            const attr = element.getAttribute('data-i18n-attr');

            const translation = this.t(key);

            if (attr) {
                element.setAttribute(attr, translation);
            } else {
                element.textContent = translation;
            }
        });

        this.updatePrices();

        this.updateLanguageButton();
    }

    updatePrices() {
        document.querySelectorAll('.price-amount').forEach((priceElement) => {
        const usdPrice = priceElement.getAttribute('data-price-usd');
        const uahPrice = priceElement.getAttribute('data-price-uah');
        
        if (this.currentLanguage === 'uk') {
            priceElement.textContent = uahPrice === '0' ? '₴0' : `₴${uahPrice}`;
        } else {
            priceElement.textContent = usdPrice === '0' ? '$0' : `$${usdPrice}`;
        }
        });
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
