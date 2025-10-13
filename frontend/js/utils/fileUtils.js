export function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

export function formatDateTime(dateString) {
    if (!dateString) return '-';

    const utcString = dateString.endsWith('Z') ? dateString : dateString + 'Z';
    const localDate = new Date(utcString);
    const now = new Date();

    // If today, show time only
    if (localDate.toDateString() === now.toDateString()) {
        return localDate.toLocaleTimeString('ru-RU', {
            hour: '2-digit',
            minute: '2-digit',
            hour12: false
        });
    } else {
        // Otherwise show date
        return localDate.toLocaleDateString('ru-RU', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
    }
}

export function getFileIcon(item) {
    if (item.type === 'folder') {
        return { icon: 'folder', class: 'folder' };
    }

    const ext = item.name.split('.').pop().toLowerCase();
    const mimeType = item.mimeType || '';

    if (mimeType.startsWith('image/')) {
        return { icon: 'image', class: 'image' };
    } else if (ext === 'pdf' || mimeType === 'application/pdf') {
        return { icon: 'picture_as_pdf', class: 'pdf' };
    } else if (ext === 'html' || mimeType === 'text/html') {
        return { icon: 'code', class: 'html' };
    } else if (mimeType.startsWith('text/')) {
        return { icon: 'description', class: 'file' };
    } else {
        return { icon: 'description', class: 'file' };
    }
}


export function downloadBlob(blob, filename) {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
}

export function buildFolderStructure(files) {
    const structure = new Map();

    for (const file of files) {
        const relativePath = file.webkitRelativePath;
        const pathParts = relativePath.split('/');
        const folderPath = pathParts.slice(0, -1).join('/');

        if (!structure.has(folderPath)) {
            structure.set(folderPath, []);
        }
        
        const fileName = pathParts[pathParts.length - 1];
        const correctedFile = new File([file], fileName, {
            type: file.type,
            lastModified: file.lastModified
        });
        structure.get(folderPath).push(correctedFile);
    }

    return structure;
}

export function isWebkitDirectorySupported() {
    return 'webkitdirectory' in document.createElement('input');
}

export function sanitizeFileName(name) {
    // Remove or replace invalid characters
    return name.trim().replace(/[<>:"/\\|?*]/g, '_');
}
