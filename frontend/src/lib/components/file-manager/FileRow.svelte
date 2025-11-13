<script>
    import { createEventDispatcher } from 'svelte';
    import { formatFileSize, formatDateTime, getFileIcon } from '$lib/utils/fileUtils';
    
    export let item;
    export let selected = false;
    export let isTrashView = false;
    
    const dispatch = createEventDispatcher();
    
    let dragOver = false;
    
    $: icon = getFileIcon(item);
    $: sizeDisplay = item.type === 'file' && item.fileSize 
        ? formatFileSize(item.fileSize) 
        : '-';
    
    function handleDragEnter(e) {
        if (item.type === 'folder') {
            e.preventDefault();
            dragOver = true;
        }
    }
    
    function handleDragLeave() {
        dragOver = false;
    }
    
    function handleDragOver(e) {
        if (item.type === 'folder') {
            e.preventDefault();
        }
    }
    
    function handleDrop(e) {
        dragOver = false;
        if (item.type === 'folder') {
            dispatch('drop', e);
        }
    }
    
    function handleContextMenu(e) {
        e.preventDefault();
        dispatch('contextmenu', { event: e, item });
    }
</script>

<tr 
    class="file-list-row"
    class:selected
    class:trash-mode={isTrashView}
    class:drag-over={dragOver}
    data-item-id={item.id}
    data-item-type={item.type}
    draggable={!isTrashView}
    on:click={(e) => dispatch('click', e)}
    on:dblclick
    on:dragstart={(e) => dispatch('dragstart', e)}
    on:dragenter={handleDragEnter}
    on:dragleave={handleDragLeave}
    on:dragover={handleDragOver}
    on:drop={handleDrop}
    on:contextmenu={handleContextMenu}
>
    <td class="col-indicator"></td>
    <td>
        <span class="file-list-icon {icon.class} material-symbols-outlined">
            {icon.icon}
        </span>
        {item.name}
    </td>
    <td>{formatDateTime(item.createdAt)}</td>
    <td>{formatDateTime(item.updatedAt)}</td>
    <td>{sizeDisplay}</td>
</tr>

<style>
    /* Copy file-list-row styles from main.css */
</style>
