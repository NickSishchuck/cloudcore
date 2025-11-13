import { writable } from 'svelte/store';

function createUploadStore() {
    const { subscribe, update } = writable([]);
    
    return {
        subscribe,
        add: (upload) => update(uploads => [...uploads, upload]),
        updateProgress: (id, progress, loaded, total) => update(uploads => 
            uploads.map(u => u.id === id ? { ...u, progress, loaded, total } : u)
        ),
        setStatus: (id, status) => update(uploads => 
            uploads.map(u => u.id === id ? { ...u, status } : u)
        ),
        remove: (id) => update(uploads => uploads.filter(u => u.id !== id)),
        clear: () => update(() => [])
    };
}

export const uploads = createUploadStore();
