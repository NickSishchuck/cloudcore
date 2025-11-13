<script>
    import { theme } from '$lib/stores/theme';

    function toggleTheme() {
        theme.update(current => current === 'dark' ? 'light' : 'dark');
    }
</script>

<button 
    class="theme-btn" 
    on:click={toggleTheme}
    title="Toggle theme"
    aria-label="Toggle theme"
>
    <span class="material-symbols-outlined theme-icon">
        {$theme === 'dark' ? 'light_mode' : 'dark_mode'}
    </span>
</button>

<style>
    .theme-btn {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 40px;
        height: 40px;
        border: 1px solid var(--border-color);
        border-radius: 50%;
        background: var(--bg-primary);
        color: var(--text-secondary);
        cursor: pointer;
        font-size: 20px;
        transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
        outline: none;
    }

    .theme-btn:hover {
        background: var(--bg-hover);
        box-shadow: 0 2px 8px var(--shadow-sm);
        transform: rotate(20deg);
    }

    .theme-btn:hover .theme-icon {
        color: var(--color-blue);
        transform: scale(1.1);
    }

    .theme-btn:active {
        transform: rotate(20deg) scale(0.95);
    }
</style>
