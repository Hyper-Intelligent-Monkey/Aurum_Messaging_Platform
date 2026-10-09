import { defineStore, acceptHMRUpdate } from 'pinia';
import { useAuthStore } from './authStore';
import { getConversations, getConversationById, searchConversations, muteConversation, unmuteConversation, getConversationMedia } from '../services/conversationService';
import { getMessages, sendMessage, editMessage, deleteMessage, markAsSeen } from '../services/messageService';
import { uploadAttachment, downloadFileBlob } from '../services/mediaService';
import { getBlockStatus, blockUser as blockUserApi, unblockUser as unblockUserApi } from '../services/userService';
import { signalrService } from '../services/signalrService';
import { showIncomingMessageNotification } from '../services/notificationService';
import router from '../router';

const recipientTypingTimeouts = new Map();

export const useChatStore = defineStore('chat', {
    state: () => ({
        conversations: [],
        activeConversationId: null,
        messages: [],
        conversationMedia: [],
        loadingConversations: false,
        loadingMoreConversations: false,
        hasMoreConversations: true,
        loadingMessages: false,
        loadingOlderMessages: false,
        hasMoreMessages: true,
        loadingMedia: false,
        hasMoreMedia: true,
        sendingMessage: false,
        typingUsers: {},
        blockStatusMap: {},
    }),

    getters: {
        // get the active conversation
        activeConversation: (state) => {
            return state.conversations.find(c => c.id === state.activeConversationId) || null;
        },

        // check if partner in active conversation is currently typing
        isPartnerTyping: (state) => {
            if (!state.activeConversationId) return false;
            const convTyping = state.typingUsers[state.activeConversationId];
            if (!convTyping) return false;
            const authStore = useAuthStore();
            const currentUserId = authStore.user?.id;
            return Object.entries(convTyping).some(([userId, isTyping]) => Number(userId) !== currentUserId && isTyping);
        },
    },

    actions: {
        // these async functions are requests to the server

        // fetch all conversations
        async fetchConversations(){
            this.loadingConversations = true;
            try {
                const data = await getConversations(30);
                if (data && Array.isArray(data)) {
                    this.conversations = data.sort((a, b) => {
                        const timeA = new Date(a.lastMessage?.sentAt || a.createdAt).getTime();
                        const timeB = new Date(b.lastMessage?.sentAt || b.createdAt).getTime();
                        return timeB - timeA;
                    });
                    // If we received a full page of 30, there may be more
                    this.hasMoreConversations = data.length === 30;
                    data.forEach(c => {
                        signalrService.joinConversation(c.id).catch(() => {});
                    });
                } else {
                    this.conversations = [];
                    this.hasMoreConversations = false;
                }
            } catch (error) {
                console.error("Failed to load conversations:", error);
                this.conversations = [];
                this.hasMoreConversations = false;
            } finally {
                this.loadingConversations = false;
            }
        },

        // Silently sync conversations in background to discover newly created chats
        async syncConversations() {
            try {
                const data = await getConversations(30);
                if (data && Array.isArray(data)) {
                    // Create a map of existing conversations to avoid duplicates
                    const existingMap = new Map(this.conversations.map(c => [c.id, c]));
                    // Check if there are any new conversations
                    let hasChanges = false;
                    

                    data.forEach(newConv => {
                        // Check if the conversation already exists in the list
                        const existing = existingMap.get(newConv.id);
                        if (!existing) {
                            hasChanges = true;
                            // add new conversation to the list
                            this.conversations.push(newConv);
                            // join the conversation or in the rooms to monitor new messages
                            signalrService.joinConversation(newConv.id).catch(() => {});

                            // Trigger notification for the first message from this new contact
                            if (newConv.lastMessage) {
                                const currentUserId = useAuthStore().user?.id;
                                // Check if the message is from another user to prevent
                                // alerts for messages you sent yourself.
                                const isFromOther = Number(newConv.lastMessage.senderId) !== Number(currentUserId);
                                if (isFromOther) {
                                    // this is better for conversation group chats, but using just "sender" for this would be enough

                                    // indicates the users who are in the conversation except the current user
                                    const partner = newConv.participants?.find(p => Number(p.userId) !== Number(currentUserId));
                                    // indicates the user who sent the message
                                    const sender = newConv.participants?.find(p => Number(p.userId) === Number(newConv.lastMessage.senderId)) || { username: newConv.lastMessage.senderName };
                                    
                                    // Show notification for the first message from this new contact
                                    showIncomingMessageNotification(
                                        newConv.lastMessage,
                                        sender,
                                        newConv,
                                        currentUserId,
                                        this.activeConversationId,
                                        // When the user clicks on the notification, select the conversation
                                        async (conversationId) => {
                                            if (partner?.username) {
                                                await this.selectConversation(conversationId);
                                                router.push({ name: 'chat', params: { username: partner.username } });
                                            }
                                        }
                                    );
                                }
                            }
                            // If the last message or unread count has changed, update the conversation
                        } else if (newConv.lastMessage?.id !== existing.lastMessage?.id || newConv.unreadCount !== existing.unreadCount) {
                            existing.lastMessage = newConv.lastMessage;
                            existing.unreadCount = newConv.unreadCount;
                            hasChanges = true;
                        }
                    });

                    if (hasChanges) {
                        // Sort conversations from newest to oldest
                        this.conversations = data.sort((a, b) => {
                            const timeA = new Date(a.lastMessage?.sentAt || a.createdAt).getTime();
                            const timeB = new Date(b.lastMessage?.sentAt || b.createdAt).getTime();
                            return timeB - timeA;
                        });
                        this.conversations = [...this.conversations];
                    }
                }
            } catch {
                // Silent catch for background sync
            }
        },

        // select a conversation from the conversation list
        async selectConversation(conversationId) {
            try {
                if (this.activeConversationId !== conversationId) {
                    this.conversationMedia = [];
                    this.hasMoreMedia = true;
                }
                const exists = this.conversations.some(c => c.id === conversationId);
                if (!exists) {
                    await this.fetchConversationDetails(conversationId);
                }

                this.activeConversationId = conversationId;

                // Execute WebSocket join and message fetching in parallel
                await Promise.all([
                    signalrService.joinConversation(conversationId).catch(err => console.warn("SignalR join non-fatal:", err)),
                    this.fetchMessages(conversationId)
                ]);

                // Fire mark seen in background without blocking
                this.markConversationAsSeen(conversationId).catch(() => {});
            } catch (err) {
                console.error("Failed to select conversation:", err);
            }
        },

        // select a conversation by recipient username from the path parameter
        async selectConversationByUsername(username, loadMessages = true) {
            if (!this.conversations || this.conversations.length === 0) {
                await this.fetchConversations();
            }

            const conv = this.conversations.find(c => 
                c.participants.some(p => p.username.toLowerCase() === username.toLowerCase())
            );

            if (conv) {
                // If this conversation is already active with messages loaded, skip redundant re-fetch
                if (this.activeConversationId === conv.id && (!loadMessages || this.messages.length > 0)) {
                    return conv;
                }
                if (loadMessages) {
                    await this.selectConversation(conv.id);
                } else {
                    this.activeConversationId = conv.id;
                    signalrService.joinConversation(conv.id).catch(() => {});
                }
                return conv;
            }
            return null;
        },

        // fetch messages in a conversation
        async fetchMessages(conversationId) {
            this.loadingMessages = true;
            try {
                const msgs = await getMessages(conversationId, 25);
                // Sort ascending by sent date (oldest at top, latest at bottom)
                this.messages = msgs && Array.isArray(msgs) 
                    ? msgs.slice().sort((a, b) => new Date(a.sentAt) - new Date(b.sentAt)) 
                    : [];
                this.hasMoreMessages = msgs && msgs.length >= 25;
            } catch (error) {
                console.error("Failed to load messages:", error);
                this.messages = [];
                this.hasMoreMessages = false;
            } finally {
                this.loadingMessages = false;
            }
        },

        // load older messages before the oldest loaded message
        async loadOlderMessages() {
            if (!this.activeConversationId || this.loadingOlderMessages || !this.hasMoreMessages) return 0;
            if (this.messages.length === 0) return 0;

            const oldestMessage = this.messages[0];
            if (!oldestMessage || !oldestMessage.sentAt) return 0;

            const beforeIso = new Date(oldestMessage.sentAt).toISOString();

            this.loadingOlderMessages = true;
            try {
                const olderMsgs = await getMessages(this.activeConversationId, 25, beforeIso);
                if (!olderMsgs || olderMsgs.length === 0) {
                    this.hasMoreMessages = false;
                    return 0;
                }

                const sortedOlder = olderMsgs.slice().sort((a, b) => new Date(a.sentAt) - new Date(b.sentAt));
                const existingIds = new Set(this.messages.map(m => m.id));
                const newOlder = sortedOlder.filter(m => !existingIds.has(m.id));

                if (newOlder.length === 0 || olderMsgs.length < 25) {
                    this.hasMoreMessages = false;
                }

                if (newOlder.length > 0) {
                    this.messages = [...newOlder, ...this.messages];
                }
                return newOlder.length;
            } catch (error) {
                console.error("Failed to load older messages:", error);
                return 0;
            } finally {
                this.loadingOlderMessages = false;
            }
        },

        // mark a conversation as seen
        async markConversationAsSeen(conversationId) {
            try {
                await markAsSeen(conversationId);
                const conv = this.conversations.find(c => c.id === conversationId);
                if (conv) {
                    conv.unreadCount = 0;
                }
            } catch (error) {
                console.error("Failed to mark conversation as seen:", error);
            }
        },

        // send a text message
        async sendTextMessage(recipientId, content, parentMessageId = null) {
            if (!content.trim()) return;
            if (!this.activeConversationId && !recipientId) return;

            this.sendingMessage = true;
            try {
                const messageData = {
                    conversationId: this.activeConversationId || null,
                    recipientId: recipientId || null,
                    content,
                    parentMessageId
                };
                const sentMessage = await sendMessage(messageData);
                if (sentMessage) {
                    if (!this.activeConversationId && sentMessage.conversationId) {
                        this.activeConversationId = sentMessage.conversationId;
                        try {
                            await signalrService.joinConversation(sentMessage.conversationId);
                        } catch (sigErr) {
                            console.warn("Could not join conversation SignalR group:", sigErr);
                        }
                        await this.fetchConversationDetails(sentMessage.conversationId);
                    }
                    if (!this.messages.some(m => m.id === sentMessage.id)) {
                        this.messages.push(sentMessage);
                    }
                    const conv = this.conversations.find(c => c.id === sentMessage.conversationId);
                    if (conv) {
                        conv.lastMessage = sentMessage;
                        conv.unreadCount = 0;
                    }
                }
                return sentMessage;
            } catch (error) {
                console.error("Failed to send message:", error);
                throw error;
            } finally {
                this.sendingMessage = false;
            }
        },

        // send a media message
        async sendMediaMessage(recipientId, file, caption = "", parentMessageId = null) {
            if (!this.activeConversationId && !recipientId) return;
            this.sendingMessage = true;
            try {
                const uploadResult = await uploadAttachment(file, this.activeConversationId);
                
                let detectedType = "Document";
                if (file.type.startsWith("image/")) {
                    detectedType = "Image";
                } else if (file.type.startsWith("video/")) {
                    detectedType = "Video";
                } else if (file.type.startsWith("audio/")) {
                    detectedType = "Audio";
                }

                const messageData = {
                    conversationId: this.activeConversationId || null,
                    recipientId: recipientId || null,
                    content: caption.trim() || null,
                    messageType: detectedType,
                    parentMessageId,
                    storedFileName: uploadResult.storedFileName,
                    originalFileName: uploadResult.originalFileName,
                    fileSize: uploadResult.fileSize,
                    contentType: uploadResult.contentType,
                };

                const sentMessage = await sendMessage(messageData);
                if (sentMessage) {
                    if (!this.activeConversationId && sentMessage.conversationId) {
                        this.activeConversationId = sentMessage.conversationId;
                        try {
                            await signalrService.joinConversation(sentMessage.conversationId);
                        } catch (sigErr) {
                            console.warn("Could not join conversation SignalR group:", sigErr);
                        }
                        await this.fetchConversationDetails(sentMessage.conversationId);
                    }
                    if (!this.messages.some(m => m.id === sentMessage.id)) {
                        this.messages.push(sentMessage);
                    }
                }
                return sentMessage;
            } catch (error) {
                console.error("Failed to send media message:", error);
                throw error;
            } finally {
                this.sendingMessage = false;
            }
        },

        // edit a user message
        async editUserMessage(messageId, content) {
            try {
                const updatedMessage = await editMessage(messageId, content);
                const msg = this.messages.find(m => m.id === messageId);
                if (msg) {
                    msg.content = updatedMessage.content;
                    msg.isEdited = updatedMessage.isEdited;
                }
                const conv = this.conversations.find(c => c.id === this.activeConversationId);
                if (conv?.lastMessage?.id === messageId) {
                    conv.lastMessage.content = updatedMessage.content;
                    conv.lastMessage.isEdited = updatedMessage.isEdited;
                }
            } catch (error) {
                console.error("Failed to edit message:", error);
                throw error;
            }
        },

        // delete a sent message
        async deleteUserMessage(messageId) {
            try {
                await deleteMessage(messageId);
                const target = this.messages.find(m => m.id === messageId);
                if (target) {
                    target.isDeleted = true;
                    target.content = "This message was deleted";
                    target.storedFileName = null;
                    target.fileUrl = null;
                    target.originalFileName = null;
                }
                if (this.activeConversationId) {
                    const conv = this.conversations.find(c => c.id === this.activeConversationId);
                    if (conv?.lastMessage?.id === messageId) {
                        conv.lastMessage.isDeleted = true;
                        conv.lastMessage.content = "This message was deleted";
                    }
                }
            } catch (error) {
                console.error("Failed to delete message:", error);
                throw error;
            }
        },

        // send a typing indicator
        async sendTypingIndicator(isTyping) {
            if (this.activeConversationId) {
                await signalrService.sendTyping(this.activeConversationId, isTyping);
            }
        },

        // mute a conversation
        async muteUserConversation(conversationId, minutes = null) {
            try {
                await muteConversation(conversationId, minutes);
                const conv = this.conversations.find(c => c.id === conversationId);
                if (conv) {
                    conv.isMuted = true;
                    conv.mutedUntil = minutes ? new Date(Date.now() + minutes * 60 * 1000).toString() : null;
                }
            } catch (error) {
                console.error("Failed to mute conversation:", error);
            }
        },

        // unmute a conversation
        async unmuteUserConversation(conversationId) {
            try {
                await unmuteConversation(conversationId);
                const conv = this.conversations.find(c => c.id === conversationId);
                if (conv) {
                    conv.isMuted = false;
                    conv.mutedUntil = null;
                }
            } catch (error) {
                console.error("Failed to unmute conversation:", error);
            }
        },

        // fetch and cache bidirectional block status in memory
        async fetchBlockStatus(userId) {
            if (!userId) return null;
            try {
                const status = await getBlockStatus(userId);
                this.blockStatusMap = {
                    ...this.blockStatusMap,
                    [userId]: status
                };
                return status;
            } catch (err) {
                console.error("Failed to fetch block status:", err);
                return null;
            }
        },

        // block user and immediately update store
        async blockUser(userId) {
            if (!userId) return;
            await blockUserApi(userId);
            this.blockStatusMap = {
                ...this.blockStatusMap,
                [userId]: {
                    ...(this.blockStatusMap[userId] || {}),
                    isBlockedByMe: true
                }
            };
        },

        // unblock user and immediately update store
        async unblockUser(userId) {
            if (!userId) return;
            await unblockUserApi(userId);
            this.blockStatusMap = {
                ...this.blockStatusMap,
                [userId]: {
                    ...(this.blockStatusMap[userId] || {}),
                    isBlockedByMe: false
                }
            };
        },

        // fetch conversation details
        async fetchConversationDetails(conversationId) {
            try {
                const conversation = await getConversationById(conversationId);
                const index = this.conversations.findIndex(c => c.id === conversationId);
                if (index !== -1) {
                    this.conversations[index] = conversation;
                } else {
                    this.conversations.unshift(conversation);
                }
                signalrService.joinConversation(conversationId).catch(() => {});
                return conversation;
            } catch (err) {
                console.error("Failed to fetch conversation details:", err);
            }
        },

        // fetch conversation media
        async fetchConversationMedia(conversationId, limit = 30, isLoadMore = false) {
            if (!isLoadMore) {
                this.conversationMedia = [];
                this.hasMoreMedia = true;
            }

            if (!this.hasMoreMedia || this.loadingMedia) return;

            this.loadingMedia = true;
            try {
                const before = isLoadMore && this.conversationMedia.length > 0
                    ? this.conversationMedia[this.conversationMedia.length - 1].sentAt
                    : null;

                const mediaItems = await getConversationMedia(conversationId, limit, before);

                if (mediaItems.length < limit) {
                    this.hasMoreMedia = false;
                }

                if (isLoadMore) {
                    this.conversationMedia.push(...mediaItems);
                } else {
                    this.conversationMedia = mediaItems;
                }
            } catch (error) {
                console.error("Failed to load conversation media:", error);
            } finally {
                this.loadingMedia = false;
            }
        },

        // download a file attachment
        async downloadAttachmentFile(filePath, originalFileName) {
            try {
                const blob = await downloadFileBlob(filePath);

                const url = window.URL.createObjectURL(blob);
                const link = document.createElement('a');
                link.href = url;
                link.setAttribute('download', originalFileName);
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                window.URL.revokeObjectURL(url);
            } catch (error) {
                console.error("Failed to download file:", error);
            }
        },

        // load older conversations
        async loadMoreConversations() {
            if (this.loadingMoreConversations || !this.hasMoreConversations || this.conversations.length === 0) {
                return;
            }

            this.loadingMoreConversations = true;
            try {
                // Find the oldest loaded conversation to use as the cursor
                const oldest = this.conversations[this.conversations.length - 1];
                const cursor = oldest?.lastMessage?.sentAt || oldest?.createdAt;
                if (!cursor) {
                    this.hasMoreConversations = false;
                    return;
                }

                const data = await getConversations(30, cursor);
                if (data && Array.isArray(data) && data.length > 0) {
                    // Prevent duplicate entries
                    const existingIds = new Set(this.conversations.map(c => c.id));
                    const newConvs = data.filter(c => !existingIds.has(c.id));

                    this.conversations.push(...newConvs);
                    this.conversations.sort((a, b) => {
                        const timeA = new Date(a.lastMessage?.sentAt || a.createdAt).getTime();
                        const timeB = new Date(b.lastMessage?.sentAt || b.createdAt).getTime();
                        return timeB - timeA;
                    });
                    this.hasMoreConversations = data.length === 30;
                } else {
                    this.hasMoreConversations = false;
                }
            } catch (error) {
                console.error("Failed to load more conversations:", error);
            } finally {
                this.loadingMoreConversations = false;
            }
        },

        // this is for receiving persistent responses from the server
        initSignalRListeners() {
            this.removeSignalRListeners();

            // reflect incoming messages in the UI
            this.onReceiveMessage = (message) => {
                // Immediately clear typing indicator for the sender when their message arrives
                const typingKey = `${message.conversationId}_${message.senderId}`;
                if (recipientTypingTimeouts.has(typingKey)) {
                    clearTimeout(recipientTypingTimeouts.get(typingKey));
                    recipientTypingTimeouts.delete(typingKey);
                }
                if (this.typingUsers[message.conversationId]?.[message.senderId]) {
                    const convTyping = { ...(this.typingUsers[message.conversationId] || {}) };
                    delete convTyping[message.senderId];
                    this.typingUsers = {
                        ...this.typingUsers,
                        [message.conversationId]: convTyping
                    };
                }

                if (message.conversationId === this.activeConversationId) {
                    if (!this.messages.some(m => m.id === message.id)) {
                        this.messages.push(message);
                    }
                    this.markConversationAsSeen(message.conversationId);
                }

                const convIndex = this.conversations.findIndex(c => c.id === message.conversationId);
                let conv = null;
                if (convIndex !== -1) {
                    const [existing] = this.conversations.splice(convIndex, 1);
                    existing.lastMessage = message;
                    if (this.activeConversationId !== message.conversationId) {
                        existing.unreadCount = (existing.unreadCount || 0) + 1;
                    }
                    this.conversations.unshift(existing);
                    conv = existing;
                } else {
                    this.fetchConversationDetails(message.conversationId);
                }

                // Trigger Browser Notification & Audio Chime
                const currentUserId = useAuthStore().user?.id;
                const partner = conv?.participants?.find(p => p.userId !== currentUserId);
                const sender = conv?.participants?.find(p => p.userId === message.senderId) || { username: message.senderName };

                showIncomingMessageNotification(
                    message,
                    sender,
                    conv,
                    currentUserId,
                    this.activeConversationId,
                    async (conversationId) => {
                        if (partner?.username) {
                            await this.selectConversation(conversationId);
                            router.push({ name: 'chat', params: { username: partner.username } });
                        }
                    }
                );
            };

            // reflect typing indicators in the UI
            this.onUserTyping = ({ conversationId, userId, isTyping }) => {
                const typingKey = `${conversationId}_${userId}`;
                if (recipientTypingTimeouts.has(typingKey)) {
                    clearTimeout(recipientTypingTimeouts.get(typingKey));
                    recipientTypingTimeouts.delete(typingKey);
                }

                const convTyping = { ...(this.typingUsers[conversationId] || {}) };
                if (isTyping) {
                    convTyping[userId] = true;
                    // Auto-expire after 3.5s safety fallback
                    const timer = setTimeout(() => {
                        if (this.typingUsers[conversationId]?.[userId]) {
                            const updatedTyping = { ...(this.typingUsers[conversationId] || {}) };
                            delete updatedTyping[userId];
                            this.typingUsers = {
                                ...this.typingUsers,
                                [conversationId]: updatedTyping
                            };
                        }
                        recipientTypingTimeouts.delete(typingKey);
                    }, 3500);
                    recipientTypingTimeouts.set(typingKey, timer);
                } else {
                    delete convTyping[userId];
                }
                this.typingUsers = {
                    ...this.typingUsers,
                    [conversationId]: convTyping
                };
            };

            // reflect user presence changes in the UI
            this.onUserPresenceChanged = ({ userId, isOnline, lastSeen }) => {
                let updated = false;
                this.conversations.forEach(conv => {
                    const participant = conv.participants?.find(p => Number(p.userId) === Number(userId));
                    if (participant) {
                        participant.isOnline = isOnline;
                        participant.lastSeen = lastSeen;
                        updated = true;
                    }
                });
                if (updated) {
                    this.conversations = [...this.conversations];
                }
            };

            // reflect user avatar changes in the UI
            this.onUserAvatarChanged = ({ userId, avatar }) => {
                let updated = false;
                this.conversations.forEach(conv => {
                    const participant = conv.participants?.find(p => Number(p.userId) === Number(userId));
                    if (participant) {
                        participant.avatar = avatar;
                        updated = true;
                    }
                });
                if (updated) {
                    this.conversations = [...this.conversations];
                }
            };

            // reflect messages seen status in the UI
            this.onMessagesSeen = ({ conversationId, seenByUserId }) => {
                const currentUserId = useAuthStore().user?.id;
                if (conversationId === this.activeConversationId) {
                    // only mark sent messages as seen when the other user viewed them
                    if (seenByUserId && seenByUserId !== currentUserId) {
                        this.messages.forEach(m => {
                            if (m.senderId === currentUserId) {
                                m.isSeen = true;
                            }
                        });
                    }
                }

                // If WE were the one who viewed them, reset our unread badge for this conversation
                if (seenByUserId === currentUserId) {
                    const conv = this.conversations.find(c => c.id === conversationId);
                    if (conv) {
                        conv.unreadCount = 0;
                    }
                }
            };

            // reflect message deletions in the UI
            this.onMessageDeleted = ({ messageId, conversationId }) => {
                if (conversationId === this.activeConversationId) {
                    const target = this.messages.find(m => m.id === messageId);
                    if (target) {
                        target.isDeleted = true;
                        target.content = "This message was deleted";
                        target.storedFileName = null;
                        target.fileUrl = null;
                        target.originalFileName = null;
                    }
                }
                const conv = this.conversations.find(c => c.id === conversationId);
                if (conv?.lastMessage?.id === messageId) {
                    conv.lastMessage.isDeleted = true;
                    conv.lastMessage.content = "This message was deleted";
                }
            };

            // reflect message edits in the UI
            this.onMessageEdited = (updatedMessage) => {
                // Immediately clear typing indicator for the editor when their edit arrives
                const typingKey = `${updatedMessage.conversationId}_${updatedMessage.senderId}`;
                if (recipientTypingTimeouts.has(typingKey)) {
                    clearTimeout(recipientTypingTimeouts.get(typingKey));
                    recipientTypingTimeouts.delete(typingKey);
                }
                // once the message is updated, remove the sender's typing indicator
                if (this.typingUsers[updatedMessage.conversationId]?.[updatedMessage.senderId]) {
                    // make a copy of the typing users object because it can cause reactive issues
                    const convTyping = { ...(this.typingUsers[updatedMessage.conversationId] || {}) };
                    delete convTyping[updatedMessage.senderId];
                    this.typingUsers = {
                        ...this.typingUsers,
                        [updatedMessage.conversationId]: convTyping
                    };
                }

                if (updatedMessage.conversationId === this.activeConversationId) {
                    const target = this.messages.find(m => m.id === updatedMessage.id);
                    if (target) {
                        target.content = updatedMessage.content;
                        target.isEdited = updatedMessage.isEdited;
                    }
                }
                const conv = this.conversations.find(c => c.id === updatedMessage.conversationId);
                if (conv?.lastMessage?.id === updatedMessage.id) {
                    conv.lastMessage.content = updatedMessage.content;
                    conv.lastMessage.isEdited = updatedMessage.isEdited;
                }
            };

            // listeners for incoming events from the server
            signalrService.on("ReceiveMessage", this.onReceiveMessage);
            signalrService.on("UserTyping", this.onUserTyping);
            signalrService.on("UserPresenceChanged", this.onUserPresenceChanged);
            signalrService.on("UserAvatarChanged", this.onUserAvatarChanged);
            signalrService.on("MessagesSeen", this.onMessagesSeen);
            signalrService.on("MessageDeleted", this.onMessageDeleted);
            signalrService.on("MessageEdited", this.onMessageEdited);
        },

        // remove specific signalr listeners, disconnect works the same thing but for all handlers
        removeSignalRListeners() {
            if (this.onReceiveMessage) signalrService.off("ReceiveMessage", this.onReceiveMessage);
            if (this.onUserTyping) signalrService.off("UserTyping", this.onUserTyping);
            if (this.onUserPresenceChanged) signalrService.off("UserPresenceChanged", this.onUserPresenceChanged);
            if (this.onUserAvatarChanged) signalrService.off("UserAvatarChanged", this.onUserAvatarChanged);
            if (this.onMessagesSeen) signalrService.off("MessagesSeen", this.onMessagesSeen);
            if (this.onMessageDeleted) signalrService.off("MessageDeleted", this.onMessageDeleted);
            if (this.onMessageEdited) signalrService.off("MessageEdited", this.onMessageEdited);
        },
    }
});

// prevents code changes from reloading the page
if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useChatStore, import.meta.hot));
}