<script setup>
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from "vue";
import { useRoute, useRouter } from "vue-router";
import ChatHeader from "../../components/ChatHeader.vue";
import MessageInput from "../../components/MessageInput.vue";
import MessageContainer from "../../components/MessageContainer.vue";
import { useAuthStore } from "../../store/authStore";
import { useChatStore } from "../../store/chatStore";
import { searchUsers } from "../../services/userService";

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const chatStore = useChatStore();

const messageListRef = ref(null);
const uncontactedPartner = ref(null);

const activeMenuMessageId = ref(null);
const editingMessage = ref(null);
const replyingToMessage = ref(null);

const currentUserId = computed(() => authStore.user?.id || 0);
const activeConversation = computed(() => chatStore.activeConversation);

// identify the partner of the active conversation
const partner = computed(() => {
    if (activeConversation.value?.participants) {
        return activeConversation.value.participants.find(p => p.userId !== currentUserId.value) || activeConversation.value.participants[0];
    }
    if (uncontactedPartner.value) {
        return uncontactedPartner.value;
    }
    // Optimistic fallback on page refresh while conversation details are loading
    if (route.params.username) {
        return {
            username: route.params.username,
            avatar: null,
            isOnline: false,
            lastSeen: null
        };
    }
    return null;
});

const isChatReady = ref(false);

const isLoadingChat = computed(() => {
    return (chatStore.loadingConversations || chatStore.loadingMessages) && chatStore.messages.length === 0;
});

const alignBottomInstantly = () => {
    if (messageListRef.value) {
        messageListRef.value.scrollTop = messageListRef.value.scrollHeight;
    }
};

const scrollToBottom = () => {
    nextTick(() => {
        alignBottomInstantly();
        requestAnimationFrame(() => {
            alignBottomInstantly();
        });
    });
};

// seemlessly loading older messages when scrolling to the top
const handleScroll = async () => {
    if (!messageListRef.value || chatStore.loadingOlderMessages || !chatStore.hasMoreMessages) return;

    if (messageListRef.value.scrollTop <= 120) {
        const container = messageListRef.value;
        const prevScrollHeight = container.scrollHeight;
        const prevScrollTop = container.scrollTop;

        const count = await chatStore.loadOlderMessages();
        if (count > 0) {
            nextTick(() => {
                if (container) {
                    const diff = container.scrollHeight - prevScrollHeight;
                    container.scrollTop = prevScrollTop + diff;
                }
            });
        }
    }
};

// loading a conversation
const validateAndLoadChat = async (targetUsername) => {
    if (!targetUsername) {
        router.replace({ name: 'error', query: { code: '400', message: 'No username provided.' } });
        return;
    }

    // Prevent chatting with self
    if (authStore.user?.username && targetUsername.toLowerCase() === authStore.user.username.toLowerCase()) {
        router.replace({ name: 'error', query: { code: '400', message: 'You cannot start a chat with yourself.' } });
        return;
    }

    // Check if an active conversation exists
    const conv = await chatStore.selectConversationByUsername(targetUsername);
    if (conv) {
        uncontactedPartner.value = null;
        scrollToBottom();
        setTimeout(scrollToBottom, 50);
        nextTick(() => {
            isChatReady.value = true;
        });
        return;
    }

    // 3. Fallback: Search registered users in database for uncontacted user
    try {
        const results = await searchUsers(targetUsername, 1, 0);
        const matched = results.find(u => u.username.toLowerCase() === targetUsername.toLowerCase());

        if (matched) {
            uncontactedPartner.value = {
                userId: matched.id,
                username: matched.username,
                avatar: matched.avatar,
                isOnline: false,
                lastSeen: null
            };
            chatStore.activeConversationId = null;
            chatStore.messages = [];
            nextTick(() => {
                isChatReady.value = true;
            });
            return;
        }
    } catch (err) {
        console.error("Failed to verify user in database:", err);
    }

    router.replace({
        name: 'error',
        query: { code: '404', message: `User "${targetUsername}" not found.` }
    });
};

const handleReply = (message) => {
    replyingToMessage.value = message;
    editingMessage.value = null;
};

const handleEdit = (message) => {
    editingMessage.value = message;
    replyingToMessage.value = null;
};

const handleSaveEdit = async (newContent) => {
    if (editingMessage.value && newContent.trim()) {
        await chatStore.editUserMessage(editingMessage.value.id, newContent.trim());
        editingMessage.value = null;
    }
};

const shouldShowDateDivider = (currentMsg, index) => {
    if (index === 0) return true;
    const prevMsg = chatStore.messages[index - 1];
    if (!currentMsg.sentAt || !prevMsg.sentAt) return false;

    const currentDate = new Date(currentMsg.sentAt).toDateString();
    const prevDate = new Date(prevMsg.sentAt).toDateString();
    return currentDate !== prevDate;
};

// divide the messages by the date they were sent
const formatDateDivider = (dateString) => {
    if (!dateString) return '';
    const date = new Date(dateString);
    const now = new Date();

    const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const startOfTarget = new Date(date.getFullYear(), date.getMonth(), date.getDate());

    const diffDays = Math.round((startOfToday - startOfTarget) / (1000 * 60 * 60 * 24));

    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Yesterday';
    if (diffDays > 1 && diffDays < 7) {
        return date.toLocaleDateString(undefined, { weekday: 'long' });
    }
    return date.toLocaleDateString(undefined, { 
        month: '2-digit', 
        day: '2-digit', 
        year: '2-digit' 
    });
};

// when selecting a replied message scroll to that message
const scrollToMessage = (targetMessageId) => {
    const targetEl = document.getElementById(`message-${targetMessageId}`);
    if (targetEl) {
        targetEl.scrollIntoView({ behavior: "smooth", block: "center" });
        targetEl.classList.remove("pulse-highlight");
        void targetEl.offsetWidth;
        targetEl.classList.add("pulse-highlight");
        setTimeout(() => {
            targetEl.classList.remove("pulse-highlight");
        }, 1600);
    } else {
        console.warn(`Message #${targetMessageId} is not in current view.`);
    }
};

onMounted(async () => {
    if (route.params.username) {
        await validateAndLoadChat(route.params.username);
    }
});

// load a new conversation when the username changes
watch(() => route.params.username, async (newUsername) => {
    if (newUsername) {
        isChatReady.value = false;
        await validateAndLoadChat(newUsername);
    }
});

// load messages when the chat is ready
watch(() => chatStore.loadingMessages, (isLoading) => {
    if (!isLoading) {
        nextTick(() => {
            alignBottomInstantly();
            requestAnimationFrame(() => {
                alignBottomInstantly();
                requestAnimationFrame(() => {
                    isChatReady.value = true;
                });
            });
        });
    } else {
        isChatReady.value = false;
    }
});

// scroll to bottom when a new conversation is selected
watch(() => chatStore.activeConversationId, (newId) => {
    if (newId) {
        uncontactedPartner.value = null;
        if (!chatStore.loadingMessages) {
            nextTick(() => {
                alignBottomInstantly();
                isChatReady.value = true;
            });
        }
    }
});

// scroll to bottom when a new message is added
watch(() => chatStore.messages[chatStore.messages.length - 1]?.id, (newLastId, oldLastId) => {
    if (newLastId && newLastId !== oldLastId) {
        scrollToBottom();
    }
}, { flush: 'post' });

onUnmounted(() => {
    chatStore.activeConversationId = null;
});
</script>

<template src="./template.html"></template>
<style scoped src="./style.css"></style>