<script>
    import { createEventDispatcher } from 'svelte';
    import Modal from './Modal.svelte';
    import { t } from '$lib/stores/i18n';
    
    export let open = false;
    export let title = '';
    export let message = '';
    export let confirmText = 'Confirm';
    export let cancelText = 'Cancel';
    export let type = 'info';
    
    const dispatch = createEventDispatcher();
    
    function handleConfirm() {
        dispatch('confirm');
        open = false;
    }
    
    function handleCancel() {
        dispatch('cancel');
        open = false;
    }
    
    $: buttonClass = type === 'danger' ? 'modal-btn-danger' : 'modal-btn-primary';
</script>

<Modal bind:open {title} size="sm" on:close={handleCancel}>
    <p>{message}</p>
    
    <svelte:fragment slot="footer">
        <button class="modal-btn modal-btn-secondary" on:click={handleCancel}>
            {cancelText}
        </button>
        <button class="modal-btn {buttonClass}" on:click={handleConfirm}>
            {confirmText}
        </button>
    </svelte:fragment>
</Modal>
