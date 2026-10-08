<script setup>
import { computed, ref, onMounted, onUnmounted } from "vue";
import { getFileUrl } from "../services/mediaService";
import { useChatStore } from "../store/chatStore";
import { NoSymbolIcon } from "@heroicons/vue/24/outline";

import QuotedMessagePreview from "./MessageContainer/QuotedMessagePreview.vue";
import MessageImageAttachment from "./MessageContainer/MessageImageAttachment.vue";
import MessageAudioPlayer from "./MessageContainer/MessageAudioPlayer.vue";
import MessageDocumentCard from "./MessageContainer/MessageDocumentCard.vue";
import MessageVideoAttachment from "./MessageContainer/MessageVideoAttachment.vue";
import MessageActionMenu from "./MessageContainer/MessageActionMenu.vue";
import Lightbox from "./MediaLightbox.vue";

const chatStore = useChatStore();

const props = defineProps({
    message: {
        type: Object,
        required: true
    },
    currentUserId: {
        type: Number,
        required: true
    },
    partnerName: {
        type: String,
        default: "User"
    },
    activeMenuId: {
        type: [Number, null],
        default: null
    }
});

const emit = defineEmits(["reply", "edit", "open-menu", "close-menu", "navigate-to-message"]);

const isOwner = computed(() => props.message.senderId === props.currentUserId);
const isMenuOpen = computed(() => props.activeMenuId === props.message.id);

const menuRef = ref(null);
const pressTimer = ref(null);
const openUpward = ref(false);

// Calculate popup for messages
const calculateMenuDirection = () => {
    if (menuRef.value) {
        const rect = menuRef.value.getBoundingClientRect();
        openUpward.value = (window.innerHeight - rect.bottom) < 190;
    }
};

// File extension extraction
const fileExtension = computed(() => {
    const name = props.message.originalFileName || props.message.storedFileName || '';
    const parts = name.split('.');
    return parts.length > 1 ? parts.pop().toUpperCase() : '';
});

// Image type detections
const isImage = computed(() => {
    return props.message.messageType === 'Image' || (props.message.contentType && props.message.contentType.startsWith('image/'));
});

// Video type detection
const isVideo = computed(() => {
    return props.message.messageType === 'Video' ||
        (props.message.contentType && props.message.contentType.startsWith('video/')) ||
        ['MP4', 'WEBM', 'MOV', 'MKV', 'AVI'].includes(fileExtension.value);
});

// Audio type detection
const isAudio = computed(() => {
    return props.message.messageType === 'Audio' ||
        (props.message.contentType && props.message.contentType.startsWith('audio/')) ||
        ['MP3', 'WAV', 'OGG', 'AAC'].includes(fileExtension.value);
});

// Media type detection
const isMedia = computed(() => {
    return isImage.value || isVideo.value || !!(props.message.storedFileName || props.message.fileUrl);
});

// Image URL
const imageUrl = computed(() => {
    const rawPath = props.message.storedFileName || props.message.fileUrl;
    if (!rawPath) return null;
    return getFileUrl(rawPath);
});

// Video URL
const videoUrl = computed(() => {
    if (!isVideo.value) return null;
    const rawPath = props.message.storedFileName || props.message.fileUrl;
    return rawPath ? getFileUrl(rawPath) : null;
});

// Audio URL
const audioUrl = computed(() => {
    if (!isAudio.value) return null;
    const rawPath = props.message.storedFileName || props.message.fileUrl;
    return rawPath ? getFileUrl(rawPath) : null;
});

// Parent message quote handling
const parentMessage = computed(() => {
    if (!props.message.parentMessageId) return null;
    return chatStore.messages.find(m => m.id === props.message.parentMessageId) || null;
});

const parentAuthorName = computed(() => {
    if (!parentMessage.value) return '';
    return parentMessage.value.senderId === props.currentUserId ? 'You' : (props.partnerName || 'User');
});

// Emoji detection
const isOnlyEmojis = computed(() => {
    if (!props.message.content || isMedia.value) return false;
    const text = props.message.content.trim();
    if (!text) return false;
    const emojiRegex = /^(?:\s*(?:\p{Extended_Pictographic}|\p{Emoji_Presentation}|\p{Emoji}\uFE0F)(?:\u200D(?:\p{Extended_Pictographic}|\p{Emoji_Presentation}|\p{Emoji}\uFE0F)|[\u{1F3FB}-\u{1F3FF}]|\uFE0F)*\s*)+$/u;
    return emojiRegex.test(text);
});

// Emoji count for displayment style
const emojiCount = computed(() => {
    if (!isOnlyEmojis.value) return 0;
    const text = props.message.content.trim();
    const segmenter = new Intl.Segmenter('en', { granularity: 'grapheme' });
    const segments = [...segmenter.segment(text)].filter(s => s.segment.trim().length > 0);
    return segments.length;
});

const isSingleEmoji = computed(() => emojiCount.value === 1 && !parentMessage.value);
const isMediumEmoji = computed(() => (emojiCount.value === 2 || emojiCount.value === 3) && !parentMessage.value);

// Formatted content with safe links
const formattedContent = computed(() => {
    if (!props.message.content) return '';
    const escaped = props.message.content
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');

    const urlRegex = /(https?:\/\/[^\s]+|www\.[^\s]+)/g;
    return escaped.replace(urlRegex, (url) => {
        const href = url.startsWith('www.') ? `https://${url}` : url;
        return `<a href="${href}" target="_blank" rel="noopener noreferrer" class="chat-link text-decoration-underline text-break" onclick="event.stopPropagation()">${url}</a>`;
    });
});

// Formatted time
const formattedTime = computed(() => {
    if (!props.message.sentAt) return '';
    const date = new Date(props.message.sentAt);
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
});

// Lightbox state
const isImageExpanded = ref(false);

const openImageModal = () => {
    isImageExpanded.value = true;
};

const closeImageModal = () => {
    isImageExpanded.value = false;
};

// Touch long-press handlers
const handleTouchStart = () => {
    if (props.message.isDeleted) return;
    pressTimer.value = setTimeout(() => {
        calculateMenuDirection();
        if (navigator.vibrate) navigator.vibrate(40);
        emit("open-menu", props.message.id);
    }, 500);
};

const handleTouchEnd = () => {
    clearTimeout(pressTimer.value);
};

const handleTouchMove = () => {
    clearTimeout(pressTimer.value);
};

// Context menu (right click)
const handleContextMenu = (e) => {
    e.preventDefault();
    if (props.message.isDeleted) return;
    calculateMenuDirection();
    if (isMenuOpen.value) {
        emit("close-menu");
    } else {
        emit("open-menu", props.message.id);
    }
};

// Actions
const handleReply = () => {
    emit("close-menu");
    emit("reply", props.message);
};

const handleEdit = () => {
    emit("close-menu");
    emit("edit", props.message);
};

const handleDelete = async () => {
    emit("close-menu");
    await chatStore.deleteUserMessage(props.message.id);
};

const handleDownload = () => {
    emit("close-menu");
    const rawPath = props.message.storedFileName || props.message.fileUrl;
    if (rawPath) {
        chatStore.downloadAttachmentFile(rawPath, props.message.originalFileName || 'attachment');
    }
};

const handleQuoteClick = (parentId) => {
    emit("navigate-to-message", parentId);
};

const handleClickOutside = (e) => {
    if (isMenuOpen.value && menuRef.value && !menuRef.value.contains(e.target)) {
        emit("close-menu");
    }
};

onMounted(() => {
    document.addEventListener("click", handleClickOutside);
});

onUnmounted(() => {
    document.removeEventListener("click", handleClickOutside);
    clearTimeout(pressTimer.value);
});
</script>

<template>
    <div class="px-3 py-1 position-relative">
        <div :class="[isOwner ? 'd-flex justify-content-end mb-2' : 'd-flex justify-content-start mb-2']">
            <div 
                ref="menuRef"
                :id="`message-${message.id}`"
                :class="[
                    'position-relative message-bubble transition-all', 
                    props.message.isDeleted
                        ? 'deleted-bubble px-3 py-2 shadow-none'
                        : (isSingleEmoji 
                            ? 'bg-transparent shadow-none border-0 p-0' 
                            : (isOwner ? 'owner-bubble px-3 py-2 shadow-sm' : 'partner-bubble px-3 py-2 shadow-sm'))
                ]"
                :style="isSingleEmoji && !props.message.isDeleted ? 'max-width: 75%; min-width: auto; user-select: none;' : 'max-width: 75%; min-width: 70px; width: fit-content; user-select: none;'"
                @touchstart="handleTouchStart"
                @touchend="handleTouchEnd"
                @touchmove="handleTouchMove"
                @contextmenu.prevent="handleContextMenu"
            >
                <!-- Quoted Parent Message -->
                <QuotedMessagePreview 
                    v-if="parentMessage && !props.message.isDeleted"
                    :parent-message="parentMessage"
                    :parent-author-name="parentAuthorName"
                    @jump="handleQuoteClick"
                />

                <!-- Image Attachment -->
                <MessageImageAttachment 
                    v-if="isImage && imageUrl && !props.message.isDeleted"
                    :image-url="imageUrl"
                    :alt="props.message.originalFileName || 'Image Attachment'"
                    @open="openImageModal"
                    class="message-container-mt"
                />

                <!-- Video Attachment -->
                <MessageVideoAttachment 
                    v-else-if="isVideo && videoUrl && !props.message.isDeleted"
                    :video-url="videoUrl"
                    :alt="props.message.originalFileName || 'Video Attachment'"
                    @open="openImageModal"
                    class="message-container-mt"
                />

                <!-- Audio Attachment -->
                <MessageAudioPlayer 
                    v-else-if="isAudio && audioUrl && !props.message.isDeleted"
                    :message="props.message"
                    :audio-url="audioUrl"
                    :file-extension="fileExtension"
                    class="message-container-mt"
                />

                <!-- File Attachment (Non-Image/Non-Audio/Non-Video) -->
                <MessageDocumentCard 
                    v-else-if="(props.message.storedFileName || props.message.fileUrl) && !props.message.isDeleted"
                    :message="props.message"
                    :file-extension="fileExtension"
                    @download="handleDownload"
                    class="message-container-mt"
                />

                <!-- Text Content -->
                <div class="d-flex flex-column">
                    <div v-if="props.message.isDeleted" class="deleted-content d-flex align-items-center gap-2 py-1">
                        <NoSymbolIcon class="flex-shrink-0 text-muted" style="width: 16px; height: 16px; opacity: 0.65;" />
                        <span class="fst-italic text-muted">{{ isOwner ? 'You deleted this message' : 'This message was deleted' }}</span>
                    </div>

                    <div v-else-if="isSingleEmoji" class="text-center" style="font-size: 3.5rem; line-height: 1.1;">
                        {{ props.message.content.trim() }}
                    </div>

                    <div v-else-if="isMediumEmoji" class="d-flex align-items-center gap-1" style="font-size: 2.2rem; line-height: 1.2;">
                        {{ props.message.content.trim() }}
                    </div>

                    <p 
                        v-else-if="props.message.content" 
                        class="m-0" 
                        style="font-size: var(--font-size-md); line-height: 1.4; word-wrap: break-word; white-space: pre-wrap;"
                        v-html="formattedContent"
                    ></p>

                    <!-- Time & Status -->
                    <div 
                        v-if="!isSingleEmoji || props.message.isDeleted"
                        class="d-flex align-items-center justify-content-end gap-1 mt-1 opacity-75" 
                        :class="{ 'badge bg-dark bg-opacity-25 text-white rounded-pill px-2 py-1 align-self-end shadow-sm': isSingleEmoji && !props.message.isDeleted }"
                        style="font-size: 0.65rem;"
                    >
                        <span v-if="props.message.isEdited && !props.message.isDeleted" class="fst-italic me-1">(edited)</span>
                        <span v-if="!props.message.isDeleted">{{ formattedTime }}</span>

                        <template v-if="isOwner && !props.message.isDeleted">
                            <i v-if="props.message.isSeen" class="fa fa-check-double text-info ms-1" title="Seen"></i>
                            <i v-else-if="props.message.isDelivered" class="fa fa-check-double text-secondary ms-1" title="Delivered"></i>
                            <i v-else class="fa fa-check text-secondary ms-1" title="Sent"></i>
                        </template>
                    </div>
                </div>

                <!-- Action Menu Popover -->
                <MessageActionMenu 
                    v-if="isMenuOpen && !props.message.isDeleted"
                    :is-owner="isOwner"
                    :is-media="isMedia"
                    :open-upward="openUpward"
                    @reply="handleReply"
                    @edit="handleEdit"
                    @download="handleDownload"
                    @delete="handleDelete"
                />
            </div>
        </div>

        <!-- Media Lightbox Modal (Images & Videos) -->
        <Lightbox 
            v-if="imageUrl || videoUrl"
            :visible="isImageExpanded"
            :image-url="imageUrl || videoUrl"
            :file-name="props.message.originalFileName || 'Attachment'"
            :media-type="props.message.messageType"
            :content-type="props.message.contentType"
            @close="closeImageModal"
            @download="handleDownload"
        />
    </div>
</template>

<style scoped>
.message-container-mt {
    margin-top: 5px;
}

.message-bubble {
    border-radius: 12px;
}
.transition-all {
    transition: all 0.25s ease;
}
.owner-bubble {
    background-color: var(--bg-input);
    color: var(--text-primary);
    border-top-right-radius: 2px;
}
.partner-bubble {
    background-color: var(--gold-white);
    color: var(--text-primary);
    border-top-left-radius: 2px;
}

:deep(.chat-link) {
    color: var(--accent-primary);
    text-underline-offset: 3px;
    font-weight: 500;
}
:deep(.chat-link:hover) {
    opacity: 0.8;
}

.deleted-bubble {
    background-color: rgba(128, 128, 128, 0.08) !important;
    border: 1px dashed rgba(128, 128, 128, 0.28) !important;
    border-radius: 12px !important;
}

.deleted-content {
    font-size: 0.85rem;
    color: var(--text-muted);
    font-style: italic;
    user-select: none;
}
</style>