<script>
    import { fly } from 'svelte/transition';
    import { createEventDispatcher, onMount } from 'svelte';
    
    export let type = 'info';
    export let title = '';
    export let message = '';
    export let duration = 5000;
    
    const dispatch = createEventDispatcher();
    
    const icons = {
        success: 'check_circle',
        error: 'error',
        warning: 'warning',
        info: 'info'
    };
    
    let progressWidth = 100;
    let interval;
    
    onMount(() => {
        if (duration > 0) {
            const step = 100 / (duration / 50);
            interval = setInterval(() => {
                progressWidth -= step;
                if (progressWidth <= 0) {
                    clearInterval(interval);
                    dispatch('close');
                }
            }, 50);
        }
        
        return () => clearInterval(interval);
    });
    
    function close() {
        dispatch('close');
    }
</script>

<div 
    class="notification {type}"
    transition:fly={{ x: 400, duration: 400 }}
    role="alert"
    aria-live="polite"
>
    <div class="notification-icon-wrapper">
        <span class="notification-icon material-symbols-outlined">{icons[type]}</span>
    </div>
    <div class="notification-content">
        {#if title}
            <div class="notification-title">{title}</div>
        {/if}
        <div class="notification-message">{message}</div>
    </div>
    <button class="notification-close" on:click={close} aria-label="Close">
        <span class="material-symbols-outlined">close</span>
    </button>
    {#if duration > 0}
        <div class="notification-progress" style="width: {progressWidth}%"></div>
    {/if}
</div>

<style>
    .notification {
        position: relative;
        background: var(--bg-primary);
        color: var(--text-primary);
        padding: 16px 20px;
        border-radius: 12px;
        box-shadow: 0 8px 24px var(--shadow-lg), 0 4px 12px var(--shadow-md);
        display: flex;
        align-items: flex-start;
        gap: 16px;
        min-width: 320px;
        max-width: 400px;
        border-left: 4px solid var(--color-blue);
        overflow: hidden;
    }
    
    .notification.success { border-left-color: var(--color-green); }
    .notification.error { border-left-color: var(--color-red); }
    .notification.warning { border-left-color: var(--color-yellow); }
    .notification.info { border-left-color: var(--color-blue); }
    
    .notification-icon-wrapper {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 40px;
        height: 40px;
        border-radius: 50%;
        flex-shrink: 0;
        background: var(--bg-secondary);
    }
    
    .notification.success .notification-icon-wrapper {
        background: rgba(52, 168, 83, 0.1);
    }
    
    .notification.error .notification-icon-wrapper {
        background: rgba(234, 67, 53, 0.1);
    }
    
    .notification.warning .notification-icon-wrapper {
        background: rgba(251, 188, 4, 0.1);
    }
    
    .notification-icon {
        font-size: 24px;
        transition: transform 0.3s;
    }
    
    .notification.success .notification-icon { color: var(--color-green); }
    .notification.error .notification-icon { color: var(--color-red); }
    .notification.warning .notification-icon { color: var(--color-yellow); }
    .notification.info .notification-icon { color: var(--color-blue); }
    
    .notification-content {
        flex: 1;
        display: flex;
        flex-direction: column;
        gap: 4px;
    }
    
    .notification-title {
        font-size: 15px;
        font-weight: 600;
        color: var(--text-primary);
        line-height: 1.4;
    }
    
    .notification-message {
        font-size: 13px;
        color: var(--text-secondary);
        line-height: 1.5;
    }
    
    .notification-close {
        background: transparent;
        border: none;
        cursor: pointer;
        padding: 6px;
        border-radius: 50%;
        color: var(--text-secondary);
        transition: all 0.2s ease;
        flex-shrink: 0;
        width: 32px;
        height: 32px;
        display: flex;
        align-items: center;
        justify-content: center;
    }
    
    .notification-close:hover {
        background: var(--bg-hover);
        color: var(--text-primary);
        transform: rotate(90deg);
    }
    
    .notification-progress {
        position: absolute;
        bottom: 0;
        left: 0;
        height: 3px;
        background: currentColor;
        opacity: 0.3;
        border-radius: 0 0 0 12px;
        transition: width 0.05s linear;
    }
</style>
