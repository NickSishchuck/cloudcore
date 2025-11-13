<script>
    import { createEventDispatcher, tick } from 'svelte';
    import Modal from './Modal.svelte';
    import { t } from '$lib/stores/i18n';
    
    export let open = false;
    export let item = null;
    export let isLoading = false;
    
    const dispatch = createEventDispatcher();
    
    let newName = '';
    let inputElement;
    
    $: if (open && item) {
        newName = item.name;
        tick().then(() => {
            if (inputElement) {
                inputElement.focus();
                
                // Select filename without extension
                const lastDot = newName.lastIndexOf('.');
                if (lastDot > 0 && item.type === 'file') {
                    inputElement.setSelectionRange(0, lastDot);
                } else {
                    inputElement.select();
                }
            }
        });
    }
    
    function handleRename() {
        if (newName.trim() && newName !== item.name) {
            dispatch('rename', { item, newName: newName.trim() });
        }
    }
    
    function handleCancel() {
        dispatch('cancel');
        open = false;
    }
    
    function handleKeyDown(e) {
        if (e.key === 'Enter' && !isLoading) {
            handleRename();
        }
    }
</script>

<Modal bind:open title={$t('renameItem')} size="sm" on:close={handleCancel}>
    <label for="renameInput">{$t('newName')}</label>
    <input 
        type="text" 
        id="renameInput"
        class="rename-input"
        bind:value={newName}
        bind:this={inputElement}
        on:keydown={handleKeyDown}
        disabled={isLoading}
        autocomplete="off"
        spellcheck="false"
    />
    <p class="rename-hint">
        {item?.type === 'folder' ? $t('folderNameHint') : $t('fileNameHint')}
    </p>
    
    <svelte:fragment slot="footer">
        <button 
            class="modal-btn modal-btn-secondary" 
            on:click={handleCancel}
            disabled={isLoading}
        >
            {$t('cancel')}
        </button>
        <button 
            class="modal-btn modal-btn-primary"
            on:click={handleRename}
            disabled={isLoading || !newName.trim() || newName === item?.name}
        >
            {isLoading ? $t('processing') : $t('rename')}
        </button>
    </svelte:fragment>
</Modal>
