<script>
    import { createEventDispatcher } from 'svelte';
    import FileRow from './FileRow.svelte';
    import { t } from '$lib/stores/i18n';
    
    export let files = [];
    export let isLoading = false;
    export let isTrashView = false;
    export let selectedItems = new Set();
    
    const dispatch = createEventDispatcher();
    
    let sortColumn = 'name';
    let sortDirection = 'asc';
    let draggedItems = new Set();
    
    // Selection logic
    function handleRowClick(item, event) {
        if (event.ctrlKey || event.metaKey) {
            // Multi-select
            toggleSelection(item.id);
        } else if (event.shiftKey && selectedItems.size > 0) {
            // Range select
            selectRange(item.id);
        } else {
            // Single select
            selectedItems = new Set([item.id]);
        }
        dispatch('itemsSelected', selectedItems);
    }
    
    function toggleSelection(itemId) {
        if (selectedItems.has(itemId)) {
            selectedItems.delete(itemId);
        } else {
            selectedItems.add(itemId);
        }
        selectedItems = selectedItems; // Trigger reactivity
    }
    
    function selectRange(endId) {
        const startId = Array.from(selectedItems)[selectedItems.size - 1];
        const startIndex = files.findIndex(f => f.id === startId);
        const endIndex = files.findIndex(f => f.id === endId);
        
        const [min, max] = [Math.min(startIndex, endIndex), Math.max(startIndex, endIndex)];
        
        for (let i = min; i <= max; i++) {
            selectedItems.add(files[i].id);
        }
        selectedItems = selectedItems;
    }
    
    function handleRowDoubleClick(item) {
        if (item.type === 'folder' && !isTrashView) {
            dispatch('folderOpen', item);
        } else if (item.type === 'file') {
            dispatch('download', item);
        }
    }
    
    // Sorting
    function handleSort(column) {
        if (sortColumn === column) {
            sortDirection = sortDirection === 'asc' ? 'desc' : 'asc';
        } else {
            sortColumn = column;
            sortDirection = 'asc';
        }
    }
    
    $: sortedFiles = [...files].sort((a, b) => {
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
    
    // Drag and Drop
    function handleDragStart(item, event) {
        if (!selectedItems.has(item.id)) {
            selectedItems = new Set([item.id]);
        }
        draggedItems = new Set(selectedItems);
        
        event.dataTransfer.effectAllowed = 'move';
        event.dataTransfer.setData('text/plain', JSON.stringify(Array.from(draggedItems)));
    }
    
    function handleDrop(targetItem, event) {
        event.preventDefault();
        
        if (targetItem.type !== 'folder') return;
        
        const draggedIds = JSON.parse(event.dataTransfer.getData('text/plain'));
        
        // Don't drop onto itself
        if (draggedIds.includes(targetItem.id)) return;
        
        dispatch('move', {
            itemIds: draggedIds,
            targetFolderId: targetItem.id
        });
    }
    
    // Keyboard shortcuts
    function handleKeyDown(event) {
        if (event.key === 'a' && (event.ctrlKey || event.metaKey)) {
            event.preventDefault();
            selectedItems = new Set(files.map(f => f.id));
            dispatch('itemsSelected', selectedItems);
        } else if (event.key === 'Delete' && selectedItems.size > 0) {
            event.preventDefault();
            dispatch('delete', Array.from(selectedItems));
        } else if (event.key === 'Escape') {
            selectedItems = new Set();
            dispatch('itemsSelected', selectedItems);
        }
    }
</script>

<svelte:window on:keydown={handleKeyDown} />

<div class="file-container" class:dragover={false}>
    {#if isLoading}
        <div class="skeleton-loader">
            {#each Array(10) as _, i}
                <div class="skeleton-row">
                    <div class="skeleton-icon"></div>
                    <div class="skeleton-text skeleton-name"></div>
                    <div class="skeleton-text skeleton-date"></div>
                    <div class="skeleton-text skeleton-date"></div>
                    <div class="skeleton-text skeleton-size"></div>
                </div>
            {/each}
        </div>
    {:else if files.length === 0}
        <div class="empty-state">
            <span class="material-symbols-outlined empty-icon">folder_open</span>
            <h3>{$t('emptyFolder')}</h3>
            <p>{$t('uploadGetStarted')}</p>
        </div>
    {:else}
        <table class="file-list">
            <thead class="file-list-header">
                <tr>
                    <th class="col-indicator"></th>
                    <th class="sortable" on:click={() => handleSort('name')}>
                        {$t('name')}
                        {#if sortColumn === 'name'}
                            <span class="sort-icon material-symbols-outlined">
                                {sortDirection === 'asc' ? 'arrow_upward' : 'arrow_downward'}
                            </span>
                        {/if}
                    </th>
                    <th class="sortable" on:click={() => handleSort('createdAt')}>
                        {$t('created')}
                        {#if sortColumn === 'createdAt'}
                            <span class="sort-icon material-symbols-outlined">
                                {sortDirection === 'asc' ? 'arrow_upward' : 'arrow_downward'}
                            </span>
                        {/if}
                    </th>
                    <th class="sortable" on:click={() => handleSort('updatedAt')}>
                        {$t('modified')}
                        {#if sortColumn === 'updatedAt'}
                            <span class="sort-icon material-symbols-outlined">
                                {sortDirection === 'asc' ? 'arrow_upward' : 'arrow_downward'}
                            </span>
                        {/if}
                    </th>
                    <th class="sortable" on:click={() => handleSort('fileSize')}>
                        {$t('size')}
                        {#if sortColumn === 'fileSize'}
                            <span class="sort-icon material-symbols-outlined">
                                {sortDirection === 'asc' ? 'arrow_upward' : 'arrow_downward'}
                            </span>
                        {/if}
                    </th>
                </tr>
            </thead>
            <tbody>
                {#each sortedFiles as file (file.id)}
                    <FileRow
                        item={file}
                        selected={selectedItems.has(file.id)}
                        {isTrashView}
                        on:click={(e) => handleRowClick(file, e.detail)}
                        on:dblclick={() => handleRowDoubleClick(file)}
                        on:dragstart={(e) => handleDragStart(file, e.detail)}
                        on:drop={(e) => handleDrop(file, e.detail)}
                        on:contextmenu
                    />
                {/each}
            </tbody>
        </table>
    {/if}
</div>

<style>
    /* Copy styles from frontend/css/main.css for file-container, file-list, etc. */
    /* Include: .file-container, .file-list, .file-list-header, .skeleton-loader, .empty-state */
</style>
