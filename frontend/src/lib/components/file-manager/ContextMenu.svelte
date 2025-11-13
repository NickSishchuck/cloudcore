<script>
    import { createEventDispatcher, onMount } from 'svelte';
    import { fade } from 'svelte/transition';
    import { t } from '$lib/stores/i18n';
    
    export let visible = false;
    export let x = 0;
    export let y = 0;
    export let item = null;
    export let isTrashView = false;
    
    const dispatch = createEventDispatcher();
    
    let menuElement;
    
    $: if (visible && menuElement) {
        adjustPosition();
    }
    
    function adjustPosition() {
        const rect = menuElement.getBoundingClientRect();
        
        if (rect.right > window.innerWidth) {
            x = window.innerWidth - rect.width - 10;
        }
        
        if (rect.bottom > window.innerHeight) {
            y = window.innerHeight - rect.height - 10;
        }
    }
    
    function handleAction(action) {
        dispatch('action', action);
    }
    
    function handleClickOutside(event) {
        if (menuElement && !menuElement.contains(event.target)) {
            dispatch('close');
        }
    }
    
    onMount(() => {
        document.addEventListener('click', handleClickOutside);
        return () => document.removeEventListener('click', handleClickOutside);
    });
</script>

<svelte:window on:scroll={() => dispatch('close')} />

{#if visible && item}
    <div 
        class="context-menu"
        bind:this={menuElement}
        style="left: {x}px; top: {y}px;"
        transition:fade={{ duration: 150 }}
    >
        {#if isTrashView}
            <div class="context-menu-item" on:click={() => handleAction('restore')}>
                <span class="material-symbols-outlined">restore_from_trash</span>
                <span>{$t('restore')}</span>
            </div>
            
            <div class="context-menu-separator"></div>
            
            <div class="context-menu-item danger" on:click={() => handleAction('delete-permanently')}>
                <span class="material-symbols-outlined">delete_forever</span>
                <span>{$t('deletePermanently')}</span>
            </div>
        {:else}
            {#if item.type === 'file'}
                <div class="context-menu-item" on:click={() => handleAction('download')}>
                    <span class="material-symbols-outlined">download</span>
                    <span>{$t('downloadFile')}</span>
                </div>
            {:else}
                <div class="context-menu-item" on:click={() => handleAction('download-folder')}>
                    <span class="material-symbols-outlined">download</span>
                    <span>{$t('downloadFolder')}</span>
                </div>
            {/if}
            
            <div class="context-menu-separator"></div>
            
            <div class="context-menu-item" on:click={() => handleAction('rename')}>
                <span class="material-symbols-outlined">edit</span>
                <span>{$t('rename')}</span>
            </div>
            
            <div class="context-menu-item" on:click={() => handleAction('move')}>
                <span class="material-symbols-outlined">drive_file_move</span>
                <span>{$t('move')}</span>
            </div>
            
            <div class="context-menu-separator"></div>
            
            <div class="context-menu-item danger" on:click={() => handleAction('delete')}>
                <span class="material-symbols-outlined">delete</span>
                <span>{$t('delete')}</span>
            </div>
        {/if}
    </div>
{/if}

<style>
    /* Copy context-menu styles from main.css */
</style>
