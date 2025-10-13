export class ApiClient {
    constructor(baseUrl = 'http://localhost:5000') {
        this.baseUrl = baseUrl;
        this.authToken = localStorage.getItem('cloudcore_token');
    }

    setAuthToken(token) {
        this.authToken = token;
        localStorage.setItem('cloudcore_token', token);
    }

    clearAuthToken() {
        this.authToken = null;
        localStorage.removeItem('cloudcore_token');
        localStorage.removeItem('cloudcore_user');
    }

    getHeaders(includeAuth = true) {
        const headers = {
            'Content-Type': 'application/json'
        };

        if (includeAuth && this.authToken) {
            headers['Authorization'] = `Bearer ${this.authToken}`;
        }

        return headers;
    }

    async handleResponse(response) {
        if (response.status === 401) {
            this.clearAuthToken();
            throw new Error('Unauthorized');
        }

        const data = await response.json().catch(() => ({}));

        if (!response.ok) {
            throw new Error(data.message || `HTTP ${response.status}`);
        }

        return data;
    }

    // Auth endpoints
    async login(username, password) {
        const response = await fetch(`${this.baseUrl}/auth/login`, {
            method: 'POST',
            headers: this.getHeaders(false),
            body: JSON.stringify({ username, password })
        });
        return this.handleResponse(response);
    }

    async register(username, email, password) {
        const response = await fetch(`${this.baseUrl}/auth/register`, {
            method: 'POST',
            headers: this.getHeaders(false),
            body: JSON.stringify({ username, email, password })
        });
        return this.handleResponse(response);
    }

    // File/Folder endpoints
    async getFiles(userId, params = {}) {
        const queryString = new URLSearchParams(params).toString();
        const url = `${this.baseUrl}/user/${userId}/mydrive?${queryString}`;

        const response = await fetch(url, {
            method: 'GET',
            headers: this.getHeaders()
        });
        return this.handleResponse(response);
    }

    async getItemByName(userId, name, parentId)
    {
        let url = `${this.baseUrl}/user/${userId}/mydrive/get/name?name=${name}`;

         if (parentId !== undefined && parentId !== null) {
            url += `&parentId=${parentId}`;
        }
        const response = await fetch(url, {
            method: 'GET',
            headers: this.getHeaders()
        });
        return this.handleResponse(response);
    }

    async getTrash(userId, params = {}) {
        const queryString = new URLSearchParams(params).toString();
        const url = `${this.baseUrl}/user/${userId}/mydrive/trash?${queryString}`;

        const response = await fetch(url, {
            method: 'GET',
            headers: this.getHeaders()
        });
        return this.handleResponse(response);
    }

    async uploadFile(userId, file, parentId = null) {
        const formData = new FormData();
        formData.append('file', file);
        if (parentId) {
            formData.append('parentId', parentId);
        }

        const response = await fetch(`${this.baseUrl}/user/${userId}/mydrive/upload`, {
            method: 'POST',
            headers: {
                Authorization: `Bearer ${this.authToken}`
            },
            body: formData
        });
        return this.handleResponse(response);
    }

    async createFolder(userId, name, parentId = null) {
        const response = await fetch(`${this.baseUrl}/user/${userId}/mydrive/createfolder`, {
            method: 'POST',
            headers: this.getHeaders(),
            body: JSON.stringify({ name, parentId })
        });
        return this.handleResponse(response);
    }

    async downloadFile(userId, fileId) {
        const response = await fetch(`${this.baseUrl}/user/${userId}/mydrive/${fileId}/download`, {
            method: 'GET',
            headers: {
                Authorization: `Bearer ${this.authToken}`
            }
        });

        if (!response.ok) {
            throw new Error('Download failed');
        }

        return response.blob();
    }

    async downloadFolder(userId, folderId) {
        const response = await fetch(`${this.baseUrl}/user/${userId}/mydrive/${folderId}/downloadfolder`, {
            method: 'GET',
            headers: {
                Authorization: `Bearer ${this.authToken}`
            }
        });

        if (!response.ok) {
            const error = await response.json().catch(() => ({}));
            throw new Error(error.message || 'Download failed');
        }

        return response.blob();
    }

    async downloadMultipleItems(userId, itemIds) {
        const response = await fetch(`${this.baseUrl}/user/${userId}/mydrive/download/multiple`, {
            method: 'POST',
            headers: this.getHeaders(),
            body: JSON.stringify(itemIds) // Send array directly
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Failed to download items');
        }

        return await response.blob();
    }

    async renameItem(userId, itemId, newName) {
        const response = await fetch(`${this.baseUrl}/user/${userId}/mydrive/${itemId}/rename`, {
            method: 'PUT',
            headers: this.getHeaders(),
            body: JSON.stringify(newName)
        });
        return this.handleResponse(response);
    }

    async deleteItem(userId, itemId) {
        const response = await fetch(`${this.baseUrl}/user/${userId}/mydrive/${itemId}/delete`, {
            method: 'DELETE',
            headers: this.getHeaders()
        });
        return this.handleResponse(response);
    }

    async bulkDeleteItems(userId, itemIds, options = {}) {
        const {
            concurrency = 5,
            onProgress = null,
            onItemComplete = null
        } = options;

        const results = {
            succeeded: [],
            failed: [],
            total: itemIds.length,
            completedCount: 0
        };

        for (let i = 0; i < itemIds.length; i += concurrency) {
            const batch = itemIds.slice(i, i + concurrency);
            
            const batchPromises = batch.map(async (itemId) => {
                try {
                    const result = await this.deleteItem(userId, itemId);
                    results.succeeded.push({ itemId, result });
                    
                    if (onItemComplete) {
                        onItemComplete(itemId, result, null);
                    }
                    
                    return { itemId, status: 'fulfilled', result };
                } catch (error) {
                    results.failed.push({ 
                        itemId, 
                        error: error.message,
                        errorCode: error.code 
                    });
                    
                    if (onItemComplete) {
                        onItemComplete(itemId, null, error);
                    }
                    
                    return { itemId, status: 'rejected', error: error.message };
                }
            });

            await Promise.allSettled(batchPromises);
            
            results.completedCount = results.succeeded.length + results.failed.length;
            
            if (onProgress) {
                onProgress(results.completedCount, results.total, batch);
            }
        }

        return results;
    }

    async restoreItem(userId, itemId) {
        const response = await fetch(`${this.baseUrl}/user/${userId}/mydrive/${itemId}/restore`, {
            method: 'PUT',
            headers: this.getHeaders()
        });
        return this.handleResponse(response);
    }

    async bulkRestoreItems(userId, itemIds, options = {}) {
        const {
            concurrency = 5,
            onProgress = null,
            onItemComplete = null
        } = options;

        const results = {
            succeeded: [],
            failed: [],
            total: itemIds.length,
            completedCount: 0
        };

        for (let i = 0; i < itemIds.length; i += concurrency) {
            const batch = itemIds.slice(i, i + concurrency);
            
            const batchPromises = batch.map(async (itemId) => {
                try {
                    const result = await this.restoreItem(userId, itemId);
                    results.succeeded.push({ itemId, result });
                    
                    if (onItemComplete) {
                        onItemComplete(itemId, result, null);
                    }
                    
                    return { itemId, status: 'fulfilled', result };
                } catch (error) {
                    results.failed.push({ 
                        itemId, 
                        error: error.message,
                        errorCode: error.code 
                    });
                    
                    if (onItemComplete) {
                        onItemComplete(itemId, null, error);
                    }
                    
                    return { itemId, status: 'rejected', error: error.message };
                }
            });

            await Promise.allSettled(batchPromises);
            
            results.completedCount = results.succeeded.length + results.failed.length;
            
            if (onProgress) {
                onProgress(results.completedCount, results.total, batch);
            }
        }

        return results;
    }

    async getFolderPath(userId, folderId) {
        const response = await fetch(`${this.baseUrl}/user/${userId}/mydrive/folder/path/${folderId}`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        if (!response.ok) {
            throw new Error('Failed to get folder path');
        }

        return response.text();
    }

    async moveItem(userId, itemId, targetFolderId) {
        const targetId = targetFolderId === null || targetFolderId === undefined ? 0 : targetFolderId;

        const response = await fetch(`${this.baseUrl}/user/${userId}/mydrive/${itemId}/move/${targetId}`, {
            method: 'POST',
            headers: this.getHeaders()
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || errorData.title || `Failed to move item: ${response.statusText}`);
        }

        return await response.json();
    }

    async bulkMoveItems(userId, itemIds, targetFolderId, options = {}) {
        const {
            concurrency = 5,
            onProgress = null,
            onItemComplete = null
        } = options;

        const results = {
            succeeded: [],
            failed: [],
            total: itemIds.length,
            completedCount: 0
        };

        for (let i = 0; i < itemIds.length; i += concurrency) {
            const batch = itemIds.slice(i, i + concurrency);
            
            const batchPromises = batch.map(async (itemId) => {
                try {
                    const result = await this.moveItem(userId, itemId, targetFolderId);
                    results.succeeded.push({ itemId, result });
                    
                    if (onItemComplete) {
                        onItemComplete(itemId, result, null);
                    }
                    
                    return { itemId, status: 'fulfilled', result };
                } catch (error) {
                    results.failed.push({ 
                        itemId, 
                        error: error.message,
                        errorCode: error.code 
                    });
                    
                    if (onItemComplete) {
                        onItemComplete(itemId, null, error);
                    }
                    
                    return { itemId, status: 'rejected', error: error.message };
                }
            });

            await Promise.allSettled(batchPromises);
            
            results.completedCount = results.succeeded.length + results.failed.length;
            
            if (onProgress) {
                onProgress(results.completedCount, results.total, batch);
            }
        }

        return results;
    }

    uploadFileWithProgress(userId, file, parentId = null, onProgress = null) {
        const xhr = new XMLHttpRequest();

        const uploadPromise = new Promise((resolve, reject) => {
            const formData = new FormData();
            formData.append('file', file);
            if (parentId) {
                formData.append('parentId', parentId);
            }

            xhr.upload.addEventListener('progress', (e) => {
                if (e.lengthComputable && onProgress) {
                    const progress = (e.loaded / e.total) * 100;
                    onProgress(progress, e.loaded, e.total);
                }
            });

            xhr.addEventListener('load', () => {
                if (xhr.status >= 200 && xhr.status < 300) {
                    try {
                        const data = JSON.parse(xhr.responseText);
                        resolve(data);
                    } catch (error) {
                        reject(new Error('Failed to parse response'));
                    }
                } else {
                    try {
                        const errorData = JSON.parse(xhr.responseText);
                        reject(new Error(errorData.message || `Upload failed: ${xhr.status}`));
                    } catch (error) {
                        reject(new Error(`Upload failed: ${xhr.status}`));
                    }
                }
            });

            xhr.addEventListener('error', () => {
                reject(new Error('Network error during upload'));
            });

            xhr.addEventListener('abort', () => {
                reject(new Error('Upload cancelled'));
            });

            xhr.open('POST', `${this.baseUrl}/user/${userId}/mydrive/upload`);
            xhr.setRequestHeader('Authorization', `Bearer ${this.authToken}`);
            xhr.send(formData);
        });

        uploadPromise.cancel = () => {
            xhr.abort();
        };

        uploadPromise.xhr = xhr;

        return uploadPromise;
    }

    async uploadMultipleFilesWithProgress(userId, files, parentId = null, onProgress = null, onFileComplete = null) {
        const uploadPromises = [];

        for (const file of files) {
            const uploadId = `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

            const promise = this.uploadFileWithProgress(userId, file, parentId, (progress, loaded, total) => {
                if (onProgress) {
                    onProgress(uploadId, progress, loaded, total, file);
                }
            }).then((result) => {
                if (onFileComplete) {
                    onFileComplete(uploadId, file, result);
                }
                return result;
            });

            uploadPromises.push({ uploadId, promise, file });
        }

        return uploadPromises;
    }
}
