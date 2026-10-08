<script setup>
import { ref, onMounted, onUnmounted, nextTick, watch, computed } from "vue";
import { 
    FaceSmileIcon, 
    XMarkIcon, 
    PencilSquareIcon, 
    ArrowUturnLeftIcon,
    NoSymbolIcon
} from "@heroicons/vue/24/outline";
import { PaperAirplaneIcon, PlusIcon } from "@heroicons/vue/24/solid";
import EmojiPicker from "vue3-emoji-picker";
import "vue3-emoji-picker/css";
import { useChatStore } from "../store/chatStore";
import { useAuthStore } from "../store/authStore";
import { useToastStore } from "../store/toastStore";
import { getBlockStatus, unblockUser } from "../services/userService";

const chatStore = useChatStore();
const authStore = useAuthStore();
const toastStore = useToastStore();

const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB in bytes, file limit
const MAX_MESSAGE_LENGTH = 2000; // message length limit

const props = defineProps({
    recipientId: {
        type: Number,
        required: false,
        default: null
    },
    isDeletedAccount: {
        type: Boolean,
        default: false
    },
    editingMessage: {
        type: Object,
        default: null
    },
    replyingToMessage: {
        type: Object,
        default: null
    }
});

const emit = defineEmits(["cancel-edit", "save-edit", "cancel-reply"]);

const inputEl = ref("");
const showEmojis = ref(false);
const selectedFile = ref(null);
const fileBlobUrl = ref("");
const isUploading = ref(false);

// Blocking indicator state
const isBlockedByMe = ref(false);
const isBlockedByThem = ref(false);
const isUnblocking = ref(false);

const textareaRef = ref(null);
const emojiRef = ref(null);

const checkBlockedStatus = async () => {
    if (!props.recipientId) {
        isBlockedByMe.value = false;
        isBlockedByThem.value = false;
        return;
    }
    try {
        const status = await getBlockStatus(props.recipientId);
        isBlockedByMe.value = !!status?.isBlockedByMe;
        isBlockedByThem.value = !!status?.isBlockedByThem;
    } catch (err) {
        console.error("Failed to check blocked status:", err);
    }
};

const handleUnblock = async () => {
    if (!props.recipientId) return;
    isUnblocking.value = true;
    try {
        await unblockUser(props.recipientId);
        isBlockedByMe.value = false;
    } catch (err) {
        console.error("Failed to unblock user:", err);
    } finally {
        isUnblocking.value = false;
    }
};

watch(() => props.recipientId, () => {
    isBlockedByThem.value = false;
    checkBlockedStatus();
}, { immediate: true });

// Adjust height automatically as user types or adds newlines
const adjustTextareaHeight = () => {
    nextTick(() => {
        if (textareaRef.value) {
            textareaRef.value.style.height = "auto";
            textareaRef.value.style.height = `${Math.min(textareaRef.value.scrollHeight, 120)}px`;
        }
    });
};

let typingTimeout = null;

// Helper to immediately cancel typing timeout and notify the server
const clearTyping = () => {
    if (typingTimeout) {
        clearTimeout(typingTimeout);
        typingTimeout = null;
    }
    chatStore.sendTypingIndicator(false);
};

// Handle input field view changes
const handleInput = () => {
    adjustTextareaHeight();

    if (chatStore.activeConversationId) {
        chatStore.sendTypingIndicator(true);

        if (typingTimeout) clearTimeout(typingTimeout);
        typingTimeout = setTimeout(() => {
            chatStore.sendTypingIndicator(false);
            typingTimeout = null;
        }, 2500);
    }
};

// Watch for editingMessage changes
watch(() => props.editingMessage, (msg) => {
    if (msg) {
        inputEl.value = msg.content || "";
        adjustTextareaHeight();
        nextTick(() => textareaRef.value?.focus());
    } else {
        clearTyping();
        inputEl.value = "";
        adjustTextareaHeight();
    }
});

// Calculate display author for reply banner
const replyingAuthor = computed(() => {
    if (!props.replyingToMessage) return "";
    return props.replyingToMessage.senderId === authStore.user?.id ? "You" : (props.replyingToMessage.senderName || "User");
});

// Edit action handlers
const handleCancelEdit = () => {
    clearTyping();
    emit("cancel-edit");
};

// Confirm edit action
const handleConfirmEdit = () => {
    const text = inputEl.value.trim();
    if (!text) return;
    if (text.length > MAX_MESSAGE_LENGTH) {
        toastStore.error("Message cannot exceed 2000 characters.");
        return;
    }
    clearTyping();
    emit("save-edit", text);
};

// Handle keydown events
const handleKeydown = (e) => {
    if (e.key === "Escape") {
        if (props.editingMessage) handleCancelEdit();
        if (props.replyingToMessage) emit("cancel-reply");
        return;
    }
    if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        if (props.editingMessage) {
            handleConfirmEdit();
        } else {
            handleSubmit();
        }
    }
};

// Handle emoji selection
const handleEmojiSelect = (emoji) => {
    inputEl.value += emoji.i;
    adjustTextareaHeight();
};

// Handle file change
const handleFileChange = (event) => {
    const file = event.target.files[0];
    if (!file) return;

    if (file.size > MAX_FILE_SIZE) {
        toastStore.error("File size exceeds the 5MB limit.");
        event.target.value = "";
        return;
    }

    selectedFile.value = file;
    if (file.type.startsWith('image/')) {
        fileBlobUrl.value = URL.createObjectURL(file);
    } else {
        fileBlobUrl.value = "";
    }
};

// Remove selected file
const removeSelectedFile = () => {
    if (fileBlobUrl.value) {
        URL.revokeObjectURL(fileBlobUrl.value);
    }
    selectedFile.value = null;
    fileBlobUrl.value = "";
};

// Handle message submission
const handleSubmit = async () => {
    if (isUploading.value) return;
    const textContent = inputEl.value.trim();
    if (!textContent && !selectedFile.value) return;

    if (textContent.length > MAX_MESSAGE_LENGTH) {
        toastStore.error("Message is too long (max 2000 characters).");
        return;
    }

    // Immediately cancel typing indicator before sending message
    clearTyping();

    isUploading.value = true;
    try {
        const parentId = props.replyingToMessage ? props.replyingToMessage.id : null;
        if (selectedFile.value) {
            await chatStore.sendMediaMessage(props.recipientId, selectedFile.value, textContent, parentId);
        } else {
            await chatStore.sendTextMessage(props.recipientId, textContent, parentId);
        }

        // Reset inputs
        inputEl.value = "";
        removeSelectedFile();
        showEmojis.value = false;
        if (props.replyingToMessage) {
            emit("cancel-reply");
        }
        
        // Reset textarea height back to 1 row
        nextTick(() => {
            if (textareaRef.value) {
                textareaRef.value.style.height = "auto";
            }
        });
    } catch (error) {
        console.error("Failed to send message:", error);
        if (error.message && error.message.includes("blocked")) {
            if (error.message.includes("you have been blocked")) {
                isBlockedByThem.value = true;
            } else if (error.message.includes("user you have blocked")) {
                isBlockedByMe.value = true;
            }
        }
    } finally {
        isUploading.value = false;
    }
};

// Close emoji picker when clicking outside
const handleClickOutside = (event) => {
    if (showEmojis.value && emojiRef.value && !emojiRef.value.contains(event.target)) {
        showEmojis.value = false;
    }
};

onMounted(() => {
    document.addEventListener("click", handleClickOutside);
});

onUnmounted(() => {
    document.removeEventListener("click", handleClickOutside);
    clearTyping();
});
</script>

<template>
    <div class="position-relative chat-input-wrapper">
        <div v-if="isDeletedAccount" class="px-4 py-3 chat-content-container mx-auto">
            <div class="blocked-input-bar p-3 d-flex align-items-center justify-content-center gap-2 rounded-4 shadow-sm text-center">
                <span class="fs-7 fw-medium text-secondary-theme">This account has been deleted. You cannot send new messages.</span>
            </div>
        </div>

        <div v-else-if="isBlockedByMe" class="px-4 py-3 chat-content-container mx-auto">
            <div class="blocked-input-bar p-3 d-flex align-items-center justify-content-between gap-3 rounded-4 shadow-sm">
                <div class="d-flex align-items-center gap-2 min-w-0">
                    <NoSymbolIcon style="width: 20px; height: 20px;" class="text-danger flex-shrink-0" />
                    <span class="fs-7 fw-semibold text-main-theme text-truncate">You blocked this user</span>
                </div>
                <button type="button" @click="handleUnblock" :disabled="isUnblocking" class="btn-unblock-pill flex-shrink-0">
                    <span v-if="isUnblocking" class="spinner-border spinner-border-sm me-1" role="status"></span>
                    <span>Unblock</span>
                </button>
            </div>
        </div>

        <div v-else-if="isBlockedByThem" class="px-4 py-3 chat-content-container mx-auto">
            <div class="blocked-input-bar p-3 d-flex align-items-center justify-content-center gap-2 rounded-4 shadow-sm text-center">
                <NoSymbolIcon style="width: 20px; height: 20px;" class="text-secondary-theme text-danger flex-shrink-0" />
                <span class="fs-7 fw-medium text-secondary-theme">You cannot send messages to this user because you are blocked</span>
            </div>
        </div>

        <template v-else>
            <!-- Replying Banner (Above Input) -->
            <div v-if="replyingToMessage" class="banner-wrapper">
                <div class="chat-content-container mx-auto px-3 py-2 d-flex align-items-center justify-content-between">
                    <div class="d-flex align-items-center gap-3 overflow-hidden min-w-0">
                        <div class="banner-accent-bar"></div>
                        <div class="text-truncate min-w-0">
                            <div class="banner-label text-accent-theme">
                                <ArrowUturnLeftIcon style="width: 13px; height: 13px;" class="me-1 flex-shrink-0" />
                                <span>REPLYING TO {{ replyingAuthor.toUpperCase() }}</span>
                            </div>
                            <div class="banner-snippet text-truncate">
                                {{ replyingToMessage.content || (replyingToMessage.storedFileName ? '[Media Attachment]' : '') }}
                            </div>
                        </div>
                    </div>
                    <button type="button" @click="emit('cancel-reply')" class="btn-banner-close ms-2" title="Cancel reply">
                        <XMarkIcon style="width: 16px; height: 16px;" />
                    </button>
                </div>
            </div>

            <!-- Editing Banner (Above Input) -->
            <div v-if="editingMessage" class="banner-wrapper">
                <div class="chat-content-container mx-auto px-3 py-2 d-flex align-items-center justify-content-between">
                    <div class="d-flex align-items-center gap-3 overflow-hidden min-w-0">
                        <div class="banner-accent-bar"></div>
                        <div class="text-truncate min-w-0">
                            <div class="banner-label text-accent-theme">
                                <PencilSquareIcon style="width: 13px; height: 13px;" class="me-1 flex-shrink-0" />
                                <span>EDITING MESSAGE</span>
                            </div>
                            <div class="banner-snippet text-truncate">
                                {{ editingMessage.content }}
                            </div>
                        </div>
                    </div>
                    <button type="button" @click="handleCancelEdit" class="btn-banner-close ms-2" title="Cancel edit">
                        <XMarkIcon style="width: 16px; height: 16px;" />
                    </button>
                </div>
            </div>

            <!-- Selected File Preview -->
            <div 
                v-if="selectedFile" 
                class="position-absolute bottom-100 start-0 w-100 py-3 d-flex align-items-center justify-content-center z-3 bg-opacity-10 bg-black"
            >
                <div v-if="fileBlobUrl" class="position-relative d-inline-block">
                    <button
                        type="button"
                        class="position-absolute top-0 end-0 m-1 d-flex align-items-center justify-content-center rounded-circle bg-dark bg-opacity-60 text-white border-0 cursor-pointer shadow-sm"
                        style="width: 22px; height: 22px; z-index: 10;" 
                        @click="removeSelectedFile"
                        title="Remove image"
                    >
                        <XMarkIcon style="width: 14px; height: 14px;" />
                    </button>
                    <img 
                        :src="fileBlobUrl" 
                        alt="Preview" 
                        class="rounded-3 shadow border border-white border-opacity-50" 
                        style="max-width: 130px; max-height: 130px; object-fit: contain;"
                    />
                </div>

                <div 
                    v-else 
                    class="position-relative d-flex align-items-center gap-2 px-3 py-2 rounded-pill shadow-sm"
                    style="background: rgba(255, 255, 255, 0.85); backdrop-filter: blur(8px); border: 1px solid rgba(0, 0, 0, 0.1);"
                >
                    <i class="fa fa-file text-warning fs-4"></i>
                    <span class="text-dark small fw-medium pe-2 text-truncate" style="max-width: 200px;">{{ selectedFile.name }}</span>
                    <button
                        type="button"
                        class="d-flex align-items-center justify-content-center rounded-circle bg-secondary bg-opacity-25 text-secondary border-0 p-1 cursor-pointer"
                        style="width: 20px; height: 20px;"
                        @click="removeSelectedFile"
                        title="Remove file"
                    >
                        <XMarkIcon style="width: 12px; height: 12px;" />
                    </button>
                </div>
            </div>

            <!-- Message Form -->
            <form @submit.prevent="handleSubmit" class="p-3 d-flex align-items-end gap-2 message-form chat-content-container mx-auto">
                <div v-if="!editingMessage" class="d-flex align-items-center gap-2 pb-1">
                    <!-- Add Media + Sign Button -->
                    <label for="fileUpload" class="btn-add-media m-0 cursor-pointer" title="Add Media">
                        <PlusIcon style="width: 20px; height: 20px;" />
                        <input type="file" @change="handleFileChange" hidden id="fileUpload" />
                    </label>
                    
                    <!-- Emoji Container -->
                    <div ref="emojiRef" class="position-relative">
                        <button type="button" @click.stop="showEmojis = !showEmojis" class="btn-emoji" title="Insert Emoji">
                            <FaceSmileIcon style="width: 24px; height: 24px;" />
                        </button>
                        
                        <!-- Emoji Picker Popup -->
                        <div v-if="showEmojis" class="position-absolute bottom-100 start-0 mb-2 z-3 shadow">
                            <EmojiPicker @select="handleEmojiSelect" theme="light" />
                        </div>
                    </div>
                </div>

                <!-- Auto-growing Textarea -->
                <div class="flex-grow-1 min-w-0">
                    <textarea 
                        ref="textareaRef"
                        rows="1"
                        maxlength="2000"
                        class="form-control chat-input-field px-4 py-2" 
                        :placeholder="editingMessage ? 'Edit your message...' : 'Type a message...'" 
                        v-model="inputEl"
                        @input="handleInput"
                        @keydown="handleKeydown"
                    ></textarea>
                </div>

                <!-- Action Area -->
                <div v-if="editingMessage" class="d-flex align-items-center gap-2 pb-1 flex-shrink-0">
                    <button 
                        type="button" 
                        :disabled="!inputEl.trim()" 
                        @click="handleConfirmEdit" 
                        class="btn-modern-save"
                        title="Save edit"
                    >
                        <span>Save</span>
                    </button>
                </div>

                <div v-else class="pb-1 flex-shrink-0">
                    <button 
                        :disabled="(!inputEl.trim() && !selectedFile) || isUploading" 
                        type="submit"
                        class="btn-send-message"
                        title="Send message"
                    >
                        <span v-if="isUploading" class="spinner-border spinner-border-sm" role="status"></span>
                        <PaperAirplaneIcon v-else style="width: 20px; height: 20px;" />
                    </button>
                </div>
            </form>
        </template>
    </div>
</template>

<style scoped>
.chat-content-container {
    width: 100%;
    max-width: 860px;
    margin-left: auto;
    margin-right: auto;
}

.cursor-pointer {
    cursor: pointer;
}
.chat-input-wrapper {
    background-color: var(--bg-chat-header);
}
.message-form {
    background-color: transparent;
}

/* Blocked Status Indicator Styles */
.blocked-input-bar {
    background-color: var(--bg-input, #202c33);
    border: 1px solid var(--border-light, rgba(255, 255, 255, 0.08));
}

.text-main-theme {
    color: var(--text-primary, #e9edef);
}

.text-secondary-theme {
    color: var(--text-secondary, #8696a0);
}

.btn-unblock-pill {
    background-color: transparent;
    border: 1.5px solid var(--accent-primary, #d1a153);
    color: var(--accent-primary, #d1a153);
    border-radius: 20px;
    padding: 5px 16px;
    font-size: 0.8rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s ease;
}

.btn-unblock-pill:hover:not(:disabled) {
    background-color: var(--accent-primary, #d1a153);
    color: var(--text-inversed, #111b21);
    box-shadow: 0 2px 8px rgba(209, 161, 83, 0.3);
}

.fs-7 {
    font-size: 0.85rem;
}

/* Modern Top Banner (Replying & Editing) */
.banner-wrapper {
    background-color: rgba(255, 255, 255, 0.45);
    backdrop-filter: blur(8px);
    border-bottom: 1px solid var(--border-light);
    animation: slideDown 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}

.banner-accent-bar {
    width: 3.5px;
    height: 32px;
    background-color: var(--accent-primary);
    border-radius: 4px;
    flex-shrink: 0;
}

.banner-label {
    font-size: 0.72rem;
    font-weight: 700;
    letter-spacing: 0.6px;
    display: flex;
    align-items: center;
    color: var(--accent-primary);
    margin-bottom: 2px;
}

.banner-snippet {
    font-size: 0.85rem;
    color: var(--text-secondary);
    line-height: 1.3;
}

.btn-banner-close {
    width: 28px;
    height: 28px;
    border-radius: 50%;
    background-color: rgba(0, 0, 0, 0.05);
    border: none;
    color: var(--text-secondary);
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    flex-shrink: 0;
    transition: all 0.2s ease;
}

.btn-banner-close:hover {
    background-color: rgba(0, 0, 0, 0.12);
    color: var(--text-primary);
    transform: scale(1.05);
}

/* Modern Action Buttons */
.btn-modern-cancel {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 8px 14px;
    border-radius: 20px;
    background-color: rgba(255, 255, 255, 0.65);
    border: 1px solid var(--border-light);
    color: var(--text-secondary);
    font-size: 0.82rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s ease;
    white-space: nowrap;
}

.btn-modern-cancel:hover {
    background-color: #ffffff;
    color: var(--text-primary);
    border-color: var(--accent-primary);
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.06);
}

.btn-modern-save {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 8px 16px;
    border-radius: 20px;
    background-color: var(--accent-primary);
    border: none;
    color: var(--text-inversed);
    font-size: 0.82rem;
    font-weight: 600;
    cursor: pointer;
    box-shadow: 0 2px 6px hsla(38, 55%, 50%, 0.35);
    transition: all 0.2s ease;
    white-space: nowrap;
}

.btn-modern-save:hover:not(:disabled) {
    background-color: var(--accent-hover);
    box-shadow: 0 4px 10px hsla(38, 67%, 38%, 0.4);
    transform: translateY(-1px);
}

.btn-modern-save:disabled {
    opacity: 0.45;
    cursor: not-allowed;
    box-shadow: none;
    transform: none;
}

/* Media & Emoji Buttons */
.btn-add-media {
    width: 38px;
    height: 38px;
    border-radius: 50%;
    background-color: var(--bg-input);
    border: 1px solid var(--border-light);
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--accent-primary);
    transition: all 0.2s ease;
}
.btn-add-media:hover {
    background-color: var(--accent-primary);
    color: var(--text-inversed);
}
.btn-emoji {
    background: transparent;
    border: none;
    color: var(--text-secondary);
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    padding: 6px;
    border-radius: 50%;
    transition: color 0.2s ease, background-color 0.2s ease;
}
.btn-emoji:hover {
    color: var(--accent-primary);
    background-color: rgba(0, 0, 0, 0.05);
}

/* Chat Input Field & Scrollbar */
.chat-input-field {
    background-color: var(--bg-input) !important;
    color: var(--text-primary) !important;
    border: 1px solid var(--border-light) !important;
    border-radius: 22px !important;
    outline: none;
    box-shadow: none !important;
    resize: none;
    min-height: 42px;
    max-height: 96px;
    line-height: 1.4;
    overflow-y: auto;
}
.chat-input-field::-webkit-scrollbar {
    width: 4px;
}
.chat-input-field::-webkit-scrollbar-thumb {
    background-color: var(--border-light);
    border-radius: 4px;
}
.chat-input-field::placeholder {
    color: var(--text-muted);
}
.chat-input-field:focus {
    border-color: var(--accent-primary) !important;
    box-shadow: 0 0 0 3px hsla(38, 55%, 50%, 0.15) !important;
}

.btn-send-message {
    width: 42px;
    height: 42px;
    border-radius: 50%;
    background-color: var(--accent-primary);
    color: var(--text-inversed);
    border: none;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    transition: all 0.2s ease;
    flex-shrink: 0;
}
.btn-send-message:hover:not(:disabled) {
    background-color: var(--accent-hover);
}
.btn-send-message:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

@keyframes slideDown {
    from {
        opacity: 0;
        transform: translateY(-6px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}
</style>
