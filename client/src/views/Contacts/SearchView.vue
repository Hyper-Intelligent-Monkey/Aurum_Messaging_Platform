<script setup>
import { ref, computed, watch } from "vue";
import { ChevronDownIcon, ChevronRightIcon } from "@heroicons/vue/24/solid";
import Contact from "../../components/Contact.vue";
import LoadMoreButton from "../../components/LoadMoreButton.vue";
import { useAuthStore } from "../../store/authStore.js";
import { useChatStore } from "../../store/chatStore.js";
import { searchUsers } from "../../services/userService.js";
import { searchConversations } from "../../services/conversationService.js";

const props = defineProps({
    search: {
        type: String,
        default: ""
    }
});

defineEmits(["select"]);

const authStore = useAuthStore();
const chatStore = useChatStore();

const showContacted = ref(true);
const showUncontacted = ref(true);

const toggleContacted = () => { showContacted.value = !showContacted.value; };
const toggleUncontacted = () => { showUncontacted.value = !showUncontacted.value; };

// Active Chats Search State
const searchedContactedList = ref([]);
const isSearchingContacted = ref(false);
const loadingMoreContacted = ref(false);
const hasMoreContacted = ref(false);

// Discover Users Search State
const uncontactedUsers = ref([]);
const uncontactedOffset = ref(0);
const isSearchingDatabase = ref(false);
const loadingMoreUncontacted = ref(false);
const hasMoreUncontacted = ref(false);

const currentUserId = computed(() => authStore.user?.id || 0);

// Server-side paginated search (10 per batch)
const executeSearch = async (query) => {
    const trimmed = query?.trim() || "";
    if (trimmed) {
        isSearchingContacted.value = true;
        isSearchingDatabase.value = true;
        uncontactedOffset.value = 0;

        try {
            // Fetch initial 10 active chats matching search
            const contactedResults = await searchConversations(trimmed, 10);
            searchedContactedList.value = contactedResults || [];
            hasMoreContacted.value = (contactedResults?.length || 0) >= 10;
        } catch (err) {
            console.error("Contacted chats search failed:", err);
            searchedContactedList.value = [];
            hasMoreContacted.value = false;
        } finally {
            isSearchingContacted.value = false;
        }

        try {
            // Fetch initial 10 uncontacted users matching search from database
            const userResults = await searchUsers(trimmed, 10, 0);
            uncontactedUsers.value = (userResults || []).map(u => ({
                id: `user-${u.id}`,
                participants: [{ userId: u.id, username: u.username, avatar: u.avatar, isOnline: false, lastSeen: null }],
                lastMessage: null,
                unreadCount: 0
            }));
            hasMoreUncontacted.value = (userResults?.length || 0) >= 10;
        } catch (err) {
            console.error("Database user search failed:", err);
            uncontactedUsers.value = [];
            hasMoreUncontacted.value = false;
        } finally {
            isSearchingDatabase.value = false;
        }
    } else {
        searchedContactedList.value = [];
        hasMoreContacted.value = false;
        uncontactedUsers.value = [];
        hasMoreUncontacted.value = false;
        uncontactedOffset.value = 0;
    }
};

// Load More Active Chats
const loadMoreContacted = async () => {
    if (loadingMoreContacted.value || !hasMoreContacted.value || searchedContactedList.value.length === 0) return;
    loadingMoreContacted.value = true;
    try {
        const lastChat = searchedContactedList.value[searchedContactedList.value.length - 1];
        const cursor = lastChat?.lastMessage?.createdAt || lastChat?.createdAt;
        if (!cursor) {
            hasMoreContacted.value = false;
            return;
        }
        const results = await searchConversations(props.search.trim(), 10, cursor);
        if (results && results.length > 0) {
            searchedContactedList.value.push(...results);
            hasMoreContacted.value = results.length >= 10;
        } else {
            hasMoreContacted.value = false;
        }
    } catch (err) {
        console.error("Failed to load more contacted chats:", err);
    } finally {
        loadingMoreContacted.value = false;
    }
};

// Load More Discovered Users
const loadMoreUncontacted = async () => {
    if (loadingMoreUncontacted.value || !hasMoreUncontacted.value) return;
    loadingMoreUncontacted.value = true;
    try {
        const nextOffset = uncontactedOffset.value + 10;
        const results = await searchUsers(props.search.trim(), 10, nextOffset);
        if (results && results.length > 0) {
            const newUsers = results
                .filter(u => !uncontactedUsers.value.some(existing => existing.id === `user-${u.id}`))
                .map(u => ({
                    id: `user-${u.id}`,
                    title: null,
                    participants: [{ userId: u.id, username: u.username, avatar: u.avatar, isOnline: false, lastSeen: null }],
                    lastMessage: null,
                    unreadCount: 0
                }));
            uncontactedUsers.value.push(...newUsers);
            uncontactedOffset.value = nextOffset;
            hasMoreUncontacted.value = results.length >= 10;
        } else {
            hasMoreUncontacted.value = false;
        }
    } catch (err) {
        console.error("Failed to load more uncontacted users:", err);
    } finally {
        loadingMoreUncontacted.value = false;
    }
};

// Watch for search changes
watch(() => props.search, (newQuery) => {
    executeSearch(newQuery);
}, { immediate: true });
</script>

<template>
    <div class="search-view">
        <!-- Already Contacted -->
        <div class="mb-2">
            <div 
                @click="toggleContacted" 
                class="px-3 py-2 d-flex justify-content-between align-items-center cursor-pointer list-section-header"
            >
                <span class="fw-bold text-uppercase small">Active Chats</span>
                <ChevronDownIcon v-if="showContacted" class="header-chevron" />
                <ChevronRightIcon v-else class="header-chevron" />
            </div>

            <Transition name="section-expand" appear>
                <div v-if="showContacted">
                    <div v-if="isSearchingContacted" class="text-center p-3">
                        <div class="spinner-border spinner-border-sm text-warning" role="status"></div>
                    </div>
                    <div v-else-if="searchedContactedList.length > 0">
                        <TransitionGroup name="contact-pop" appear tag="div">
                            <Contact 
                                v-for="(conv, index) in searchedContactedList" 
                                :key="conv.id"
                                :conversation="conv"
                                :currentUserId="currentUserId"
                                :isActive="conv.id === chatStore.activeConversationId"
                                :style="{ animationDelay: `${index * 0.04}s` }"
                                @select="$emit('select', conv)"
                            />
                        </TransitionGroup>

                        <!-- Load More Active Chats Button -->
                        <LoadMoreButton
                            :hasMore="hasMoreContacted"
                            :loading="loadingMoreContacted"
                            idleText="Load more chats"
                            loadingText="Loading older chats"
                            @click="loadMoreContacted"
                        />
                    </div>
                    <div v-else class="px-3 py-2 text-secondary small fst-italic">
                        No existing chats matching "{{ search }}"
                    </div>
                </div>
            </Transition>
        </div>

        <!--  Discover Uncontacted Users  -->
        <div>
            <div 
                @click="toggleUncontacted" 
                class="px-3 py-2 d-flex justify-content-between align-items-center cursor-pointer list-section-header"
            >
                <span class="fw-bold text-uppercase small">Discover Users</span>
                <ChevronDownIcon v-if="showUncontacted" class="header-chevron" />
                <ChevronRightIcon v-else class="header-chevron" />
            </div>

            <Transition name="section-expand" appear>
                <div v-if="showUncontacted">
                    <div v-if="isSearchingDatabase" class="text-center p-3">
                        <div class="spinner-border spinner-border-sm text-warning" role="status"></div>
                    </div>
                    <div v-else-if="uncontactedUsers.length > 0">
                        <TransitionGroup name="contact-pop" appear tag="div">
                            <Contact 
                                v-for="(user, index) in uncontactedUsers" 
                                :key="user.id"
                                :conversation="user"
                                :currentUserId="currentUserId"
                                :isActive="user.id === chatStore.activeConversationId"
                                :isUncontacted="true"
                                :style="{ animationDelay: `${index * 0.04}s` }"
                                @select="$emit('select', user)"
                            />
                        </TransitionGroup>

                        <!-- Load More Discovered Users Button -->
                        <LoadMoreButton
                            :hasMore="hasMoreUncontacted"
                            :loading="loadingMoreUncontacted"
                            idleText="Load more users"
                            loadingText="Loading more users"
                            @click="loadMoreUncontacted"
                        />
                    </div>
                    <div v-else class="px-3 py-2 text-secondary small fst-italic">
                        No new users found matching "{{ search }}"
                    </div>
                </div>
            </Transition>
        </div>
    </div>
</template>

<style scoped>
.contact-pop-enter-active {
    animation: contactPopIn 0.4s cubic-bezier(0.34, 1.56, 0.64, 1) both;
}

@keyframes contactPopIn {
    0% {
        opacity: 0;
        transform: translateX(-30px) scale(0.94);
    }
    100% {
        opacity: 1;
        transform: translateX(0) scale(1);
    }
}

.contact-pop-leave-active {
    transition: all 0.2s ease-in;
}

.contact-pop-leave-to {
    opacity: 0;
    transform: translateX(-20px);
}

.section-expand-enter-active {
    transition: all 0.3s ease-out;
}

.section-expand-leave-active {
    transition: all 0.2s ease-in;
}

.section-expand-enter-from,
.section-expand-leave-to {
    opacity: 0;
    transform: translateX(-20px);
}

.list-section-header {
    background-color: rgba(0, 0, 0, 0.04);
    color: var(--text-secondary);
    border-bottom: 1px solid var(--border-subtle);
    user-select: none;
    transition: background-color 0.2s ease;
}

.list-section-header:hover {
    background-color: rgba(0, 0, 0, 0.08);
}

.header-chevron {
    width: 16px;
    height: 16px;
    color: var(--text-secondary);
}

.cursor-pointer {
    cursor: pointer;
}
</style>

