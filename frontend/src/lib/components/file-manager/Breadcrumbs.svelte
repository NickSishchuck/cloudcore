<script>
    import { createEventDispatcher } from 'svelte';
    import { t } from '$lib/stores/i18n';
    
    export let path = [];
    export let isTrashView = false;
    
    const dispatch = createEventDispatcher();
    
    function handleClick(index) {
        dispatch('navigate', index);
    }
    
    function handleDragOver(e, index) {
        e.preventDefault();
        e.currentTarget.classList.add('drag-over-breadcrumb');
    }
    
    function handleDragLeave(e) {
        e.currentTarget.classList.remove('drag-over-breadcrumb');
    }
    
    function handleDrop(e, index) {
        e.preventDefault();
        e.currentTarget.classList.remove('drag-over-breadcrumb');
        
        const draggedIds = JSON.parse(e.dataTransfer.getData('text/plain'));
        const targetFolderId = index === -1 ? null : path[index].id;
        
        dispatch('move', { itemIds: draggedIds, targetFolderId });
    }
</script>

<div class="breadcrumbs" id="breadcrumbs">
    {#if isTrashView}
        <span class="breadcrumb current">{$t('trash')}</span>
    {:else}
        <button 
            class="breadcrumb"
            class:current={path.length === 0}
            on:click={() => handleClick(-1)}
            on:dragover={(e) => handleDragOver(e, -1)}
            on:dragleave={handleDragLeave}
            on:drop={(e) => handleDrop(e, -1)}
        >
            {$t('myDrive')}
        </button>
        
        {#each path as folder, index}
            <span class="breadcrumb-separator"> > </span>
            <button 
                class="breadcrumb"
                class:current={index === path.length - 1}
                on:click={() => handleClick(index)}
                on:dragover={(e) => handleDragOver(e, index)}
                on:dragleave={handleDragLeave}
                on:drop={(e) => handleDrop(e, index)}
            >
                {folder.name}
            </button>
        {/each}
    {/if}
</div>

<style>
    /* Copy breadcrumb styles from main.css */
</style>
