import { useToastStore } from "../store/toastStore";
import { useAuthStore } from "../store/authStore";
import router from "../router";

// server url
const BASE_URL = import.meta.env.VITE_API_URL;

// sending and receiving protected data
export async function apiFetch(endpoint, options = {}) {
    const token = localStorage.getItem("jwt_token");

    const headers = {
        "Content-Type": "application/json",
        ...(token ? { "Authorization": `Bearer ${token}` }: {} ),
        ...options.headers,
    };

    // Deleting content type when sending media
    // and allowing the browser to set it automatically
    if (options.body instanceof FormData) {
        delete headers["Content-Type"];
    }

    const config = {...options, headers };

    try {
        const response = await fetch(`${BASE_URL}${endpoint}`, config);

        let data;

        const contentType = response.headers.get("content-type");
        if (contentType && contentType.includes("application/json")) {
            data = await response.json();
        } else {
            data = await response.text();
        }

        if (!response.ok) {
            // Prevents invalid authorization and unauthenticated sessions from accessing
            if (response.status === 401 && !endpoint.includes("/auth/")) {
                const authStore = useAuthStore();
                const wasAuthenticated = authStore.isAuthenticated || !!localStorage.getItem("jwt_token");
                await authStore.logOut();
                if (wasAuthenticated) {
                    useToastStore().error("Session expired.");
                }
                if (router.currentRoute.value.name !== 'login') {
                    router.push({ name: 'login' });
                }
            }
            // provides the cause of the error
            const errorMessage = (typeof data === "object" && data.message) ? data.message : "An error occurred";
            const error = new Error(errorMessage);
            error.status = response.status;
            throw error;
        }
        // success
        return data;
    } catch (error) {
        if (error.name === "TypeError") {
            useToastStore().error("Server is unresponsive.");
            throw new Error("Server is unresponsive.");
        }
        throw error;
    }
}

// this allows the client to retreive media files from the server
// because apiFetch() doesn't allow media transfer or blob responses
// this allows downloadable media files
export async function apiFetchBlob(endpoint, options = {}) {
    const token = localStorage.getItem("jwt_token");

    const headers = {
        ...(token ? { "Authorization": `Bearer ${token}` } : {}),
        ...options.headers,
    };

    const config = { ...options, headers };
    const response = await fetch(`${BASE_URL}${endpoint}`, config);

    if (!response.ok) {
         // Prevents invalid authorization and unauthenticated sessions from accessing
        if (response.status === 401 && !endpoint.includes("/auth/")) {
            const authStore = useAuthStore();
            const wasAuthenticated = authStore.isAuthenticated || !!localStorage.getItem("jwt_token");
            await authStore.logOut();
            if (wasAuthenticated) {
                useToastStore().error("Session expired.");
            }
            if (router.currentRoute.value.name !== 'login') {
                router.push({ name: 'login' });
            }
        }
        throw new Error("Failed to download file.");
    }
    // success, returns media file
    return await response.blob();
}