<script setup>
import { ref, computed, watch } from "vue";
import { 
    CameraIcon, 
    TrashIcon, 
    KeyIcon, 
    UserIcon, 
    EnvelopeIcon, 
    CheckCircleIcon,
    ExclamationCircleIcon,
    EyeIcon,
    EyeSlashIcon,
    MagnifyingGlassPlusIcon,
    MagnifyingGlassMinusIcon,
    BellIcon
} from "@heroicons/vue/24/outline";
import { useAuthStore } from "../store/authStore";
import { useUserStore } from "../store/userStore";
import { useToastStore } from "../store/toastStore";
import { getFileUrl, isAvatarFailed, markAvatarFailed, isAvatarLoaded, markAvatarLoaded } from "../services/mediaService";
import { changePassword, setPassword } from "../services/userService";
import { 
    getNotificationPermission, 
    requestNotificationPermission,
    getNotificationPreferences,
    saveNotificationPreferences,
    isNotificationSupported,
    isVibrationSupported
} from "../services/notificationService";
import BaseModal from "../components/BaseModal.vue";

const authStore = useAuthStore();
const userStore = useUserStore();
const toastStore = useToastStore();

// Avatar state
const selectedFile = ref(null);
const previewBlob = ref("");
const isUploading = ref(false);

// Crop Modal state
const showCropModal = ref(false);
const rawImageSrc = ref("");
const rawImageElement = ref(null);
const cropScale = ref(1);
const cropOffset = ref({ x: 0, y: 0 });
const isDragging = ref(false);
const dragStart = ref({ x: 0, y: 0 });

// Remove Avatar Modal state
const showRemoveAvatarModal = ref(false);

// Password modal state
const showPasswordModal = ref(false);
const currentPassword = ref("");
const newPassword = ref("");
const confirmPassword = ref("");
const showCurrentPassword = ref(false);
const showNewPassword = ref(false);
const showConfirmPassword = ref(false);
const isSavingPassword = ref(false);
const passwordError = ref("");
const passwordSuccess = ref("");

const isLoaded = ref(false);
const hasImageError = ref(false);

const user = computed(() => authStore.user);
const hasPassword = computed(() => !!authStore.user?.hasPassword);
const notifPermission = ref(getNotificationPermission());
const notifPrefs = ref(getNotificationPreferences());
const isSupported = ref(isNotificationSupported());
const hasVibrationSupport = ref(isVibrationSupported());

// save notification preferences
const handleSavePrefs = () => {
    saveNotificationPreferences(notifPrefs.value);
};

// toggle notification popup
const handleToggleDesktop = async (event) => {
    const shouldEnable = event.target.checked;
    if (shouldEnable) {
        if (notifPermission.value === "denied") {
            toastStore.error("Notifications are blocked by your browser settings. Please allow notifications for this site.");
            notifPrefs.value.desktop = false;
            handleSavePrefs();
            return;
        }
        if (notifPermission.value !== "granted") {
            const granted = await requestNotificationPermission();
            notifPermission.value = getNotificationPermission();
            if (!granted) {
                notifPrefs.value.desktop = false;
                handleSavePrefs();
                return;
            }
        }
        notifPrefs.value.desktop = true;
        handleSavePrefs();
    } else {
        notifPrefs.value.desktop = false;
        handleSavePrefs();
    }
};

// determine avatar URL
const avatarUrl = computed(() => {
    if (previewBlob.value) {
        return previewBlob.value;
    }
    if (user.value?.avatar) {
        return getFileUrl(user.value.avatar);
    }
    return null;
});

// watch for avatar changes
watch(avatarUrl, (newUrl) => {
    isLoaded.value = isAvatarLoaded(newUrl);
    hasImageError.value = isAvatarFailed(newUrl);
}, { immediate: true });

const handleImageLoad = () => {
    isLoaded.value = true;
    markAvatarLoaded(avatarUrl.value);
};

const handleImageError = () => {
    hasImageError.value = true;
    markAvatarFailed(avatarUrl.value);
};

// File Selection -> Open Crop Modal
const handleFileSelect = (event) => {
    const file = event.target.files[0];
    if (file) {
        // only allow images up to 5MB
        if (file.size > 5 * 1024 * 1024) {
            toastStore.error("Avatar image exceeds the 5MB limit.");
            event.target.value = "";
            return;
        }

        const url = URL.createObjectURL(file);
        rawImageSrc.value = url;
        cropScale.value = 1;
        cropOffset.value = { x: 0, y: 0 };
        
        const img = new Image();
        img.onload = () => {
            rawImageElement.value = img;
            showCropModal.value = true;
        };
        img.src = url;
    }
    // Reset file input value so selecting the same file again works
    event.target.value = "";
};

// Helper to clamp drag offsets so image edges never enter the 220px circle mask
const clampOffset = (tx, ty, scale) => {
    if (!rawImageElement.value) return { x: tx, y: ty };
    const img = rawImageElement.value;
    const imgRatio = img.naturalWidth / img.naturalHeight;
    const baseW = imgRatio > 1 ? 220 * imgRatio : 220;
    const baseH = imgRatio > 1 ? 220 : 220 / imgRatio;
    
    // 220px circle mask radius is 110px from frame center (140px)
    const maxTx = (baseW * scale) / 2 - 110;
    const maxTy = (baseH * scale) / 2 - 110;

    return {
        x: Math.max(-maxTx, Math.min(maxTx, tx)),
        y: Math.max(-maxTy, Math.min(maxTy, ty))
    };
};

// Computed style for cropper image element
const cropImageStyle = computed(() => {
    if (!rawImageElement.value) return {};
    const img = rawImageElement.value;
    const imgRatio = img.naturalWidth / img.naturalHeight;
    const baseW = imgRatio > 1 ? 220 * imgRatio : 220;
    const baseH = imgRatio > 1 ? 220 : 220 / imgRatio;
    const left = (280 - baseW) / 2;
    const top = (280 - baseH) / 2;

    return {
        width: `${baseW}px`,
        height: `${baseH}px`,
        left: `${left}px`,
        top: `${top}px`,
        transform: `translate(${cropOffset.value.x}px, ${cropOffset.value.y}px) scale(${cropScale.value})`,
        transformOrigin: "center center"
    };
});

// Drag & Drop handlers for crop viewport
const startDrag = (event) => {
    isDragging.value = true;
    const clientX = event.touches ? event.touches[0].clientX : event.clientX;
    const clientY = event.touches ? event.touches[0].clientY : event.clientY;
    dragStart.value = {
        x: clientX - cropOffset.value.x,
        y: clientY - cropOffset.value.y
    };
};

const onDrag = (event) => {
    if (!isDragging.value) return;
    const clientX = event.touches ? event.touches[0].clientX : event.clientX;
    const clientY = event.touches ? event.touches[0].clientY : event.clientY;
    const rawX = clientX - dragStart.value.x;
    const rawY = clientY - dragStart.value.y;
    cropOffset.value = clampOffset(rawX, rawY, cropScale.value);
};

const stopDrag = () => {
    isDragging.value = false;
};

// zoom in/out
const onWheel = (event) => {
    const delta = event.deltaY < 0 ? 0.05 : -0.05;
    const newScale = parseFloat(Math.min(Math.max(1, cropScale.value + delta), 3).toFixed(2));
    cropScale.value = newScale;
    cropOffset.value = clampOffset(cropOffset.value.x, cropOffset.value.y, newScale);
};

// update zoom scale
const updateZoomScale = (val) => {
    const newScale = parseFloat(val);
    cropScale.value = newScale;
    cropOffset.value = clampOffset(cropOffset.value.x, cropOffset.value.y, newScale);
};

// Confirm Crop & Generate Cropped Image File (Pixel-Perfect Math)
const confirmCrop = () => {
    if (!rawImageElement.value) return;

    const img = rawImageElement.value;
    const canvas = document.createElement("canvas");
    const outputSize = 300; // Output cropped avatar dimensions
    canvas.width = outputSize;
    canvas.height = outputSize;
    const ctx = canvas.getContext("2d");

    const imgRatio = img.naturalWidth / img.naturalHeight;
    const baseW = imgRatio > 1 ? 220 * imgRatio : 220;
    const baseH = imgRatio > 1 ? 220 : 220 / imgRatio;
    const s = cropScale.value;
    const tx = cropOffset.value.x;
    const ty = cropOffset.value.y;

    // Convert frame circle top-left (30px, 30px) to base element coordinates
    const elemX = baseW / 2 - (110 + tx) / s;
    const elemY = baseH / 2 - (110 + ty) / s;

    // Scale to natural raw image dimensions
    const scaleFactor = img.naturalWidth / baseW;
    const srcX = elemX * scaleFactor;
    const srcY = elemY * scaleFactor;
    const srcW = (220 / s) * scaleFactor;
    const srcH = (220 / s) * scaleFactor;

    // Render clipped circular avatar output on canvas
    ctx.beginPath();
    ctx.arc(outputSize / 2, outputSize / 2, outputSize / 2, 0, Math.PI * 2);
    ctx.closePath();
    ctx.clip();

    ctx.drawImage(img, srcX, srcY, srcW, srcH, 0, 0, outputSize, outputSize);

    canvas.toBlob((blob) => {
        if (blob) {
            const croppedFile = new File([blob], "cropped_avatar.png", { type: "image/png" });
            selectedFile.value = croppedFile;
            previewBlob.value = URL.createObjectURL(blob);
        }
        showCropModal.value = false;
    }, "image/png");
};

// cancel crop
const cancelCrop = () => {
    showCropModal.value = false;
    rawImageSrc.value = "";
    rawImageElement.value = null;
};

// save avatar
const handleSaveAvatar = async () => {
    if (!selectedFile.value) return;
    isUploading.value = true;
    try {
        await userStore.updateUserAvatar(selectedFile.value);
        selectedFile.value = null;
        previewBlob.value = "";
    } catch (err) {
        console.error("Failed to update profile picture:", err);
    } finally {
        isUploading.value = false;
    }
};

const openRemoveAvatarModal = () => {
    showRemoveAvatarModal.value = true;
};

const closeRemoveAvatarModal = () => {
    showRemoveAvatarModal.value = false;
};

// remove avatar
const confirmRemoveAvatar = async () => {
    isUploading.value = true;
    try {
        await userStore.removeUserAvatar();
        previewBlob.value = "";
        selectedFile.value = null;
        showRemoveAvatarModal.value = false;
        toastStore.success("Profile picture removed");
    } catch (err) {
        console.error("Failed to remove avatar:", err);
        toastStore.error(err.message || "Failed to remove avatar");
    } finally {
        isUploading.value = false;
    }
};

const openPasswordModal = () => {
    passwordError.value = "";
    passwordSuccess.value = "";
    currentPassword.value = "";
    newPassword.value = "";
    confirmPassword.value = "";
    showPasswordModal.value = true;
};

const closePasswordModal = () => {
    showPasswordModal.value = false;
};

// change password using current password changed with new password
const handleChangePassword = async () => {
    passwordError.value = "";
    passwordSuccess.value = "";

    if (hasPassword.value && !currentPassword.value) {
        passwordError.value = "Please enter your current password.";
        return;
    }
    if (!newPassword.value) {
        passwordError.value = "Please enter a new password.";
        return;
    }
    if (newPassword.value.length < 8) {
        passwordError.value = "New password must be at least 8 characters long.";
        return;
    }
    if (hasPassword.value && newPassword.value === currentPassword.value) {
        passwordError.value = "New password cannot be the same as current password.";
        return;
    }
    if (newPassword.value !== confirmPassword.value) {
        passwordError.value = "New password and confirmation do not match.";
        return;
    }

    isSavingPassword.value = true;
    try {
        if (hasPassword.value) {
            await changePassword(currentPassword.value, newPassword.value);
            toastStore.success("Password updated successfully!");
        } else {
            await setPassword(newPassword.value);
            authStore.user.hasPassword = true;
            localStorage.setItem("user", JSON.stringify(authStore.user));
            toastStore.success("Password set successfully! You can now log in with email and password.");
        }
        currentPassword.value = "";
        newPassword.value = "";
        confirmPassword.value = "";
        closePasswordModal();
    } catch (err) {
        passwordError.value = err.message || (hasPassword.value ? "Failed to update password. Please check your current password." : "Failed to set password.");
    } finally {
        isSavingPassword.value = false;
    }
};

const hasAvatar = computed(() => {
    return !!(previewBlob.value || user.value?.avatar) && !hasImageError.value;
});
</script>

<template>
    <div class="profile-page h-100 d-flex flex-column">

        <div class="profile-content p-4 flex-grow-1 d-flex flex-column align-items-center ">
            <div class="profile-card shadow-lg p-4 p-md-5 rounded-4 text-center">
                <!-- Profile Title -->
                <div class="text-center mb-4 pb-2 border-bottom-theme">
                    <h4 class="fw-bold mb-0 text-accent">Profile Settings</h4>
                </div>

                <!-- Avatar Section -->
                <div class="avatar-section position-relative d-inline-block mx-auto mb-3">
                    <img 
                        v-if="hasAvatar"
                        :src="avatarUrl" 
                        alt="Profile Picture" 
                        class="avatar-img rounded-circle shadow"
                        loading="eager"
                        decoding="async"
                        referrerpolicy="no-referrer"
                        @load="handleImageLoad"
                        @error="handleImageError"
                    />

                    <div v-else class="avatar-fallback shadow d-flex align-items-center justify-content-center mx-auto">
                        <UserIcon style="width: 52px; height: 52px;" class="text-secondary" />
                    </div>
                    
                    <label 
                        for="avatarUpload" 
                        class="upload-badge position-absolute bottom-0 end-0 rounded-circle cursor-pointer shadow hover-scale"
                        title="Upload New Photo"
                    >
                        <CameraIcon style="width: 20px; height: 20px;" />
                        <input type="file" @change="handleFileSelect" hidden id="avatarUpload" accept="image/*" />
                    </label>
                </div>

                <!-- Avatar Actions -->
                <div v-if="selectedFile" class="mb-4">
                    <button 
                        @click="handleSaveAvatar" 
                        class="btn btn-accent btn-sm fw-semibold me-2"
                        :disabled="isUploading"
                    >
                        {{ isUploading ? "Saving..." : "Save New Photo" }}
                    </button>
                    <button 
                        @click="selectedFile = null; previewBlob = ''" 
                        class="btn btn-outline-theme btn-sm"
                        :disabled="isUploading"
                    >
                        Cancel
                    </button>
                </div>

                <div v-else-if="user?.avatar" class="mb-4">
                    <button 
                        @click="openRemoveAvatarModal" 
                        class="btn btn-outline-danger-theme btn-sm d-inline-flex align-items-center gap-1"
                        :disabled="isUploading"
                    >
                        <TrashIcon style="width: 14px; height: 14px;" />
                        Remove Avatar
                    </button>
                </div>
                <div v-else class="mb-4"></div>

                <!-- User Details Card -->
                <div class="info-card p-3 rounded-3 text-start mb-4">
                    <div class="d-flex align-items-center gap-3 mb-3">
                        <div class="icon-box rounded-circle p-2 d-flex align-items-center justify-content-center">
                            <UserIcon style="width: 20px; height: 20px;" />
                        </div>
                        <div class="flex-grow-1 min-w-0">
                            <small class="text-secondary-theme d-block text-uppercase fw-semibold" style="font-size: 0.7rem;">Username</small>
                            <span class="fs-6 fw-semibold text-main-theme text-break">{{ user?.username || "N/A" }}</span>
                        </div>
                    </div>

                    <div class="d-flex align-items-center gap-3">
                        <div class="icon-box rounded-circle p-2 d-flex align-items-center justify-content-center">
                            <EnvelopeIcon style="width: 20px; height: 20px;" />
                        </div>
                        <div class="flex-grow-1 min-w-0">
                            <small class="text-secondary-theme d-block text-uppercase fw-semibold" style="font-size: 0.7rem;">Email Address</small>
                            <span class="fs-6 text-main-theme text-opacity-75 text-break">{{ user?.email || "N/A" }}</span>
                        </div>
                    </div>
                </div>

                <!-- Change Password Action Button -->
                <div>
                    <button 
                        @click="openPasswordModal" 
                        class="btn btn-change-password w-100 fw-semibold d-flex align-items-center justify-content-center gap-2 py-2 rounded-3"
                    >
                        <KeyIcon style="width: 18px; height: 18px;" />
                        <span>{{ hasPassword ? "Change Password" : "Set Password" }}</span>
                    </button>
                </div>

                <!-- Notification Preferences Card -->
                <div class="info-card p-3 rounded-3 text-start mt-4 mb-2">
                    <div class="d-flex align-items-center justify-content-between mb-2 pb-2 border-bottom" style="border-color: var(--border-light, rgba(255, 255, 255, 0.1)) !important;">
                        <h6 class="fw-semibold text-main-theme mb-0 d-flex align-items-center gap-2" style="font-size: 0.85rem;">
                            <BellIcon style="width: 16px; height: 16px;" class="text-accent-theme" />
                            <span>Notification Preferences</span>
                        </h6>
                    </div>

                    <!-- Device Notifications -->
                    <div class="d-flex align-items-center justify-content-between py-2 border-bottom" style="border-color: var(--border-light, rgba(255, 255, 255, 0.08)) !important;">
                        <div class="min-w-0 pe-2 flex-grow-1">
                            <span class="fs-6 fw-semibold text-main-theme d-block" style="font-size: 0.9rem !important;">Device Notifications</span>
                            <small class="text-secondary-theme d-block" style="font-size: 0.72rem;">Banners on your device</small>
                            <div class="mt-1 d-flex gap-1 align-items-center">
                                <span v-if="notifPermission === 'denied'" class="badge bg-danger text-white" style="font-size: 0.65rem;">Blocked in browser settings</span>
                                <span v-else-if="!isSupported" class="badge bg-secondary text-white" style="font-size: 0.65rem;">Not supported by browser</span>
                                <span v-else-if="notifPermission === 'granted' && notifPrefs.desktop" class="badge bg-success bg-opacity-75 text-white" style="font-size: 0.65rem;">Enabled</span>
                                <span v-else-if="notifPermission === 'default'" class="badge bg-warning text-dark" style="font-size: 0.65rem;">Permission needed</span>
                            </div>
                        </div>
                        <div class="form-check form-switch m-0 flex-shrink-0">
                            <input 
                                class="form-check-input custom-switch" 
                                :class="{ 'cursor-pointer': notifPermission !== 'denied' && isSupported }"
                                type="checkbox" 
                                :checked="notifPrefs.desktop && notifPermission === 'granted'"
                                :disabled="notifPermission === 'denied' || !isSupported"
                                @change="handleToggleDesktop"
                            />
                        </div>
                    </div>

                    <!-- Message Sound -->
                    <div class="d-flex align-items-center justify-content-between py-2 border-bottom" style="border-color: var(--border-light, rgba(255, 255, 255, 0.08)) !important;">
                        <div class="min-w-0 pe-2">
                            <span class="fs-6 fw-semibold text-main-theme d-block" style="font-size: 0.9rem !important;">Message Sounds</span>
                            <small class="text-secondary-theme" style="font-size: 0.72rem;">Play audio chime on messages</small>
                        </div>
                        <div class="form-check form-switch m-0 flex-shrink-0">
                            <input 
                                class="form-check-input custom-switch cursor-pointer" 
                                type="checkbox" 
                                v-model="notifPrefs.sound"
                                @change="handleSavePrefs"
                            />
                        </div>
                    </div>

                    <!-- Tab Flashing -->
                    <div class="d-flex align-items-center justify-content-between py-2" :class="{ 'border-bottom': hasVibrationSupport }" style="border-color: var(--border-light, rgba(255, 255, 255, 0.08)) !important;">
                        <div class="min-w-0 pe-2">
                            <span class="fs-6 fw-semibold text-main-theme d-block" style="font-size: 0.9rem !important;">Tab Title Alerts</span>
                            <small class="text-secondary-theme" style="font-size: 0.72rem;">Flash tab when in background</small>
                        </div>
                        <div class="form-check form-switch m-0 flex-shrink-0">
                            <input 
                                class="form-check-input custom-switch cursor-pointer" 
                                type="checkbox" 
                                v-model="notifPrefs.tabTitle"
                                @change="handleSavePrefs"
                            />
                        </div>
                    </div>

                    <!-- Device Vibration (Mobile / Tablets) -->
                    <div v-if="hasVibrationSupport" class="d-flex align-items-center justify-content-between pt-2">
                        <div class="min-w-0 pe-2">
                            <span class="fs-6 fw-semibold text-main-theme d-block" style="font-size: 0.9rem !important;">Device Vibration</span>
                            <small class="text-secondary-theme" style="font-size: 0.72rem;">Vibrate phone on incoming messages</small>
                        </div>
                        <div class="form-check form-switch m-0 flex-shrink-0">
                            <input 
                                class="form-check-input custom-switch cursor-pointer" 
                                type="checkbox" 
                                v-model="notifPrefs.vibrate"
                                @change="handleSavePrefs"
                            />
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Interactive Crop Avatar Modal -->
        <div v-if="showCropModal" class="modal-backdrop-theme d-flex align-items-center justify-content-center p-3">
            <div class="modal-card-theme p-4 rounded-4 shadow-lg w-100 text-center position-relative" style="max-width: 380px;">
                <!-- Modal Header -->
                <div class="d-flex align-items-center justify-content-between mb-3 pb-2 border-bottom-theme">
                    <h5 class="fw-bold mb-0 text-main-theme">Crop Profile Avatar</h5>
                </div>

                <!-- Interactive Crop Viewport Frame -->
                <div 
                    class="crop-viewport position-relative mx-auto rounded-3 mb-3 cursor-grab"
                    :class="{ 'cursor-grabbing': isDragging }"
                    @mousedown="startDrag"
                    @mousemove="onDrag"
                    @mouseup="stopDrag"
                    @mouseleave="stopDrag"
                    @touchstart.passive="startDrag"
                    @touchmove.passive="onDrag"
                    @touchend="stopDrag"
                    @wheel.prevent="onWheel"
                >
                    <!-- Background Movable Image -->
                    <img 
                        :src="rawImageSrc" 
                        alt="Raw Avatar"
                        class="crop-target-image position-absolute"
                        :style="cropImageStyle"
                    />

                    <!-- Circular Mask Overlay with 3x3 Grid Lines -->
                    <div class="circle-crop-mask position-absolute top-50 start-50 translate-middle pointer-events-none">
                        <div class="crop-grid-lines position-absolute w-100 h-100">
                            <div class="grid-line line-v-1"></div>
                            <div class="grid-line line-v-2"></div>
                            <div class="grid-line line-h-1"></div>
                            <div class="grid-line line-h-2"></div>
                        </div>
                    </div>
                </div>

                <p class="text-secondary-theme fs-7 mb-3">Drag image to adjust position & scroll to zoom</p>

                <!-- Zoom Controls -->
                <div class="d-flex align-items-center justify-content-center gap-2 mb-4 px-2">
                    <MagnifyingGlassMinusIcon class="text-secondary-theme" style="width: 18px; height: 18px;" />
                    <input 
                        type="range" 
                        min="1" 
                        max="3" 
                        step="0.05" 
                        :value="cropScale"
                        @input="updateZoomScale($event.target.value)"
                        class="form-range custom-zoom-slider flex-grow-1"
                    />
                    <MagnifyingGlassPlusIcon class="text-secondary-theme" style="width: 18px; height: 18px;" />
                </div>

                <!-- Actions -->
                <div class="d-flex gap-2">
                    <button @click="cancelCrop" class="btn btn-outline-theme w-50 fw-semibold">
                        Cancel
                    </button>
                    <button @click="confirmCrop" class="btn btn-accent w-50 fw-semibold">
                        Crop & Save
                    </button>
                </div>
            </div>
        </div>

        <!-- Custom Remove Avatar Confirmation Modal via BaseModal -->
        <BaseModal
            :is-open="showRemoveAvatarModal"
            title="Remove Profile Picture"
            action-text="Remove"
            action-variant="danger"
            size="sm"
            :is-loading="isUploading"
            @close="closeRemoveAvatarModal"
            @action="confirmRemoveAvatar"
        >
            <template #icon>
                <TrashIcon class="red-text" style="width: 20px; height: 20px;" />
            </template>

            <p class="text-secondary-theme fs-7 mb-0 text-start">
                Are you sure you want to remove your profile picture?
            </p>
        </BaseModal>

        <!-- Change Password Modal via BaseModal -->
        <BaseModal
            :is-open="showPasswordModal"
            :title="hasPassword ? 'Change Password' : 'Set Password'"
            :action-text="hasPassword ? 'Update Password' : 'Set Password'"
            :is-loading="isSavingPassword"
            size="sm"
            @close="closePasswordModal"
            @action="handleChangePassword"
        >
            <template #icon>
                <KeyIcon class="text-accent" style="width: 20px; height: 20px;" />
            </template>

            <!-- Success Alert -->
            <div v-if="passwordSuccess" class="alert alert-success-theme d-flex align-items-center gap-2 p-2 mb-3 rounded-3 fs-7">
                <CheckCircleIcon style="width: 18px; height: 18px; flex-shrink: 0;" />
                <span>{{ passwordSuccess }}</span>
            </div>

            <!-- Error Alert -->
            <div v-if="passwordError" class="alert alert-danger-theme d-flex align-items-center gap-2 p-2 mb-3 rounded-3 fs-7">
                <ExclamationCircleIcon style="width: 18px; height: 18px; flex-shrink: 0;" />
                <span>{{ passwordError }}</span>
            </div>

            <form @submit.prevent="handleChangePassword" autocomplete="off">
                <!-- Current Password (only required if user already has a password) -->
                <div v-if="hasPassword" class="mb-3 text-start">
                    <label class="form-label text-secondary-theme fs-7 mb-1">Current Password</label>
                    <div class="position-relative">
                        <input
                            :type="showCurrentPassword ? 'text' : 'password'" 
                            v-model="currentPassword"
                            class="form-control form-control-theme pe-5"
                            placeholder="Enter current password"
                            autocomplete="off"
                        />
                        <button 
                            type="button"
                            class="btn-toggle-eye position-absolute end-0 top-50 translate-middle-y me-2 border-0 bg-transparent"
                            @click="showCurrentPassword = !showCurrentPassword"
                        >
                            <component :is="showCurrentPassword ? EyeSlashIcon : EyeIcon" style="width: 18px; height: 18px;" />
                        </button>
                    </div>
                </div>

                <!-- New Password -->
                <div class="mb-3 text-start">
                    <label class="form-label text-secondary-theme fs-7 mb-1">New Password</label>
                    <div class="position-relative">
                        <input 
                            :type="showNewPassword ? 'text' : 'password'" 
                            v-model="newPassword"
                            class="form-control form-control-theme pe-5"
                            placeholder="Enter new password"
                            autocomplete="new-password"
                        />
                        <button 
                            type="button"
                            class="btn-toggle-eye position-absolute end-0 top-50 translate-middle-y me-2 border-0 bg-transparent"
                            @click="showNewPassword = !showNewPassword"
                        >
                            <component :is="showNewPassword ? EyeSlashIcon : EyeIcon" style="width: 18px; height: 18px;" />
                        </button>
                    </div>
                </div>

                <!-- Confirm Password -->
                <div class="mb-2 text-start">
                    <label class="form-label text-secondary-theme fs-7 mb-1">Confirm New Password</label>
                    <div class="position-relative">
                        <input 
                            :type="showConfirmPassword ? 'text' : 'password'" 
                            v-model="confirmPassword"
                            class="form-control form-control-theme pe-5"
                            placeholder="Confirm new password"
                            autocomplete="new-password"
                        />
                        <button 
                            type="button"
                            class="btn-toggle-eye position-absolute end-0 top-50 translate-middle-y me-2 border-0 bg-transparent"
                            @click="showConfirmPassword = !showConfirmPassword"
                        >
                            <component :is="showConfirmPassword ? EyeSlashIcon : EyeIcon" style="width: 18px; height: 18px;" />
                        </button>
                    </div>
                </div>
            </form>
        </BaseModal>
    </div>
</template>

<style scoped>
.red-text {
    color: var(--status-red);
}

.profile-page {
    height: 100%;
    width: 100%;
}

.profile-content {
    background-color: var(--bg-chat-area);
    height: 100%;
    width: 100%;
    overflow-y: auto;
}

.profile-card {
    background-color: var(--bg-chat-header, #111b21);
    border: 1px solid var(--border-light, rgba(255, 255, 255, 0.08));
    max-width: 680px;
    width: 100%;
}

.border-bottom-theme {
    border-bottom: 1px solid var(--border-light, rgba(255, 255, 255, 0.08));
}

.text-accent {
    color: var(--accent-primary, #d1a153);
}

.text-main-theme {
    color: var(--text-primary);
}

.text-secondary-theme {
    color: var(--text-secondary, #8696a0);
}

.avatar-img {
    width: 120px;
    height: 120px;
    object-fit: cover;
    border: 3px solid var(--accent-primary, #d1a153);
    transition: opacity 0.2s ease-in-out;
}

.avatar-fallback {
    width: 120px;
    height: 120px;
    border-radius: 50%;
    background-color: var(--bg-input, #202c33);
    border: 3px solid var(--accent-primary, #d1a153);
}

.upload-badge {
    width: 36px;
    height: 36px;
    display: flex;
    align-items: center;
    justify-content: center;
    background-color: var(--accent-primary, #d1a153);
    color: #111b21;
    border-radius: 50%;
    transition: transform 0.2s ease-in-out, background-color 0.2s ease;
}

.upload-badge:hover {
    transform: scale(1.1);
    background-color: var(--accent-hover, #b88c42);
}

.info-card {
    background-color: var(--bg-input, #202c33);
    border: 1px solid var(--border-light, rgba(255, 255, 255, 0.05));
}

.icon-box {
    background-color: var(--bg-chat-header, #111b21);
    color: var(--accent-primary, #d1a153);
}

.btn-change-password {
    background-color: var(--bg-input, #202c33);
    color: var(--accent-primary, #d1a153);
    border: 1px solid var(--accent-primary, #d1a153);
    transition: all 0.2s ease;
}

.btn-change-password:hover {
    background-color: var(--accent-primary, #d1a153);
    color: var(--text-inversed);
}

/* Crop Modal & Viewport Styles */
.crop-viewport {
    width: 280px;
    height: 280px;
    background-color: #0b141a;
    overflow: hidden;
    user-select: none;
    touch-action: none;
}

.cursor-grab {
    cursor: grab;
}

.cursor-grabbing {
    cursor: grabbing;
}

.crop-target-image {
    transition: transform 0.05s ease-out;
    pointer-events: none;
}

.circle-crop-mask {
    width: 220px;
    height: 220px;
    border-radius: 50%;
    box-shadow: 0 0 0 9999px rgba(11, 20, 26, 0.78);
    border: 2px solid var(--accent-primary, #d1a153);
}

.pointer-events-none {
    pointer-events: none;
}

.crop-grid-lines {
    top: 0;
    left: 0;
    border-radius: 50%;
    overflow: hidden;
}

.grid-line {
    position: absolute;
    background-color: rgba(255, 255, 255, 0.25);
}

.line-v-1 {
    top: 0;
    left: 33.33%;
    width: 1px;
    height: 100%;
}

.line-v-2 {
    top: 0;
    left: 66.66%;
    width: 1px;
    height: 100%;
}

.line-h-1 {
    top: 33.33%;
    left: 0;
    width: 100%;
    height: 1px;
}

.line-h-2 {
    top: 66.66%;
    left: 0;
    width: 100%;
    height: 1px;
}

.custom-zoom-slider {
    accent-color: var(--accent-primary, #d1a153);
}

.custom-zoom-slider::-webkit-slider-thumb {
    background-color: var(--accent-primary, #d1a153);
}

.custom-zoom-slider::-moz-range-thumb {
    background-color: var(--accent-primary, #d1a153);
}

/* Modal styles */
.modal-backdrop-theme {
    position: fixed;
    top: 0;
    left: 0;
    width: 100vw;
    height: 100vh;
    background-color: rgba(0, 0, 0, 0.65);
    backdrop-filter: blur(4px);
    z-index: 1050;
}

.modal-card-theme {
    background-color: var(--bg-chat-header, #111b21);
    border: 1px solid var(--border-light, rgba(255, 255, 255, 0.12));
    color: var(--text-primary, #e9edef);
}

.btn-close-theme {
    color: var(--text-secondary);
    transition: color 0.2s ease;
}

.btn-close-theme:hover {
    color: var(--text-primary, #e9edef);
}

.form-control-theme {
    background-color: var(--bg-input, #202c33);
    border: 1px solid var(--border-light, rgba(255, 255, 255, 0.1));
    color: var(--text-primary, #e9edef);
}

.form-control-theme:focus {
    background-color: var(--bg-input, #202c33);
    border-color: var(--accent-primary, #d1a153);
    color: var(--text-primary, #e9edef);
    box-shadow: 0 0 0 0.2rem rgba(209, 161, 83, 0.25);
}

.form-control-theme::placeholder {
    color: var(--text-secondary, #8696a0);
}

.btn-toggle-eye {
    color: var(--text-secondary, #8696a0);
    cursor: pointer;
}

.btn-toggle-eye:hover {
    color: var(--text-primary, #e9edef);
}

.btn-accent {
    background-color: var(--accent-primary);
    color: var(--text-inversed);
    border: none;
}

.btn-accent:hover:not(:disabled), .btn-accent:focus:not(:disabled) {
    background-color: var(--accent-hover);
    color: var(--text-inversed);
    border: none;
}

.btn-outline-theme {
    color: var(--text-secondary, #8696a0);
    border: 1px solid var(--border-light, rgba(255, 255, 255, 0.15));
}

.btn-outline-theme:hover, .btn-outline-theme:focus {
    color: var(--text-primary, #e9edef);
    background-color: rgba(255, 255, 255, 0.05);
}

.btn-outline-danger-theme {
    color: var(--status-red);
    border: 1px solid var(--status-red);
}

.btn-outline-danger-theme:hover {
    background-color: rgba(239, 68, 68, 0.1);
}

.alert-success-theme {
    background-color: rgba(34, 197, 94, 0.15);
    border: 1px solid rgba(34, 197, 94, 0.3);
    color: #4ade80;
}

.alert-danger-theme {
    background-color: rgba(239, 68, 68, 0.15);
    border: 1px solid rgba(239, 68, 68, 0.3);
    color: #f87171;
}

.fs-7 {
    font-size: 0.85rem;
}

.custom-switch{
    cursor: pointer;
}

.custom-switch:checked {
    background-color: var(--accent-primary, #d1a153) !important;
    border-color: var(--accent-primary, #d1a153) !important;
}

.form-check-input.custom-switch:focus {
    outline: 0 !important;
    box-shadow: none !important;
    border-color: var(--border-light, rgba(255, 255, 255, 0.25)) !important;
}

.form-check-input.custom-switch:checked:focus {
    border-color: var(--accent-primary, #d1a153) !important;
    box-shadow: none !important;
}
</style>