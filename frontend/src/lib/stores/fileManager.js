import { writable, derived } from 'svelte/store';

function createFileManagerStore() {
    const { subscribe, set, update } = writable({
        files: [],
        currentFolderId: null,
        breadcrumbPath: [],
        selectedItems: new Set(),
        isLoading: false,
        currentSection: 'mydrive',
        sortColumn: 'name',
        sortDirection: 'asc'
    });
    
    return {
        subscribe,
        setFiles: (files) => update(state => ({ ...state, files })),
        setLoading: (isLoading) => update(state => ({ ...state, isLoading })),
        setCurrentFolder: (folderId) => update(state => ({ ...state, currentFolderId: folderId })),
        setBreadcrumbPath: (path) => update(state => ({ ...state, breadcrumbPath: path })),
        setSelectedItems: (items) => update(state => ({ ...state, selectedItems: new Set(items) })),
        clearSelection: () => update(state => ({ ...state, selectedItems: new Set() })),
        toggleSelection: (itemId) => update(state => {
            const selected = new Set(state.selectedItems);
            if (selected.has(itemId)) {
                selected.delete(itemId);
            } else {
                selected.add(itemId);
            }
            return { ...state, selectedItems: selected };
        }),
        setSection: (section) => update(state => ({ ...state, currentSection: section })),
        setSort: (column, direction) => update(state => ({ 
            ...state, 
            sortColumn: column, 
            sortDirection: direction 
        })),
        reset: () => set({
            files: [],
            currentFolderId: null,
            breadcrumbPath: [],
            selectedItems: new Set(),
            isLoading: false,
            currentSection: 'mydrive',
            sortColumn: 'name',
            sortDirection: 'asc'
        })
    };
}

export const fileManager = createFileManagerStore();

export const sortedFiles = derived(fileManager, $fileManager => {
    const { files, sortColumn, sortDirection } = $fileManager;
    
    return [...files].sort((a, b) => {
        let aVal = a[sortColumn];
        let bVal = b[sortColumn];
        
        if (sortColumn === 'name') {
            aVal = aVal?.toLowerCase();
            bVal = bVal?.toLowerCase();
        }
        
        if (aVal < bVal) return sortDirection === 'asc' ? -1 : 1;
        if (aVal > bVal) return sortDirection === 'asc' ? 1 : -1;
        return 0;
    });
});

export const selectedCount = derived(fileManager, $fileManager => 
    $fileManager.selectedItems.size
);
