<script>
    import { locale, switchLanguage } from '$lib/stores/i18n';
</script>

<button 
    class="language-btn" 
    on:click={switchLanguage}
    title="Switch language"
    aria-label="Switch language"
>
    <span class="material-symbols-outlined">language</span>
    <span class="language-code">{$locale === 'en' ? 'EN' : 'UA'}</span>
</button>

<style>
    .language-btn {
        display: flex;
        align-items: center;
        gap: 6px;
        padding: 8px 12px;
        border: 1px solid var(--border-color);
        border-radius: 20px;
        background: var(--bg-primary);
        color: var(--text-secondary);
        cursor: pointer;
        font-size: 14px;
        height: 40px;
        transition: all 0.3s ease;
        outline: none;
    }

    .language-btn:hover {
        background: var(--bg-hover);
        box-shadow: 0 1px 3px var(--shadow-sm);
    }

    .language-btn:hover .material-symbols-outlined,
    .language-btn:hover .language-code {
        color: var(--color-blue);
    }

    .language-btn:active {
        transform: scale(0.95);
    }

    .language-code {
        font-weight: 500;
        font-size: 12px;
    }
</style>
