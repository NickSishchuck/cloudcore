<script>
    import { createEventDispatcher, onDestroy } from 'svelte';
    import { slide } from 'svelte/transition';
    import { t } from '$lib/stores/i18n';
    import { formatFileSize } from '$lib/utils/fileUtils';
    
    export let uploads = [];
    
    const dispatch = createEventDispatcher();
    
    let minimized = false;
    
    $: visible = uploads.length > 0;
    
    function toggleMinimize() {
        minimized = !minimized;
    }
    
    function handleCancel(uploadId) {
        dispatch('cancel', uploadId);
    }
    
    function getStatusText(status) {
        const statuses = {
            uploading: $t('uploading'),
            completed: $t('uploadComplete'),
            error: $t('uploadFailed'),
            cancelled: $t('uploadCancelled')
        };
        return statuses[status] || status;
    }
</script>

{#if visible}
    <div 
        class="upload-progress-container"
        class:visible
        class:minimized
        transition:slide={{ duration: 300 }}
    >
        <div class="upload-progress-header">
            <h3>{$t('uploadingFiles')}</h3>
            <button 
                class="upload-minimize-btn"
                on:click={toggleMinimize}
                aria-label={minimized ? 'Expand' : 'Minimize'}
            >
                <span class="material-symbols-outlined">
                    {minimized ? 'expand_less' : 'expand_more'}
                </span>
            </button>
        </div>
        
        {#if !minimized}
            <div class="upload-progress-list">
                {#each uploads as upload (upload.id)}
                    <div 
                        class="upload-item"
                        class:uploading={upload.status === 'uploading'}
                        class:completed={upload.status === 'completed'}
                        class:error={upload.status === 'error'}
                        transition:slide={{ duration: 200 }}
                    >
                        <div class="upload-item-header">
                            <span class="material-symbols-outlined upload-item-icon">
                                {#if upload.status === 'uploading'}
                                    upload_file
                                {:else if upload.status === 'completed'}
                                    check_circle
                                {:else if upload.status === 'error'}
                                    error
                                {:else}
                                    cancel
                                {/if}
                            </span>
                            
                            <div class="upload-item-info">
                                <div class="upload-item-name" title={upload.fileName}>
                                    {upload.fileName}
                                </div>
                                <div class="upload-item-status">
                                    {#if upload.status === 'uploading'}
                                        {Math.round(upload.progress)}% • 
                                        {formatFileSize(upload.loaded)} / {formatFileSize(upload.total)}
                                    {:else}
                                        {getStatusText(upload.status)}
                                    {/if}
                                </div>
                            </div>
                            
                            {#if upload.status === 'uploading'}
                                <button 
                                    class="upload-item-cancel"
                                    on:click={() => handleCancel(upload.id)}
                                    aria-label="Cancel upload"
                                >
                                    <span class="material-symbols-outlined">close</span>
                                </button>
                            {/if}
                        </div>
                        
                        <div class="upload-progress-bar-container">
                            <div 
                                class="upload-progress-bar"
                                style="width: {upload.progress}%"
                            ></div>
                        </div>
                    </div>
                {/each}
            </div>
        {/if}
    </div>
{/if}

<style>
    /* Copy upload-progress-container styles from main.css */
</style>
