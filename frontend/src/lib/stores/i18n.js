import { writable, derived } from 'svelte/store';
import { browser } from '$app/environment';
import translations from '$lib/i18n/translations';

const stored = browser ? localStorage.getItem('cloudcore_language') : null;
export const locale = writable(stored || 'en');

locale.subscribe(value => {
    if (browser) {
        localStorage.setItem('cloudcore_language', value);
    }
});

export const t = derived(locale, $locale => {
    return (key, params = {}) => {
        let translation = translations[$locale]?.[key] || translations['en']?.[key] || key;
        
        Object.keys(params).forEach(param => {
            translation = translation.replace(`{${param}}`, params[param]);
        });
        
        return translation;
    };
});

export function switchLanguage() {
    locale.update(current => current === 'en' ? 'uk' : 'en');
}
