import { I18n } from '../translations.js';

export class ContextMenuManager {
    constructor() {
        this.i18n = new I18n();
        this.menuElement = document.getElementById('contextMenu');
        this.currentItem = null;
        this.isTrashView = false;
        this.handlers = {};
        
        this.setupEventListeners();
    }

    setupEventListeners() {
        document.addEventListener('click', () => this.hide());
        document.addEventListener('contextmenu', (e) => {
            if (!e.target.closest('.file-list-row')) {
                this.hide();
            }
        });
    }

    show(e, item, isTrashView = false) {
        e.preventDefault();
        e.stopPropagation();

        this.currentItem = item;
        this.isTrashView = isTrashView;

        if (!this.menuElement) return;

        this.menuElement.innerHTML = this.buildMenuHTML(item, isTrashView);
        this.attachMenuHandlers();

        this.menuElement.style.display = 'block';
        this.menuElement.style.left = `${e.pageX}px`;
        this.menuElement.style.top = `${e.pageY}px`;

        // Adjust position if menu goes off screen
        const rect = this.menuElement.getBoundingClientRect();
        if (rect.right > window.innerWidth) {
            this.menuElement.style.left = `${e.pageX - rect.width}px`;
        }
        if (rect.bottom > window.innerHeight) {
            this.menuElement.style.top = `${e.pageY - rect.height}px`;
        }
    }

    buildMenuHTML(item, isTrashView) {
        if (isTrashView) {
            return `
                <div class="context-menu-item" data-action="restore">
                    <span class="context-menu-icon">🔄</span>
                    <span>${this.i18n.t('restore')}</span>
                </div>
                <div class="context-menu-separator"></div>
                <div class="context-menu-item danger" data-action="delete-permanently">
                    <span class="context-menu-icon">❌</span>
                    <span>${this.i18n.t('deletePermanently')}</span>
                </div>
            `;
        }

        if (item.type === 'folder') {
            return `
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
            `;
        } else {
            return `
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
            `;
        }
    }

    attachMenuHandlers() {
        this.menuElement.querySelectorAll('.context-menu-item').forEach(menuItem => {
            menuItem.addEventListener('click', (e) => {
                e.stopPropagation();
                const action = e.currentTarget.dataset.action;
                this.handleAction(action, this.currentItem);
                this.hide();
            });
        });
    }

    handleAction(action, item) {
        const handler = this.handlers[action];
        if (handler && typeof handler === 'function') {
            handler(item);
        }
    }

    hide() {
        if (this.menuElement) {
            this.menuElement.style.display = 'none';
        }
        this.currentItem = null;
    }

    setHandler(action, handler) {
        this.handlers[action] = handler;
    }

    setHandlers(handlers) {
        this.handlers = { ...this.handlers, ...handlers };
    }
}
