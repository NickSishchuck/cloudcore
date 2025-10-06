import { I18n } from './translations.js';
import { ApiClient } from './api.js';
import { getNotificationManager } from './ui/notifications.js';
import { formatFileSize, formatDateTime, getFileIcon, downloadBlob } from './utils/fileUtils.js';

class CloudCoreDrive {
    constructor() {
        this.i18n = new I18n();
        this.api = new ApiClient();
        this.notifications = getNotificationManager();
        
        // State
        this.currentUserId = null;
        this.currentUser = null;
        this.currentFolderId = null;
        this.isTrashView = false;
        this.breadcrumbPath = [];
        this.selectedItems = new Set();
        
        // Pagination
        this.currentPage = 1;
        this.pageSize = 30;
        this.hasNextPage = false;
        this.isLoadingMore = false;
        this.allLoadedItems = [];
        
        // Sorting
        this.sortBy = 'name';
        this.sortDir = 'asc';
        this.currentSearchQuery = null;
        
        this.initializeAuth();
    }

    initializeAuth() {
        const token = localStorage.getItem('cloudcore_token');
        const userStr = localStorage.getItem('cloudcore_user');

        if (!token || !userStr) {
            console.log('No authentication found, redirecting to login...');
            window.location.href = 'login.html';
            return;
        }

        this.api.setAuthToken(token);
        this.currentUser = JSON.parse(userStr);
        this.currentUserId = this.currentUser.id;

        console.log('🔐 Authenticated as:', this.currentUser.username);
        
        document.getElementById('userInfo').textContent = this.currentUser.username;
        
        this.initializeEventListeners();
        this.i18n.updateUI();
        this.loadFiles();
    }

    initializeEventListeners() {
        // Logout button
        document.getElementById('logoutBtn').addEventListener('click', () => this.logout());
        
        // Language switcher
        document.getElementById('languageBtn').addEventListener('click', () => {
            this.i18n.switchLanguage();
            location.reload();
        });
        
        // New dropdown
        document.getElementById('newButton').addEventListener('click', (e) => {
            e.stopPropagation();
            this.toggleNewDropdown();
        });
        
        // Upload handlers
        document.getElementById('uploadFiles').addEventListener('click', () => {
            this.hideNewDropdown();
            document.getElementById('fileInput').click();
        });
        
        document.getElementById('uploadFolder').addEventListener('click', () => {
            this.hideNewDropdown();
            document.getElementById('folderInput').click();
        });
        
        // File inputs
        document.getElementById('fileInput').addEventListener('change', (e) => this.handleFileUpload(e));
        document.getElementById('folderInput').addEventListener('change', (e) => this.handleFolderUpload(e));
        
        // Search
        document.getElementById('searchBox').addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                this.performSearch(e.target.value);
            }
        });
        
        // Sidebar navigation
        document.querySelectorAll('.sidebar-item').forEach(item => {
            item.addEventListener('click', (e) => this.handleSidebarClick(e));
        });
        
        // Sorting headers
        const headers = ['name', 'created', 'modified', 'size'];
        headers.forEach(header => {
            const th = document.querySelector(`th[data-i18n="${header}"]`);
            if (th) {
                th.addEventListener('click', () => this.applySort(header));
            }
        });
        
        // Scroll for infinite loading
        const container = document.getElementById('fileContainer');
        container.addEventListener('scroll', this.debounce((e) => this.handleScroll(e), 120));
        
        // Close dropdowns on outside click
        document.addEventListener('click', () => this.hideNewDropdown());
        document.addEventListener('click', () => this.hideContextMenu());
        
        // Setup drag and drop
        this.setupDragAndDrop();
    }

    async loadFiles(folderId = null, resetPagination = true, isTrashView = false) {
        if (resetPagination) {
            this.currentPage = 1;
            this.allLoadedItems = [];
            this.hasNextPage = false;
        }

        if (resetPagination) this.showLoading();
        else this.showInfiniteLoader();

        try {
            this.isTrashView = isTrashView;
            
            const params = {
                page: String(this.currentPage),
                pageSize: String(this.pageSize),
                sortBy: this.sortBy,
                sortDir: this.sortDir
            };
            
            if (this.currentSearchQuery) {
                params.searchQuery = this.currentSearchQuery;
            }
            
            if (!isTrashView && folderId !== null) {
                params.parentId = String(folderId);
            }
            
            const result = isTrashView 
                ? await this.api.getTrash(this.currentUserId, params)
                : await this.api.getFiles(this.currentUserId, params);
            
            const newItems = Array.isArray(result?.data) ? result.data : [];
            const pagination = result?.pagination;
            
            if (pagination) {
                this.hasNextPage = Boolean(pagination.hasNext);
            }
            
            if (resetPagination) {
                this.allLoadedItems = newItems;
            } else {
                this.allLoadedItems.push(...newItems);
            }
            
            this.renderFiles();
            this.currentFolderId = folderId;
            this.updateBreadcrumbs();
            
        } catch (error) {
            console.error('loadFiles error:', error);
            this.notifications.error('Failed to load files');
        } finally {
            this.hideInfiniteLoader();
            if (resetPagination) this.hideLoading();
        }
    }

    renderFiles() {
        const listBody = document.getElementById('fileListBody');
        const fileList = document.getElementById('fileList');
        
        if (this.allLoadedItems.length === 0) {
            this.showEmptyState();
            return;
        }
        
        this.hideEmptyState();
        
        if (this.currentPage === 1) {
            listBody.innerHTML = '';
        }
        
        fileList.style.display = 'table';
        
        this.allLoadedItems.forEach(item => {
            // Skip if already rendered
            if (listBody.querySelector(`[data-item-id="${item.id}"]`)) {
                return;
            }
            
            const row = this.createFileRow(item);
            listBody.appendChild(row);
        });
        
        this.updateSortIndicators();
    }

    createFileRow(item) {
        const row = document.createElement('tr');
        row.className = `file-list-row ${this.isTrashView ? 'trash-mode' : ''}`;
        row.dataset.itemId = item.id;
        row.dataset.itemType = item.type;

        const icon = getFileIcon(item);
        const sizeDisplay = item.type === 'file'
            ? (item.fileSize ? formatFileSize(item.fileSize) : '-')
            : '-';

        row.innerHTML = `
            <td><span class="file-list-icon ${icon.class}">${icon.emoji}</span> ${item.name}</td>
            <td>${formatDateTime(item.createdAt)}</td>
            <td>${formatDateTime(item.updatedAt)}</td>
            <td>${sizeDisplay}</td>
        `;

        row.addEventListener('click', (e) => this.handleFileClick(e, item));
        row.addEventListener('dblclick', (e) => this.handleFileDoubleClick(e, item));
        row.addEventListener('contextmenu', (e) => this.showContextMenu(e, item));

        return row;
    }

    async downloadFile(file) {
        try {
            this.notifications.info(this.i18n.t('downloading') + ' ' + file.name);
            const blob = await this.api.downloadFile(this.currentUserId, file.id);
            downloadBlob(blob, file.name);
            this.notifications.success(this.i18n.t('downloaded') + ': ' + file.name);
        } catch (error) {
            console.error('Download error:', error);
            this.notifications.error(this.i18n.t('failedDownload') + ' ' + file.name);
        }
    }

    logout() {
        console.log('👋 Signing out...');
        this.api.clearAuthToken();
        window.location.href = 'login.html';
    }

    // UI Helper Methods
    showLoading() {
        document.getElementById('loadingState').style.display = 'flex';
        document.getElementById('fileList').style.display = 'none';
        document.getElementById('emptyState').style.display = 'none';
    }

    hideLoading() {
        document.getElementById('loadingState').style.display = 'none';
        document.getElementById('fileList').style.display = 'table';
    }

    showEmptyState() {
        document.getElementById('emptyState').style.display = 'flex';
        document.getElementById('fileList').style.display = 'none';
    }

    hideEmptyState() {
        document.getElementById('emptyState').style.display = 'none';
    }

    showInfiniteLoader() {
        // Implementation for infinite scroll loader
    }

    hideInfiniteLoader() {
        // Implementation
    }

    toggleNewDropdown() {
        const button = document.getElementById('newButton');
        const dropdown = document.getElementById('newDropdown');
        button.classList.toggle('active');
        dropdown.classList.toggle('show');
    }

    hideNewDropdown() {
        const button = document.getElementById('newButton');
        const dropdown = document.getElementById('newDropdown');
        button.classList.remove('active');
        dropdown.classList.remove('show');
    }

    hideContextMenu() {
        document.getElementById('contextMenu').style.display = 'none';
    }

    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    // Stub methods - implement as needed
    handleFileUpload(e) { /* Implement */ }
    handleFolderUpload(e) { /* Implement */ }
    handleFileClick(e, item) { /* Implement */ }
    handleFileDoubleClick(e, item) { /* Implement */ }
    handleSidebarClick(e) { /* Implement */ }
    handleScroll(e) { /* Implement */ }
    showContextMenu(e, item) { /* Implement */ }
    applySort(column) { /* Implement */ }
    updateSortIndicators() { /* Implement */ }
    performSearch(query) { /* Implement */ }
    updateBreadcrumbs() { /* Implement */ }
    setupDragAndDrop() { /* Implement */ }
}

// Initialize app when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    new CloudCoreDrive();
});
