<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import { 
    UserIcon, 
    BellIcon, 
    BellSlashIcon, 
    NoSymbolIcon, 
    ChatBubbleLeftEllipsisIcon,
    PhotoIcon,
    DocumentIcon,
    PlayIcon,
    MusicalNoteIcon,
    ChevronDownIcon,
    ChevronUpIcon,
    ArrowDownTrayIcon,
    XMarkIcon
} from "@heroicons/vue/24/outline";
import { EllipsisVerticalIcon } from "@heroicons/vue/24/solid";
import { useChatStore } from "../store/chatStore";
import { useAuthStore } from "../store/authStore";
import { getFileUrl, isAvatarLoaded, markAvatarLoaded, isAvatarFailed, markAvatarFailed } from "../services/mediaService";
import { blockUser, unblockUser, getBlockedUsers } from "../services/userService";
import MediaLightbox from "../components/MediaLightbox.vue";
import MuteModal from "../components/MuteModal.vue";

const route = useRoute();
const router = useRouter();
const chatStore = useChatStore();
const authStore = useAuthStore();

// Synchronous block status from store for Frame-0 instantaneous state
const isBlocked = computed(() => !!(partner.value?.userId && chatStore.blockStatusMap[partner.value.userId]?.isBlockedByMe));
const isBlockingAction = ref(false);
const hasImageError = ref(false);
const isAvatarLoadedState = ref(false);
const isPageContentLoaded = ref(false);

// Expandable Media State & Responsive Widths
const isMediaExpanded = ref(false);
const menuContainerRef = ref(null);
const containerWidth = ref(typeof window !== 'undefined' ? window.innerWidth : 600);


const updateWidth = () => {
    if (menuContainerRef.value) {
        containerWidth.value = menuContainerRef.value.clientWidth;
    } else if (typeof window !== 'undefined') {
        containerWidth.value = window.innerWidth;
    }
};

const visibleCount = computed(() => {
    const w = containerWidth.value;
    if (w >= 560) return 6;
    if (w >= 380) return 4;
    return 3;
});

// Document Popover State (Guarantees strictly one popup at a time)
const activeDocMenuId = ref(null);

// Lightbox Modal State
const lightboxVisible = ref(false);
const activeMediaItem = ref(null);
const uncontactedPartner = ref(null);

const currentUserId = computed(() => authStore.user?.id || 0);
const activeConversation = computed(() => chatStore.activeConversation);

const partner = computed(() => {
    if (activeConversation.value?.participants) {
        return activeConversation.value.participants.find(p => p.userId !== currentUserId.value) || activeConversation.value.participants[0];
    }
    return uncontactedPartner.value;
});

const showMuteModal = ref(false);

const isMuted = computed(() => {
    if (!activeConversation.value.isMuted) return false;
    if (activeConversation.value.mutedUntil  && new Date(activeConversation.value.mutedUntil) <= new Date()) {
        return false;
    }
    return true;
});

// Format mute status text
const formattedMuteStatus = computed(() => {
    if (!isMuted.value) {
        return "Receive alerts for new messages";
    }
    if (!activeConversation.value?.mutedUntil) {
        return "Muted indefinitely";
    }

    const date = new Date(activeConversation.value.mutedUntil);

    if (isNaN(date.getTime())) return "Notification are muted";

    const now = new Date();
    const timeStr = date.toLocaleTimeString([], {hour: '2-digit', minute: '2-digit'});

    const isToday = date.toDateString() === now.toDateString();

    const tommorrow = new Date(now);
    tommorrow.setDate(now.getDate() + 1);
    const isTommorow = date.toDateString() === tommorrow.toDateString();

    if (isToday) return `Muted unitl ${timeStr}`;
    if (isTommorow) return `Muted until tomorrow at ${timeStr}`;
    return `Muted until ${date.toLocaleDateString([], { month: 'short', day: 'numeric' })} at ${timeStr}`;
});

const avatarUrl = computed(() => partner.value?.avatar ? getFileUrl(partner.value.avatar) : "");
const hasAvatar = computed(() => !!partner.value?.avatar && !hasImageError.value);

watch(avatarUrl, (newUrl) => {
    isAvatarLoadedState.value = isAvatarLoaded(newUrl);
    hasImageError.value = isAvatarFailed(newUrl);
}, { immediate: true });

const handleImageLoad = () => {
    isAvatarLoadedState.value = true;
    markAvatarLoaded(avatarUrl.value);
};

const handleImageError = () => {
    hasImageError.value = true;
    markAvatarFailed(avatarUrl.value);
};

// Format last seen time
const formattedLastSeen = computed(() => {
    if (!partner.value?.lastSeen) return null;
    const date = new Date(partner.value.lastSeen);
    if (isNaN(date.getTime())) return null;

    const now = new Date();
    const timeStr = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    const isToday = date.toDateString() === now.toDateString();

    const yesterday = new Date(now);
    yesterday.setDate(now.getDate() - 1);
    const isYesterday = date.toDateString() === yesterday.toDateString();

    if (isToday) return `today at ${timeStr}`;
    if (isYesterday) return `yesterday at ${timeStr}`;
    return `${date.toLocaleDateString([], { month: 'short', day: 'numeric' })} at ${timeStr}`;
});

// Displayed Media Pagination (Only show 6, 4, or 3 items when collapsed depending on container width)
const displayedMedia = computed(() => {
    if (isMediaExpanded.value) {
        return chatStore.conversationMedia;
    }
    return chatStore.conversationMedia.slice(0, visibleCount.value);
});

// Back button handler
const handleBack = () => {
    if (partner.value?.username) {
        router.push({ name: 'chat', params: { username: partner.value.username } });
    } else {
        router.back();
    }
};

// Mute/Unmute user
const toggleMute = async () => {
    if (!activeConversation.value) return;
    if (isMuted.value) {
        await chatStore.unmuteUserConversation(activeConversation.value.id);
    } else {
        showMuteModal.value = true;
    }
};

// Block/Unblock user
const toggleBlock = async () => {
    if (!partner.value?.userId) return;
    isBlockingAction.value = true;
    try {
        if (isBlocked.value) {
            await chatStore.unblockUser(partner.value.userId);
        } else {
            await chatStore.blockUser(partner.value.userId);
        }
    } catch (err) {
        console.error("Failed to toggle block:", err);
    } finally {
        isBlockingAction.value = false;
    }
};

// Check if user is blocked (runs in background to ensure fresh state)
const checkBlockedStatus = async () => {
    if (!partner.value?.userId) return;
    await chatStore.fetchBlockStatus(partner.value.userId);
};

// Robust Media Format Helpers (Handles ContentType and file extensions returned from GetConversationMedia)
// Image
const isImageFormat = (item) => {
    if (!item) return false;
    const type = (item.messageType || '').toLowerCase();
    const ct = (item.contentType || '').toLowerCase();
    const fn = (item.originalFileName || item.storedFileName || '').toLowerCase();
    return type === 'image' || ct.startsWith('image/') || !!fn.match(/\.(jpg|jpeg|png|gif|webp|svg)$/i);
};
// Video
const isVideoFormat = (item) => {
    if (!item) return false;
    const type = (item.messageType || '').toLowerCase();
    const ct = (item.contentType || '').toLowerCase();
    const fn = (item.originalFileName || item.storedFileName || '').toLowerCase();
    return type === 'video' || ct.startsWith('video/') || !!fn.match(/\.(mp4|webm|mov|mkv|avi)$/i);
};
// Audio
const isAudioFormat = (item) => {
    if (!item) return false;
    const type = (item.messageType || '').toLowerCase();
    const ct = (item.contentType || '').toLowerCase();
    const fn = (item.originalFileName || item.storedFileName || '').toLowerCase();
    return type === 'audio' || ct.startsWith('audio/') || !!fn.match(/\.(mp3|wav|ogg|m4a|aac)$/i);
};
// Any media
const isMediaFormat = (item) => {
    return isImageFormat(item) || isVideoFormat(item) || isAudioFormat(item);
};

// Handle media item click and show media
const handleMediaItemClick = (item) => {
    if (isMediaFormat(item)) {
        activeMediaItem.value = item;
        lightboxVisible.value = true;
    }
};

const toggleDocMenu = (item, event) => {
    if (event) event.stopPropagation();
    const key = item.messageId || item.id || item.storedFileName;
    activeDocMenuId.value = activeDocMenuId.value === key ? null : key;
};

// Handle save file action
const handleSaveDoc = (item) => {
    chatStore.downloadAttachmentFile(item.storedFileName, item.originalFileName);
    activeDocMenuId.value = null;
};
// Handle cancel file action
const handleCancelDocMenu = (event) => {
    if (event) event.stopPropagation();
    activeDocMenuId.value = null;
};

const showScrollTop = ref(false);

const handleScroll = (e) => {
    const el = e.target;
    showScrollTop.value = el.scrollTop > 250;

    // Auto load more media when near bottom of menu container
    if (isMediaExpanded.value && chatStore.hasMoreMedia && !chatStore.loadingMedia) {
        const scrollBottom = el.scrollHeight - el.scrollTop - el.clientHeight;
        if (scrollBottom < 150) {
            loadMoreMedia();
        }
    }
};

// appears when the view has a long height
const scrollToTop = () => {
    if (menuContainerRef.value) {
        menuContainerRef.value.scrollTo({ top: 0, behavior: 'smooth' });
    }
};

// Toggle media expanded state
const toggleMediaExpand = async () => {
    isMediaExpanded.value = !isMediaExpanded.value;
    if (isMediaExpanded.value && chatStore.conversationMedia.length <= visibleCount.value && chatStore.hasMoreMedia) {
        await loadMoreMedia();
    }
};

const loadMoreMedia = async () => {
    if (activeConversation.value?.id) {
        await chatStore.fetchConversationMedia(activeConversation.value.id, 30, true);
    }
};

onMounted(async () => {
    updateWidth();
    if (typeof window !== 'undefined') {
        window.addEventListener('resize', updateWidth);
    }

    const targetUsername = route.params.username;
    if (targetUsername) {
        if (!chatStore.activeConversation) {
            await chatStore.selectConversationByUsername(targetUsername, false);
        }
        // If no conversation exists in DB for this user, they are uncontacted -> redirect back to chat
        if (!chatStore.activeConversation) {
            router.replace({ name: 'chat', params: { username: targetUsername } });
            return;
        }
    } else {
        router.replace({ name: 'contacts' });
        return;
    }
    isPageContentLoaded.value = true;
    checkBlockedStatus();
    if (activeConversation.value?.id) {
        chatStore.fetchConversationMedia(activeConversation.value.id);
    }
});

onUnmounted(() => {
    if (typeof window !== 'undefined') {
        window.removeEventListener('resize', updateWidth);
    }
});
</script>
<template>
    <div 
        ref="menuContainerRef" 
        class="menu-container d-flex flex-column vh-100 overflow-auto position-relative px-3" 
        @scroll="handleScroll"
        @click="activeDocMenuId = null"
    >

        <!-- Loading State during page refresh -->
        <div v-if="!isPageContentLoaded" class="d-flex align-items-center justify-content-center flex-grow-1 py-5">
            <div class="spinner-border text-warning" role="status">
                <span class="visually-hidden">Loading menu...</span>
            </div>
        </div>

        <!-- Main Body -->
        <div v-else class="menu-body flex-grow-1 mx-auto w-100 pt-4" style="max-width: 680px;">
            <!-- Profile Card -->
            <div class="profile-card text-center p-4 rounded-4 shadow-sm mb-3">
                <div class="avatar-wrapper position-relative mx-auto mb-3">

                    <img 
                        v-if="hasAvatar" 
                        :src="avatarUrl" 
                        :alt="partner?.username" 
                        class="avatar-lg shadow" 
                        loading="eager" 
                        decoding="async" 
                        referrerpolicy="no-referrer" 
                        @load="handleImageLoad" 
                        @error="handleImageError" 
                    />
                    <div v-else class="avatar-fallback-lg shadow d-flex align-items-center justify-content-center mx-auto">
                        <UserIcon style="width: 44px; height: 44px;" class="text-secondary" />
                    </div>
                </div>

                <h4 class="fw-bold mb-1 text-primary-theme">{{ partner?.username || 'User' }}</h4>
                <p v-if="partner?.isOnline" class="text-online small fw-medium mb-3">Online</p>
                <p v-else-if="formattedLastSeen" class="text-muted small mb-3">Last seen {{ formattedLastSeen }}</p>

                <!-- Shortcut Buttons -->
                <div class="d-flex justify-content-center gap-3 mt-3">
                    <button @click="handleBack" class="btn-action-shortcut">
                        <ChatBubbleLeftEllipsisIcon style="width: 20px; height: 20px;" />
                        <span>Message</span>
                    </button>
                </div>
            </div>

            <!-- Options Card: Mute & Block -->
            <div class="options-card rounded-4 shadow-sm mb-3 overflow-hidden">
                <div class="option-item d-flex align-items-center justify-content-between p-3 border-bottom cursor-pointer" @click="toggleMute">
                    <div class="d-flex align-items-center gap-3">
                        <BellSlashIcon v-if="isMuted" style="width: 22px; height: 22px; color: var(--status-red);"/>
                        <BellIcon v-else style="width: 22px; height: 22px;" class="text-secondary" />
                        <div>
                            <div class="fw-semibold text-primary-theme">Mute Notifications</div>
                            <div class="small text-muted">{{ formattedMuteStatus }}</div>
                        </div>
                    </div>
                    <div class="form-check form-switch m-0" style="pointer-events: none;">
                        <input class="form-check-input" type="checkbox" :checked="isMuted" />
                    </div>
                </div>

                <div class="option-item d-flex align-items-center justify-content-between p-3" @click="toggleBlock">
                    <div class="d-flex align-items-center gap-3" :class="[isBlocked ? 'red-text' : 'normal-text']">
                        <NoSymbolIcon :class="[isBlocked ? 'red-text' : 'text-secondary']" style="width: 22px; height: 22px;"/>
                        <div>
                            <div class="fw-semibold">{{ isBlocked ? 'Unblock Contact' : 'Block Contact' }}</div>    
                            <div class="small text-muted">{{ isBlocked ? 'Allow messages from user' : 'Block user from messaging you' }}</div>
                        </div>
                    </div>
                    <span v-if="isBlockingAction" class="spinner-border spinner-border-sm text-danger" role="status"></span>
                </div>
            </div>

            <!-- Expandable Shared Media Gallery Card -->
            <div class="media-card rounded-4 shadow-sm p-3 mb-4">
                <div class="d-flex align-items-center justify-content-between mb-3">
                    <h6 class="fw-bold m-0 text-primary-theme d-flex align-items-center gap-2">
                        <PhotoIcon style="width: 20px; height: 20px;" class="text-accent-theme" />
                        <span>Media, Links & Docs</span>
                    </h6>
                    <button v-if="chatStore.conversationMedia.length > 0 && (chatStore.conversationMedia.length > visibleCount || chatStore.hasMoreMedia)" @click="toggleMediaExpand" class="btn btn-sm btn-link text-accent-theme text-decoration-none p-0 fw-semibold d-flex align-items-center gap-1">
                        <span>{{ isMediaExpanded ? 'Show less' : "See all" }}</span>
                        <ChevronUpIcon v-if="isMediaExpanded" style="width: 14px; height: 14px;" />
                        <ChevronDownIcon v-else style="width: 14px; height: 14px;" />
                    </button>
                </div>

                <!-- Media Loading State -->
                <div v-if="chatStore.loadingMedia && chatStore.conversationMedia.length === 0" class="text-center py-4">
                    <span class="spinner-border spinner-border-sm text-warning" role="status"></span>
                </div>

                <!-- Empty Media State -->
                <div v-else-if="chatStore.conversationMedia.length === 0" class="text-center py-4 text-muted small">
                    No shared media or documents yet
                </div>

                <!-- Expandable Media Grid -->
                <div v-else class="media-grid">
                    <div 
                        v-for="item in displayedMedia" 
                        :key="item.messageId || item.id || item.storedFileName" 
                        class="media-grid-item rounded-3 position-relative"
                        :class="{ 'is-clickable-media': isMediaFormat(item) }"
                        :title="item.originalFileName || item.storedFileName || 'Attachment'"
                        @click="handleMediaItemClick(item)"
                    >
                        <!-- Image -->
                        <img 
                            v-if="isImageFormat(item)" 
                            :src="getFileUrl(item.storedFileName)" 
                            :alt="item.originalFileName || 'Image'" 
                            :title="item.originalFileName || item.storedFileName || 'Image'"
                            class="media-thumb rounded-3" 
                        />
                        
                        <!-- Video -->
                        <div 
                            v-else-if="isVideoFormat(item)" 
                            class="position-relative w-100 h-100 overflow-hidden rounded-3 media-pointer" 
                            :title="item.originalFileName || item.storedFileName || 'Video'"
                        >
                            <video 
                                :src="getFileUrl(item.storedFileName)" 
                                class="media-thumb rounded-3" 
                                style="object-fit: cover;" 
                                preload="metadata"
                            ></video>
                            <div class="position-absolute top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center bg-dark bg-opacity-25">
                                <div class="rounded-circle d-flex align-items-center justify-content-center p-1" style="background: rgba(0, 0, 0, 0.6);">
                                    <PlayIcon style="width: 20px; height: 20px; margin-left: 2px;" class="text-white" />
                                </div>
                            </div>
                        </div>

                        <!-- Audio -->
                        <div v-else-if="isAudioFormat(item)" class="media-pointer media-doc-thumb d-flex flex-column align-items-center justify-content-center p-2 text-center bg-opacity-10 rounded-3">
                            <MusicalNoteIcon style="width: 24px; height: 24px;" class="text-warning mb-1" />
                            <span class="doc-name text-truncate w-100 small">{{ item.originalFileName || 'Audio' }}</span>
                        </div>

                        <!-- Document / Non-Media File with White Action Popover -->
                        <div v-else class="media-doc-thumb position-relative d-flex flex-column align-items-center justify-content-center p-2 text-center rounded-3">
                            <div class="position-absolute top-0 end-0 m-1 z-2">
                                <button 
                                    type="button" 
                                    class="btn-doc-ellipsis border-0 bg-transparent text-secondary p-1 rounded-circle" 
                                    @click.stop="toggleDocMenu(item, $event)" 
                                    title="Options"
                                >
                                    <EllipsisVerticalIcon style="width: 16px; height: 16px;" />
                                </button>

                                <!-- White Action Menu (Matching MessageActionMenu.vue) -->
                                <div v-if="activeDocMenuId === (item.messageId || item.id || item.storedFileName)" class="doc-action-popup shadow-lg">
                                    <button type="button" @click.stop="handleSaveDoc(item)" class="popup-action-item" title="Save File">
                                        <ArrowDownTrayIcon class="action-icon text-info" />
                                        <span class="action-label">Save</span>
                                    </button>
                                    <div class="menu-divider"></div>
                                    <button type="button" @click.stop="handleCancelDocMenu($event)" class="popup-action-item" title="Cancel">
                                        <XMarkIcon class="action-icon text-muted" />
                                        <span class="action-label">Cancel</span>
                                    </button>
                                </div>
                            </div>
                            <DocumentIcon style="width: 24px; height: 24px;" class="text-secondary mb-1" />
                            <span class="doc-name text-truncate w-100 small pe-2 ps-2">{{ item.originalFileName || 'Document' }}</span>
                        </div>
                    </div>
                </div>

                <!-- Load More Media when Expanded -->
                <div v-if="isMediaExpanded && chatStore.hasMoreMedia" class="text-center mt-3">
                    <button @click="loadMoreMedia" class="btn btn-sm btn-outline-secondary rounded-pill px-3" :disabled="chatStore.loadingMedia">
                        <span v-if="chatStore.loadingMedia" class="spinner-border spinner-border-sm me-1" role="status"></span>
                        Load More Media
                    </button>
                </div>
            </div>
        </div>

        <!-- Global Lightbox Modal -->
        <MediaLightbox 
            :visible="lightboxVisible" 
            :media-url="activeMediaItem ? getFileUrl(activeMediaItem.storedFileName) : ''"
            :media-type="activeMediaItem?.messageType"
            :content-type="activeMediaItem?.contentType"
            :file-name="activeMediaItem?.originalFileName || 'Attachment'" 
            @close="lightboxVisible = false"
            @download="chatStore.downloadAttachmentFile(activeMediaItem?.storedFileName, activeMediaItem?.originalFileName)"
        />

        <!-- Floating Scroll To Top Button -->
        <Transition name="fade">
            <button 
                v-if="showScrollTop" 
                @click="scrollToTop" 
                class="btn-scroll-top shadow-lg"
                title="Scroll to Top"
            >
                <ChevronUpIcon style="width: 20px; height: 20px;" />
            </button>
        </Transition>
    </div>
    <MuteModal 
        :is-open="showMuteModal" 
        :conversation-id="activeConversation?.id" 
        @close="showMuteModal = false" 
    />
</template>

<style scoped>
.normal-text {
    color: var(--text-primary);
}

.red-text {
    color: var(--status-red);
}

.menu-container {
    background-color: var(--bg-chat-area);
}

.btn-back-floating {
    background-color: var(--bg-chat-header);
    border: 1px solid var(--border-light);
    color: var(--text-primary);
}

.btn-icon {
    width: 38px;
    height: 38px;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 50%;
    cursor: pointer;
    transition: background-color 0.2s ease;
}

.btn-icon:hover {
    background-color: rgba(0, 0, 0, 0.07);
}

.btn-doc-ellipsis {
    width: 24px;
    height: 24px;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: background-color 0.2s ease;
    cursor: pointer !important;
}

.btn-doc-ellipsis:hover {
    background-color: rgba(0, 0, 0, 0.1) !important;
    color: var(--text-primary) !important;
}

/* Crisp White Action Popover Menu (Matching MessageActionMenu.vue) */
.doc-action-popup {
    position: absolute;
    z-index: 1050;
    top: calc(100% + 2px);
    right: 0;
    min-width: 130px;
    background-color: #ffffff !important;
    border: 1px solid rgba(0, 0, 0, 0.12) !important;
    border-radius: 8px;
    padding: 4px 0;
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.16), 0 2px 6px rgba(0, 0, 0, 0.08);
    display: flex;
    flex-direction: column;
    animation: fadeInScale 0.15s ease-out;
}

.popup-action-item {
    display: flex;
    align-items: center;
    gap: 10px;
    width: 100%;
    padding: 8px 14px;
    background: transparent;
    border: none;
    cursor: pointer;
    font-size: 0.85rem;
    color: #111827 !important;
    transition: background-color 0.15s ease;
    text-align: left;
}

.popup-action-item:hover {
    background-color: #f3f4f6 !important;
}

.action-icon {
    width: 18px;
    height: 18px;
    flex-shrink: 0;
}

.action-label {
    font-weight: 500;
}

.menu-divider {
    height: 1px;
    background-color: var(--border-light);
    margin: 4px 0;
}

@keyframes fadeInScale {
    from {
        opacity: 0;
        transform: translateY(-4px) scale(0.96);
    }
    to {
        opacity: 1;
        transform: translateY(0) scale(1);
    }
}

.profile-card, .options-card, .media-card {
    background-color: var(--bg-chat-header);
    border: 1px solid var(--border-light);
}

.avatar-wrapper {
    width: 100px;
    height: 100px;
}

.avatar-lg {
    width: 100%;
    height: 100%;
    border-radius: 50%;
    object-fit: cover;
    border: 2px solid var(--border-light);
    box-shadow: var(--shadow-md);
    transition: opacity 0.2s ease-in-out;
}

.avatar-fallback-lg {
    width: 100%;
    height: 100%;
    border-radius: 50%;
    background-color: var(--bg-input);
    border: 2px solid var(--border-light);
    box-shadow: var(--shadow-md);
}

.btn-action-shortcut {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 8px 18px;
    border-radius: 20px;
    background-color: var(--bg-input);
    border: 1px solid var(--border-light);
    color: var(--text-primary);
    font-size: 0.85rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s ease;
}

.btn-action-shortcut:hover {
    border-color: var(--accent-primary);
    color: var(--accent-primary);
}

.btn-action-shortcut.active {
    background-color: var(--accent-tint-subtle, rgba(201, 147, 59, 0.15));
    border-color: var(--accent-primary);
    color: var(--accent-primary);
}

.text-primary-theme {
    color: var(--text-primary);
}

.text-accent-theme {
    color: var(--accent-primary);
}

.text-online {
    color: var(--status-green, #22c55e);
}

.option-item {
    cursor: pointer;
}

.option-item:hover {
    background-color: rgba(0, 0, 0, 0.03);
}

.media-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(90px, 1fr));
    gap: 8px;
}

.media-grid-item {
    aspect-ratio: 1;
    border: 1px solid var(--border-light);
    cursor: default;
    transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.media-grid-item.is-clickable-media {
    cursor: pointer;
}

.media-grid-item.is-clickable-media:hover {
    transform: scale(1.04);
}

.media-pointer {
    cursor: pointer;
}

.media-thumb {
    width: 100%;
    height: 100%;
    object-fit: cover;
    transition: transform 0.2s ease;
}

.media-doc-thumb {
    width: 100%;
    height: 100%;
    background-color: var(--bg-input);
}

/* Theme Customization for Mute Toggle Switch */
.form-check-input {
    background-color: var(--bg-input) !important;
    border-color: var(--border-light) !important;
    cursor: pointer;
    transition: background-color 0.2s ease, border-color 0.2s ease, box-shadow 0.2s ease;
}

.form-check-input:checked {
    background-color: var(--accent-primary) !important;
    border-color: var(--accent-hover) !important;
}

.form-check-input:focus {
    border-color: var(--accent-primary) !important;
    box-shadow: 0 0 0 0.25rem var(--accent-tint-light) !important;
}

.btn-scroll-top {
    position: fixed;
    bottom: 28px;
    right: 28px;
    width: 44px;
    height: 44px;
    border-radius: 50%;
    background-color: var(--accent-primary, #d1a153);
    color: #ffffff;
    border: 1px solid rgba(255, 255, 255, 0.2);
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    z-index: 1000;
    transition: all 0.25s ease;
}

.btn-scroll-top:hover {
    transform: translateY(-3px) scale(1.06);
    background-color: var(--accent-hover, #a87424);
}
</style>