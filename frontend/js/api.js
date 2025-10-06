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
            window.location.href = 'login.html';
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
                'Authorization': `Bearer ${this.authToken}`
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
                'Authorization': `Bearer ${this.authToken}`
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
                'Authorization': `Bearer ${this.authToken}`
            }
        });
        
        if (!response.ok) {
            const error = await response.json().catch(() => ({}));
            throw new Error(error.message || 'Download failed');
        }
        
        return response.blob();
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

    async restoreItem(userId, itemId) {
        const response = await fetch(`${this.baseUrl}/user/${userId}/mydrive/${itemId}/restore`, {
            method: 'PUT',
            headers: this.getHeaders()
        });
        return this.handleResponse(response);
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
}
