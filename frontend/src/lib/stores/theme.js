import { writable } from 'svelte/store';
import { browser } from '$app/environment';

const stored = browser ? localStorage.getItem('cloudcore-theme') : null;
export const theme = writable(stored || 'light');

theme.subscribe(value => {
    if (browser) {
        localStorage.setItem('cloudcore-theme', value);
        document.documentElement.setAttribute('data-theme', value);
    }
});
