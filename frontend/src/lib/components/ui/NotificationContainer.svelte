<script>
    import { notifications } from '$lib/stores/notifications';
    import Notification from './Notification.svelte';
</script>

<div class="notification-container">
    {#each $notifications as notification (notification.id)}
        <Notification
            type={notification.type}
            title={notification.title}
            message={notification.message}
            duration={notification.duration}
            on:close={() => notifications.remove(notification.id)}
        />
    {/each}
</div>

<style>
    .notification-container {
        position: fixed;
        top: 80px;
        right: 24px;
        z-index: 10002;
        display: flex;
        flex-direction: column;
        gap: 12px;
        max-width: 400px;
        pointer-events: none;
    }
    
    .notification-container :global(*) {
        pointer-events: auto;
    }
</style>
