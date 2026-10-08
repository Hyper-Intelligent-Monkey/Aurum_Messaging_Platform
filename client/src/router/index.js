import { createRouter, createWebHistory } from "vue-router";
import Login from "../views/Login.vue";
import Register from "../views/Register.vue";
import { useAuthStore } from "../store/authStore";
const routes = [
    {   // contacts
        path: "/",
        redirect: () => {
            const authStore = useAuthStore();
            return authStore.user?.username ? `/${authStore.user.username}` : "/login"
        }
    },
    {   // login
        path: "/login",
        name: "login",
        component: Login,
        meta: {
            auth: false,
            guestOnly: true
        }
    },
    {   // register
        path: "/register",
        name: "register",
        component: Register,
        meta: {
            auth: false,
            guestOnly: true,
        }
    },
    {   // forgot password
        path: "/forgot-password",
        name: "forgot-password",
        component: () => import("../views/ForgotPassword.vue"),
        meta: {
            auth: false,
            guestOnly: true,
        }
    },
    {   // reset password
        path: "/reset-password",
        name: "reset-password",
        component: () => import("../views/ResetPassword.vue"),
        meta: {
            auth: false,
            guestOnly: true,
        }
    },
    {   // contacts
        path: "/:username",
        name: "contacts",
        component: () => import("../views/Contacts/Contacts.vue"),
        meta: {
            auth: true,
            showNavbar: true
        }
    },
    {   // profile of the current user
        path: "/:username/profile",
        name: "profile",
        component: () => import("../views/Profile.vue"),
        meta: {
            auth: true,
            showNavbar: true
        }
    },
    {   // chat with another user
        path: "/chat/:username",
        name: "chat",
        component: () => import("../views/Chat/index.vue"),
        meta: { 
            auth: true, 
            showNavbar: false 
        }
    },
    {   // error page
        path: "/error",
        name: "error",
        component: () => import("../views/NotFound.vue"),
        meta: {
            auth: false,
            showNavbar: false
        }
    },
    {   // catch-all route
        path: "/:pathMatch(.*)*",
        redirect: { name: "error", query: { code: "404", message: "Page not found." } }
    },
    {   // chat menu
        path: "/menu/:username",
        name: "chat-menu",
        component: () => import("../views/ChatMenu.vue"),
        meta: {
            auth: true,
            showNavbar: false
        }
    }
];
const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes
});

// Auto-inject username  of the current user for user-scoped routes when omitted
function injectDefaultParams(to) {
    if (typeof to === 'string') return to;
    if (to && typeof to === 'object' && to.name && (!to.params || !to.params.username)) {
        if (to.name === 'contacts' || to.name === 'profile') {
            const authStore = useAuthStore();
            const username = authStore.user?.username;
            if (username) {
                return {
                    ...to,
                    params: {
                        ...to.params,
                        username
                    }
                };
            }
        }
    }
    return to;
}

const originalPush = router.push.bind(router); 
const originalReplace = router.replace.bind(router);
const originalResolve = router.resolve.bind(router);

// Navigates to the route and adds a new entry to browser history
router.push = (to) => originalPush(injectDefaultParams(to));
// Navigates to the route and replaces the current history entry
router.replace = (to) => originalReplace(injectDefaultParams(to));
// Generates the URL/route object without navigating
router.resolve = (to, currentLocation) => originalResolve(injectDefaultParams(to), currentLocation);
  
let isInitialAuthChecked = false;

router.beforeEach(async (to, from, next) => {
    const authStore = useAuthStore();
    // Check if the user is authenticated
    if (!isInitialAuthChecked && authStore.token) {
        isInitialAuthChecked = true;
        await authStore.checkAuth();
    }

    // Ensure websocket connection
    if (authStore.isAuthenticated) {
        authStore.ensureSignalRConnected();
    }

    const isAuthenticated = authStore.isAuthenticated && !!authStore.user;
    const currentUsername = authStore.user?.username;
    const isAuthRequired = to.matched.some(record => record.meta.auth);
    const isGuestOnly = to.matched.some(record => record.meta.guestOnly);

    // Not authenticated user are redirection to login
    if (isAuthRequired && !isAuthenticated) {
        return next({ name: "login" });
    }

    // Authenticated users are redirected to their contacts if visiting login or register
    if (isGuestOnly && isAuthenticated && currentUsername) {
        return next({ name: "contacts", params: { username: currentUsername } });
    }

    // Direct access to reset-password is not allowed without a verified email from forgot-password
    if (to.name === "reset-password") {
        const resetEmail = sessionStorage.getItem("reset_email");
        if (!resetEmail) {
            return next({ name: "forgot-password" });
        }
    }

    // enforce username paths for contacts and profile
    if (isAuthenticated && currentUsername && (to.name === 'contacts' || to.name === 'profile')) {
        if (!to.params.username || to.params.username !== currentUsername) {
            return next({ name: to.name, params: { ...to.params, username: currentUsername } });
        }
    }

    return next();
});
export default router;