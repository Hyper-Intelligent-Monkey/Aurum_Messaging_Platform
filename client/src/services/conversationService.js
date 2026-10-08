import { apiFetch } from "./api";

// get all conversations
export async function getConversations(limit = 30, before = null) {
    let url = `/conversations?limit=${limit}`;
    if (before) {
        url += `&before=${encodeURIComponent(before)}`;
    }
    return await apiFetch(url, {
        method: "GET",
    });
}

// get a single conversation
export async function getConversationById(id) {
    return await apiFetch(`/conversations/${id}`, {
        method: "GET",
    })
}

// search contacted users
export async function searchConversations(query, limit = 10, before = null) {
    let url = `/conversations/search?query=${encodeURIComponent(query)}&limit=${limit}`;
    if (before) {
        url += `&before=${encodeURIComponent(before)}`;
    }
    return await apiFetch(url, {
        method: "GET",
    });
}

// mute a conversation
export async function muteConversation(conversationId, minutes = null) {
    let url = `/conversations/${conversationId}/mute`;
    if (minutes) {
        url += `?minutes=${minutes}`;
    }
    return await apiFetch(url, {
        method: "POST",
    });
}

// unmute a conversation
export async function unmuteConversation(conversationId) {
    return await apiFetch(`/conversations/${conversationId}/unmute`, {
        method: "POST",
    });
}

// get paginated conversation media
export async function getConversationMedia(conversationId, limit = 25, before = null) {
    let url = `/conversations/${conversationId}/media?limit=${limit}`;
    if (before) {
        url += `&before=${encodeURIComponent(before)}`;
    }
    return await apiFetch(url, {
        method: "GET",
    });
}