<script setup>
import { computed } from "vue";
import { useRouter, useRoute } from "vue-router";
import { ArrowLeftIcon } from "@heroicons/vue/24/solid";
import { useAuthStore } from "../store/authStore";

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();

const errorCode = computed(() => route.query.code || "404");

// error message
const errorMessage = computed(() => {
    if (route.query.message) return route.query.message;
    if (errorCode.value === "400") return "Bad Request. The request was invalid.";
    return "The page or user you are looking for does not exist.";
});

// if logged in, redirect to contacts else redirect to login
const handleReturn = () => {
    if (authStore.user?.username) {
        router.push({ name: "contacts" });
    } else {
        router.push({ name: "login" });
    }
};
</script>

<template>
    <div class="error-page d-flex flex-column align-items-center justify-content-center vh-100 p-4 text-center">
        <div class="error-card p-4 d-flex flex-column align-items-center">
            <h1 class="error-code m-0">{{ errorCode }}</h1>
            <h3 class="error-title mt-2 mb-3">Notice</h3>
            <p class="error-message mb-4">{{ errorMessage }}</p>
            <button @click="handleReturn" class="btn-return d-flex align-items-center gap-2">
                <ArrowLeftIcon style="width: 18px; height: 18px;" />
                <span>Return to Chats</span>
            </button>
        </div>
    </div>
</template>

<style scoped>
.error-page {
    background-color: var(--bg-chat-area);
}

.error-card {
    background-color: var(--bg-sidebar);
    border: 1px solid var(--border-light);
    border-radius: var(--radius-md, 12px);
    max-width: 400px;
    width: 100%;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);
}

.error-code {
    font-size: 3.5rem;
    font-weight: 800;
    color: var(--accent-primary);
    line-height: 1;
}

.error-title {
    font-size: 1.25rem;
    font-weight: 600;
    color: var(--text-primary);
}

.error-message {
    font-size: 0.95rem;
    color: var(--text-secondary);
    line-height: 1.5;
}

.btn-return {
    background-color: var(--accent-primary);
    color: var(--text-inversed);
    border: none;
    padding: 0.55rem 1.25rem;
    border-radius: var(--radius-sm, 8px);
    font-weight: 600;
    font-size: 0.9rem;
    cursor: pointer;
    transition: background-color 0.2s ease;
}

.btn-return:hover {
    background-color: var(--accent-hover);
}
</style>

