import { auth } from '$lib/stores/auth';
import { get } from 'svelte/store';

const BASE_URL = 'http://localhost:5000';

class ApiClient {
    constructor() {
        this.baseUrl = BASE_URL;
    }

    getHeaders(includeAuth = true) {
        const headers = {
            'Content-Type': 'application/json'
        };

        if (includeAuth) {
            const authState = get(auth);
            if (authState.token) {
                headers['Authorization'] = `Bearer ${authState.token}`;
            }
        }

        return headers;
    }

    async handleResponse(response) {
        if (response.status === 401) {
            auth.logout();
            throw new Error('Unauthorized');
        }

        let data;
        try {
            data = await response.json();
        } catch (e) {
            data = {};
        }

        if (!response.ok) {
            const error = new Error(data.message || `HTTP ${response.status}`);
            error.errorCode = data.errorCode || data.code || null;
            error.status = response.status;
            error.data = data;
            throw error;
        }

        return data;
    }

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

    async getFiles(userId, params = {}) {
        const queryString = new URLSearchParams(params).toString();
        const url = `${this.baseUrl}/user/${userId}/mydrive?${queryString}`;

        const response = await fetch(url, {
            method: 'GET',
            headers: this.getHeaders()
        });
        return this.handleResponse(response);
    }

    async uploadFile(userId, file, parentId = null, onProgress = null) {
        const formData = new FormData();
        formData.append('file', file);
        if (parentId) {
            formData.append('parentId', parentId);
        }

        const xhr = new XMLHttpRequest();

        const uploadPromise = new Promise((resolve, reject) => {
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
                    reject(new Error(`Upload failed: ${xhr.status}`));
                }
            });

            xhr.addEventListener('error', () => reject(new Error('Network error')));
            xhr.addEventListener('abort', () => reject(new Error('Upload cancelled')));

            xhr.open('POST', `${this.baseUrl}/user/${userId}/mydrive/upload`);
            const authState = get(auth);
            if (authState.token) {
                xhr.setRequestHeader('Authorization', `Bearer ${authState.token}`);
            }
            xhr.send(formData);
        });

        uploadPromise.cancel = () => xhr.abort();
        return uploadPromise;
    }

    async createFolder(userId, name, parentId = null) {
        const response = await fetch(`${this.baseUrl}/user/${userId}/mydrive/createfolder`, {
            method: 'POST',
            headers: this.getHeaders(),
            body: JSON.stringify({ name, parentId })
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

    async renameItem(userId, itemId, newName) {
        const response = await fetch(`${this.baseUrl}/user/${userId}/mydrive/${itemId}/rename`, {
            method: 'PUT',
            headers: this.getHeaders(),
            body: JSON.stringify(newName)
        });
        return this.handleResponse(response);
    }

    async downloadFile(userId, fileId) {
        const response = await fetch(`${this.baseUrl}/user/${userId}/mydrive/${fileId}/download`, {
            method: 'GET',
            headers: { Authorization: `Bearer ${get(auth).token}` }
        });

        if (!response.ok) {
            throw new Error('Download failed');
        }

        return response.blob();
    }

    async moveItem(userId, itemId, targetFolderId) {
        const targetId = targetFolderId === null || targetFolderId === undefined ? 0 : targetFolderId;
        const response = await fetch(`${this.baseUrl}/user/${userId}/mydrive/${itemId}/move/${targetId}`, {
            method: 'POST',
            headers: this.getHeaders()
        });
        return this.handleResponse(response);
    }

    async getPersonalStorage(userId) {
        const response = await fetch(`${this.baseUrl}/user/${userId}/storage/personal`, {
            method: 'GET',
            headers: this.getHeaders()
        });
        return this.handleResponse(response);
    }
}

export const api = new ApiClient();
