<script setup>
import { ref, computed, watch } from "vue";
import { storeToRefs } from "pinia";
import { useRouter, useRoute } from "vue-router";
import Logo from "../assets/MessagingPlatformLogo.png";
import { useAuthStore } from "../store/authStore";
import { getFileUrl, isAvatarFailed, markAvatarFailed, isAvatarLoaded, markAvatarLoaded } from "../services/mediaService";
import { ArrowLeftStartOnRectangleIcon, UserIcon, ArrowLeftIcon } from "@heroicons/vue/24/solid";

const authStore = useAuthStore();
const router = useRouter();
const route = useRoute();

const { user, isAuthenticated } = storeToRefs(authStore);

const isLoaded = ref(false);
const hasImageError = ref(false);

// Check if currently viewing auth pages (login/register)
const isGuestAuthPage = computed(() => {
    // available public routes
    const authRoutes = ["login", "register", "forgot-password", "reset-password"];
    // current path
    const currentPath = typeof window !== "undefined" ? window.location.pathname : "";
    // check if current route is a public route
    return authRoutes.includes(route.name) || authRoutes.some(r => currentPath.includes(`/${r}`));
});

// Check if currently on profile page
const isProfilePage = computed(() => {
    const currentPath = typeof window !== "undefined" ? window.location.pathname : "";
    return route.name === "profile" || currentPath.endsWith("/profile");
});

// Log out user
const handleLogOut = async () => {
    await authStore.logOut();
    router.push("/login");
};

// Display user info
const displayUser = computed(() => {
    if (isAuthenticated.value && user.value) {
        return user.value;
    }
    return {
        id: 0,
        username: "Guest User",
        avatar: null
    };
});

// Navigate back to contacts or login if unauthenticated
const handleBack = () => {
    if (isAuthenticated.value && displayUser.value?.username) {
        router.push({ name: 'contacts', params: { username: displayUser.value.username } });
    } else {
        router.push("/login");
    }
};

// avatar url
const avatarUrl = computed(() => {
    return displayUser.value?.avatar ? getFileUrl(displayUser.value.avatar) : "";
});

// Single definition determining whether to show the <img> or the fallback <UserIcon />
const hasAvatar = computed(() => {
    return !!displayUser.value?.avatar && !hasImageError.value;
});

// watch for avatar error during change
watch(avatarUrl, (newUrl) => {
    isLoaded.value = isAvatarLoaded(newUrl);
    hasImageError.value = isAvatarFailed(newUrl);
}, { immediate: true });

// cache loaded avatar in memory
const handleImageLoad = () => {
    isLoaded.value = true;
    markAvatarLoaded(avatarUrl.value);
};

// handles if the avatar image is an error
const handleImageError = () => {
    hasImageError.value = true;
    markAvatarFailed(avatarUrl.value);
};
</script>

<template>
    <header class="app-navbar">
        <nav class="navbar-container">
            <!-- Aurum Messaging Logo & Title -->
            <div>
                <button 
                    v-if="isProfilePage" 
                    @click="handleBack" 
                    class="btn-nav-back" 
                    title="Back to contacts"
                    aria-label="Back"
                >
                    <ArrowLeftIcon style="width: 22px; height: 22px;" />
                </button>

                <router-link 
                    v-else
                    :to="isAuthenticated && displayUser.username ? { name: 'contacts', params: { username: displayUser.username } } : { name: 'login' }" 
                    class="d-flex align-items-center gap-2 text-decoration-none"
                >
                    <img 
                        :src="Logo" 
                        alt="Aurum Messaging Logo" 
                        class="navbar-logo" 
                        width="36" 
                        height="36" 
                        fetchpriority="high" 
                    />
                    <span class="navbar-title">Aurum Messaging</span>
                </router-link>
            </div>
            
            <div v-if="!isGuestAuthPage" class="d-flex align-items-center gap-2">
                <!-- Profile -->
                <router-link 
                    v-if="!isProfilePage"
                    :to="{ name: 'profile', params: { username: displayUser.username } }" 
                    class="nav-avatar-btn text-decoration-none" 
                    :title="displayUser.username"
                >

                    <img 
                        v-if="hasAvatar" 
                        :src="avatarUrl" 
                        class="nav-avatar" 
                        :alt="displayUser.username" 
                        loading="eager" 
                        decoding="async" 
                        referrerpolicy="no-referrer" 
                        @load="handleImageLoad" 
                        @error="handleImageError" 
                    />

                    <!-- Fallback icon displayed while loading or if no avatar/error -->
                    <div v-else class="nav-avatar-fallback">
                        <UserIcon class="nav-user-icon" />
                    </div>
                </router-link>
                
                <!-- Logout  -->
                <button 
                    @click="handleLogOut" 
                    class="nav-icon-btn" 
                    title="Log Out"
                    aria-label="Log Out"
                >
                    <ArrowLeftStartOnRectangleIcon class="logout-icon" />
                </button>
            </div>
        </nav>
    </header>
</template>

<style scoped>
.btn-nav-back {
    width: 38px;
    height: 38px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: transparent;
    border: none;
    border-radius: 50%;
    color: var(--text-primary);
    cursor: pointer;
    transition: background-color 0.2s ease, transform 0.1s ease;
    flex-shrink: 0;
}

.btn-nav-back:hover {
    background-color: rgba(0, 0, 0, 0.07);
}

.btn-nav-back:active {
    transform: scale(0.94);
}

.app-navbar {
    background-color: var(--accent-light);
    height: 56px;
    min-height: 56px;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    box-shadow: var(--shadow-sm);
    z-index: 100;
}

.navbar-container {
    width: 100%;
    height: 100%;
    padding: 10px 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
}
.navbar-container div,
.navbar-container a {
    height: 100%;
}

.back-icon {
    width: 18px;
    height: 18px;
}

.navbar-logo {
    width: 36px;
    height: 36px;
    aspect-ratio: 1 / 1;
    object-fit: contain;
    flex-shrink: 0;
}
.navbar-title {
    color: var(--accent-primary);
    font-weight: 700;
    font-size: var(--font-size-lg);
    font-family: var(--font-family);
}

.nav-avatar-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 38px;
    height: 38px;
    position: relative;
    border-radius: var(--radius-full);
    transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
    flex-shrink: 0;
}

.nav-avatar {
    width: 36px;
    height: 36px;
    border-radius: var(--radius-full);
    object-fit: cover;
    border: 1.5px solid rgba(255, 255, 255, 0.8);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.12);
    transition: opacity 0.2s ease-in-out;
}

.nav-avatar-fallback {
    width: 36px;
    height: 36px;
    border-radius: var(--radius-full);
    background-color: rgba(255, 255, 255, 0.75);
    border: 1px solid var(--border-light);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.08);
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--accent-primary);
    transition: all 0.2s ease;
}

.nav-user-icon {
    width: 20px;
    height: 20px;
}

.nav-avatar-btn:hover {
    transform: translateY(-2px) scale(1.08);
}


.nav-avatar-btn:hover .nav-avatar-fallback {
    background-color: #ffffff;
    border-color: var(--accent-hover);
    color: var(--accent-hover);
    box-shadow: 0 0 4px var(--accent-hover);
}

/* Logout Action Button */
.nav-icon-btn {
    width: 36px;
    height: 36px;
    border-radius: var(--radius-full);
    border: 1px solid var(--border-light);
    background-color: rgba(255, 255, 255, 0.75);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.08);
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
}

.nav-icon-btn:hover {
    transform: translateY(-2px) scale(1.08);
    background-color: #ffffff;
    border-color: var(--status-red);
    box-shadow: 0 0 4px var(--status-red);
}

.logout-icon {
    width: 20px;
    height: 20px;
    color: var(--text-secondary);
    transition: color 0.2s ease-in-out;
}

.nav-icon-btn:hover .logout-icon {
    color: var(--status-red);
}

.btn-guest-login {
    background-color: rgba(255, 255, 255, 0.15);
    color: var(--text-white);
    padding: 0.35rem 1rem;
    border-radius: var(--radius-sm);
    text-decoration: none;
    font-size: var(--font-size-sm);
    font-weight: 600;
    transition: background-color 0.2s ease;
}

.btn-guest-login:hover {
    background-color: rgba(255, 255, 255, 0.25);
}
</style>