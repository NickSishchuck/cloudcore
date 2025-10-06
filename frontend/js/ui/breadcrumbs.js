import { I18n } from '../translations.js';

export class BreadcrumbManager {
    constructor(api, currentUserId) {
        this.api = api;
        this.currentUserId = currentUserId;
        this.i18n = new I18n();
        this.breadcrumbPath = [];
        this.currentFolderId = null;
        this.isTrashView = false;
    }

    update(isTrashView = false) {
        this.isTrashView = isTrashView;
        const breadcrumbs = document.getElementById('breadcrumbs');
        if (!breadcrumbs) return;
        
        breadcrumbs.innerHTML = '';

        const homeBreadcrumb = document.createElement('a');
        homeBreadcrumb.className = 'breadcrumb';
        homeBreadcrumb.href = '#';

        if (isTrashView) {
            homeBreadcrumb.textContent = this.i18n.t('trash');
            homeBreadcrumb.style.cursor = 'default';
            homeBreadcrumb.style.pointerEvents = 'none';
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
        this.currentFolderId = null;
        this.breadcrumbPath = [];
        if (this.onNavigate) {
            this.onNavigate(null);
        }
    }

    navigateToFolderInPath(index) {
        const targetFolder = this.breadcrumbPath[index];
        this.currentFolderId = targetFolder.id;
        this.breadcrumbPath = this.breadcrumbPath.slice(0, index + 1);
        if (this.onNavigate) {
            this.onNavigate(targetFolder.id);
        }
    }

    async navigateToFolder(folder) {
        this.breadcrumbPath.push({
            id: folder.id,
            name: folder.name
        });
        this.currentFolderId = folder.id;
        if (this.onNavigate) {
            this.onNavigate(folder.id);
        }
    }

    async buildPathFromSearch(folder) {
        try {
            const folderPath = await this.api.getFolderPath(this.currentUserId, folder.id);
            const pathParts = folderPath.split(/[/\\]/).filter(part => part.trim() !== '');
            
            this.breadcrumbPath = pathParts.map((name, index) => ({
                id: index === pathParts.length - 1 ? folder.id : null,
                name: name
            }));
        } catch (error) {
            console.error('Error building breadcrumb:', error);
            this.breadcrumbPath = [{
                id: folder.id,
                name: folder.name
            }];
        }
    }

    clear() {
        this.breadcrumbPath = [];
        this.currentFolderId = null;
    }

    setNavigationHandler(handler) {
        this.onNavigate = handler;
    }
}
