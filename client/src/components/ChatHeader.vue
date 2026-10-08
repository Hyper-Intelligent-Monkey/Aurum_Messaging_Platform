<script setup>
import { computed, ref, watch } from "vue";
import { useRouter } from "vue-router";
import { ArrowLeftIcon, EllipsisVerticalIcon, UserIcon } from "@heroicons/vue/24/solid";
import { getFileUrl } from "../services/mediaService";
import { useChatStore } from "../store/chatStore";

const router = useRouter();
const chatStore = useChatStore();

const props = defineProps({
    partner: {
        type: Object,
        default: () => null
    },
    title: {
        type: String,
        default: ""
    }
});

const emit = defineEmits(["options"]);

const hasImageError = ref(false);

// Reset error state whenever partner or avatar changes
watch(() => props.partner?.avatar, () => {
    hasImageError.value = false;
});

// Determine whether an avatar image should be rendered
const hasAvatar = computed(() => {
    const av = props.partner?.avatar;
    if (!av || av === 'defaults/avatar.png' || av === 'default_avatar.png') {
        return false;
    }
    return !hasImageError.value;
});

// Determine avatar URL
const avatarUrl = computed(() => {
    if (props.partner?.avatar) {
        return getFileUrl(props.partner.avatar);
    }
    return null;
});

// display recepient name
const displayName = computed(() => {
    return props.title || props.partner?.username || "Chat";
});

// display last seen time
const formattedLastSeen = computed(() => {
    if (!props.partner?.lastSeen) return null;
    const date = new Date(props.partner.lastSeen);
    if (isNaN(date.getTime())) return null;

    const now = new Date();
    const timeStr = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    // Compare calendar dates in local time
    const isToday = date.toDateString() === now.toDateString();

    const yesterday = new Date(now);
    yesterday.setDate(now.getDate() - 1);
    const isYesterday = date.toDateString() === yesterday.toDateString();

    if (isToday) {
        return `today at ${timeStr}`;
    }
    if (isYesterday) {
        return `yesterday at ${timeStr}`;
    }

    const diffDays = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24));
    if (diffDays < 7) {
        const weekday = date.toLocaleDateString([], { weekday: 'long' });
        return `${weekday} at ${timeStr}`;
    }

    return `${date.toLocaleDateString([], { month: 'short', day: 'numeric' })} at ${timeStr}`;
});

const handleBack = () => {
    router.push({ name: 'contacts' });
};

// navigate to chat menu of the recepient
const navigateToMenu = () => {
    if (props.partner?.username && chatStore.activeConversationId) {
        router.push({ name: 'chat-menu', params: { username: props.partner.username } });
    }
};
</script>

<template>
    <header class="chat-header-bar px-3 d-flex flex-row justify-content-between align-items-center">
        <!-- Left Side: Back button + Avatar + Details -->
        <div class="d-flex align-items-center gap-2 min-w-0">
            <button @click="handleBack" class="btn-icon" title="Back to chats" aria-label="Back">
                <ArrowLeftIcon style="width: 22px; height: 22px;" />
            </button>
            
            <div class="avatar-container position-relative flex-shrink-0">
                <img 
                    v-if="hasAvatar" 
                    class="avatar-img" 
                    :src="avatarUrl" 
                    :alt="displayName" 
                    @error="hasImageError = true"
                />
                <div v-else class="avatar-fallback d-flex align-items-center justify-content-center">
                    <UserIcon style="width: 22px; height: 22px;" class="user-icon" />
                </div>
            
                <span 
                    v-if="props.partner?.isOnline" 
                    class="status-indicator indicator-online" 
                    title="Online"
                ></span>
                <span 
                    v-else-if="props.partner?.lastSeen" 
                    class="status-indicator indicator-offline" 
                    title="Offline"
                ></span>
            </div>
            
            <div class="user-details d-flex flex-column justify-content-center min-w-0 ms-1">
                <h6 class="contact-name m-0 text-truncate">{{ displayName }}</h6>
                <!-- Status text: only shows Online or Last Seen, empty for uncontacted users -->
                <div v-if="formattedLastSeen && !props.partner?.isOnline" class="status-wrapper d-flex align-items-center">
                    <span v-if="formattedLastSeen" class="status-text text-offline text-truncate">
                        Last seen {{ formattedLastSeen }}
                    </span>
                </div>
            </div>
        </div>

        <!-- Right Side: 3 Vertical Ellipses (Only shown for contacted users) -->
        <div class="d-flex align-items-center gap-1">
            <button v-if="props.partner && chatStore.activeConversationId" @click="navigateToMenu" class="btn-icon" title="Options" aria-label="Chat options">
                <EllipsisVerticalIcon style="width: 22px; height: 22px;" />
            </button>
        </div>
    </header>
</template>

<style scoped>
.chat-header-bar {
    height: 64px;
    background-color: var(--bg-chat-header);
    border-bottom: 1px solid var(--border-light);
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
    width: 100%;
    z-index: 10;
}

.min-w-0 {
    min-width: 0;
}

.btn-icon {
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

.btn-icon:hover {
    background-color: rgba(0, 0, 0, 0.07);
}

.btn-icon:active {
    transform: scale(0.94);
}

.avatar-container {
    width: 42px;
    height: 42px;
}

.avatar-img {
    width: 100%;
    height: 100%;
    border-radius: 50%;
    object-fit: cover;
    border: 1.5px solid var(--border-light);
}

.avatar-fallback {
    width: 100%;
    height: 100%;
    border-radius: 50%;
    background-color: var(--bg-input);
    border: 1.5px solid var(--border-light);
    color: var(--text-secondary);
}

.user-icon {
    opacity: 0.8;
}

.status-indicator {
    position: absolute;
    bottom: 1px;
    right: 1px;
    width: 11px;
    height: 11px;
    border: 2px solid var(--bg-chat-header);
    border-radius: 50%;
}

.indicator-online {
    background-color: var(--status-green);
}

.indicator-offline {
    background-color: var(--status-red);
}

.contact-name {
    font-size: 1.025rem;
    font-weight: 600;
    color: var(--text-primary);
    line-height: 1.25;
}

.status-wrapper {
    margin-top: 1px;
}

.status-text {
    font-size: 0.76rem;
    line-height: 1.2;
}

.text-online {
    color: var(--status-green, #22c55e);
    font-weight: 500;
}

.text-offline {
    color: var(--text-secondary);
    opacity: 0.85;
}
</style>