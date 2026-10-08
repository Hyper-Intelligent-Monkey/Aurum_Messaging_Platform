import Logo from "../assets/MessagingPlatformLogo.png";
import { getFileUrl } from "./mediaService";
import { useAuthStore } from "../store/authStore";

let originalTitle = typeof document !== "undefined" ? document.title : "Messaging Platform";
let titleInterval = null;

const PREFS_KEY = "user_notification_preferences";

// gets user notification preferences in local storage
export function getNotificationPreferences() {
    try {
        const raw = localStorage.getItem(PREFS_KEY);
        if (raw) {
            const parsed = JSON.parse(raw);
            return {
                sound: parsed.sound ?? true,
                desktop: parsed.desktop ?? true,
                tabTitle: parsed.tabTitle ?? true,
                vibrate: parsed.vibrate ?? true,
            };
        }
    } catch {}
    return { sound: true, desktop: true, tabTitle: true, vibrate: true };
}


export function saveNotificationPreferences(prefs) {
    try {
        localStorage.setItem(PREFS_KEY, JSON.stringify(prefs));
    } catch {}
}

// Check if device vibration is supported
export function isVibrationSupported() {
    return typeof navigator !== "undefined" && "vibrate" in navigator;
}

// Vibrate device on incoming message
export function vibrateDevice(pattern = [200, 100, 200]) {
    if (isVibrationSupported()) {
        try {
            navigator.vibrate(pattern);
        } catch {}
    }
}

// Check if Notification API is available in browser
export function isNotificationSupported() {
    return typeof window !== "undefined" && "Notification" in window;
}

// Get browser notification permission
export function getNotificationPermission() {
    if (!isNotificationSupported()) return "denied";
    return Notification.permission;
}

// Request permission on the browser
export async function requestNotificationPermission() {
    if (!isNotificationSupported()) return false;
    if (Notification.permission === "granted") return true;

    try {
        const permission = await Notification.requestPermission();
        return permission === "granted";
    } catch {
        return false;
    }
}

let audioCtx = null;

// Get audio context for allowing notification sounds
function getAudioContext() {
    if (!audioCtx && typeof window !== "undefined") {
        const AudioCtx = window.AudioContext || window.webkitAudioContext;
        if (AudioCtx) {
            audioCtx = new AudioCtx();
        }
    }
    return audioCtx;
}

// Play pleasant chime using Web Audio API
export async function playNotificationSound() {
    try {
        const ctx = getAudioContext(); // Get audio context
        if (!ctx) return;
        if (ctx.state === "suspended") {
            await ctx.resume();
        }
        const now = ctx.currentTime;

        // Tone 1
        const osc1 = ctx.createOscillator();
        const gain1 = ctx.createGain();
        osc1.type = "sine";
        osc1.frequency.setValueAtTime(800, now);
        gain1.gain.setValueAtTime(0.15, now);
        gain1.gain.exponentialRampToValueAtTime(0.001, now + 0.25);
        osc1.connect(gain1);
        gain1.connect(ctx.destination);
        osc1.start(now);
        osc1.stop(now + 0.25);

        // Tone 2
        const osc2 = ctx.createOscillator();
        const gain2 = ctx.createGain();
        osc2.type = "sine";
        osc2.frequency.setValueAtTime(1060, now + 0.1);
        gain2.gain.setValueAtTime(0.15, now + 0.1);
        gain2.gain.exponentialRampToValueAtTime(0.001, now + 0.35);
        osc2.connect(gain2);
        gain2.connect(ctx.destination);
        osc2.start(now + 0.1);
        osc2.stop(now + 0.35);
    } catch {
        // Ignored if audio context cannot play before user interaction
    }
}

// Temporarily flash tab title until window is focused
export function flashTabTitle(senderName) {
    if (typeof document === "undefined") return;
    // no notifications if window is already focused
    if (document.hasFocus() && document.visibilityState === "visible") return;

    if (titleInterval) clearInterval(titleInterval);

    let toggle = false;
    // flash tab title every second
    titleInterval = setInterval(() => {
        document.title = toggle ? `💬 (${senderName})` : "New Message!";
        toggle = !toggle;
    }, 1000);

    // remove tab title flash on window focus
    const onFocus = () => {
        clearInterval(titleInterval);
        titleInterval = null;
        document.title = originalTitle;
        window.removeEventListener("focus", onFocus);
    };
    window.addEventListener("focus", onFocus);
}

// Main notification dispatcher
export function showIncomingMessageNotification(message, sender, conversation, currentUserId, activeConversationId, onClick) {
    if (!message) return;

    const authStore = useAuthStore();
    const myId = currentUserId ?? authStore.user?.id;
    const myUsername = authStore.user?.username;

    // Don't notify if the current user sent it
    const isSentByMe = (myId != null && Number(message.senderId) === Number(myId)) ||
                       (myUsername && message.senderName && myUsername.toLowerCase() === message.senderName.toLowerCase());
    if (isSentByMe) return;

    // Don't notify if conversation is muted
    if (conversation?.isMuted) return;

    const prefs = getNotificationPreferences();

    // Notification sound if enabled
    if (prefs.sound) {
        playNotificationSound();
    }

    // Vibrate device if enabled
    if (prefs.vibrate) {
        vibrateDevice();
    }

    const senderName = sender?.username || message.senderName || "New message";

    // Flash tab title if enabled and window is unfocused
    const isAppFocused = typeof document !== "undefined" && document.hasFocus() && document.visibilityState === "visible";
    if (prefs.tabTitle && !isAppFocused) {
        flashTabTitle(senderName);
    }

    // System Desktop Notification check
    if (!prefs.desktop || !isNotificationSupported() || Notification.permission !== "granted") return;

    // Suppress system desktop popup only if the user is already actively focused inside this conversation
    const isViewingThisChat = Number(activeConversationId) === Number(message.conversationId);
    if (document.hasFocus() && isViewingThisChat) return;

    // Format preview text
    let bodyText = message.content || "";
    if (message.type !== 0 && !message.content) {
        bodyText = "Sent a file attachment";
    }

    // Resolve avatar URL
    const iconUrl = sender?.avatar ? getFileUrl(sender.avatar) : new URL(Logo, window.location.origin).href;

    try {
        // creates a notification popup in the device
        const notification = new Notification(senderName, {
            body: bodyText,
            icon: iconUrl,
            tag: `chat-${message.conversationId}`, // group messages by chat
            renotify: true
        });

        notification.onclick = () => {
            window.focus();
            if (typeof onClick === "function") {
                onClick(message.conversationId);
            }
            notification.close();
        };
    } catch (err) {
        console.warn("Failed to create system notification:", err);
    }
}