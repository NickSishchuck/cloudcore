<script>
    import { createEventDispatcher } from 'svelte';
    import { t } from '$lib/stores/i18n';
    import { fade, slide } from 'svelte/transition';
    
    export let selectedCount = 0;
    export let isTrashView = false;
    export let visible = true;
    
    const dispatch = createEventDispatcher();
    
    $: showActions = selectedCount > 0 && !isTrashView;
    $: showTrashActions = selectedCount > 0 && isTrashView;
    $: canRename = selectedCount === 1;
</script>

{#if visible}
    <div class="toolbar" class:visible transition:slide={{ duration: 200 }}>
        <div class="toolbar-left">
            <!-- New Folder Button (always visible when not in trash) -->
            {#if !isTrashView}
                <button 
                    class="toolbar-btn" 
                    id="newFolderBtn"
                    on:click={() => dispatch('newFolder')}
                >
                    <span class="material-symbols-outlined">create_new_folder</span>
                    <span>{$t('newFolder')}</span>
                </button>
            {/if}
            
            <!-- Empty Trash Button (trash view only) -->
            {#if isTrashView}
                <button 
                    class="toolbar-btn toolbar-btn-danger" 
                    id="emptyTrashBtn"
                    on:click={() => dispatch('emptyTrash')}
                >
                    <span class="material-symbols-outlined">delete_sweep</span>
                    <span>{$t('deleteAllForever')}</span>
                </button>
            {/if}
            
            <!-- Regular Actions -->
            {#if showActions}
                <div class="toolbar-actions" transition:slide={{ duration: 200, axis: 'x' }}>
                    <button 
                        class="toolbar-btn icon-btn toolbar-close-btn"
                        on:click={() => dispatch('clearSelection')}
                    >
                        <span class="material-symbols-outlined">close</span>
                    </button>
                    
                    <button class="toolbar-btn" on:click={() => dispatch('download')}>
                        <span class="material-symbols-outlined">download</span>
                        <span>{$t('download')}</span>
                    </button>
                    
                    <button class="toolbar-btn" on:click={() => dispatch('move')}>
                        <span class="material-symbols-outlined">drive_file_move</span>
                        <span>{$t('move')}</span>
                    </button>
                    
                    <button 
                        class="toolbar-btn" 
                        on:click={() => dispatch('rename')}
                        disabled={!canRename}
                    >
                        <span class="material-symbols-outlined">edit</span>
                        <span>{$t('rename')}</span>
                    </button>
                    
                    <button 
                        class="toolbar-btn toolbar-btn-danger"
                        on:click={() => dispatch('delete')}
                    >
                        <span class="material-symbols-outlined">delete</span>
                        <span>{$t('delete')}</span>
                    </button>
                    
                    <div class="toolbar-divider"></div>
                    
                    <span class="toolbar-selection-count">
                        {selectedCount} {$t('selected')}
                    </span>
                </div>
            {/if}
            
            <!-- Trash Actions -->
            {#if showTrashActions}
                <div class="toolbar-actions-trash" transition:slide={{ duration: 200, axis: 'x' }}>
                    <button 
                        class="toolbar-btn icon-btn toolbar-close-btn"
                        on:click={() => dispatch('clearSelection')}
                    >
                        <span class="material-symbols-outlined">close</span>
                    </button>
                    
                    <button class="toolbar-btn" on:click={() => dispatch('restore')}>
                        <span class="material-symbols-outlined">restore_from_trash</span>
                        <span>{$t('restore')}</span>
                    </button>
                    
                    <button 
                        class="toolbar-btn toolbar-btn-danger"
                        on:click={() => dispatch('deletePermanently')}
                    >
                        <span class="material-symbols-outlined">delete_forever</span>
                        <span>{$t('deletePermanently')}</span>
                    </button>
                    
                    <div class="toolbar-divider"></div>
                    
                    <span class="toolbar-selection-count">
                        {selectedCount} {$t('selected')}
                    </span>
                </div>
            {/if}
        </div>
    </div>
{/if}

<style>
    /* Copy toolbar styles from main.css */
</style>
