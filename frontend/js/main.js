import { I18n } from './translations.js';
import { ApiClient } from './api.js';
import { getNotificationManager } from './ui/notifications.js';
import { UploadProgressManager } from './utils/uploadProgress.js';
import {
    formatFileSize,
    formatDateTime,
    getFileIcon,
    downloadBlob,
    buildFolderStructure,
    isWebkitDirectorySupported
} from './utils/fileUtils.js';

class CloudCoreDrive {
    constructor() {
        this.i18n = new I18n();
        this.api = new ApiClient();
        this.notifications = getNotificationManager(this.i18n);
        this.uploadProgress = new UploadProgressManager(this.i18n);

        // Application state
        this.currentUserId = null;
        this.currentUser = null;
        this.currentFolderId = null;
        this.isTrashView = false;
        this.breadcrumbPath = [];
        this.selectedItems = new Set();

        // Pagination settings
        this.currentPage = 1;
        this.pageSize = 30;
        this.hasNextPage = false;
        this.isLoadingMore = false;
        this.allLoadedItems = [];

        // Sorting settings
        this.sortBy = 'name';
        this.sortDir = 'asc';
        this.currentSearchQuery = null;

        // DOM element references
        this.fileListBody = null;
        this.fileList = null;
        this.contextMenu = null;

        // Initialize application
        this.initializeTheme();
        this.initializeToolbar();
        this.initializeAuth();
        this.initializeDeselectOnClick();
        this.initializeKeyboardShortcuts();
    }

    // ═══════════════════════════════════════════════════════════════════
    // THEME MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════

    initializeTheme() {
        const savedTheme = localStorage.getItem('cloudcore-theme');
        const systemPrefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        const currentTheme = savedTheme || (systemPrefersDark ? 'dark' : 'light');

        this.setTheme(currentTheme);
    }

    setTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('cloudcore-theme', theme);
    }

    toggleTheme() {
        const currentTheme = document.documentElement.getAttribute('data-theme');
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        this.setTheme(newTheme);
    }

    // ═══════════════════════════════════════════════════════════════════
    // AUTHENTICATION
    // ═══════════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════════
    // EVENT LISTENERS SETUP
    // ═══════════════════════════════════════════════════════════════════

    initializeEventListeners() {
        console.log('Setting up event listeners...');

        // Theme toggle button
        document.getElementById('themeBtn').addEventListener('click', () => {
            console.log('Theme toggle clicked');
            this.toggleTheme();
        });

        // Logout button
        document.getElementById('logoutBtn').addEventListener('click', () => {
            console.log('Logout clicked');
            this.showLogoutModal();
        });

        // Language switcher
        document.getElementById('languageBtn').addEventListener('click', () => {
            console.log('Language switch clicked');
            this.i18n.switchLanguage();
            location.reload();
        });

        // New dropdown menu
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
                this.notifications.error(this.i18n.t('folderUploadNotSupported'));
                return;
            }
            document.getElementById('folderInput').click();
        });

        // File input change handlers
        document.getElementById('fileInput').addEventListener('change', (e) => {
            console.log('File input changed:', e.target.files.length);
            this.handleFileUpload(e);
        });

        document.getElementById('folderInput').addEventListener('change', (e) => {
            console.log('Folder input changed:', e.target.files.length);
            this.handleFolderUpload(e);
        });

        // Search functionality
        document.getElementById('searchBox').addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                this.performSearch(e.target.value);
            }
        });

        // Sidebar navigation
        document.querySelectorAll('.sidebar-item').forEach((item) => {
            item.addEventListener('click', (e) => this.handleSidebarClick(e));
        });

        // Sorting headers
        ['name', 'created', 'modified', 'size'].forEach((header) => {
            const th = document.querySelector(`th[data-i18n="${header}"]`);
            if (th) {
                th.addEventListener('click', () => this.applySort(header));
            }
        });

        // Infinite scroll
        const container = document.getElementById('fileContainer');
        container.addEventListener(
            'scroll',
            this.debounce((e) => this.handleScroll(e), 120)
        );

        // Close dropdowns and context menu on outside click
        document.addEventListener('click', () => {
            this.hideNewDropdown();
            this.hideContextMenu();
        });

        // Setup drag and drop functionality
        this.setupDragAndDrop();

        console.log('Event listeners setup complete');
    }

    initializeKeyboardShortcuts() {
        // Use capture phase to catch event before browser default actions
        document.addEventListener(
            'keydown',
            async (e) => {
                // Ignore shortcuts if user is typing in an input field
                if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.isContentEditable) {
                    return;
                }

                // Ignore shortcuts if a modal is open
                const isModalOpen = document.querySelector('.modal.show');
                if (isModalOpen) {
                    return;
                }

                // Handle Ctrl+A / Cmd+A (both lowercase and uppercase 'a')
                if ((e.ctrlKey || e.metaKey) && (e.key === 'a' || e.key === 'A' || e.code === 'KeyA')) {
                    e.preventDefault();
                    e.stopPropagation();
                    console.log('Ctrl+A pressed - selecting all items');
                    await this.selectAll(); // ← Make it async
                    return;
                }

                switch (e.key) {
                    case 'Delete':
                        // Delete selected items
                        if (this.selectedItems.size > 0) {
                            e.preventDefault();
                            console.log('Delete key pressed - deleting selected items');
                            this.deleteSelectedItems();
                        }
                        break;

                    case 'F2':
                        // Rename selected item (only if single item selected)
                        if (this.selectedItems.size === 1) {
                            e.preventDefault();
                            const item = Array.from(this.selectedItems)[0];
                            console.log('F2 key pressed - renaming item');
                            this.renameItem(item);
                        }
                        break;

                    case 'Escape':
                        // Clear selection
                        if (this.selectedItems.size > 0) {
                            e.preventDefault();
                            console.log('Escape pressed - clearing selection');
                            this.clearSelection();
                        }
                        break;
                }
            },
            true
        ); // ← Keep 'true' for capture phase

        console.log('Keyboard shortcuts initialized');
    }

    // Helper method to select all items
    async selectAll() {
        console.log('Select All - Loading all items first...');

        // Load all items if there are more pages
        while (this.hasNextPage) {
            await this.loadMoreFiles();
        }

        // Check if there are items to select
        if (this.allLoadedItems.length === 0) {
            console.log('No items to select');
            return;
        }

        // Clear current selection
        document.querySelectorAll('.file-list-row.selected').forEach((el) => {
            el.classList.remove('selected');
        });
        this.selectedItems.clear();

        // Select all loaded items
        this.allLoadedItems.forEach((item) => {
            this.selectedItems.add(item);
            const row = this.fileListBody.querySelector(`[data-item-id="${item.id}"]`);
            if (row) {
                row.classList.add('selected');
            }
        });

        this.updateToolbar();
        console.log('Selected all items:', this.selectedItems.size, 'out of', this.allLoadedItems.length);

        // Show notification
        this.notifications.show(
            this.i18n.t('selectedAllItems', { count: this.selectedItems.size }) ||
                `Selected ${this.selectedItems.size} items`,
            'info'
        );
    }
    // ═══════════════════════════════════════════════════════════════════
    // FILE LOADING AND RENDERING
    // ═══════════════════════════════════════════════════════════════════

    async loadFiles(folderId = null, resetPagination = true, isTrashView = false) {
        console.log('loadFiles called:', { folderId, resetPagination, isTrashView });

        this.currentFolderId = folderId;
        if (resetPagination) {
            this.currentPage = 1;
            this.allLoadedItems = [];
            this.hasNextPage = false;
            this.clearSelection();
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
            this.updateBreadcrumbs();
        } catch (error) {
            console.error('loadFiles error:', error);
            this.notifications.error(this.i18n.t('failedRename'));
        } finally {
            if (resetPagination) this.hideLoading();
        }
    }

    renderFiles() {
        console.log('Rendering files:', this.allLoadedItems.length);

        // Update selected items references
        const newSelectedItems = new Set();
        for (const selectedItem of this.selectedItems) {
            const updatedItem = this.allLoadedItems.find((i) => i.id === selectedItem.id);
            if (updatedItem) {
                newSelectedItems.add(updatedItem);
            }
        }
        this.selectedItems = newSelectedItems;

        this.fileList.style.display = 'table';
        this.fileListBody.innerHTML = '';

        if (this.allLoadedItems.length === 0) {
            const emptyRow = document.createElement('tr');
            if (this.currentSearchQuery) {
                emptyRow.innerHTML = `
                    <td colspan="5" style="padding: 0; border: none;">
                        <div class="empty-state">
                            <span class="material-symbols-outlined empty-icon">search_off</span>
                            <h3 data-i18n="noSearchResults">No results found</h3>
                            <p data-i18n="noSearchResultsMessage">Try a different search query</p>
                        </div>
                    </td>
                `;
            } else if (this.isTrashView) {
                emptyRow.innerHTML = `
                    <td colspan="5" style="padding: 0; border: none;">
                        <div class="empty-state">
                            <span class="material-symbols-outlined empty-icon">delete_outline</span>
                            <h3 data-i18n="emptyTrash">Trash is empty</h3>
                            <p data-i18n="emptyTrashMessage">Deleted files will be stored here for 30 days</p>
                        </div>
                    </td>
                `;
            } else {
                emptyRow.innerHTML = `
                    <td colspan="5" style="padding: 0; border: none;">
                        <div class="empty-state">
                            <span class="material-symbols-outlined empty-icon">folder_open</span>
                            <h3 data-i18n="emptyFolder">This folder is empty</h3>
                            <p data-i18n="uploadGetStarted">Upload files or folders to get started</p>
                        </div>
                    </td>
                `;
            }
            this.fileListBody.appendChild(emptyRow);
            this.i18n.updateUI();
        } else {
            this.allLoadedItems.forEach((item) => {
                const row = this.createFileRow(item);
                this.fileListBody.appendChild(row);
            });
        }

        this.updateSortIndicators();
        console.log('Files rendered');
    }

    // ═══════════════════════════════════════════════════════════════════
    // FILE ROW CREATION
    // ═══════════════════════════════════════════════════════════════════

    createFileRow(item) {
        const row = document.createElement('tr');
        row.className = this.isTrashView ? 'file-list-row trash-mode' : 'file-list-row';
        row.dataset.itemId = item.id;
        row.dataset.itemType = item.type;
        row.draggable = !this.isTrashView;

        const iconInfo = getFileIcon(item);
        const sizeDisplay = item.type === 'file' ? (item.fileSize ? formatFileSize(item.fileSize) : '-') : '-';

        // Create table cells
        const indicatorCell = document.createElement('td');
        indicatorCell.className = 'col-indicator';
        row.appendChild(indicatorCell);

        const nameCell = document.createElement('td');
        nameCell.innerHTML = `<span class="material-symbols-outlined file-list-icon ${iconInfo.class}">${iconInfo.icon}</span>${item.name}`;
        row.appendChild(nameCell);

        const createdCell = document.createElement('td');
        createdCell.textContent = formatDateTime(item.createdAt);
        row.appendChild(createdCell);

        const modifiedCell = document.createElement('td');
        modifiedCell.textContent = formatDateTime(item.updatedAt);
        row.appendChild(modifiedCell);

        const sizeCell = document.createElement('td');
        sizeCell.textContent = sizeDisplay;
        row.appendChild(sizeCell);

        // Apply selection state
        if (this.selectedItems.has(item)) {
            row.classList.add('selected');
        }

        // Attach event handlers
        row.addEventListener('click', (e) => this.handleFileClick(e, item, row));
        row.addEventListener('dblclick', (e) => this.handleFileDoubleClick(e, item));
        row.addEventListener('contextmenu', (e) => this.showContextMenu(e, item));

        if (!this.isTrashView) {
            row.addEventListener('dragstart', (e) => {
                // Prevent drag if Shift key is pressed (for range selection)
                if (e.shiftKey) {
                    e.preventDefault();
                    console.log('Drag prevented - Shift key is pressed');
                    return;
                }
                this.handleRowDragStart(e, item, row);
            });

            row.addEventListener('dragend', (e) => this.handleRowDragEnd(e, row));

            // Drop handling only for folders
            if (item.type === 'folder') {
                row.addEventListener('dragover', (e) => {
                    if (this.isDraggingInternal && e.dataTransfer.types.includes('text/plain')) {
                        e.preventDefault();
                        // Allow event to bubble to document for ghost tracking
                        e.dataTransfer.dropEffect = 'move';

                        // Visual feedback
                        if (!this.selectedItems.has(item)) {
                            row.classList.add('drag-over');
                        }
                    }
                });

                row.addEventListener('dragleave', (e) => {
                    if (!row.contains(e.relatedTarget)) {
                        row.classList.remove('drag-over');
                    }
                });

                row.addEventListener('drop', (e) => {
                    if (e.dataTransfer.types.includes('text/plain')) {
                        e.preventDefault();
                        e.stopPropagation();
                        row.classList.remove('drag-over');
                        this.handleRowDrop(e, item);
                    }
                });
            }
        }

        return row;
    }

    // ═══════════════════════════════════════════════════════════════════
    // DRAG AND DROP - ROW HANDLERS
    // ═══════════════════════════════════════════════════════════════════

    handleRowDragStart(e, item, row) {
        console.log('=== DRAG START ===');

        // Auto-select the item if not already selected
        if (!this.selectedItems.has(item)) {
            document.querySelectorAll('.file-list-row.selected').forEach((el) => el.classList.remove('selected'));
            this.selectedItems.clear();
            this.selectedItems.add(item);
            row.classList.add('selected');
        }

        this.draggedItems = Array.from(this.selectedItems).map((i) => i.id);
        this.dragSourceType = 'internal';
        this.isDraggingInternal = true;

        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', JSON.stringify(this.draggedItems));

        // Hide default drag image
        const emptyImage = new Image();
        emptyImage.src = 'data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7';
        e.dataTransfer.setDragImage(emptyImage, 0, 0);

        // Dim all selected rows
        document.querySelectorAll('.file-list-row.selected').forEach((selectedRow) => {
            selectedRow.classList.add('dragging-selected');
        });

        // Create custom drag ghost
        this.customDragGhost = this.createCustomDragGhost(item);
        document.body.appendChild(this.customDragGhost);

        // Set cursor to top-left corner of ghost
        this.dragOffsetX = 0;
        this.dragOffsetY = 0;

        console.log(`Initial mouse: X=${e.clientX}, Y=${e.clientY}`);

        // Set initial ghost position
        this.updateDragGhostPosition(e.clientX, e.clientY);

        // Fade in ghost element
        requestAnimationFrame(() => {
            if (this.customDragGhost) {
                this.customDragGhost.style.opacity = '1';
                console.log('Ghost element visible');
            }
        });

        row.classList.add('dragging');
        console.log('Dragging items:', this.draggedItems);
    }

    createCustomDragGhost(item) {
        const ghost = document.createElement('div');
        ghost.className = 'custom-drag-ghost';

        const iconInfo = getFileIcon(item);
        const count = this.selectedItems.size;
        const displayName = item.name.length > 30 ? item.name.substring(0, 30) + '...' : item.name;

        ghost.innerHTML = `
            <span class="material-symbols-outlined ${iconInfo.class}">${iconInfo.icon}</span>
            <span class="drag-ghost-text">${displayName}</span>
            ${count > 1 ? `<span class="drag-ghost-count">${count}</span>` : ''}
        `;

        ghost.style.cssText = `
            position: fixed;
            left: 0;
            top: 0;
            transform: translate(-9999px, -9999px);
            background: var(--bg-primary);
            border: 2px solid var(--color-blue);
            border-radius: 8px;
            padding: 12px 20px;
            box-shadow: 0 10px 30px rgba(0, 0, 0, 0.4);
            display: flex;
            align-items: center;
            gap: 12px;
            z-index: 99999999;
            pointer-events: none;
            opacity: 0;
            transition: opacity 0.15s ease;
            will-change: transform;
            backdrop-filter: blur(10px);
            font-weight: 500;
        `;

        console.log('Custom drag ghost created');
        return ghost;
    }

    updateDragGhostPosition(clientX, clientY) {
        if (!this.customDragGhost) {
            console.log('Ghost element not found!');
            return;
        }

        const x = clientX - this.dragOffsetX;
        const y = clientY - this.dragOffsetY;

        console.log(`Mouse: X=${clientX}, Y=${clientY} | Ghost: X=${x}, Y=${y}`);

        this.customDragGhost.style.transform = `translate(${x}px, ${y}px)`;
    }

    handleRowDragEnd(e, row) {
        row.classList.remove('dragging');

        // Remove custom drag ghost
        if (this.customDragGhost) {
            this.customDragGhost.style.opacity = '0';
            setTimeout(() => {
                if (this.customDragGhost && this.customDragGhost.parentNode) {
                    document.body.removeChild(this.customDragGhost);
                }
                this.customDragGhost = null;
            }, 150);
        }

        // Remove dimming from all selected rows
        document.querySelectorAll('.file-list-row.dragging-selected').forEach((selectedRow) => {
            selectedRow.classList.remove('dragging-selected');
        });

        // Remove drag-over class from all rows
        document.querySelectorAll('.file-list-row').forEach((r) => {
            r.classList.remove('drag-over');
        });

        this.isDraggingInternal = false;
        console.log('Drag ended');
    }

    async handleRowDrop(e, targetItem) {
        e.preventDefault();
        e.stopPropagation();

        // Remove drag-over styling
        document.querySelectorAll('.file-list-row').forEach((r) => {
            r.classList.remove('drag-over');
        });

        // Validate drop target
        if (targetItem.type !== 'folder') {
            console.log('Target is not a folder');
            return;
        }

        if (!this.draggedItems || this.draggedItems.length === 0) {
            console.log('No items to move');
            return;
        }

        // Prevent moving folder into itself
        if (this.draggedItems.includes(targetItem.id)) {
            console.log('Cannot move folder into itself');
            this.draggedItems = null;
            this.dragSourceType = null;
            this.isDraggingInternal = false;
            return;
        }

        try {
            console.log(`Moving ${this.draggedItems.length} item(s) to folder:`, targetItem.name);

            const result = await this.api.bulkMoveItems(this.currentUserId, this.draggedItems, targetItem.id, {
                concurrency: 5,
                onProgress: (completed, total) => {
                    console.log(`Move progress: ${completed}/${total}`);
                }
            });

            if (result.failed.length === 0) {
                const itemsText = result.succeeded.length === 1 ? '1 item' : `${result.succeeded.length} items`;
                this.notifications.success(`Moved ${itemsText} to ${targetItem.name}`);
            } else {
                this.notifications.warning(`Moved ${result.succeeded.length} items. Failed: ${result.failed.length}`);
            }

            this.selectedItems.clear();
            await this.loadFiles(this.currentFolderId, true);
        } catch (error) {
            console.error('Move error:', error);
            this.notifications.error(this.i18n.t('failedToMove'));
        }

        this.draggedItems = null;
        this.dragSourceType = null;
    }

    // ═══════════════════════════════════════════════════════════════════
    // SELECTION MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════

    initializeDeselectOnClick() {
        document.addEventListener('click', (e) => {
            // Check if click is on elements that should NOT clear selection
            const clickedOnRow = e.target.closest('.file-list-row');
            const clickedOnContextMenu = e.target.closest('.context-menu');
            const clickedOnModal = e.target.closest('.modal');
            const clickedOnToolbarActions = e.target.closest('.toolbar-actions, .toolbar-actions-trash');

            // Elements that SHOULD clear selection when clicked
            const clickedOnSearch = e.target.closest('.search-container');
            const clickedOnNewButton = e.target.closest('.new-button, .new-dropdown');
            const clickedOnViewButtons = e.target.closest('#viewGridBtn, #viewListBtn, #sortBtn');
            const clickedOnNewFolderBtn = e.target.closest('#newFolderBtn');
            const clickedOnEmptyTrashBtn = e.target.closest('#emptyTrashBtn');

            // If clicked on search, new button, or view buttons - clear selection
            if (
                clickedOnSearch ||
                clickedOnNewButton ||
                clickedOnViewButtons ||
                clickedOnNewFolderBtn ||
                clickedOnEmptyTrashBtn
            ) {
                this.clearSelection();
                return;
            }

            // If clicked outside interactive elements - clear selection
            if (!clickedOnRow && !clickedOnContextMenu && !clickedOnModal && !clickedOnToolbarActions) {
                this.clearSelection();
            }
        });
    }

    clearSelection() {
        // Remove visual selection
        document.querySelectorAll('.file-list-row.selected').forEach((row) => {
            row.classList.remove('selected');
        });

        // Clear selection set
        this.selectedItems.clear();

        // Update toolbar
        this.updateToolbar();

        console.log('Selection cleared');
    }

    handleFileClick(e, item, row) {
        e.stopPropagation();
        console.log('File clicked:', item.name);

        const isAlreadySelected = this.selectedItems.has(item);

        if (e.ctrlKey || e.metaKey) {
            // Ctrl/Cmd: Toggle selection
            if (isAlreadySelected) {
                this.selectedItems.delete(item);
                row.classList.remove('selected');
            } else {
                this.selectedItems.add(item);
                row.classList.add('selected');
            }
            this.lastSelectedItem = item;
        } else if (e.shiftKey && this.lastSelectedItem) {
            // Shift: Range selection
            e.preventDefault();
            this.selectRange(this.lastSelectedItem, item);
        } else {
            // Regular click: Clear and select only this item
            document.querySelectorAll('.file-list-row.selected').forEach((el) => el.classList.remove('selected'));
            this.selectedItems.clear();
            this.selectedItems.add(item);
            row.classList.add('selected');
            this.lastSelectedItem = item;
        }

        this.updateToolbar();
    }

    selectRange(startItem, endItem) {
        const rows = Array.from(this.fileListBody.querySelectorAll('.file-list-row'));
        const startIndex = this.allLoadedItems.findIndex((i) => i.id === startItem.id);
        const endIndex = this.allLoadedItems.findIndex((i) => i.id === endItem.id);

        if (startIndex === -1 || endIndex === -1) return;

        const [minIndex, maxIndex] = [Math.min(startIndex, endIndex), Math.max(startIndex, endIndex)];

        // Clear current selection
        document.querySelectorAll('.file-list-row.selected').forEach((el) => el.classList.remove('selected'));
        this.selectedItems.clear();

        // Select range
        for (let i = minIndex; i <= maxIndex; i++) {
            const item = this.allLoadedItems[i];
            this.selectedItems.add(item);
            rows[i]?.classList.add('selected');
        }
    }

    handleFileDoubleClick(e, item) {
        console.log('File double-clicked:', item.name);

        // Ignore double-click in trash
        if (this.isTrashView) return;

        if (item.type === 'folder') {
            this.navigateToFolder(item);
        } else {
            this.downloadFile(item);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // CONTEXT MENU
    // ═══════════════════════════════════════════════════════════════════

    showContextMenu(e, item) {
        e.preventDefault();
        e.stopPropagation();
        console.log('Context menu for:', item.name);

        if (!this.contextMenu) return;

        // Auto-select item if not already selected
        if (!this.selectedItems.has(item)) {
            document.querySelectorAll('.file-list-row.selected').forEach((el) => el.classList.remove('selected'));
            this.selectedItems.clear();
            this.selectedItems.add(item);
            const row = this.fileListBody.querySelector(`[data-item-id="${item.id}"]`);
            if (row) row.classList.add('selected');
            this.updateToolbar();
        }

        const count = this.selectedItems.size;
        const hasMultiple = count > 1;

        let menuHTML;

        if (this.isTrashView) {
            // ═══════════════════════════════════════════════════════════════
            // TRASH VIEW CONTEXT MENU
            // ═══════════════════════════════════════════════════════════════
            menuHTML = `
                <div class="context-menu-item" data-action="restore">
                    <span class="material-symbols-outlined">restore_from_trash</span>
                    <span>${this.i18n.t('restore')} ${hasMultiple ? `(${count})` : ''}</span>
                </div>
                <div class="context-menu-separator"></div>
                <div class="context-menu-item danger" data-action="delete-permanently">
                    <span class="material-symbols-outlined">delete_forever</span>
                    <span>${this.i18n.t('deletePermanently')} ${hasMultiple ? `(${count})` : ''}</span>
                </div>
            `;
        } else {
            // ═══════════════════════════════════════════════════════════════
            // NORMAL VIEW CONTEXT MENU
            // ═══════════════════════════════════════════════════════════════

            if (hasMultiple) {
                // Multiple items selected - simplified menu
                menuHTML = `
                    <div class="context-menu-item" data-action="download-multiple">
                        <span class="material-symbols-outlined">download</span>
                        <span>${this.i18n.t('download')} (${count})</span>
                    </div>
                    <div class="context-menu-item" data-action="move-multiple">
                        <span class="material-symbols-outlined">drive_file_move</span>
                        <span>${this.i18n.t('move')} (${count})</span>
                    </div>
                    <div class="context-menu-separator"></div>
                    <div class="context-menu-item danger" data-action="delete-multiple">
                        <span class="material-symbols-outlined">delete</span>
                        <span>${this.i18n.t('delete')} (${count})</span>
                    </div>
                `;
            } else {
                // Single item - full menu
                if (item.type === 'folder') {
                    menuHTML = `
                        <div class="context-menu-item" data-action="open">
                            <span class="material-symbols-outlined">folder_open</span>
                            <span>${this.i18n.t('open')}</span>
                        </div>
                        <div class="context-menu-separator"></div>
                        <div class="context-menu-item" data-action="download-folder">
                            <span class="material-symbols-outlined">download</span>
                            <span>${this.i18n.t('downloadFolder')}</span>
                        </div>
                        <div class="context-menu-item" data-action="move-multiple">
                            <span class="material-symbols-outlined">drive_file_move</span>
                            <span>${this.i18n.t('move')}</span>
                        </div>
                        <div class="context-menu-separator"></div>
                        <div class="context-menu-item" data-action="rename">
                            <span class="material-symbols-outlined">edit</span>
                            <span>${this.i18n.t('rename')}</span>
                        </div>
                        <div class="context-menu-separator"></div>
                        <div class="context-menu-item danger" data-action="delete">
                            <span class="material-symbols-outlined">delete</span>
                            <span>${this.i18n.t('deleteFolder')}</span>
                        </div>
                    `;
                } else {
                    menuHTML = `
                        <div class="context-menu-item" data-action="download">
                            <span class="material-symbols-outlined">download</span>
                            <span>${this.i18n.t('downloadFile')}</span>
                        </div>
                        <div class="context-menu-separator"></div>
                        <div class="context-menu-item" data-action="rename">
                            <span class="material-symbols-outlined">edit</span>
                            <span>${this.i18n.t('rename')}</span>
                        </div>
                        <div class="context-menu-separator"></div>
                        <div class="context-menu-item danger" data-action="delete">
                            <span class="material-symbols-outlined">delete</span>
                            <span>${this.i18n.t('delete')}</span>
                        </div>
                    `;
                }
            }
        }

        this.contextMenu.innerHTML = menuHTML;

        // Attach click handlers to menu items
        this.contextMenu.querySelectorAll('.context-menu-item').forEach((menuItem) => {
            menuItem.addEventListener('click', (e) => {
                e.stopPropagation();
                const action = e.currentTarget.dataset.action;
                console.log('Context menu action:', action);
                this.handleContextAction(action, item);
                this.hideContextMenu();
            });
        });

        // Position context menu
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
            case 'open':
                this.navigateToFolder(item);
                break;
            case 'download':
                this.downloadFile(item);
                break;
            case 'download-folder':
                this.downloadFolder(item);
                break;
            case 'download-multiple':
                this.downloadSelectedItems();
                break;
            case 'move':
                this.notifications.info('Not implemented yet');
            case 'move-multiple':
                this.notifications.info('Not implemented yet');
                break;
            case 'rename':
                this.renameItem(item);
                break;
            case 'delete':
                this.deleteItem(item);
            case 'delete-multiple':
                this.deleteSelectedItems();
                break;
            case 'delete-permanently':
                this.notifications.info('Not implemented yet');
                break;
            case 'restore':
                this.restoreSelectedItems();
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // NAVIGATION
    // ═══════════════════════════════════════════════════════════════════

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
            const pathParts = folderPath.split(/[/\\]/).filter((part) => part.trim() !== '');

            this.breadcrumbPath = pathParts.map((name, index) => ({
                id: index === pathParts.length - 1 ? folder.id : null,
                name: name
            }));
        } catch (error) {
            console.error('Error building breadcrumb:', error);
            this.breadcrumbPath = [{ id: folder.id, name: folder.name }];
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

    // ═══════════════════════════════════════════════════════════════════
    // FILE OPERATIONS
    // ═══════════════════════════════════════════════════════════════════

    async downloadFile(file) {
        try {
            console.log('Downloading file:', file.name);
            this.notifications.info(this.i18n.t('downloading', { filename: file.name }));
            const blob = await this.api.downloadFile(this.currentUserId, file.id);
            downloadBlob(blob, file.name);
            this.notifications.success(this.i18n.t('downloaded', { filename: file.name }));
        } catch (error) {
            console.error('Download error:', error);
            this.notifications.error(this.i18n.t('failedDownload', { filename: file.name }));
        }
    }

    async downloadFolder(folder) {
        try {
            console.log('Downloading folder:', folder.name);
            this.notifications.info(this.i18n.t('creatingArchive', { foldername: folder.name }));
            const blob = await this.api.downloadFolder(this.currentUserId, folder.id);
            downloadBlob(blob, folder.name + '.zip');
            this.notifications.success(this.i18n.t('downloaded', { filename: folder.name + '.zip' }));
        } catch (error) {
            console.error('Download folder error:', error);
            this.notifications.error(this.i18n.t('failedDownload', { filename: folder.name }));
        }
    }

    async renameItem(item) {
        this.showRenameModal(item);
    }

    async deleteItem(item) {
        this.showDeleteModal(item);
    }

    async restoreItem(item) {
        try {
            console.log('Restoring item:', item.name);
            this.notifications.info(this.i18n.t('restoring', { filename: item.name }));
            await this.api.restoreItem(this.currentUserId, item.id);
            this.notifications.success(this.i18n.t('restored', { filename: item.name }));

            const row = this.fileListBody.querySelector(`[data-item-id="${item.id}"]`);
            if (row) row.remove();

            this.allLoadedItems = this.allLoadedItems.filter((i) => i.id !== item.id);

            if (this.allLoadedItems.length === 0) {
                this.showEmptyState();
            }
        } catch (error) {
            console.error('Restore error:', error);
            this.notifications.error(this.i18n.t('failedRestore', { filename: item.name }));
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // MODALS
    // ═══════════════════════════════════════════════════════════════════
    showCreateFolderModal() {
        const modal = document.getElementById('createFolderModal');
        const overlay = document.getElementById('deleteModalOverlay');
        const input = document.getElementById('folderNameInput');
        const hint = document.getElementById('folderNameHint');

        if (!modal || !overlay || !input) {
            console.error('Create folder modal elements not found');
            return;
        }

        // Set default folder name
        input.value = this.i18n.t('untitledFolder') || 'Untitled folder';
        hint.textContent = '';

        const confirmBtn = document.getElementById('createFolderConfirmBtn');
        const cancelBtn = document.getElementById('createFolderCancelBtn');
        const closeBtn = document.getElementById('createFolderModalClose');

        if (!confirmBtn || !cancelBtn || !closeBtn) {
            console.error('Create folder modal buttons not found');
            return;
        }

        // Validate folder name
        const validateFolderName = () => {
            const name = input.value.trim();

            // Check for empty name
            if (name.length === 0) {
                hint.textContent = this.i18n.t('folderNameRequired') || 'Folder name is required';
                hint.style.color = 'var(--color-error)';
                confirmBtn.disabled = true;
                return false;
            }

            // Check for invalid characters
            const invalidChars = /[<>:"/\\|?*]/g;
            if (invalidChars.test(name)) {
                hint.textContent = this.i18n.t('invalidCharacters') || 'Invalid characters: < > : " / \\ | ? *';
                hint.style.color = 'var(--color-error)';
                confirmBtn.disabled = true;
                return false;
            }

            // Check length
            if (name.length > 250) {
                hint.textContent = this.i18n.t('nameTooLong') || 'Name is too long (max 250 characters)';
                hint.style.color = 'var(--color-error)';
                confirmBtn.disabled = true;
                return false;
            }

            // Valid
            hint.textContent = '';
            confirmBtn.disabled = false;
            return true;
        };

        // Input validation on change
        input.addEventListener('input', validateFolderName);

        const handleConfirm = async () => {
            const folderName = input.value.trim();
            if (!validateFolderName()) return;

            this.hideCreateFolderModal();
            await this.performCreateFolder(folderName);

            // Cleanup
            input.removeEventListener('input', validateFolderName);
            confirmBtn.removeEventListener('click', handleConfirm);
            cancelBtn.removeEventListener('click', handleCancel);
            closeBtn.removeEventListener('click', handleCancel);
            overlay.removeEventListener('click', handleCancel);
            input.removeEventListener('keydown', handleKeyDown);
        };

        const handleCancel = () => {
            this.hideCreateFolderModal();

            // Cleanup
            input.removeEventListener('input', validateFolderName);
            confirmBtn.removeEventListener('click', handleConfirm);
            cancelBtn.removeEventListener('click', handleCancel);
            closeBtn.removeEventListener('click', handleCancel);
            overlay.removeEventListener('click', handleCancel);
            input.removeEventListener('keydown', handleKeyDown);
        };

        const handleKeyDown = (e) => {
            if (e.key === 'Enter' && !confirmBtn.disabled) {
                e.preventDefault();
                handleConfirm();
            } else if (e.key === 'Escape') {
                e.preventDefault();
                handleCancel();
            }
        };

        // Attach event listeners
        confirmBtn.addEventListener('click', handleConfirm);
        cancelBtn.addEventListener('click', handleCancel);
        closeBtn.addEventListener('click', handleCancel);
        overlay.addEventListener('click', handleCancel);
        input.addEventListener('keydown', handleKeyDown);

        // Show modal
        this.showModal(modal, overlay);

        // Focus and select input text
        setTimeout(() => {
            input.focus();
            input.select();
        }, 100);

        // Initial validation
        validateFolderName();

        console.log('Create folder modal shown');
    }

    hideCreateFolderModal() {
        const modal = document.getElementById('createFolderModal');
        const overlay = document.getElementById('deleteModalOverlay');

        this.hideModal(modal, overlay);

        console.log('Create folder modal hidden');
    }

    async performCreateFolder(folderName) {
        try {
            console.log('Creating folder:', folderName, 'in parent:', this.currentFolderId);
            this.notifications.info(this.i18n.t('creatingFolder', { foldername: folderName }));

            const result = await this.api.createFolder(this.currentUserId, folderName, this.currentFolderId);

            this.notifications.success(
                this.i18n.t('folderCreated', {
                    foldername: result.folderName || folderName
                })
            );

            // Reload current folder
            await this.loadFiles(this.currentFolderId, true, this.isTrashView);
        } catch (error) {
            console.error('Create folder error:', error);

            const errorData = error.response?.data;

            if (errorData?.code === 'NAME_CONFLICT') {
                this.notifications.error(this.i18n.t('folderNameConflict') || 'A folder with this name already exists');
            } else if (errorData?.code === 'PARENT_NOT_FOUND') {
                this.notifications.error(this.i18n.t('parentFolderNotFound') || 'Parent folder not found');
            } else {
                this.notifications.error(
                    this.i18n.t('failedCreateFolder', {
                        foldername: folderName
                    })
                );
            }
        }
    }
    showRenameModal(item) {
        const modal = document.getElementById('renameModal');
        const overlay = document.getElementById('deleteModalOverlay');
        const input = document.getElementById('renameInput');
        const hint = document.getElementById('renameHint');

        if (!modal || !overlay || !input) {
            console.error('Rename modal elements not found');
            return;
        }

        input.value = item.name;
        hint.textContent = this.i18n.t('renameHint') || 'Enter a new name for this item';

        const confirmBtn = document.getElementById('renameConfirmBtn');
        const cancelBtn = document.getElementById('renameCancelBtn');
        const closeBtn = document.getElementById('renameModalClose');

        if (!confirmBtn || !cancelBtn || !closeBtn) {
            console.error('Rename modal buttons not found');
            return;
        }

        const validateName = () => {
            const newName = input.value.trim();
            const isValid = newName.length > 0 && newName !== item.name;
            confirmBtn.disabled = !isValid;
            return isValid;
        };

        input.addEventListener('input', validateName);

        const handleConfirm = async () => {
            const newName = input.value.trim();
            if (!validateName()) return;

            this.hideRenameModal();
            await this.performRename(item, newName);

            // Cleanup event listeners
            input.removeEventListener('input', validateName);
            confirmBtn.removeEventListener('click', handleConfirm);
            cancelBtn.removeEventListener('click', handleCancel);
            closeBtn.removeEventListener('click', handleCancel);
            overlay.removeEventListener('click', handleCancel);
            input.removeEventListener('keydown', handleKeyDown);
        };

        const handleCancel = () => {
            this.hideRenameModal();

            // Cleanup event listeners
            input.removeEventListener('input', validateName);
            confirmBtn.removeEventListener('click', handleConfirm);
            cancelBtn.removeEventListener('click', handleCancel);
            closeBtn.removeEventListener('click', handleCancel);
            overlay.removeEventListener('click', handleCancel);
            input.removeEventListener('keydown', handleKeyDown);
        };

        const handleKeyDown = (e) => {
            if (e.key === 'Enter' && validateName()) {
                e.preventDefault();
                handleConfirm();
            } else if (e.key === 'Escape') {
                e.preventDefault();
                handleCancel();
            }
        };

        // Attach event listeners
        confirmBtn.addEventListener('click', handleConfirm);
        cancelBtn.addEventListener('click', handleCancel);
        closeBtn.addEventListener('click', handleCancel);
        overlay.addEventListener('click', handleCancel);
        input.addEventListener('keydown', handleKeyDown);

        // Show modal
        this.showModal(modal, overlay);

        // Focus and select input text
        setTimeout(() => {
            input.focus();
            input.select();
        }, 100);

        // Initial validation
        validateName();

        console.log('Rename modal shown for:', item.name);
    }

    hideRenameModal() {
        const modal = document.getElementById('renameModal');
        const overlay = document.getElementById('deleteModalOverlay');

        this.hideModal(modal, overlay);

        console.log('Rename modal hidden');
    }

    async performRename(item, newName) {
        try {
            console.log('Renaming item:', item.name, 'to', newName);
            this.notifications.info(this.i18n.t('renaming', { filename: item.name }));

            await this.api.renameItem(this.currentUserId, item.id, newName);

            this.notifications.success(
                this.i18n.t('renamed', {
                    oldName: item.name,
                    newName: newName
                })
            );

            this.loadFiles(this.currentFolderId, true, this.isTrashView);
        } catch (error) {
            console.error('Rename error:', error);
            this.notifications.error(this.i18n.t('failedRename', { filename: item.name }));
        }
    }

    showDeleteModal(item) {
        const modal = document.getElementById('deleteModal');
        const overlay = document.getElementById('deleteModalOverlay');
        const messageEl = document.getElementById('deleteModalMessage');

        if (!modal || !overlay || !messageEl) {
            console.error('Modal elements not found');
            return;
        }

        const message = this.isTrashView
            ? this.i18n.t('confirmDeletePermanent', { filename: item.name })
            : this.i18n.t('confirmDelete', { filename: item.name });

        messageEl.textContent = message;

        const confirmBtn = document.getElementById('deleteConfirmBtn');
        const cancelBtn = document.getElementById('deleteCancelBtn');
        const closeBtn = document.getElementById('deleteModalClose');

        if (!confirmBtn || !cancelBtn || !closeBtn) {
            console.error('Modal buttons not found');
            return;
        }

        const handleConfirm = () => {
            this.hideDeleteModal();
            this.performDelete(item);

            // Cleanup event listeners
            confirmBtn.removeEventListener('click', handleConfirm);
            cancelBtn.removeEventListener('click', handleCancel);
            closeBtn.removeEventListener('click', handleCancel);
            overlay.removeEventListener('click', handleCancel);
        };

        const handleCancel = () => {
            this.hideDeleteModal();

            // Cleanup event listeners
            confirmBtn.removeEventListener('click', handleConfirm);
            cancelBtn.removeEventListener('click', handleCancel);
            closeBtn.removeEventListener('click', handleCancel);
            overlay.removeEventListener('click', handleCancel);
        };

        // Attach event listeners
        confirmBtn.addEventListener('click', handleConfirm);
        cancelBtn.addEventListener('click', handleCancel);
        closeBtn.addEventListener('click', handleCancel);
        overlay.addEventListener('click', handleCancel);

        // Show modal
        this.showModal(modal, overlay);

        console.log('Delete modal shown for:', item.name);
    }

    hideDeleteModal() {
        const modal = document.getElementById('deleteModal');
        const overlay = document.getElementById('deleteModalOverlay');

        this.hideModal(modal, overlay);

        console.log('Delete modal hidden');
    }

    async performDelete(item) {
        try {
            console.log('Deleting item:', item.name);
            this.notifications.info(this.i18n.t('deleting', { filename: item.name }));

            await this.api.deleteItem(this.currentUserId, item.id);

            const row = this.fileListBody.querySelector(`[data-item-id="${item.id}"]`);
            if (row) row.remove();

            this.allLoadedItems = this.allLoadedItems.filter((i) => i.id !== item.id);

            if (this.allLoadedItems.length === 0) {
                this.showEmptyState();
            }
        } catch (error) {
            console.error('Delete error:', error);
            this.notifications.error(this.i18n.t('failedDelete', { filename: item.name }));
        }
    }

    showLogoutModal() {
        const modal = document.getElementById('logoutModal');
        const overlay = document.getElementById('deleteModalOverlay');

        if (!modal || !overlay) {
            console.error('Logout modal elements not found');
            return;
        }

        const confirmBtn = document.getElementById('logoutConfirmBtn');
        const cancelBtn = document.getElementById('logoutCancelBtn');
        const closeBtn = document.getElementById('logoutModalClose');

        if (!confirmBtn || !cancelBtn || !closeBtn) {
            console.error('Logout modal buttons not found');
            return;
        }

        const handleConfirm = () => {
            this.hideLogoutModal();
            this.logout();

            // Cleanup event listeners
            confirmBtn.removeEventListener('click', handleConfirm);
            cancelBtn.removeEventListener('click', handleCancel);
            closeBtn.removeEventListener('click', handleCancel);
            overlay.removeEventListener('click', handleCancel);
        };

        const handleCancel = () => {
            this.hideLogoutModal();

            // Cleanup event listeners
            confirmBtn.removeEventListener('click', handleConfirm);
            cancelBtn.removeEventListener('click', handleCancel);
            closeBtn.removeEventListener('click', handleCancel);
            overlay.removeEventListener('click', handleCancel);
        };

        // Attach event listeners
        confirmBtn.addEventListener('click', handleConfirm);
        cancelBtn.addEventListener('click', handleCancel);
        closeBtn.addEventListener('click', handleCancel);
        overlay.addEventListener('click', handleCancel);

        // Show modal
        this.showModal(modal, overlay);

        console.log('Logout modal shown');
    }

    hideLogoutModal() {
        const modal = document.getElementById('logoutModal');
        const overlay = document.getElementById('deleteModalOverlay');

        this.hideModal(modal, overlay);

        console.log('Logout modal hidden');
    }

    logout() {
        console.log('Signing out...');
        this.api.clearAuthToken();
        window.location.href = 'login.html';
    }

    showModal(modal, overlay) {
        if (modal) modal.classList.add('show');
        if (overlay) overlay.classList.add('show');
    }

    hideModal(modal, overlay) {
        if (modal) modal.classList.remove('show');
        if (overlay) overlay.classList.remove('show');
    }

    // ═══════════════════════════════════════════════════════════════════
    // TOOLBAR MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════

    updateToolbar() {
        const toolbarActions = document.getElementById('toolbarActions');
        const toolbarActionsTrash = document.getElementById('toolbarActionsTrash');
        const selectionCount = document.getElementById('selectionCount');
        const selectionCountTrash = document.getElementById('selectionCountTrash');
        const newFolderBtn = document.getElementById('newFolderBtn');
        const emptyTrashBtn = document.getElementById('emptyTrashBtn');
        const renameBtn = document.getElementById('renameToolbarBtn');
        const downloadBtn = document.getElementById('downloadBtn');

        const count = this.selectedItems.size;

        if (this.isTrashView) {
            // ═══════════════════════════════════════════════════════════════
            // TRASH VIEW MODE
            // ═══════════════════════════════════════════════════════════════

            // Always hide normal actions and New Folder in trash
            if (toolbarActions) toolbarActions.style.display = 'none';
            if (newFolderBtn) newFolderBtn.style.display = 'none';

            if (count > 0) {
                // Show trash actions, hide Empty Trash button
                if (toolbarActionsTrash) toolbarActionsTrash.style.display = 'flex';
                if (emptyTrashBtn) emptyTrashBtn.style.display = 'none';

                const text = this.i18n.t('selectionCount', { count });
                if (selectionCountTrash) selectionCountTrash.textContent = text;
            } else {
                // Hide trash actions, show Empty Trash button
                if (toolbarActionsTrash) toolbarActionsTrash.style.display = 'none';
                if (emptyTrashBtn) emptyTrashBtn.style.display = 'flex';
            }
        } else {
            // ═══════════════════════════════════════════════════════════════
            // NORMAL MODE (My Drive)
            // ═══════════════════════════════════════════════════════════════

            // Always hide trash actions and Empty Trash in normal mode
            if (toolbarActionsTrash) toolbarActionsTrash.style.display = 'none';
            if (emptyTrashBtn) emptyTrashBtn.style.display = 'none';

            if (count > 0) {
                // Show action buttons, HIDE New Folder button
                if (toolbarActions) toolbarActions.style.display = 'flex';
                if (newFolderBtn) newFolderBtn.style.display = 'none';

                const text = this.i18n.t('selectionCount', { count });
                if (selectionCount) selectionCount.textContent = text;

                // Rename only available for single item
                if (renameBtn) {
                    renameBtn.disabled = count !== 1;
                }

                // Download disabled for folders
                if (downloadBtn && count === 1) {
                    const selectedItem = Array.from(this.selectedItems)[0];
                }
            } else {
                // Hide action buttons, SHOW New Folder button
                if (toolbarActions) toolbarActions.style.display = 'none';
                if (newFolderBtn) newFolderBtn.style.display = 'flex';
            }
        }
    }

    initializeToolbar() {
        // Clear selection buttons
        const clearSelectionBtn = document.getElementById('clearSelectionBtn');
        if (clearSelectionBtn) {
            clearSelectionBtn.addEventListener('click', () => {
                console.log('Clear selection clicked');
                this.clearSelection();
            });
        }

        const clearSelectionBtnTrash = document.getElementById('clearSelectionBtnTrash');
        if (clearSelectionBtnTrash) {
            clearSelectionBtnTrash.addEventListener('click', () => {
                console.log('Clear selection (trash) clicked');
                this.clearSelection();
            });
        }

        // Normal mode buttons
        const newFolderBtn = document.getElementById('newFolderBtn');
        if (newFolderBtn) {
            newFolderBtn.addEventListener('click', () => this.createNewFolder());
        }

        const downloadBtn = document.getElementById('downloadBtn');
        if (downloadBtn) {
            downloadBtn.addEventListener('click', () => this.downloadSelectedItems());
        }

        const moveBtn = document.getElementById('moveBtn');
        if (moveBtn) {
            moveBtn.addEventListener('click', () => this.moveSelectedItems());
        }

        const renameToolbarBtn = document.getElementById('renameToolbarBtn');
        if (renameToolbarBtn) {
            renameToolbarBtn.addEventListener('click', () => {
                const item = Array.from(this.selectedItems)[0];
                if (item) this.renameItem(item);
            });
        }

        const deleteToolbarBtn = document.getElementById('deleteToolbarBtn');
        if (deleteToolbarBtn) {
            deleteToolbarBtn.addEventListener('click', () => this.deleteSelectedItems());
        }

        // Trash mode buttons
        const restoreBtn = document.getElementById('restoreBtn');
        if (restoreBtn) {
            restoreBtn.addEventListener('click', () => this.restoreSelectedItems());
        }

        const deletePermanentlyBtn = document.getElementById('deletePermanentlyBtn');
        if (deletePermanentlyBtn) {
            deletePermanentlyBtn.addEventListener('click', () => this.deletePermanentlySelectedItems());
        }

        const emptyTrashBtn = document.getElementById('emptyTrashBtn');
        if (emptyTrashBtn) {
            emptyTrashBtn.addEventListener('click', () => this.emptyTrash());
        }

        // View toggle buttons
        const viewGridBtn = document.getElementById('viewGridBtn');
        const viewListBtn = document.getElementById('viewListBtn');

        if (viewGridBtn) {
            viewGridBtn.addEventListener('click', () => {
                viewGridBtn.classList.add('active');
                viewListBtn?.classList.remove('active');
            });
        }

        if (viewListBtn) {
            viewListBtn.addEventListener('click', () => {
                viewListBtn.classList.add('active');
                viewGridBtn?.classList.remove('active');
            });
        }
        this.initializeEmptyTrashButton();
    }

    createNewFolder() {
        console.log('Create new folder clicked');
        this.showCreateFolderModal();
    }

    async downloadSelectedItems() {
        if (this.selectedItems.size === 0) return;

        const items = Array.from(this.selectedItems);
        const count = items.length;

        try {
            if (count === 1) {
                // Single item - use existing download methods
                const item = items[0];
                if (item.type === 'file') {
                    await this.downloadFile(item);
                } else {
                    await this.downloadFolder(item);
                }
            } else {
                // Multiple items - download as ZIP
                console.log(`Downloading ${count} items as archive`);

                this.notifications.info(this.i18n.t('creatingArchive'));

                const itemIds = items.map((item) => item.id);
                const blob = await this.api.downloadMultipleItems(this.currentUserId, itemIds);

                // Generate filename for archive
                const timestamp = new Date().toISOString().slice(0, 10);
                const fileName = `CloudCore-Archive-${timestamp}.zip`;

                downloadBlob(blob, fileName);
                this.notifications.success(this.i18n.t('downloaded', { filename: fileName }));
            }
        } catch (error) {
            console.error('Download selected items error:', error);
            this.notifications.error(this.i18n.t('failedDownload'));
        }
    }

    async restoreSelectedItems() {
        if (this.selectedItems.size === 0) return;

        const items = Array.from(this.selectedItems);
        const count = items.length;

        try {
            console.log(`Restoring ${count} items from trash`);

            const result = await this.api.bulkRestoreItems(
                this.currentUserId,
                items.map((item) => item.id),
                {
                    concurrency: 5,
                    onProgress: (completed, total) => {
                        console.log(`Restore progress: ${completed}/${total}`);
                    },
                    onItemComplete: (itemId, result, error) => {
                        if (error) {
                            console.error(`Failed to restore item ${itemId}:`, error);
                        }
                    }
                }
            );

            if (result.failed.length === 0) {
                this.notifications.success(this.i18n.t('restoredItems', { count: result.succeeded.length }));
            } else {
                this.notifications.warning(
                    this.i18n.t('restoredPartial', {
                        succeeded: result.succeeded.length,
                        failed: result.failed.length
                    })
                );
            }

            this.selectedItems.clear();
            await this.loadFiles(null, true, true);
            this.updateToolbar();
        } catch (error) {
            console.error('Restore error:', error);
            this.notifications.error(this.i18n.t('failedRestoreMultiple'));
        }
    }

    async deleteSelectedItems() {
        if (this.selectedItems.size === 0) return;

        const items = Array.from(this.selectedItems);
        const count = items.length;

        // Show confirmation modal
        const message =
            count === 1
                ? this.i18n.t('confirmDelete', { filename: items[0].name })
                : this.i18n.t('confirmDeleteMultiple', { count });

        const modal = document.getElementById('deleteModal');
        const overlay = document.getElementById('deleteModalOverlay');
        const messageEl = document.getElementById('deleteModalMessage');

        if (!modal || !overlay || !messageEl) return;

        messageEl.textContent = message;

        const confirmed = await new Promise((resolve) => {
            const confirmBtn = document.getElementById('deleteConfirmBtn');
            const cancelBtn = document.getElementById('deleteCancelBtn');
            const closeBtn = document.getElementById('deleteModalClose');

            const handleConfirm = () => {
                cleanup();
                resolve(true);
            };

            const handleCancel = () => {
                cleanup();
                resolve(false);
            };

            const cleanup = () => {
                modal.classList.remove('show');
                overlay.classList.remove('show');
                confirmBtn.removeEventListener('click', handleConfirm);
                cancelBtn.removeEventListener('click', handleCancel);
                closeBtn.removeEventListener('click', handleCancel);
                overlay.removeEventListener('click', handleCancel);
            };

            confirmBtn.addEventListener('click', handleConfirm);
            cancelBtn.addEventListener('click', handleCancel);
            closeBtn.addEventListener('click', handleCancel);
            overlay.addEventListener('click', handleCancel);

            modal.classList.add('show');
            overlay.classList.add('show');
        });

        if (!confirmed) return;

        try {
            console.log(`Deleting ${count} items`);

            const result = await this.api.bulkDeleteItems(
                this.currentUserId,
                items.map((item) => item.id),
                {
                    concurrency: 5,
                    onProgress: (completed, total) => {
                        console.log(`Delete progress: ${completed}/${total}`);
                    },
                    onItemComplete: (itemId, result, error) => {
                        if (error) {
                            console.error(`Failed to delete item ${itemId}:`, error);
                        }
                    }
                }
            );

            if (result.failed.length === 0) {
                this.notifications.success(this.i18n.t('deletedMultiple', { count: result.succeeded.length }));
            } else {
                this.notifications.warning(
                    this.i18n.t('deletedPartial', {
                        succeeded: result.succeeded.length,
                        failed: result.failed.length
                    })
                );
            }

            this.selectedItems.clear();
            await this.loadFiles(this.currentFolderId, true);
            this.updateToolbar();
        } catch (error) {
            console.error('Delete error:', error);
            this.notifications.error(this.i18n.t('failedDelete'));
        }
    }

    async deletePermanentlySelectedItems() {
        if (this.selectedItems.size === 0) return;

        const items = Array.from(this.selectedItems);
        const count = items.length;

        // Build confirmation message
        const message =
            count === 1
                ? this.i18n.t('confirmDeletePermanent', { filename: items[0].name }) ||
                  `Delete "${items[0].name}" permanently? This action cannot be undone.`
                : this.i18n.t('confirmDeletePermanentMultiple', { count }) ||
                  `Delete ${count} items permanently? This action cannot be undone.`;

        // Show confirmation modal
        const modal = document.getElementById('deleteModal');
        const overlay = document.getElementById('deleteModalOverlay');
        const messageEl = document.getElementById('deleteModalMessage');
        const titleEl = document.getElementById('deleteModalTitle');

        if (!modal || !overlay || !messageEl) return;

        // Update modal content
        if (titleEl) titleEl.textContent = this.i18n.t('deletePermanently') || 'Delete Permanently';
        messageEl.textContent = message;

        const confirmed = await new Promise((resolve) => {
            const confirmBtn = document.getElementById('deleteConfirmBtn');
            const cancelBtn = document.getElementById('deleteCancelBtn');
            const closeBtn = document.getElementById('deleteModalClose');

            const handleConfirm = () => {
                cleanup();
                resolve(true);
            };

            const handleCancel = () => {
                cleanup();
                resolve(false);
            };

            const cleanup = () => {
                modal.classList.remove('show');
                overlay.classList.remove('show');
                confirmBtn.removeEventListener('click', handleConfirm);
                cancelBtn.removeEventListener('click', handleCancel);
                closeBtn.removeEventListener('click', handleCancel);
                overlay.removeEventListener('click', handleCancel);
            };

            confirmBtn.addEventListener('click', handleConfirm);
            cancelBtn.addEventListener('click', handleCancel);
            closeBtn.addEventListener('click', handleCancel);
            overlay.addEventListener('click', handleCancel);

            this.showModal(modal, overlay);
        });

        if (!confirmed) return;

        try {
            console.log(`Permanently deleting ${count} items`);

            for (const item of items) {
                await this.api.deletePermanently(this.currentUserId, item.id);
            }

            const successText =
                count === 1
                    ? this.i18n.t('deletedPermanently', { filename: items[0].name }) ||
                      `"${items[0].name}" deleted permanently`
                    : this.i18n.t('deletedPermanentlyMultiple', { count }) || `${count} items deleted permanently`;

            this.notifications.success(successText);

            this.selectedItems.clear();
            await this.loadFiles(null, true, true);
            this.updateToolbar();
        } catch (error) {
            console.error('Permanent delete error:', error);
            this.notifications.error(this.i18n.t('failedDeletePermanently') || 'Failed to delete permanently');
        }
    }

    async showEmptyTrashModal() {
        const modal = document.getElementById('emptyTrashModal');
        const overlay = document.getElementById('deleteModalOverlay');
        const messageEl = document.getElementById('emptyTrashModalMessage');
        const progressSection = document.getElementById('emptyTrashProgress');
        const progressText = document.getElementById('emptyTrashProgressText');
        const progressCount = document.getElementById('emptyTrashProgressCount');
        const progressBar = document.getElementById('emptyTrashProgressBar');
        const confirmBtn = document.getElementById('emptyTrashConfirmBtn');
        const cancelBtn = document.getElementById('emptyTrashCancelBtn');
        const closeBtn = document.getElementById('emptyTrashModalClose');

        if (!modal || !overlay) return;

        // Reset modal state
        if (messageEl) messageEl.style.display = 'block';
        if (progressSection) progressSection.style.display = 'none';
        if (progressBar) progressBar.style.width = '0%';
        confirmBtn.disabled = false;
        cancelBtn.disabled = false;

        let isOperationInProgress = false;

        const handleConfirm = async () => {
            if (isOperationInProgress) return;

            isOperationInProgress = true;
            confirmBtn.disabled = true;
            cancelBtn.disabled = true;
            confirmBtn.textContent = this.i18n.t('processing') || 'Processing...';

            // Hide message, show progress
            if (messageEl) messageEl.style.display = 'none';
            if (progressSection) progressSection.style.display = 'block';

            try {
                await this.performEmptyTrash(progressText, progressCount, progressBar);
                cleanup();
                this.hideModal(modal, overlay);
            } catch (error) {
                console.error('Empty trash error:', error);
                isOperationInProgress = false;
                confirmBtn.disabled = false;
                cancelBtn.disabled = false;
                confirmBtn.textContent = this.i18n.t('emptyTrash') || 'Empty Trash';
            }
        };

        const handleCancel = () => {
            if (!isOperationInProgress) {
                cleanup();
                this.hideModal(modal, overlay);
            }
        };

        const cleanup = () => {
            confirmBtn.removeEventListener('click', handleConfirm);
            cancelBtn.removeEventListener('click', handleCancel);
            closeBtn.removeEventListener('click', handleCancel);
            overlay.removeEventListener('click', handleOverlayClick);
        };

        const handleOverlayClick = (e) => {
            if (e.target === overlay && !isOperationInProgress) {
                handleCancel();
            }
        };

        confirmBtn.addEventListener('click', handleConfirm);
        cancelBtn.addEventListener('click', handleCancel);
        closeBtn.addEventListener('click', handleCancel);
        overlay.addEventListener('click', handleOverlayClick);

        this.showModal(modal, overlay);
    }

    async performEmptyTrash(progressText, progressCount, progressBar) {
        try {
            console.log('Starting empty trash operation...');

            progressText.textContent = this.i18n.t('loadingTrashItems') || 'Loading trash items...';
            const trashItems = await this.api.getTrashItems(this.currentUserId);

            if (!trashItems || trashItems.length === 0) {
                this.notifications.show(this.i18n.t('trashAlreadyEmpty') || 'Trash is already empty', 'info');
                return;
            }

            const totalItems = trashItems.length;
            let processedItems = 0;

            progressText.textContent = this.i18n.t('deletingItems') || 'Deleting items...';
            progressCount.textContent = `0 / ${totalItems}`;

            for (const item of trashItems) {
                await this.api.deletePermanently(this.currentUserId, item.id);
                processedItems++;

                const progress = Math.round((processedItems / totalItems) * 100);
                progressBar.style.width = `${progress}%`;
                progressCount.textContent = `${processedItems} / ${totalItems}`;

                await new Promise((resolve) => setTimeout(resolve, 50));
            }

            progressBar.style.width = '100%';
            this.notifications.show(
                this.i18n.t('trashEmptiedCount', { count: totalItems }) || `${totalItems} items deleted permanently`,
                'success'
            );

            await this.loadFiles(null, true, true);

            console.log('Empty trash completed successfully');
        } catch (error) {
            console.error('Empty trash error:', error);
            this.notifications.show(this.i18n.t('failedEmptyTrash') || 'Failed to empty trash', 'error');
            throw error;
        }
    }

    initializeEmptyTrashButton() {
        const emptyTrashBtn = document.getElementById('emptyTrashBtn');
        if (emptyTrashBtn) {
            emptyTrashBtn.addEventListener('click', () => this.showEmptyTrashModal());
            console.log('Empty trash button initialized');
        } else {
            console.warn('Empty trash button not found');
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // SIDEBAR NAVIGATION
    // ═══════════════════════════════════════════════════════════════════

    handleSidebarClick(e) {
        console.log('Sidebar clicked');

        document.querySelectorAll('.sidebar-item').forEach((item) => {
            item.classList.remove('active');
        });
        e.currentTarget.classList.add('active');

        const section = e.currentTarget.dataset.section;
        const searchContainer = document.querySelector('.search-container');

        if (section === 'trash') {
            searchContainer.style.display = 'none';
            this.breadcrumbPath = [];
            this.currentFolderId = null;
            this.selectedItems.clear();
            this.loadFiles(null, true, true);
            this.updateToolbar();
        } else if (section === 'mydrive') {
            searchContainer.style.display = '';
            this.isTrashView = false;
            this.selectedItems.clear();
            this.navigateToRoot();
            this.updateToolbar();
        } else {
            this.notifications.info('Feature not implemented: ' + section);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // FILE UPLOAD WITH PROGRESS
    // ═══════════════════════════════════════════════════════════════════

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
            const uploadId = `${Date.now()}-${i}-${Math.random().toString(36).substr(2, 9)}`;

            try {
                console.log(`Uploading file ${i + 1}/${files.length}:`, file.name);

                const uploadPromise = this.api.uploadFileWithProgress(
                    this.currentUserId,
                    file,
                    this.currentFolderId,
                    (progress, loaded, total) => {
                        this.uploadProgress.updateProgress(uploadId, progress, loaded, total);
                    }
                );

                this.uploadProgress.addUpload(uploadId, file.name, file.size, () => {
                    uploadPromise.cancel();
                    console.log('Upload cancelled:', file.name);
                });

                await uploadPromise;

                successCount++;
                this.uploadProgress.completeUpload(uploadId);
                console.log(`✓ Uploaded: ${file.name}`);
            } catch (error) {
                console.error('Upload error:', file.name, error);
                errorCount++;

                if (error.message === 'Upload cancelled') {
                } else {
                    this.uploadProgress.errorUpload(uploadId, error.message || 'Upload failed');
                }
            }
        }

        if (errorCount === 0 && successCount > 0) {
            this.notifications.show(
                this.i18n.t('uploadSuccessMultiple', { count: successCount }) ||
                    `${successCount} file(s) uploaded successfully`,
                'success'
            );
        } else if (successCount > 0) {
            this.notifications.show(
                this.i18n.t('uploadPartial', { successCount, errorCount }) ||
                    `${successCount} uploaded, ${errorCount} failed`,
                'warning'
            );
        }

        console.log(`Upload complete: ${successCount} success, ${errorCount} errors`);

        await this.loadFiles(this.currentFolderId, true);
        e.target.value = '';
    }

    async handleFolderUpload(e) {
        const files = Array.from(e.target.files);
        console.log('handleFolderUpload:', files.length, 'files');

        if (!files || files.length === 0) return;

        const validFiles = files.filter((f) => f.webkitRelativePath);
        if (validFiles.length === 0) {
            this.notifications.error(this.i18n.t('invalidFolderStructure'));
            e.target.value = '';
            return;
        }

        const folderStructure = buildFolderStructure(validFiles);

        const firstFilePath = validFiles[0].webkitRelativePath;
        const rootFolderName = firstFilePath ? firstFilePath.split('/')[0] : null;

        if (!rootFolderName) {
            this.notifications.error(this.i18n.t('invalidFolderStructure'));
            e.target.value = '';
            return;
        }

        console.log('Root folder name:', rootFolderName);

        try {
            const folderExists = await this.checkFolderExists(rootFolderName, this.currentFolderId);
            if (folderExists) {
                this.notifications.warning(this.i18n.t('folderAlreadyExistsCancelled', { foldername: rootFolderName }));

                let fileIndex = 0;
                for (const [folderPath, folderFiles] of folderStructure.entries()) {
                    for (const file of folderFiles) {
                        const uploadId = `blocked-${Date.now()}-${fileIndex++}-${Math.random()
                            .toString(36)
                            .substr(2, 9)}`;
                        const displayName = file.webkitRelativePath || file.name;

                        this.uploadProgress.addUpload(uploadId, displayName, file.size);
                        this.uploadProgress.errorUpload(uploadId, this.i18n.t('uploadBlockedFolderExists'));
                    }
                }

                e.target.value = '';
                return;
            }
        } catch (error) {
            console.error('Error checking folder existence:', error);
        }

        this.notifications.info(this.i18n.t('uploadingFolder', { count: validFiles.length }));

        this.uploadFolderIdCache = new Map();
        this.uploadFolderIdCache.set('', this.currentFolderId);

        let successCount = 0;
        let errorCount = 0;
        let fileIndex = 0;

        for (const [folderPath, folderFiles] of folderStructure) {
            try {
                const folderId = await this.createFolderPath(folderPath);

                for (const file of folderFiles) {
                    const uploadId = `folder-${Date.now()}-${fileIndex++}-${Math.random().toString(36).substr(2, 9)}`;
                    const displayName = file.webkitRelativePath || file.name;

                    try {
                        const uploadPromise = this.api.uploadFileWithProgress(
                            this.currentUserId,
                            file,
                            folderId,
                            (progress, loaded, total) => {
                                this.uploadProgress.updateProgress(uploadId, progress, loaded, total);
                            }
                        );

                        this.uploadProgress.addUpload(uploadId, displayName, file.size, () => {
                            uploadPromise.cancel();
                            console.log('Upload cancelled:', displayName);
                        });

                        await uploadPromise;

                        successCount++;
                        this.uploadProgress.completeUpload(uploadId);
                        console.log(`✓ Uploaded: ${displayName}`);
                    } catch (error) {
                        errorCount++;
                        console.error('File upload error:', displayName, error);

                        if (error.message === 'Upload cancelled') {
                            this.uploadProgress.errorUpload(uploadId, this.i18n.t('uploadCancelled') || 'Cancelled');
                        } else {
                            const errorMsg = this.i18n.getTranslatedError(error, 'uploadFailed');
                            this.uploadProgress.errorUpload(uploadId, errorMsg);
                        }
                    }
                }
            } catch (error) {
                console.error('Folder creation error:', folderPath, error);
                errorCount += folderFiles.length;

                folderFiles.forEach((file) => {
                    const uploadId = `folder-${Date.now()}-${fileIndex++}-${Math.random().toString(36).substr(2, 9)}`;
                    const displayName = file.webkitRelativePath || file.name;
                    this.uploadProgress.addUpload(uploadId, displayName, file.size);
                    this.uploadProgress.errorUpload(
                        uploadId,
                        this.i18n.t('uploadFailedFolderError') || 'Failed: folder creation error'
                    );
                });
            }
        }

        this.uploadFolderIdCache = null;

        if (errorCount === 0 && successCount > 0) {
            this.notifications.success(this.i18n.t('uploadFolderSuccess', { count: successCount }));
        } else if (successCount > 0) {
            this.notifications.warning(
                this.i18n.t('uploadFolderPartial', {
                    successCount: successCount,
                    errorCount: errorCount
                })
            );
        } else if (errorCount > 0) {
            this.notifications.error(this.i18n.t('uploadFolderFailed', { count: errorCount }));
        }

        await this.loadFiles(this.currentFolderId, true);
        e.target.value = '';
    }

    async createFolderPath(folderPath) {
        if (!folderPath) return this.currentFolderId;

        if (this.uploadFolderIdCache && this.uploadFolderIdCache.has(folderPath)) {
            return this.uploadFolderIdCache.get(folderPath);
        }

        const pathParts = folderPath.split('/').filter((p) => p.length > 0);
        let currentParentId = this.currentFolderId;
        let pathSoFar = '';

        for (const folderName of pathParts) {
            if (!folderName) continue;

            pathSoFar = pathSoFar ? `${pathSoFar}/${folderName}` : folderName;

            if (this.uploadFolderIdCache && this.uploadFolderIdCache.has(pathSoFar)) {
                currentParentId = this.uploadFolderIdCache.get(pathSoFar);
                console.log(`Cache hit for: ${pathSoFar} -> ${currentParentId}`);
                continue;
            }

            try {
                const existingId = await this.findExistingFolderByName(folderName, currentParentId);

                if (existingId) {
                    currentParentId = existingId;

                    if (this.uploadFolderIdCache) {
                        this.uploadFolderIdCache.set(pathSoFar, existingId);
                    }

                    console.log(`Found existing folder: ${folderName} -> ${currentParentId}`);
                    continue;
                }

                const result = await this.api.createFolder(this.currentUserId, folderName, currentParentId);
                currentParentId = result.folderId || result.id;

                if (this.uploadFolderIdCache) {
                    this.uploadFolderIdCache.set(pathSoFar, currentParentId);
                }

                console.log(`Created folder: ${folderName} -> ${currentParentId}`);
            } catch (error) {
                console.error(`Error processing folder "${folderName}":`, error);
                throw error;
            }
        }

        return currentParentId;
    }

    async findExistingFolderByName(folderName, parentId) {
        try {
            const item = await this.api.getItemByName(this.currentUserId, folderName, parentId);

            if (item && item.type === 'folder') {
                console.log(`getItemByName found folder: "${folderName}" -> ID: ${item.id}`);
                return item.id;
            }

            console.log(`getItemByName: folder "${folderName}" not found in parent ${parentId}`);
            return null;
        } catch (error) {
            if (error.status === 404 || error.message?.includes('not found')) {
                console.log(`Folder "${folderName}" does not exist in parent ${parentId}`);
                return null;
            }

            console.error(`Error in findExistingFolderByName for "${folderName}":`, error);
            return null;
        }
    }

    async checkFolderExists(folderName, parentId) {
        try {
            const folderId = await this.findExistingFolderByName(folderName, parentId);
            const exists = folderId !== null;

            if (exists) {
                console.log(
                    `⚠️ Root folder "${folderName}" already exists in parent ${parentId || 'root'} with ID: ${folderId}`
                );
            } else {
                console.log(`✓ Root folder "${folderName}" does not exist, can proceed with upload`);
            }

            return exists;
        } catch (error) {
            console.error('Error checking folder existence:', error);
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // DRAG AND DROP - EXTERNAL FILES
    // ═══════════════════════════════════════════════════════════════════

    setupDragAndDrop() {
        console.log('Setting up drag and drop');
        const container = document.getElementById('fileContainer');

        // External file drop handlers
        container.addEventListener(
            'dragover',
            (e) => {
                if (e.dataTransfer.types.includes('Files')) {
                    e.preventDefault();
                    e.stopPropagation();
                    container.classList.add('dragover');
                }
            },
            false
        );

        container.addEventListener(
            'dragenter',
            (e) => {
                if (e.dataTransfer.types.includes('Files')) {
                    e.preventDefault();
                    e.stopPropagation();
                    container.classList.add('dragover');
                }
            },
            false
        );

        container.addEventListener(
            'dragleave',
            (e) => {
                if (e.target === container || !container.contains(e.relatedTarget)) {
                    container.classList.remove('dragover');
                }
            },
            false
        );

        container.addEventListener(
            'drop',
            async (e) => {
                // Remove dimming from selected rows
                document.querySelectorAll('.file-list-row.dragging-selected').forEach((selectedRow) => {
                    selectedRow.classList.remove('dragging-selected');
                });

                if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
                    e.preventDefault();
                    e.stopPropagation();
                    container.classList.remove('dragover');

                    console.log('Files dropped');
                    const files = Array.from(e.dataTransfer.files);
                    console.log(`Dropped ${files.length} files`);

                    const input = document.getElementById('fileInput');
                    const dt = new DataTransfer();
                    files.forEach((file) => dt.items.add(file));
                    input.files = dt.files;

                    const event = new Event('change', { bubbles: true });
                    input.dispatchEvent(event);
                }
            },
            false
        );

        // Global dragover for custom ghost tracking
        let lastDragOverTime = 0;
        const DRAG_THROTTLE = 8; // ~120 FPS for smoothness

        document.addEventListener('dragover', (e) => {
            if (this.customDragGhost && this.isDraggingInternal) {
                const now = Date.now();
                if (now - lastDragOverTime >= DRAG_THROTTLE) {
                    console.log(`[DRAGOVER] X=${e.clientX}, Y=${e.clientY}`);
                    this.updateDragGhostPosition(e.clientX, e.clientY);
                    lastDragOverTime = now;
                }
            }
        });

        // Cleanup on dragend
        document.addEventListener('dragend', () => {
            console.log('Drag ended - cleaning up ghost element');
            if (this.customDragGhost) {
                this.customDragGhost.style.opacity = '0';
                setTimeout(() => {
                    if (this.customDragGhost && this.customDragGhost.parentNode) {
                        document.body.removeChild(this.customDragGhost);
                    }
                    this.customDragGhost = null;
                }, 150);
            }
            this.isDraggingInternal = false;
        });

        // Additional cleanup on drop
        document.addEventListener('drop', (e) => {
            if (this.customDragGhost) {
                this.customDragGhost.style.opacity = '0';
                setTimeout(() => {
                    if (this.customDragGhost && this.customDragGhost.parentNode) {
                        document.body.removeChild(this.customDragGhost);
                    }
                    this.customDragGhost = null;
                }, 150);
            }
            this.isDraggingInternal = false;
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    // BREADCRUMBS
    // ═══════════════════════════════════════════════════════════════════

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

            const isLast = this.breadcrumbPath.length === 0;
            if (isLast) {
                homeBreadcrumb.classList.add('current');
            }

            homeBreadcrumb.addEventListener('click', (e) => {
                e.preventDefault();
                this.navigateToRoot();
            });

            this.setupBreadcrumbDragDrop(homeBreadcrumb, null);

            breadcrumbs.appendChild(homeBreadcrumb);

            this.breadcrumbPath.forEach((folder, index) => {
                const separator = document.createElement('span');
                separator.className = 'breadcrumb-separator';
                separator.textContent = '/';
                breadcrumbs.appendChild(separator);

                const breadcrumb = document.createElement('a');
                breadcrumb.className = index === this.breadcrumbPath.length - 1 ? 'breadcrumb current' : 'breadcrumb';
                breadcrumb.href = '#';
                breadcrumb.textContent = folder.name;

                if (index < this.breadcrumbPath.length - 1) {
                    breadcrumb.addEventListener('click', (e) => {
                        e.preventDefault();
                        this.navigateToFolderInPath(index);
                    });

                    this.setupBreadcrumbDragDrop(breadcrumb, folder.id);
                }

                breadcrumbs.appendChild(breadcrumb);
            });
        }
    }

    setupBreadcrumbDragDrop(breadcrumbElement, folderId) {
        breadcrumbElement.addEventListener('dragover', (e) => {
            if (e.dataTransfer.types.includes('text/plain')) {
                e.preventDefault();
                e.stopPropagation();
                e.dataTransfer.dropEffect = 'move';
                breadcrumbElement.classList.add('drag-over-breadcrumb');
            }
        });

        breadcrumbElement.addEventListener('dragleave', (e) => {
            breadcrumbElement.classList.remove('drag-over-breadcrumb');
        });

        breadcrumbElement.addEventListener('drop', async (e) => {
            if (e.dataTransfer.types.includes('text/plain')) {
                e.preventDefault();
                e.stopPropagation();
                breadcrumbElement.classList.remove('drag-over-breadcrumb');

                if (!this.draggedItems || this.draggedItems.length === 0) {
                    console.log('No items to move');
                    return;
                }

                const targetFolderId = folderId;

                if (targetFolderId === this.currentFolderId) {
                    console.log('Cannot move to the same folder');
                    return;
                }

                try {
                    const targetName =
                        folderId === null ? this.i18n.t('myDrive') : breadcrumbElement.textContent.trim();

                    console.log(`Moving ${this.draggedItems.length} item(s) to:`, targetName);

                    const result = await this.api.bulkMoveItems(this.currentUserId, this.draggedItems, targetFolderId, {
                        concurrency: 5,
                        onProgress: (completed, total) => {
                            console.log(`Move progress: ${completed}/${total}`);
                        }
                    });

                    if (result.failed.length === 0) {
                        const successText =
                            result.succeeded.length === 1 ? '1 item' : `${result.succeeded.length} items`;
                        this.notifications.success(`Moved ${successText} to ${targetName}`);
                    } else {
                        this.notifications.warning(
                            `Moved ${result.succeeded.length} items. Failed: ${result.failed.length}`
                        );
                    }

                    this.selectedItems.clear();
                    await this.loadFiles(this.currentFolderId, true);
                } catch (error) {
                    console.error('Move error:', error);
                    this.notifications.error(error.message || this.i18n.t('failedToMove'));
                }

                this.draggedItems = null;
                this.dragSourceType = null;
            }
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    // SEARCH AND SORTING
    // ═══════════════════════════════════════════════════════════════════

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

        Object.values(headers).forEach((h) => {
            if (!h) return;
            if (!h.dataset.label) h.dataset.label = h.textContent.trim();
            h.textContent = h.dataset.label;
        });

        const active = headers[this.sortBy];
        if (active) {
            const icon = document.createElement('span');
            icon.className = 'material-symbols-outlined sort-icon';
            icon.textContent = this.sortDir === 'asc' ? 'arrow_upward' : 'arrow_downward';
            active.appendChild(icon);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // INFINITE SCROLL
    // ═══════════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════════
    // UI STATE HELPERS
    // ═══════════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════════
    // UTILITY FUNCTIONS
    // ═══════════════════════════════════════════════════════════════════

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
}

// Initialize application when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOM loaded, initializing CloudCoreDrive');
    new CloudCoreDrive();
});
