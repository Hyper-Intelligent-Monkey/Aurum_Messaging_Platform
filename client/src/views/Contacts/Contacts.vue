<script setup>
import { ref, watch, onMounted, onUnmounted } from "vue";
import { useRouter, useRoute } from "vue-router";
import { MagnifyingGlassIcon } from "@heroicons/vue/24/solid";
import HomeContactsView from "./HomeContactsView.vue";
import SearchView from "./SearchView.vue";
import { useAuthStore } from "../../store/authStore.js";
import { useChatStore } from "../../store/chatStore.js";

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const chatStore = useChatStore();

const search = ref(route.query.search || "");

// Sync search query parameter to search endpoint
watch(search, (newQuery) => {
    router.replace({
        query: { ...route.query, search: newQuery.trim() || undefined }
    });
});

// select a chat or user
const selectConversation = (conversation) => {
    const currentUserId = authStore.user?.id || 0;
    const partner = conversation.participants?.find(p => p.userId !== currentUserId) || conversation.participants?.[0];
    const targetUsername = partner?.username;
    if (!targetUsername) return;

    if (conversation.id && typeof conversation.id === "number") {
        chatStore.activeConversationId = conversation.id;
        chatStore.selectConversation(conversation.id);
    }
    router.push({ name: "chat", params: { username: targetUsername } });
};

let syncTimer = null;

onMounted(async () => {
    chatStore.activeConversationId = null;
    if (authStore.isAuthenticated) {
        await chatStore.fetchConversations();
        // sync conversation list every 3s
        syncTimer = setInterval(() => {
            if (!search.value.trim()) {
                chatStore.syncConversations();
            }
        }, 3000);
    }
});

onUnmounted(() => {
    if (syncTimer) {
        clearInterval(syncTimer);
        syncTimer = null;
    }
});
</script>

<template>
    <div class="contacts d-flex flex-column h-100">
        <!-- Search Header -->
        <div class="p-3 border-bottom search-header">
            <div class="search position-relative search-container">
                <span class="position-absolute top-50 start-0 translate-middle-y ps-3">
                    <MagnifyingGlassIcon style="width: 18px; height: 18px;" class="search-icon" />
                </span>
                <input 
                    type="search" 
                    id="chat-search-input"
                    name="search-query"
                    v-model="search"
                    class="form-control rounded-pill search-input ps-5 w-100"
                    placeholder="Search users or chats..."
                    autocomplete="off"
                    autocorrect="off"
                    autocapitalize="off"
                    spellcheck="false"
                    role="searchbox"
                />
            </div>
        </div>

        <div class="flex-grow-1 overflow-y-auto min-h-0 contacts-list-scroll">
            <HomeContactsView 
                v-if="!search.trim()" 
                @select="selectConversation" 
            />
            <SearchView 
                v-else 
                :search="search" 
                @select="selectConversation" 
            />
        </div>
    </div>
</template>

<style scoped>
.contacts {
    background-color: var(--bg-sidebar);
}

.search-header {
    background-color: var(--bg-sidebar);
    border-color: var(--border-light) !important;
}

.search-container {
    width: 100%;
}

@media (min-width: 576px) {
    .search-container {
        max-width: 400px;
    }
}

.search-input {
    background-color: var(--bg-input);
    color: var(--text-primary);
    border: 1px solid var(--border-light);
    font-size: var(--font-size-sm);
}

.search-input::placeholder {
    color: var(--text-muted);
}

.search-input:focus {
    border-color: var(--accent-primary);
    box-shadow: var(--shadow-sm);
    outline: none;
}

.search-icon {
    color: var(--text-muted);
}

.min-h-0 {
    min-height: 0;
}
</style>