import { writable, derived } from 'svelte/store';
import { browser } from '$app/environment';

function createAuthStore() {
    const { subscribe, set, update } = writable({
        token: browser ? localStorage.getItem('cloudcore_token') : null,
        user: browser ? JSON.parse(localStorage.getItem('cloudcore_user') || 'null') : null,
        isAuthenticated: false
    });

    return {
        subscribe,
        login: (token, user) => {
            if (browser) {
                localStorage.setItem('cloudcore_token', token);
                localStorage.setItem('cloudcore_user', JSON.stringify(user));
            }
            set({ token, user, isAuthenticated: true });
        },
        logout: () => {
            if (browser) {
                localStorage.removeItem('cloudcore_token');
                localStorage.removeItem('cloudcore_user');
            }
            set({ token: null, user: null, isAuthenticated: false });
        },
        setUser: (user) => {
            if (browser) {
                localStorage.setItem('cloudcore_user', JSON.stringify(user));
            }
            update(state => ({ ...state, user }));
        }
    };
}

export const auth = createAuthStore();
export const isAuthenticated = derived(auth, $auth => !!$auth.token);
export const currentUser = derived(auth, $auth => $auth.user);
