import { apiFetch } from "./api";

// update avatar
export async function updateAvatar(storedFileName) {
    const data = await apiFetch("/users/avatar", {
        method: "PUT",
        body: JSON.stringify({ storedFileName })
    });
    return data;
}

// set initial password for accounts without one (e.g. Google OAuth)
export async function setPassword(newPassword) {
    const data = await apiFetch("/users/set-password", {
        method: "POST",
        body: JSON.stringify({ newPassword })
    });
    return data;
}

// change user password
export async function changePassword(currentPassword, newPassword) {
    const data = await apiFetch("/users/password", {
        method: "PUT",
        body: JSON.stringify({ currentPassword, newPassword })
    });
    return data;
}

// search registered users on platform
export async function searchUsers(query, limit = 10, offset = 0) {
    const data = await apiFetch(`/users/search?query=${encodeURIComponent(query)}&limit=${limit}&offset=${offset}`, {
        method: "GET",
    });
    return data;
}

// block a user
export async function blockUser(userId) {
    const data = await apiFetch(`/users/block/${userId}`, {
        method: "POST",
    })
    return data;
}

// unblock a user
export async function unblockUser(userId) {
    const data = await apiFetch(`/users/unblock/${userId}`, {
        method: "DELETE",
    })
    return data;
}

// get all blocked users
export async function getBlockedUsers() {
    const data = await apiFetch(`/users/blocked`, {
        method: "GET",
    })
    return data;
}

// get bidirectional block status
export async function getBlockStatus(userId) {
    const data = await apiFetch(`/users/block-status/${userId}`, {
        method: "GET",
    });
    return data;
}

