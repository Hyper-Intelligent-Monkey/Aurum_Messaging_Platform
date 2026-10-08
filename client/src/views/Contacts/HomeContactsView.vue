<script setup>
import { computed } from "vue";
import { ChatBubbleLeftRightIcon } from "@heroicons/vue/24/outline";
import Contact from "../../components/Contact.vue";
import LoadMoreButton from "../../components/LoadMoreButton.vue";
import { useAuthStore } from "../../store/authStore.js";
import { useChatStore } from "../../store/chatStore.js";

defineEmits(["select"]);

const authStore = useAuthStore();
const chatStore = useChatStore();

const currentUserId = computed(() => authStore.user?.id || 0);

// Default recent chats list
const recentChatsList = computed(() => {
    return chatStore.conversations || [];
});
</script>

<template>
    <div class="home-contacts-view">
        <!-- Loading State -->
        <div v-if="chatStore.loadingConversations && recentChatsList.length === 0" class="text-center p-5 text-secondary">
            <span class="spinner-border spinner-border-sm text-accent-theme me-2" role="status"></span>
            <span class="small text-muted">Loading chats...</span>
        </div>

        <!-- Contacts List View -->
        <template v-else-if="recentChatsList.length > 0">
            <TransitionGroup name="contact-pop" appear tag="div">
                <Contact 
                    v-for="(conv, index) in recentChatsList" 
                    :key="conv.id"
                    :conversation="conv"
                    :currentUserId="currentUserId"
                    :isActive="conv.id === chatStore.activeConversationId"
                    :style="{ animationDelay: `${index * 0.04}s` }"
                    @select="$emit('select', conv)"
                />
            </TransitionGroup>

            <!-- Load More Chats Button -->
            <LoadMoreButton
                :hasMore="chatStore.hasMoreConversations"
                :loading="chatStore.loadingMoreConversations"
                idleText="Load more chats"
                loadingText="Loading older chats"
                @click="chatStore.loadMoreConversations()"
            />
        </template>

        <!-- Clean Empty State; No chats yet -->
        <div v-else class="text-center p-5 text-secondary">
            <ChatBubbleLeftRightIcon class="mx-auto mb-3 opacity-50 text-accent-theme" style="width: 44px; height: 44px;" />
            <h6 class="fw-semibold text-primary-theme">No chats yet</h6>
            <p class="small text-muted mb-0">Search for registered users by username to start a conversation!</p>
        </div>
    </div>
</template>

<style scoped>
.text-accent-theme {
    color: var(--accent-primary);
}

.text-primary-theme {
    color: var(--text-primary);
}

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
</style>

