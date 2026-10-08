import { defineStore, acceptHMRUpdate } from 'pinia';
import { register, login, googleLogin, logout, getStoredUser, getStoredToken, getCurrentUser, storeUser } from '../services/authService';
import { signalrService } from '../services/signalrService';
import { useChatStore } from './chatStore';
import router from '../router';


export const useAuthStore = defineStore('auth', {
    state: () => ({
        user: getStoredUser(),
        token: getStoredToken(),
        registerLoading: false,
        loginLoading: false,
        googleLoading: false,
        registerError: "",
        loginError: "",
        googleError: "",
    }),

    getters: {
        // assigns true if the user is authenticated
        isAuthenticated: (state) => !!state.token,
    },

    actions: {
        // register a new user
        async registerUser(username, email, password) {
            this.registerLoading = true;
            this.registerError = "";
            try {
                const response = await register(username, email, password);
                await new Promise((resolve) => setTimeout(resolve, 1000));
                this.registerLoading = false;
                return response;
            } catch (error) {
                this.registerError = (error.status && error.status < 500) ? (error.message || "Registration failed.") : "";
                this.registerLoading = false;
                throw error;
            }
        },

        // login a user
        async loginUser(email, password) {
            this.loginLoading = true;
            this.loginError = "";
            try {
                const response = await login(email, password);
                await new Promise((resolve) => setTimeout(resolve, 2000));
                // populate the current user object
                this.user = {
                    id: response.id, 
                    username: response.username, 
                    email: response.email,
                    avatar: response.avatar || null,
                    hasPassword: response.hasPassword
                };
                // authenticates the user
                this.token = response.token;
                // store the current user object in local storage
                storeUser(this.user);
                try {
                    await signalrService.connect();
                    useChatStore().initSignalRListeners();
                } catch (sigErr) {
                    console.warn("SignalR connection failed non-fatally:", sigErr);
                }

                this.loginLoading = false;
            } catch (error) {
                this.loginError = (error.status && error.status < 500) ? (error.message || "Invalid credentials.") : "";
                this.loginLoading = false;
                throw error;
            }
        },

        // login or register with Google
        async loginWithGoogle(idToken, username = null) {
            this.googleLoading = true;
            this.googleError = "";
            try {
                const response = await googleLogin(idToken, username);

                if (response.requiresUsername) {
                    this.googleLoading = false;
                    return response;
                }
                // populate the current user object
                this.user = {
                    id: response.id,
                    username: response.username,
                    email: response.email,
                    avatar: response.avatar || null,
                    hasPassword: response.hasPassword
                };
                // authenticates the user
                this.token = response.token;
                // store the current user object in local storage
                storeUser(this.user);

                try {
                    // establish SignalR connection
                    await signalrService.connect();
                    useChatStore().initSignalRListeners();
                } catch (sigErr) {
                    console.warn("SignalR connection failed non-fatally:", sigErr);
                }

                this.googleLoading = false;
                return response;
            } catch (error) {
                this.googleError = (error.status && error.status < 500) ? error.message : "Google authentication failed.";
                this.googleLoading = false;
                throw error;
            }
        },

        // check if user is authenticated
        async checkAuth() {
            this.token = getStoredToken();
            if (this.token) {
                try {
                    // confirms that the user is authenticated
                    const profile = await getCurrentUser();
                    this.user = profile;
                    storeUser(profile);
                } catch (err) {
                    // Only log out if the server explicitly rejected the token (401 Unauthorized)
                    if (err?.status === 401) {
                        this.logOut();
                        return;
                    }
                    // Transient network errors, browser aborted requests on refresh, or server timeouts
                    // should NEVER wipe the user's session from localStorage
                    console.warn("checkAuth non-fatal profile sync error:", err);
                }

                // Establish websocket connection non-fatally
                try {
                    await signalrService.connect();
                    useChatStore().initSignalRListeners();
                } catch (sigErr) {
                    console.warn("SignalR connection attempt failed non-fatally:", sigErr);
                }
            } else {
                this.logOut();
            }
        },

        // log out the user
        async logOut() {
            logout();
            useChatStore().removeSignalRListeners();
            await signalrService.disconnect();
            this.user = null;
            this.token = null;
            await router.push({ name: "login" }); 
        },

        // Ensure SignalR connection and listeners are active whenever the user is authenticated
        async ensureSignalRConnected() {
            if (!this.token) return;
            try {
                await signalrService.connect();
                useChatStore().initSignalRListeners();
            } catch (err) {
                console.warn("SignalR connection attempt failed non-fatally:", err);
            }
        },
    },
});

// Prevents code changes from reloading the page
if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useAuthStore, import.meta.hot));
}
