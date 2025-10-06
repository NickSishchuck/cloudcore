import { buildFolderStructure } from './fileUtils.js';

export class DragDropManager {
    constructor(container, uploadHandler) {
        this.container = container;
        this.uploadHandler = uploadHandler;
        this.setup();
    }

    setup() {
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            this.container.addEventListener(eventName, (e) => {
                e.preventDefault();
                e.stopPropagation();
            });
        });

        this.container.addEventListener('dragenter', (e) => {
            this.container.classList.add('dragover');
            this.analyzeDraggedItems(e.dataTransfer);
        });

        this.container.addEventListener('dragover', () => {
            this.container.classList.add('dragover');
        });

        this.container.addEventListener('dragleave', (e) => {
            if (!this.container.contains(e.relatedTarget)) {
                this.container.classList.remove('dragover');
                this.container.removeAttribute('data-drag-type');
            }
        });

        this.container.addEventListener('drop', async (e) => {
            this.container.classList.remove('dragover');
            this.container.removeAttribute('data-drag-type');
            this.container.classList.add('uploading');

            try {
                await this.handleDrop(e);
                this.container.classList.add('upload-success');
                setTimeout(() => this.container.classList.remove('upload-success'), 600);
            } catch (error) {
                this.container.classList.add('upload-error');
                setTimeout(() => this.container.classList.remove('upload-error'), 600);
                throw error;
            } finally {
                this.container.classList.remove('uploading');
            }
        });
    }

    analyzeDraggedItems(dataTransfer) {
        if (dataTransfer.items && dataTransfer.items.length > 0) {
            let hasFiles = false;
            let hasFolders = false;

            for (const item of dataTransfer.items) {
                if (item.kind === 'file') {
                    if ('webkitGetAsEntry' in item) {
                        const entry = item.webkitGetAsEntry();
                        if (entry) {
                            if (entry.isFile) hasFiles = true;
                            if (entry.isDirectory) hasFolders = true;
                        }
                    } else {
                        hasFiles = true;
                    }
                }
            }

            if (hasFiles && hasFolders) {
                this.container.setAttribute('data-drag-type', 'mixed');
            } else if (hasFolders) {
                this.container.setAttribute('data-drag-type', 'folder');
            } else if (hasFiles) {
                this.container.setAttribute('data-drag-type', 'files');
            }
        }
    }

    async handleDrop(e) {
        const items = Array.from(e.dataTransfer.items);

        if (items.length === 0) {
            throw new Error('No items dropped');
        }

        if (items[0] && 'webkitGetAsEntry' in items[0]) {
            await this.handleModernDrop(items);
        } else {
            const files = Array.from(e.dataTransfer.files);
            await this.handleLegacyDrop(files);
        }
    }

    async handleModernDrop(items) {
        const entries = items
            .map(item => item.webkitGetAsEntry())
            .filter(entry => entry !== null);

        const results = {
            success: 0,
            errors: 0
        };

        for (const entry of entries) {
            const result = await this.processEntry(entry);
            results.success += result.success;
            results.errors += result.errors;
        }

        if (this.uploadHandler && this.uploadHandler.onComplete) {
            this.uploadHandler.onComplete(results);
        }
    }

    async processEntry(entry, parentPath = '') {
        const results = { success: 0, errors: 0 };

        if (entry.isFile) {
            try {
                const file = await this.getFileFromEntry(entry);
                await this.uploadHandler.uploadFile(file);
                results.success++;
            } catch (error) {
                console.error('Error processing file:', entry.name, error);
                results.errors++;
            }
        } else if (entry.isDirectory) {
            try {
                const folderId = await this.uploadHandler.createFolder(entry.name);
                const childEntries = await this.readDirectoryEntries(entry);

                for (const childEntry of childEntries) {
                    const childResults = await this.processEntry(childEntry, `${parentPath}${entry.name}/`);
                    results.success += childResults.success;
                    results.errors += childResults.errors;
                }
            } catch (error) {
                console.error('Error processing folder:', entry.name, error);
                results.errors++;
            }
        }

        return results;
    }

    getFileFromEntry(fileEntry) {
        return new Promise((resolve, reject) => {
            fileEntry.file((file) => {
                const fileName = fileEntry.fullPath.split('/').pop();
                const correctedFile = new File([file], fileName, {
                    type: file.type,
                    lastModified: file.lastModified
                });
                resolve(correctedFile);
            }, reject);
        });
    }

    readDirectoryEntries(directoryEntry) {
        return new Promise((resolve) => {
            const dirReader = directoryEntry.createReader();
            const entries = [];

            const readEntries = () => {
                dirReader.readEntries((results) => {
                    if (results.length === 0) {
                        resolve(entries);
                    } else {
                        entries.push(...results);
                        readEntries();
                    }
                });
            };

            readEntries();
        });
    }

    async handleLegacyDrop(files) {
        const folderFiles = files.filter(file => file.webkitRelativePath);
        const regularFiles = files.filter(file => !file.webkitRelativePath);

        const results = { success: 0, errors: 0 };

        if (folderFiles.length > 0) {
            const folderStructure = buildFolderStructure(folderFiles);
            // Process folder structure
            for (const [path, files] of folderStructure) {
                try {
                    const folderId = await this.uploadHandler.createFolderPath(path);
                    for (const file of files) {
                        try {
                            await this.uploadHandler.uploadFile(file, folderId);
                            results.success++;
                        } catch (error) {
                            results.errors++;
                        }
                    }
                } catch (error) {
                    results.errors += files.length;
                }
            }
        }

        for (const file of regularFiles) {
            try {
                await this.uploadHandler.uploadFile(file);
                results.success++;
            } catch (error) {
                results.errors++;
            }
        }

        if (this.uploadHandler && this.uploadHandler.onComplete) {
            this.uploadHandler.onComplete(results);
        }
    }
}
