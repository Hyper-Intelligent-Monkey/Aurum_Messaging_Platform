<script>
import { ref } from 'vue';
// Shared active context menu across all contact instances to ensure only one is open at a time
const activeContactMenuId = ref(null);
</script>

<script setup>
import { computed, ref, watch, onMounted, onUnmounted } from 'vue';
import { UserIcon, BellSlashIcon } from '@heroicons/vue/24/solid';
import { BellIcon, NoSymbolIcon } from '@heroicons/vue/24/outline';
import { getFileUrl } from "../services/mediaService";
import { useChatStore } from "../store/chatStore";
import { blockUser, unblockUser, getBlockStatus } from "../services/userService";
import MuteModal from "./MuteModal.vue";

const props = defineProps({
    conversation: {
        type: Object,
        required: true
    },
    currentUserId: {
        type: Number,
        required: true
    },
    isActive: {
        type: Boolean,
        default: false
    },
    isUncontacted: {
        type: Boolean,
        default: false
    }
});

const emit = defineEmits(['select']);
const chatStore = useChatStore();

// Partner resolution (finds the other participant in the chat)
const partner = computed(() => {
    if (!props.conversation?.participants) return null;
    return props.conversation.participants.find(p => p.userId !== props.currentUserId) || props.conversation.participants[0];
});

// Single reactive declaration for image loading error and loaded state
const hasImageError = ref(false);
const isImageLoaded = ref(false);

// Reset image error and loading state whenever the contact or avatar changes
watch(() => partner.value?.avatar, () => {
    hasImageError.value = false;
    isImageLoaded.value = false;
});

// Single definition determining whether to show the <img> or the fallback <UserIcon />
const hasAvatar = computed(() => {
    const av = partner.value?.avatar;
    if (!av || av === 'defaults/avatar.png' || av === 'default_avatar.png') {
        return false;
    }
    return !hasImageError.value;
});

// determine avatar URL
const avatarUrl = computed(() => {
    return partner.value?.avatar ? getFileUrl(partner.value.avatar) : null;
});

// display recepient name
const displayName = computed(() => {
    return partner.value?.username;
});

// determine whether the recepient is online
const isOnline = computed(() => {
    return partner.value?.isOnline || false;
});

// formatted time displayed in the chat list
const formattedTime = computed(() => {
    const dateStr = props.conversation.lastMessage?.sentAt || props.conversation.createdAt;
    if (!dateStr) return '';

    const date = new Date(dateStr);
    const now = new Date();

    const isToday = date.toDateString() === now.toDateString();
    if (isToday) {
        return date.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit', hour12: true });
    }

    const diffMs = now - date;
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays < 7) {
        return date.toLocaleDateString([], { weekday: 'long' });
    }

    return date.toLocaleDateString([], { day: 'numeric', month: 'short', year: 'numeric' });
});

const isMuted = computed(() => {
    return !!props.conversation?.isMuted;
});

// Detect file type and return the appropriate FontAwesome icon
const lastMessageIcon = computed(() => {
    const msg = props.conversation?.lastMessage;
    if (!msg) return null;

    const type = (msg.messageType || '').toLowerCase();
    const name = (msg.originalFileName || msg.storedFileName || '').toLowerCase();
    const ct = (msg.contentType || '').toLowerCase();

    // Only show icons for media or attachment messages
    if (type === 'text' && !msg.storedFileName && !msg.originalFileName) {
        return null;
    }

    if (type === 'image' || ct.startsWith('image/') || /\.(jpg|jpeg|png|gif|webp|svg)$/i.test(name)) {
        return 'fa fa-camera';
    }
    if (type === 'video' || ct.startsWith('video/') || /\.(mp4|mov|avi|mkv|webm)$/i.test(name)) {
        return 'fa fa-video';
    }
    if (type === 'audio' || ct.startsWith('audio/') || /\.(mp3|wav|ogg|m4a|aac|opus)$/i.test(name)) {
        return 'fa fa-microphone';
    }
    if (name.endsWith('.pdf') || ct.includes('pdf')) {
        return 'fa fa-file-pdf';
    }
    if (name.match(/\.(doc|docx)$/i) || ct.includes('word')) {
        return 'fa fa-file-word';
    }
    if (type === 'document' || name.match(/\.(txt|rtf|odt|pages|ppt|pptx|xls|xlsx|csv)$/i)) {
        return 'fa fa-file-text';
    }
    return 'fa fa-paperclip';
});

// Context Menu & Long-press state
const menuRef = ref(null);
const menuPosition = ref({ x: 0, y: 0 });
const isMenuOpen = computed(() => activeContactMenuId.value === props.conversation.id);
const showMuteModal = ref(false);
const isBlocked = ref(false);
const isBlockLoading = ref(false);

// Touch long-press tracking
const touchTimer = ref(null);
const touchStartPos = ref({ x: 0, y: 0 });
const isLongPressActive = ref(false);

// open context menu durint event on the contact container
const openContextMenu = async (clientX, clientY) => {
    if (props.isUncontacted || partner.value?.isDeleted) return;

    const menuWidth = 195;
    const menuHeight = 90;
    let x = clientX;
    let y = clientY;

    if (x + menuWidth > window.innerWidth) {
        x = window.innerWidth - menuWidth - 12;
    }
    if (y + menuHeight > window.innerHeight) {
        y = window.innerHeight - menuHeight - 12;
    }
    if (x < 12) x = 12;
    if (y < 12) y = 12;

    menuPosition.value = { x, y };
    activeContactMenuId.value = props.conversation.id;

    if (partner.value?.userId) {
        try {
            const status = await getBlockStatus(partner.value.userId);
            if (status) {
                isBlocked.value = !!status.isBlockedByMe;
            }
        } catch (err) {
            console.error("Failed to check block status:", err);
        }
    }
};

// handle context menu by also preventing default behavior
const handleContextMenu = (e) => {
    e.preventDefault();
    openContextMenu(e.clientX, e.clientY);
};

// open context menu on long-press
const handleTouchStart = (e) => {
    if (props.isUncontacted || partner.value?.isDeleted) return;
    const touch = e.touches[0];
    touchStartPos.value = { x: touch.clientX, y: touch.clientY };
    isLongPressActive.value = false;

    touchTimer.value = setTimeout(() => {
        isLongPressActive.value = true;
        if (navigator.vibrate) navigator.vibrate(40);
        openContextMenu(touchStartPos.value.x, touchStartPos.value.y);
    }, 500);
};

const handleTouchMove = (e) => {
    if (!touchTimer.value) return;
    const touch = e.touches[0];
    const dx = touch.clientX - touchStartPos.value.x;
    const dy = touch.clientY - touchStartPos.value.y;
    // prevent opening context menu if the touch distance is too large
    if (Math.hypot(dx, dy) > 10) {
        clearTimeout(touchTimer.value);
        touchTimer.value = null;
    }
};

// clean up touch timer
const handleTouchEnd = () => {
    if (touchTimer.value) {
        clearTimeout(touchTimer.value);
        touchTimer.value = null;
    }
};

// handle click event where it opens the chat of the recepient
const handleClick = () => {
    if (isLongPressActive.value) {
        isLongPressActive.value = false;
        return;
    }
    if (isMenuOpen.value) {
        activeContactMenuId.value = null;
        return;
    }
    emit('select', props.conversation);
};

// open mute modal
const handleToggleMute = () => {
    activeContactMenuId.value = null;
    if (isMuted.value) {
        chatStore.unmuteUserConversation(props.conversation.id);
    } else {
        showMuteModal.value = true;
    }
};

// block or unblock the recepient
const handleToggleBlock = async () => {
    if (!partner.value?.userId || isBlockLoading.value) return;
    isBlockLoading.value = true;
    try {
        if (isBlocked.value) {
            await unblockUser(partner.value.userId);
            isBlocked.value = false;
        } else {
            await blockUser(partner.value.userId);
            isBlocked.value = true;
        }
    } catch (err) {
        console.error("Failed to toggle block status:", err);
    } finally {
        isBlockLoading.value = false;
        activeContactMenuId.value = null;
    }
};

// close context menu when clicking outside
const handleClickOutside = (e) => {
    if (isMenuOpen.value && menuRef.value && !menuRef.value.contains(e.target)) {
        activeContactMenuId.value = null;
    }
};

// close context menu when pressing escape
const handleKeydown = (e) => {
    if (e.key === "Escape" && isMenuOpen.value) {
        activeContactMenuId.value = null;
    }
};

// listen for click outside and escape key
onMounted(() => {
    document.addEventListener("click", handleClickOutside);
    window.addEventListener("keydown", handleKeydown);
    window.addEventListener("scroll", () => { activeContactMenuId.value = null; }, true);
});

// remove event listeners
onUnmounted(() => {
    document.removeEventListener("click", handleClickOutside);
    window.removeEventListener("keydown", handleKeydown);
    window.removeEventListener("scroll", () => { activeContactMenuId.value = null; }, true);
    if (touchTimer.value) clearTimeout(touchTimer.value);
});
</script>

<template>
    <div 
        @click="handleClick"
        @contextmenu.prevent="handleContextMenu"
        @touchstart.passive="handleTouchStart"
        @touchmove.passive="handleTouchMove"
        @touchend="handleTouchEnd"
        @touchcancel="handleTouchEnd"
        class="d-flex align-items-center p-3 cursor-pointer border-bottom border-secondary border-opacity-25 contact-item"
        :class="{ 'active-chat': isActive }"
    >
        <div class="position-relative me-3 flex-shrink-0 contact-avatar-wrapper">
            <!-- Falls back to default UserIcon while image is loading, or if no avatar / error -->
            <div v-show="!hasAvatar || !isImageLoaded" class="contact-avatar-fallback">
                <UserIcon class="contact-user-icon" />
            </div>

            <!-- Avatar image smoothly revealed once completely downloaded -->
            <img 
                v-if="hasAvatar" 
                v-show="isImageLoaded"
                class="rounded-circle contact-avatar" 
                :src="avatarUrl" 
                :alt="displayName"
                loading="eager"
                decoding="async"
                referrerpolicy="no-referrer"
                @load="isImageLoaded = true"
                @error="hasImageError = true" 
            />
            
            <span v-if="!isUncontacted && isOnline" 
                class="position-absolute bottom-0 end-0 rounded-circle status-dot" 
                style="background-color: var(--status-green);" title="Online"></span>
            <span v-else-if="!isUncontacted && !partner?.isDeleted" 
                class="position-absolute bottom-0 end-0 rounded-circle status-dot" 
                style="background-color: var(--status-red);" title="Offline"></span>
        </div>

        <div class="flex-grow-1 overflow-hidden min-w-0">
            <p class="m-0 text-white text-truncate fw-medium">
                {{ displayName }}
            </p>
            <div v-if="!isUncontacted" class="d-flex align-items-center text-truncate gap-1 mt-1 overflow-hidden min-w-0"
                :style="{ 
                    fontSize: 'var(--font-size-sm)',
                    color: conversation.unreadCount > 0 ? 'var(--text-primary)' : 'var(--text-muted)' 
                }">
                <span v-if="conversation.lastMessage?.senderId === currentUserId" class="fw-semibold text-accent flex-shrink-0 me-1">You: </span>
                <span v-if="conversation.lastMessage" class="text-truncate">
                    <i v-if="lastMessageIcon" :class="[lastMessageIcon, 'me-1']"></i>
                    {{ conversation.lastMessage.content || conversation.lastMessage.originalFileName || 'Attachment' }}
                </span>
                <span v-else class="fst-italic opacity-50 flex-shrink-0">No messages yet.</span>
            </div>
        </div>

        <div v-if="!isUncontacted" class="d-flex flex-column align-items-end justify-content-between ms-3 flex-shrink-0 align-self-stretch text-nowrap">
            <div class="d-flex align-items-center gap-1">
                <BellSlashIcon v-if="isMuted" class="muted-icon" title="Muted" />
                <small v-if="formattedTime" class="text-secondary opacity-75" style="font-size: 0.75rem;">
                    {{ formattedTime }}
                </small>
            </div>
            <div v-if="conversation.unreadCount > 0" class="mt-1">
                <span class="badge rounded-pill bg-warning text-dark fw-bold" style="font-size: var(--font-size-xs);">
                    {{ conversation.unreadCount }}
                    <span class="d-none d-md-inline">
                        unread message{{ conversation.unreadCount > 1 ? 's' : '' }}
                    </span>
                </span>
            </div>
        </div>

        <!-- MuteModal Integration -->
        <MuteModal 
            :is-open="showMuteModal" 
            :conversation-id="conversation.id" 
            @close="showMuteModal = false" 
        />

        <!-- Right-click / Longpress Context Menu -->
        <Teleport to="body">
            <div 
                v-if="isMenuOpen" 
                ref="menuRef"
                class="contact-context-menu shadow-lg"
                :style="{ left: `${menuPosition.x}px`, top: `${menuPosition.y}px` }"
                @click.stop
                @contextmenu.prevent
            >
                <!-- Mute / Unmute Option -->
                <button 
                    type="button" 
                    class="menu-item" 
                    @click="handleToggleMute"
                >
                    <component 
                        :is="isMuted ? BellSlashIcon : BellIcon" 
                        class="menu-item-icon" 
                        :class="isMuted ? 'text-accent' : 'text-secondary'" 
                    />
                    <span>{{ isMuted ? 'Unmute notifications' : 'Mute notifications' }}</span>
                </button>

                <!-- Divider Line -->
                <div v-if="partner && !partner.isDeleted" class="menu-divider"></div>

                <!-- Block / Unblock Option -->
                <button 
                    v-if="partner && !partner.isDeleted" 
                    type="button" 
                    class="menu-item red-text" 
                    @click="handleToggleBlock"
                    :disabled="isBlockLoading"
                >
                    <span v-if="isBlockLoading" class="spinner-border spinner-border-sm me-1" role="status"></span>
                    <NoSymbolIcon v-else class="menu-item-icon" :class="isBlocked ?  'red-text' : 'text-secondary'" />
                    <span>{{ isBlocked ? 'Unblock contact' : 'Block contact' }}</span>
                </button>
            </div>
        </Teleport>
    </div>
</template>

<style scoped>
.muted-icon {
    width: 14px;
    height: 14px;
    color: var(--status-red);
}

.contact-avatar-wrapper {
    width: 48px;
    height: 48px;
    position: relative;
    border-radius: 50%;
}

.contact-avatar {
    width: 48px;
    height: 48px;
    object-fit: cover;
    transition: opacity 0.2s ease-in-out;
}

.status-dot {
    width: 12px;
    height: 12px;
    border: 2px solid var(--bg-primary, #ffffff);
}

.contact-avatar-fallback {
    width: 48px;
    height: 48px;
    border-radius: var(--radius-full);
    background-color: rgba(0, 0, 0, 0.08);
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--accent-primary);
    border: 1px solid var(--accent-primary);
}

.contact-user-icon {
    width: 26px;
    height: 26px;
}

.contact-item {
    cursor: pointer;
    transition: background-color 0.2s ease;
    border-bottom: 1px solid var(--border-subtle);
    user-select: none;
    -webkit-user-select: none;
    -webkit-touch-callout: none;
}

.contact-item:hover {
    background-color: rgba(0, 0, 0, 0.06);
    border-left: 4px solid var(--accent-primary);
}

.active-chat {
    background-color: var(--accent-light) !important;
}

/* Contact Context Menu */
.contact-context-menu {
    position: fixed;
    z-index: 1060;
    min-width: 175px;
    background-color: #ffffff;
    border: 1px solid var(--border-light);
    border-radius: 8px;
    padding: 4px 0;
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12), 0 2px 6px rgba(0, 0, 0, 0.08);
    display: flex;
    flex-direction: column;
    animation: contextMenuPop 0.15s cubic-bezier(0.16, 1, 0.3, 1);
}

.menu-item {
    display: flex;
    align-items: center;
    gap: 10px;
    width: 100%;
    padding: 8px 14px;
    background: transparent;
    border: none;
    border-radius: 0;
    cursor: pointer;
    font-size: 0.85rem;
    color: var(--text-primary);
    transition: background-color 0.15s ease;
    text-align: left;
}

.menu-item:hover:not(:disabled) {
    background-color: rgba(0, 0, 0, 0.05);
}

.menu-item:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

.menu-item-icon {
    width: 18px;
    height: 18px;
    flex-shrink: 0;
}

.menu-divider {
    height: 1px;
    background-color: var(--border-light);
    margin: 4px 0;
}

.red-text {
    color: var(--status-red, #dc3545) !important;
}

@keyframes contextMenuPop {
    from {
        opacity: 0;
        transform: scale(0.95);
    }
    to {
        opacity: 1;
        transform: scale(1);
    }
}
</style>
