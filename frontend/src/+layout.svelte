<script>
    import { onMount } from 'svelte';
    import { theme } from '$lib/stores/theme';
    import '$lib/styles/global.css';
    
    onMount(() => {
        const savedTheme = localStorage.getItem('cloudcore-theme');
        const systemPrefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        const initialTheme = savedTheme || (systemPrefersDark ? 'dark' : 'light');
        theme.set(initialTheme);
    });
    
    $: if (typeof document !== 'undefined') {
        document.documentElement.setAttribute('data-theme', $theme);
    }
</script>

<slot />
