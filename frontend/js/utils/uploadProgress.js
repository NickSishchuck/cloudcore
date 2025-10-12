export class UploadProgressManager {
    constructor(i18n = null) {
        this.i18n = i18n;
        this.container = document.getElementById('uploadProgressContainer');
        this.listContainer = document.getElementById('uploadProgressList');
        this.minimizeBtn = document.getElementById('uploadMinimizeBtn');
        this.uploads = new Map();
        this.isMinimized = false;

        this.initialize();
    }

    initialize() {
        if (this.minimizeBtn) {
            this.minimizeBtn.addEventListener('click', () => this.toggleMinimize());
        }
        this.updateTranslations();
    }

    updateTranslations() {
        if (!this.i18n) return;
        
        const header = this.container?.querySelector('[data-i18n="uploadingFiles"]');
        if (header) {
            header.textContent = this.i18n.t('uploadingFiles');
        }
    }
    
    setI18n(i18n) {
        this.i18n = i18n;
        this.updateTranslations();
    }

    toggleMinimize() {
        this.isMinimized = !this.isMinimized;
        if (this.isMinimized) {
            this.container.classList.add('minimized');
        } else {
            this.container.classList.remove('minimized');
        }
    }

    show() {
        if (this.container) {
            this.container.classList.add('visible');
        }
    }

    hide() {
        if (this.container) {
            setTimeout(() => {
                if (this.uploads.size === 0) {
                    this.container.classList.remove('visible');
                }
            }, 1000);
        }
    }

    addUpload(uploadId, fileName, fileSize, onCancel = null) {
        const uploadItem = document.createElement('div');
        uploadItem.className = 'upload-item uploading';
        uploadItem.id = `upload-${uploadId}`;

        const fileSizeFormatted = this.formatFileSize(fileSize);

        uploadItem.innerHTML = `
            <div class="upload-item-header">
                <span class="material-symbols-outlined upload-item-icon">upload_file</span>
                <div class="upload-item-info">
                    <div class="upload-item-name" title="${this.escapeHtml(fileName)}">${this.escapeHtml(fileName)}</div>
                    <div class="upload-item-status">0% • ${fileSizeFormatted}</div>
                </div>
                <button class="upload-item-cancel" data-upload-id="${uploadId}" title="${this.t('cancelUpload')}">
                    <span class="material-symbols-outlined">close</span>
                </button>
            </div>
            <div class="upload-progress-bar-container">
                <div class="upload-progress-bar" style="width: 0%"></div>
            </div>
        `;

        this.listContainer.appendChild(uploadItem);


        this.uploads.set(uploadId, {
            element: uploadItem,
            fileName: fileName,
            fileSize: fileSize,
            progress: 0,
            status: 'uploading',
            cancelFn: onCancel
        });

        this.show();

        const cancelBtn = uploadItem.querySelector('.upload-item-cancel');
        if (cancelBtn) {
            cancelBtn.addEventListener('click', () => this.cancelUpload(uploadId));
        }
    }

    updateProgress(uploadId, progress, uploaded, total) {
        const upload = this.uploads.get(uploadId);
        if (!upload) return;

        const progressBar = upload.element.querySelector('.upload-progress-bar');
        const statusEl = upload.element.querySelector('.upload-item-status');

        if (progressBar) {
            progressBar.style.width = `${progress}%`;
        }

        if (statusEl) {
            const uploadedFormatted = this.formatFileSize(uploaded);
            const totalFormatted = this.formatFileSize(total);
            statusEl.textContent = `${Math.round(progress)}% • ${uploadedFormatted} / ${totalFormatted}`;
        }

        upload.progress = progress;
    }

    completeUpload(uploadId) {
        const upload = this.uploads.get(uploadId);
        if (!upload) return;

        upload.element.classList.remove('uploading');
        upload.element.classList.add('completed');
        upload.status = 'completed';

        const iconEl = upload.element.querySelector('.upload-item-icon');
        const statusEl = upload.element.querySelector('.upload-item-status');

        if (iconEl) iconEl.textContent = 'check_circle';
        if (statusEl) statusEl.textContent = 'Upload complete';

        setTimeout(() => this.removeUpload(uploadId), 3000);
    }

    errorUpload(uploadId, errorMessage = 'Upload failed') {
        const upload = this.uploads.get(uploadId);
        if (!upload) return;

        upload.element.classList.remove('uploading');
        upload.element.classList.add('error');
        upload.status = 'error';

        const iconEl = upload.element.querySelector('.upload-item-icon');
        const statusEl = upload.element.querySelector('.upload-item-status');

        if (iconEl) iconEl.textContent = 'error';
        if (statusEl) {
            statusEl.textContent = errorMessage || this.t('uploadFailed');
        }

        setTimeout(() => this.removeUpload(uploadId), 5000);
    }

    cancelUpload(uploadId) {
        const upload = this.uploads.get(uploadId);
        if (!upload) return;

        console.log('Cancelling upload:', uploadId);

        if (upload.cancelFn) {
            upload.cancelFn();
        }
        upload.element.classList.remove('uploading');
        upload.element.classList.add('error');

        const iconEl = upload.element.querySelector('.upload-item-icon');
        const statusEl = upload.element.querySelector('.upload-item-status');

        if (iconEl) iconEl.textContent = 'cancel';
        if (statusEl) statusEl.textContent = this.t('uploadCancelled');

        setTimeout(() => this.removeUpload(uploadId), 2000);
    }

    removeUpload(uploadId) {
        const upload = this.uploads.get(uploadId);
        if (!upload) return;

        upload.element.style.opacity = '0';
        upload.element.style.transform = 'translateX(100%)';
        upload.element.style.transition = 'all 0.3s ease';

        setTimeout(() => {
            upload.element.remove();
            this.uploads.delete(uploadId);

            if (this.uploads.size === 0) {
                this.hide();
            }
        }, 300);
    }

    formatFileSize(bytes) {
        if (bytes === 0) return '0 B';
        const k = 1024;
        const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    t(key, params = {}) {
        if (!this.i18n) return key;
        return this.i18n.t(key, params);
    }
}