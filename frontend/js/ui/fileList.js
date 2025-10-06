import { formatFileSize, formatDateTime, getFileIcon } from '../utils/fileUtils.js';

export class FileListManager {
    constructor() {
        this.listBody = document.getElementById('fileListBody');
        this.fileList = document.getElementById('fileList');
        this.loadingState = document.getElementById('loadingState');
        this.emptyState = document.getElementById('emptyState');
        this.isTrashView = false;
    }

    render(items, append = false) {
        if (!this.listBody || !this.fileList) return;

        if (items.length === 0 && !append) {
            this.showEmptyState();
            return;
        }

        this.hideEmptyState();

        if (!append) {
            this.listBody.innerHTML = '';
        }

        this.fileList.style.display = 'table';

        items.forEach(item => {
            // Skip if already rendered
            if (this.listBody.querySelector(`[data-item-id="${item.id}"]`)) {
                return;
            }

            const row = this.createRow(item);
            this.listBody.appendChild(row);
        });
    }

    createRow(item) {
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

        return row;
    }

    removeItem(itemId) {
        const row = this.listBody?.querySelector(`[data-item-id="${itemId}"]`);
        if (row) {
            row.remove();
        }

        // Check if list is empty
        if (this.listBody && this.listBody.children.length === 0) {
            this.showEmptyState();
        }
    }

    clear() {
        if (this.listBody) {
            this.listBody.innerHTML = '';
        }
    }

    showLoading() {
        if (this.loadingState) this.loadingState.style.display = 'flex';
        if (this.fileList) this.fileList.style.display = 'none';
        if (this.emptyState) this.emptyState.style.display = 'none';
    }

    hideLoading() {
        if (this.loadingState) this.loadingState.style.display = 'none';
        if (this.fileList) this.fileList.style.display = 'table';
    }

    showEmptyState() {
        if (this.emptyState) this.emptyState.style.display = 'flex';
        if (this.fileList) this.fileList.style.display = 'none';
    }

    hideEmptyState() {
        if (this.emptyState) this.emptyState.style.display = 'none';
    }

    setTrashView(isTrash) {
        this.isTrashView = isTrash;
    }

    attachRowHandlers(row, handlers) {
        const { onClick, onDoubleClick, onContextMenu } = handlers;

        if (onClick) {
            row.addEventListener('click', onClick);
        }
        if (onDoubleClick) {
            row.addEventListener('dblclick', onDoubleClick);
        }
        if (onContextMenu) {
            row.addEventListener('contextmenu', onContextMenu);
        }

        return row;
    }
}
