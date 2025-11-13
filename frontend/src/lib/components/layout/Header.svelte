<script>
    import { createEventDispatcher } from 'svelte';
    import { goto } from '$app/navigation';
    import { currentUser } from '$lib/stores/auth';
    import { t } from '$lib/stores/i18n';
    import ThemeToggle from '$lib/components/ui/ThemeToggle.svelte';
    import LanguageToggle from '$lib/components/ui/LanguageToggle.svelte';
    
    const dispatch = createEventDispatcher();
    
    let searchQuery = '';
    let showUserMenu = false;
    
    function handleSearch() {
        if (searchQuery.trim()) {
            dispatch('search', searchQuery.trim());
        }
    }
    
    function handleSearchInput(e) {
        if (e.key === 'Enter') {
            handleSearch();
        }
    }
    
    function toggleUserMenu() {
        showUserMenu = !showUserMenu;
    }
    
    function goToSettings() {
        showUserMenu = false;
        goto('/settings');
    }
</script>

<div class="header">
    <div class="header-left">
        <div class="logo">CloudCore Drive</div>
    </div>
    
    <div class="search-container">
        <input 
            type="text" 
            class="search-box"
            placeholder={$t('searchPlaceholder')}
            bind:value={searchQuery}
            on:keypress={handleSearchInput}
        />
        <div class="search-icon">
            <span class="material-symbols-outlined">search</span>
        </div>
    </div>
    
    <div class="header-right">
        <ThemeToggle />
        <LanguageToggle />
        
        <button 
            class="user-menu-btn"
            on:click={goToSettings}
            title={$t('settings')}
        >
            <span class="material-symbols-outlined">account_circle</span>
            <span>{$currentUser?.username || 'User'}</span>
        </button>
    </div>
</div>

<style>
    /* Copy header styles from main.css */
</style>
