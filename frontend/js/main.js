import { I18n } from './translations.js';
import { ApiClient } from './api.js';
import { getNotificationManager } from './ui/notifications.js';
import { formatFileSize, formatDateTime, getFileIcon, downloadBlob, buildFolderStructure, isWebkitDirectorySupported } from './utils/fileUtils.js';

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
        
        // DOM elements
        this.fileListBody = null;
        this.fileList = null;
        this.contextMenu = null;
        
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
        
        // Cache DOM elements
        this.fileListBody = document.getElementById('fileListBody');
        this.fileList = document.getElementById('fileList');
        this.contextMenu = document.getElementById('contextMenu');
        
        this.initializeEventListeners();
        this.i18n.updateUI();
        this.loadFiles();
    }

    initializeEventListeners() {
        console.log('Setting up event listeners...');
        
        // Logout button
        document.getElementById('logoutBtn').addEventListener('click', () => {
            console.log('Logout clicked');
            this.logout();
        });
        
        // Language switcher
        document.getElementById('languageBtn').addEventListener('click', () => {
            console.log('Language switch clicked');
            this.i18n.switchLanguage();
            location.reload();
        });
        
        // New dropdown
        document.getElementById('newButton').addEventListener('click', (e) => {
            console.log('New button clicked');
            e.stopPropagation();
            this.toggleNewDropdown();
        });
        
        // Upload handlers
        document.getElementById('uploadFiles').addEventListener('click', () => {
            console.log('Upload files clicked');
            this.hideNewDropdown();
            document.getElementById('fileInput').click();
        });
        
        document.getElementById('uploadFolder').addEventListener('click', () => {
            console.log('Upload folder clicked');
            this.hideNewDropdown();
            if (!isWebkitDirectorySupported()) {
                this.notifications.error('Folder upload not supported');
                return;
            }
            document.getElementById('folderInput').click();
        });
        
        // File inputs
        document.getElementById('fileInput').addEventListener('change', (e) => {
            console.log('File input changed:', e.target.files.length);
            this.handleFileUpload(e);
        });
        
        document.getElementById('folderInput').addEventListener('change', (e) => {
            console.log('Folder input changed:', e.target.files.length);
            this.handleFolderUpload(e);
        });
        
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
        ['name', 'created', 'modified', 'size'].forEach(header => {
            const th = document.querySelector(`th[data-i18n="${header}"]`);
            if (th) {
                th.addEventListener('click', () => this.applySort(header));
            }
        });
        
        // Scroll for infinite loading
        const container = document.getElementById('fileContainer');
        container.addEventListener('scroll', this.debounce((e) => this.handleScroll(e), 120));
        
        // Close dropdowns and context menu
        document.addEventListener('click', () => {
            this.hideNewDropdown();
            this.hideContextMenu();
        });
        
        // Setup drag and drop
        this.setupDragAndDrop();
        
        console.log('Event listeners set up complete');
    }

    async loadFiles(folderId = null, resetPagination = true, isTrashView = false) {
        console.log('loadFiles called:', { folderId, resetPagination, isTrashView });
        
        if (resetPagination) {
            this.currentPage = 1;
            this.allLoadedItems = [];
            this.hasNextPage = false;
        }

        if (resetPagination) this.showLoading();

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
            
            console.log('Fetching files with params:', params);
            
            const result = isTrashView 
                ? await this.api.getTrash(this.currentUserId, params)
                : await this.api.getFiles(this.currentUserId, params);
            
            console.log('Files received:', result);
            
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
            this.notifications.error('Failed to load files: ' + error.message);
        } finally {
            if (resetPagination) this.hideLoading();
        }
    }

    renderFiles() {
        console.log('Rendering files:', this.allLoadedItems.length);
        
        if (this.allLoadedItems.length === 0) {
            this.showEmptyState();
            return;
        }
        
        this.hideEmptyState();
        this.fileListBody.innerHTML = '';
        this.fileList.style.display = 'table';
        
        this.allLoadedItems.forEach(item => {
            const row = this.createFileRow(item);
            this.fileListBody.appendChild(row);
        });
        
        this.updateSortIndicators();
        console.log('Files rendered');
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

        // Attach event handlers
        row.addEventListener('click', (e) => this.handleFileClick(e, item, row));
        row.addEventListener('dblclick', (e) => this.handleFileDoubleClick(e, item));
        row.addEventListener('contextmenu', (e) => this.showContextMenu(e, item));

        return row;
    }

    handleFileClick(e, item, row) {
        e.stopPropagation();
        console.log('File clicked:', item.name);
        
        document.querySelectorAll('.file-list-row.selected').forEach(el => {
            el.classList.remove('selected');
        });
        row.classList.add('selected');
        this.selectedItems.clear();
        this.selectedItems.add(item);
    }

    handleFileDoubleClick(e, item) {
        console.log('File double-clicked:', item.name);
        if (this.isTrashView) return;
        
        if (item.type === 'folder') {
            this.navigateToFolder(item);
        } else {
            this.downloadFile(item);
        }
    }

    showContextMenu(e, item) {
        e.preventDefault();
        e.stopPropagation();
        console.log('Context menu for:', item.name);

        if (!this.contextMenu) return;

        const menuHTML = this.isTrashView ? `
            <div class="context-menu-item" data-action="restore">
                <span class="context-menu-icon">🔄</span>
                <span>${this.i18n.t('restore')}</span>
            </div>
            <div class="context-menu-separator"></div>
            <div class="context-menu-item danger" data-action="delete-permanently">
                <span class="context-menu-icon">❌</span>
                <span>${this.i18n.t('deletePermanently')}</span>
            </div>
        ` : (item.type === 'folder' ? `
            <div class="context-menu-item" data-action="download-folder">
                <span class="context-menu-icon">⬇️</span>
                <span>${this.i18n.t('downloadFolder')}</span>
            </div>
            <div class="context-menu-separator"></div>
            <div class="context-menu-item" data-action="rename">
                <span class="context-menu-icon">✏️</span>
                <span>${this.i18n.t('rename')}</span>
            </div>
            <div class="context-menu-separator"></div>
            <div class="context-menu-item danger" data-action="delete">
                <span class="context-menu-icon">🗑️</span>
                <span>${this.i18n.t('deleteFolder')}</span>
            </div>
        ` : `
            <div class="context-menu-item" data-action="download">
                <span class="context-menu-icon">⬇️</span>
                <span>${this.i18n.t('downloadFile')}</span>
            </div>
            <div class="context-menu-separator"></div>
            <div class="context-menu-item" data-action="rename">
                <span class="context-menu-icon">✏️</span>
                <span>${this.i18n.t('rename')}</span>
            </div>
            <div class="context-menu-separator"></div>
            <div class="context-menu-item danger" data-action="delete">
                <span class="context-menu-icon">🗑️</span>
                <span>${this.i18n.t('delete')}</span>
            </div>
        `);

        this.contextMenu.innerHTML = menuHTML;
        
        // Attach click handlers to menu items
        this.contextMenu.querySelectorAll('.context-menu-item').forEach(menuItem => {
            menuItem.addEventListener('click', (e) => {
                e.stopPropagation();
                const action = e.currentTarget.dataset.action;
                console.log('Context menu action:', action);
                this.handleContextAction(action, item);
                this.hideContextMenu();
            });
        });

        this.contextMenu.style.display = 'block';
        this.contextMenu.style.left = `${e.pageX}px`;
        this.contextMenu.style.top = `${e.pageY}px`;

        // Adjust position if off screen
        const rect = this.contextMenu.getBoundingClientRect();
        if (rect.right > window.innerWidth) {
            this.contextMenu.style.left = `${e.pageX - rect.width}px`;
        }
        if (rect.bottom > window.innerHeight) {
            this.contextMenu.style.top = `${e.pageY - rect.height}px`;
        }
    }

    hideContextMenu() {
        if (this.contextMenu) {
            this.contextMenu.style.display = 'none';
        }
    }

    handleContextAction(action, item) {
        console.log('Handling context action:', action, 'for', item.name);
        
        switch (action) {
            case 'download':
                this.downloadFile(item);
                break;
            case 'download-folder':
                this.downloadFolder(item);
                break;
            case 'rename':
                this.renameItem(item);
                break;
            case 'delete':
            case 'delete-permanently':
                this.deleteItem(item);
                break;
            case 'restore':
                this.restoreItem(item);
                break;
        }
    }

    async navigateToFolder(folder) {
        console.log('Navigating to folder:', folder.name);
        
        if (this.currentSearchQuery) {
            this.currentSearchQuery = null;
            document.getElementById('searchBox').value = '';
            await this.buildSimpleBreadcrumb(folder);
        } else {
            this.breadcrumbPath.push({
                id: folder.id,
                name: folder.name
            });
        }
        
        this.loadFiles(folder.id, true);
    }

    async buildSimpleBreadcrumb(folder) {
        try {
            const folderPath = await this.api.getFolderPath(this.currentUserId, folder.id);
            const pathParts = folderPath.split(/[/\\]/).filter(part => part.trim() !== '');
            
            this.breadcrumbPath = pathParts.map((name, index) => ({
                id: index === pathParts.length - 1 ? folder.id : null,
                name: name
            }));
        } catch (error) {
            console.error('Error building breadcrumb:', error);
            this.breadcrumbPath = [{ id: folder.id, name: folder.name }];
        }
    }

    async downloadFile(file) {
        try {
            console.log('Downloading file:', file.name);
            this.notifications.info(this.i18n.t('downloading') + ' ' + file.name);
            const blob = await this.api.downloadFile(this.currentUserId, file.id);
            downloadBlob(blob, file.name);
            this.notifications.success(this.i18n.t('downloaded') + ': ' + file.name);
        } catch (error) {
            console.error('Download error:', error);
            this.notifications.error(this.i18n.t('failedDownload') + ' ' + file.name);
        }
    }

    async downloadFolder(folder) {
        try {
            console.log('Downloading folder:', folder.name);
            this.notifications.info(this.i18n.t('creatingArchive') + ' ' + folder.name);
            const blob = await this.api.downloadFolder(this.currentUserId, folder.id);
            downloadBlob(blob, folder.name + '.zip');
            this.notifications.success(this.i18n.t('downloaded') + ': ' + folder.name + '.zip');
        } catch (error) {
            console.error('Download folder error:', error);
            this.notifications.error(this.i18n.t('failedDownload') + ' ' + folder.name);
        }
    }

    async renameItem(item) {
        const newName = prompt(`Rename "${item.name}" to:`, item.name);
        if (!newName || newName.trim() === '' || newName === item.name) return;

        try {
            console.log('Renaming item:', item.name, 'to', newName);
            this.notifications.info(this.i18n.t('renaming') + ' ' + item.name);
            await this.api.renameItem(this.currentUserId, item.id, newName.trim());
            this.notifications.success(this.i18n.t('renamed') + ' "' + newName.trim() + '"');
            this.loadFiles(this.currentFolderId, true, this.isTrashView);
        } catch (error) {
            console.error('Rename error:', error);
            this.notifications.error(this.i18n.t('failedRename') + ' ' + item.name);
        }
    }

    async deleteItem(item) {
        const confirmMsg = this.isTrashView 
            ? `Permanently delete "${item.name}"?`
            : `Delete "${item.name}"?`;
            
        if (!confirm(confirmMsg)) return;

        try {
            console.log('Deleting item:', item.name);
            this.notifications.info(this.i18n.t('deleting') + ' ' + item.name);
            await this.api.deleteItem(this.currentUserId, item.id);
            this.notifications.success(item.name + ' ' + this.i18n.t('deleted'));
            
            // Remove from UI
            const row = this.fileListBody.querySelector(`[data-item-id="${item.id}"]`);
            if (row) row.remove();
            
            this.allLoadedItems = this.allLoadedItems.filter(i => i.id !== item.id);
            
            if (this.allLoadedItems.length === 0) {
                this.showEmptyState();
            }
        } catch (error) {
            console.error('Delete error:', error);
            this.notifications.error(this.i18n.t('failedDelete') + ' ' + item.name);
        }
    }

    async restoreItem(item) {
        try {
            console.log('Restoring item:', item.name);
            this.notifications.info(this.i18n.t('restoring') + ' ' + item.name);
            await this.api.restoreItem(this.currentUserId, item.id);
            this.notifications.success(item.name + ' ' + this.i18n.t('restored'));
            
            // Remove from UI
            const row = this.fileListBody.querySelector(`[data-item-id="${item.id}"]`);
            if (row) row.remove();
            
            this.allLoadedItems = this.allLoadedItems.filter(i => i.id !== item.id);
            
            if (this.allLoadedItems.length === 0) {
                this.showEmptyState();
            }
        } catch (error) {
            console.error('Restore error:', error);
            this.notifications.error('Error restoring: ' + item.name);
        }
    }

    handleSidebarClick(e) {
        console.log('Sidebar clicked');
        
        document.querySelectorAll('.sidebar-item').forEach(item => {
            item.classList.remove('active');
        });
        e.currentTarget.classList.add('active');

        const section = e.currentTarget.dataset.section;
        const searchContainer = document.querySelector('.search-container');
        
        if (section === 'trash') {
            searchContainer.style.display = 'none';
            this.breadcrumbPath = [];
            this.currentFolderId = null;
            this.loadFiles(null, true, true);
        } else if (section === 'mydrive') {
            searchContainer.style.display = '';
            this.navigateToRoot();
        } else {
            this.notifications.info('Feature not implemented: ' + section);
        }
    }

    async handleFileUpload(e) {
        const files = Array.from(e.target.files);
        console.log('handleFileUpload:', files.length, 'files');
        
        if (!files || files.length === 0) {
            console.log('No files selected');
            return;
        }

        let successCount = 0;
        let errorCount = 0;

        for (let i = 0; i < files.length; i++) {
            const file = files[i];
            try {
                console.log(`Uploading file ${i + 1}/${files.length}:`, file.name);
                this.notifications.info(`Uploading ${file.name} (${i + 1}/${files.length})`);
                
                await this.api.uploadFile(this.currentUserId, file, this.currentFolderId);
                successCount++;
                this.notifications.success(file.name + ' uploaded');
            } catch (error) {
                console.error('Upload error:', file.name, error);
                errorCount++;
                this.notifications.error('Failed to upload ' + file.name);
            }
        }

        console.log(`Upload complete: ${successCount} success, ${errorCount} errors`);
        await this.loadFiles(this.currentFolderId, true);
        e.target.value = '';
    }

    async handleFolderUpload(e) {
        const files = Array.from(e.target.files);
        console.log('handleFolderUpload:', files.length, 'files');
        
        if (!files || files.length === 0) return;

        const folderStructure = buildFolderStructure(files);
        this.notifications.info(`Uploading folder with ${files.length} files`);

        let successCount = 0;
        let errorCount = 0;

        for (const [folderPath, folderFiles] of folderStructure) {
            try {
                const folderId = await this.createFolderPath(folderPath);
                for (const file of folderFiles) {
                    try {
                        await this.api.uploadFile(this.currentUserId, file, folderId);
                        successCount++;
                    } catch (error) {
                        errorCount++;
                    }
                }
            } catch (error) {
                errorCount += folderFiles.length;
            }
        }

        if (errorCount === 0) {
            this.notifications.success(`Successfully uploaded ${successCount} files`);
        } else {
            this.notifications.warning(`Uploaded ${successCount}, failed ${errorCount} files`);
        }

        await this.loadFiles(this.currentFolderId, true);
        e.target.value = '';
    }

    async createFolderPath(folderPath) {
        if (!folderPath) return this.currentFolderId;

        const pathParts = folderPath.split('/');
        let currentParentId = this.currentFolderId;

        for (const folderName of pathParts) {
            if (folderName) {
                try {
                    const result = await this.api.createFolder(this.currentUserId, folderName, currentParentId);
                    currentParentId = result.folderId || result.id;
                } catch (error) {
                    // Folder might exist, try to find it
                    const existingId = await this.findExistingFolderId(folderName, currentParentId);
                    if (existingId) {
                        currentParentId = existingId;
                    } else {
                        throw error;
                    }
                }
            }
        }

        return currentParentId;
    }

    async findExistingFolderId(folderName, parentId) {
        try {
            const params = parentId ? { parentId: String(parentId) } : {};
            const result = await this.api.getFiles(this.currentUserId, params);
            const folder = result.data?.find(item => item.type === 'folder' && item.name === folderName);
            return folder ? folder.id : null;
        } catch (error) {
            return null;
        }
    }

    setupDragAndDrop() {
        console.log('Setting up drag and drop');
        const container = document.getElementById('fileContainer');
        
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            container.addEventListener(eventName, (e) => {
                e.preventDefault();
                e.stopPropagation();
            });
        });

        container.addEventListener('dragenter', () => {
            container.classList.add('dragover');
        });

        container.addEventListener('dragleave', (e) => {
            if (!container.contains(e.relatedTarget)) {
                container.classList.remove('dragover');
            }
        });

        container.addEventListener('drop', async (e) => {
            container.classList.remove('dragover');
            console.log('Files dropped');
            
            const files = Array.from(e.dataTransfer.files);
            if (files.length > 0) {
                // Trigger file upload
                const input = document.getElementById('fileInput');
                const dt = new DataTransfer();
                files.forEach(file => dt.items.add(file));
                input.files = dt.files;
                
                // Manually trigger the change event
                const event = new Event('change', { bubbles: true });
                input.dispatchEvent(event);
            }
        });
    }

    updateBreadcrumbs() {
        const breadcrumbs = document.getElementById('breadcrumbs');
        if (!breadcrumbs) return;
        
        breadcrumbs.innerHTML = '';

        const homeBreadcrumb = document.createElement('a');
        homeBreadcrumb.className = 'breadcrumb';
        homeBreadcrumb.href = '#';

        if (this.isTrashView) {
            homeBreadcrumb.textContent = this.i18n.t('trash');
            homeBreadcrumb.classList.add('current');
            breadcrumbs.appendChild(homeBreadcrumb);
        } else {
            homeBreadcrumb.textContent = this.i18n.t('myDrive');
            homeBreadcrumb.addEventListener('click', (e) => {
                e.preventDefault();
                this.navigateToRoot();
            });
            breadcrumbs.appendChild(homeBreadcrumb);

            this.breadcrumbPath.forEach((folder, index) => {
                const separator = document.createElement('span');
                separator.className = 'breadcrumb-separator';
                separator.textContent = ' > ';
                breadcrumbs.appendChild(separator);

                const breadcrumb = document.createElement('a');
                breadcrumb.className = index === this.breadcrumbPath.length - 1 
                    ? 'breadcrumb current' 
                    : 'breadcrumb';
                breadcrumb.href = '#';
                breadcrumb.textContent = folder.name;

                if (index < this.breadcrumbPath.length - 1) {
                    breadcrumb.addEventListener('click', (e) => {
                        e.preventDefault();
                        this.navigateToFolderInPath(index);
                    });
                }

                breadcrumbs.appendChild(breadcrumb);
            });
        }
    }

    navigateToRoot() {
        if (this.currentSearchQuery) {
            this.currentSearchQuery = null;
            document.getElementById('searchBox').value = '';
        }
        this.currentFolderId = null;
        this.breadcrumbPath = [];
        this.loadFiles(null, true);
    }

    navigateToFolderInPath(index) {
        const targetFolder = this.breadcrumbPath[index];
        this.currentFolderId = targetFolder.id;
        this.breadcrumbPath = this.breadcrumbPath.slice(0, index + 1);
        this.loadFiles(targetFolder.id, true);
    }

    performSearch(query) {
        this.currentSearchQuery = query.trim();
        if (!this.currentSearchQuery) {
            this.loadFiles(null, true);
            return;
        }
        this.currentFolderId = null;
        this.breadcrumbPath = [];
        this.loadFiles(null, true);
    }

    applySort(column) {
        if (this.sortBy === column) {
            this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
        } else {
            this.sortBy = column;
            this.sortDir = 'asc';
        }
        this.loadFiles(this.currentFolderId, true, this.isTrashView);
    }

    updateSortIndicators() {
        const headers = {
            name: document.querySelector('th[data-i18n="name"]'),
            created: document.querySelector('th[data-i18n="created"]'),
            modified: document.querySelector('th[data-i18n="modified"]'),
            size: document.querySelector('th[data-i18n="size"]')
        };

        Object.values(headers).forEach(h => {
            if (!h) return;
            if (!h.dataset.label) h.dataset.label = h.textContent.trim();
            h.textContent = h.dataset.label;
        });

        const active = headers[this.sortBy];
        if (active) {
            const arrow = this.sortDir === 'asc' ? ' 🔼' : ' 🔽';
            active.textContent = active.dataset.label + arrow;
        }
    }

    handleScroll(e) {
        const container = e.target;
        const distanceFromBottom = container.scrollHeight - (container.scrollTop + container.clientHeight);

        if (this.isLoadingMore || !this.hasNextPage) return;
        if (distanceFromBottom <= 200) {
            this.loadMoreFiles();
        }
    }

    async loadMoreFiles() {
        if (!this.hasNextPage || this.isLoadingMore) return;
        this.isLoadingMore = true;
        this.currentPage++;

        try {
            await this.loadFiles(this.currentFolderId, false, this.isTrashView);
        } catch (error) {
            this.currentPage--;
        } finally {
            this.isLoadingMore = false;
        }
    }

    showLoading() {
        document.getElementById('loadingState').style.display = 'flex';
        this.fileList.style.display = 'none';
        document.getElementById('emptyState').style.display = 'none';
    }

    hideLoading() {
        document.getElementById('loadingState').style.display = 'none';
        this.fileList.style.display = 'table';
    }

    showEmptyState() {
        document.getElementById('emptyState').style.display = 'flex';
        this.fileList.style.display = 'none';
    }

    hideEmptyState() {
        document.getElementById('emptyState').style.display = 'none';
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

    logout() {
        console.log('Signing out...');
        this.api.clearAuthToken();
        window.location.href = 'login.html';
    }
}

// Initialize app when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOM loaded, initializing CloudCoreDrive');
    new CloudCoreDrive();
});
