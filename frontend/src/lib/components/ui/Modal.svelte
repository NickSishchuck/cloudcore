<script>
    import { createEventDispatcher, onMount, onDestroy } from 'svelte';
    import { fade, scale } from 'svelte/transition';
    
    export let open = false;
    export let title = '';
    export let size = 'md';
    
    const dispatch = createEventDispatcher();
    
    function handleClose() {
        open = false;
        dispatch('close');
    }
    
    function handleKeyDown(e) {
        if (e.key === 'Escape' && open) {
            handleClose();
        }
    }
    
    function handleOverlayClick(e) {
        if (e.target === e.currentTarget) {
            handleClose();
        }
    }
    
    $: if (typeof document !== 'undefined') {
        if (open) {
            document.body.style.overflow = 'hidden';
        } else {
            document.body.style.overflow = '';
        }
    }
    
    onDestroy(() => {
        if (typeof document !== 'undefined') {
            document.body.style.overflow = '';
        }
    });
</script>

<svelte:window on:keydown={handleKeyDown} />

{#if open}
    <div 
        class="modal-overlay show"
        on:click={handleOverlayClick}
        transition:fade={{ duration: 300 }}
    ></div>
    
    <div 
        class="modal show modal-{size}"
        transition:scale={{ duration: 400, start: 0.9 }}
    >
        <div class="modal-header">
            <h3>{title}</h3>
            <button 
                class="modal-close"
                on:click={handleClose}
                aria-label="Close"
            >
                <span class="material-symbols-outlined">close</span>
            </button>
        </div>
        
        <div class="modal-body">
            <slot></slot>
        </div>
        
        <div class="modal-footer">
            <slot name="footer">
                <button class="modal-btn modal-btn-secondary" on:click={handleClose}>
                    Cancel
                </button>
            </slot>
        </div>
    </div>
{/if}

<style>
    /* Copy modal styles from main.css and settings.css */
    
    .modal-sm { max-width: 400px; }
    .modal-md { max-width: 540px; }
    .modal-lg { max-width: 700px; }
    .modal-xl { max-width: 900px; }
</style>
