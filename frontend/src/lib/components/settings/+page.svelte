<script>
    import { onMount } from 'svelte';
    import { goto } from '$app/navigation';
    import { auth, currentUser, isAuthenticated } from '$lib/stores/auth';
    import { t } from '$lib/stores/i18n';
    import { api } from '$lib/services/api';
    import { notifications } from '$lib/stores/notifications';
    import ProfileTab from '$lib/components/settings/ProfileTab.svelte';
    import SubscriptionTab from '$lib/components/settings/SubscriptionTab.svelte';
    import SecurityTab from '$lib/components/settings/SecurityTab.svelte';
    
    let activeTab = 'profile';
    let storage = {};
    let isLoading = true;
    
    const tabs = [
        { id: 'profile', icon: 'person', label: 'profile' },
        { id: 'subscription', icon: 'subscriptions', label: 'subscription' },
        { id: 'security', icon: 'security', label: 'security' }
    ];
    
    onMount(async () => {
        if (!$isAuthenticated) {
            goto('/login');
            return;
        }
        
        await loadStorageInfo();
    });
    
    async function loadStorageInfo() {
        isLoading = true;
        try {
            storage = await api.getPersonalStorage($currentUser.id);
        } catch (error) {
            console.error('Error loading storage:', error);
            notifications.error($t('operationFailed'));
        } finally {
            isLoading = false;
        }
    }
    
    function handleBack() {
        goto('/');
    }
    
    function handleLogout() {
        auth.logout();
        notifications.success($t('signedOut'));
        goto('/login');
    }
</script>

<svelte:head>
    <title>Settings - CloudCore Drive</title>
</svelte:head>

<div class="header">
    <button id="backBtn" on:click={handleBack}>
        <span class="material-symbols-outlined">arrow_back</span>
    </button>
    
    <div class="header-controls">
        <LanguageToggle />
        <ThemeToggle />
    </div>
    
    <h1>{$t('settings')}</h1>
</div>

<div class="settings-container">
    <div class="sidebar-wrapper">
        <nav class="sidebar">
            {#each tabs as tab}
                <button
                    class="sidebar-item"
                    class:active={activeTab === tab.id}
                    on:click={() => activeTab = tab.id}
                >
                    <span class="material-symbols-outlined">{tab.icon}</span>
                    <span>{$t(tab.label)}</span>
                </button>
            {/each}
        </nav>
        
        <button class="logout-btn" on:click={handleLogout}>
            {$t('signOut')}
        </button>
    </div>
    
    <div class="tab-contents">
        {#if activeTab === 'profile'}
            <ProfileTab user={$currentUser} {storage} />
        {:else if activeTab === 'subscription'}
            <SubscriptionTab currentPlan={storage.plan} />
        {:else if activeTab === 'security'}
            <SecurityTab />
        {/if}
    </div>
</div>

<style>
    @import '$lib/styles/settings.css';
</style>
