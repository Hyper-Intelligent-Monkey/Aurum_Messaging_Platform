import { apiFetch } from "./api";

// get all messages in a conversation
export async function getMessages(conversationId, limit = 50, before = null) {
    let url = `/messages/${conversationId}?limit=${limit}`;
    if (before) {
        url += `&before=${encodeURIComponent(before)}`;
    }
    return await apiFetch(url, {
        method: "GET",
    })
}

// send a message
export async function sendMessage(messageData){
    return await apiFetch("/messages", {
        method: "POST",
        body: JSON.stringify(messageData),
    })
}

// edit a message
export async function editMessage(messageId, content) {
    return await apiFetch("/messages", {
        method: "PUT",
        body: JSON.stringify({ 
            messageId, 
            newContent: content, 
            content 
        }),
    });
}

// delete a message
export async function deleteMessage(messageId) {
    return await apiFetch(`/messages/${messageId}`, {
        method: "DELETE",
    })
}

// mark a message as seen
export async function markAsSeen(conversationId) {
    return await apiFetch(`/messages/read/${conversationId}`, {
        method: "POST",
    })
}