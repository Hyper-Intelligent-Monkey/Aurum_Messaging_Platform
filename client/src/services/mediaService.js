import { apiFetch, apiFetchBlob } from "./api";

// upload profile avatar
export async function uploadAvatar(file) {
    const formData = new FormData();
    formData.append("file", file);

    return await apiFetch("/media/upload/avatar", {
        method: "POST",
        body: formData,
    });
}

// upload file attachment in a chat
export async function uploadAttachment(file, conversationId = null) {
    const formData = new FormData();
    formData.append("file", file);
    if (conversationId) {
        formData.append("conversationId", conversationId);
    }

    return await apiFetch("/media/upload/attachment", {
        method: "POST",
        body: formData,
    });
}

// delete a file
export async function deleteFile(filePath) {
    return await apiFetch(`/media/delete/${filePath}`, {
        method: "DELETE",
    });
}

// clean download path
function normalizeFilePath(filePath) {
    if (!filePath) return "";
    let clean = filePath;
    if (clean.startsWith("/api/media/download/")) {
        clean = clean.replace("/api/media/download/", "");
    } else if (clean.startsWith("api/media/download/")) {
        clean = clean.replace("api/media/download/", "");
    }
    return clean;
}

// construct public media URL for every file type
export function getFileUrl(filePath) {
    if (!filePath) return "";
    if (filePath.startsWith("http")) return filePath;
    const clean = normalizeFilePath(filePath);
    const token = localStorage.getItem("jwt_token");
    const baseUrl = `${import.meta.env.VITE_API_URL}/media/download/${clean}`;
    return token ? `${baseUrl}?access_token=${encodeURIComponent(token)}` : baseUrl;
}

// download protected file attachment
export async function downloadFileBlob(filePath) {
    const clean = normalizeFilePath(filePath);
    return await apiFetchBlob(`/media/download/${clean}`);
}

const FAILED_AVATARS_KEY = "failed_avatar_urls";

// get failed avatars
function getFailedAvatars() {
    try {
        const stored = sessionStorage.getItem(FAILED_AVATARS_KEY);
        return stored ? JSON.parse(stored) : [];
    } catch {
        return [];
    }
}

// check failed avatar from loaded users
export function isAvatarFailed(url) {
    if (!url) return true;
    const list = getFailedAvatars();
    return list.includes(url);
}

// mark and persist failed avatars
export function markAvatarFailed(url) {
    if (!url) return;
    const list = getFailedAvatars();
    if (!list.includes(url)) {
        list.push(url);
        try {
            sessionStorage.setItem(FAILED_AVATARS_KEY, JSON.stringify(list));
        } catch {}
    }
}

// Central in-memory cache of loaded avatar URLs shared across all components and users
const loadedAvatarCache = new Set();

// Check if an avatar URL has already been loaded anywhere in the current session
export function isAvatarLoaded(url) {
    if (!url) return false;
    return loadedAvatarCache.has(url);
}

// Mark an avatar URL as loaded in memory
export function markAvatarLoaded(url) {
    if (url) {
        loadedAvatarCache.add(url);
    }
}

