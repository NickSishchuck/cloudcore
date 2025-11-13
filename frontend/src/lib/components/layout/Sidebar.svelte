<script>
    import { createEventDispatcher } from 'svelte';
    import { t } from '$lib/stores/i18n';
    
    export let currentSection = 'mydrive';
    
    const dispatch = createEventDispatcher();
    
    let showNewDropdown = false;
    
    const sections = [
        { id: 'mydrive', icon: 'storage', label: 'myDrive' },
        { id: 'recent', icon: 'schedule', label: 'recent' },
        { id: 'shared', icon: 'people', label: 'shared' },
        { id: 'trash', icon: 'delete', label: 'trash' }
    ];
    
    function handleSectionClick(sectionId) {
        dispatch('sectionChange', sectionId);
    }
    
    function toggleNewDropdown() {
        showNewDropdown = !showNewDropdown;
    }
    
    function handleUploadFiles() {
        showNewDropdown = false;
        dispatch('upload');
    }
    
    function handleUploadFolder() {
        showNewDropdown = false;
        dispatch('uploadFolder');
    }
</script>

<div class="sidebar">
    <!-- New dropdown -->
    <div class="new-dropdown">
        <button 
            class="new-button"
            class:active={showNewDropdown}
            on:click={toggleNewDropdown}
        >
            <span class="material-symbols-outlined">add</span>
            <span>{$t('new')}</span>
            <span class="material-symbols-outlined dropdown-arrow">expand_more</span>
        </button>
        
        {#if showNewDropdown}
            <div class="dropdown-content show">
                <div class="dropdown-item" on:click={handleUploadFiles}>
                    <span class="material-symbols-outlined">upload_file</span>
                    <span>{$t('uploadFiles')}</span>
                </div>
                <div class="dropdown-item" on:click={handleUploadFolder}>
                    <span class="material-symbols-outlined">drive_folder_upload</span>
                    <span>{$t('uploadFolder')}</span>
                </div>
            </div>
        {/if}
    </div>
    
    <!-- Navigation items -->
    {#each sections.slice(0, 3) as section}
        <button
            class="sidebar-item"
            class:active={currentSection === section.id}
            on:click={() => handleSectionClick(section.id)}
        >
            <span class="material-symbols-outlined">{section.icon}</span>
            <span>{$t(section.label)}</span>
        </button>
    {/each}
    
    <div class="sidebar-divider"></div>
    
    <!-- Trash -->
    <button
        class="sidebar-item"
        class:active={currentSection === 'trash'}
        on:click={() => handleSectionClick('trash')}
    >
        <span class="material-symbols-outlined">delete</span>
        <span>{$t('trash')}</span>
    </button>
</div>

<style>
    /* Copy sidebar styles from main.css */
</style>
