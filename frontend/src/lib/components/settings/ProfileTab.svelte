<script>
    import { t } from '$lib/stores/i18n';
    import { formatFileSize } from '$lib/utils/fileUtils';
    
    export let user = {};
    export let storage = {};
    
    $: storagePercent = storage.percentageUsed || 0;
    $: storageColor = storagePercent >= 90 ? '#dc3545' : 
                      storagePercent >= 75 ? '#ffc107' : '#28a745';
</script>

<div class="profile-header">
    <div class="profile-avatar">
        <span class="material-symbols-outlined">account_circle</span>
    </div>
    <h2 class="profile-name">{$t('welcomeBack', { username: user.username })}</h2>
</div>

<div class="profile-info-grid">
    <!-- Email Card -->
    <div class="info-card">
        <div class="info-card-icon">
            <span class="material-symbols-outlined">mail</span>
        </div>
        <div class="info-card-content">
            <span class="info-card-label">{$t('email')}</span>
            <strong class="info-card-value">{user.email || '-'}</strong>
        </div>
    </div>
    
    <!-- Plan Card -->
    <div class="info-card plan-card-info" data-plan-status={storage.plan || 'free'}>
        <div class="info-card-icon">
            <span class="material-symbols-outlined">workspace_premium</span>
        </div>
        <div class="info-card-content">
            <span class="info-card-label">{$t('currentPlan')}</span>
            <span class="plan-status-badge">{storage.plan || 'Free'}</span>
        </div>
    </div>
    
    <!-- Storage Card -->
    <div class="info-card storage-card">
        <div class="info-card-icon">
            <span class="material-symbols-outlined">cloud</span>
        </div>
        <div class="info-card-content">
            <span class="info-card-label">{$t('storageUsed')}</span>
            <div class="storage-bar">
                <div 
                    class="storage-bar-fill"
                    style="width: {storagePercent}%; background-color: {storageColor}"
                ></div>
            </div>
            <strong class="info-card-value storage-info">
                {formatFileSize(storage.usedMb * 1024 * 1024)} of {formatFileSize(storage.limitMb * 1024 * 1024)}
            </strong>
        </div>
    </div>
</div>

<style>
    /* Copy profile styles from settings.css */
</style>
