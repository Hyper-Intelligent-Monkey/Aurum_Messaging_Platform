<script setup>
import { ref, computed, nextTick, onMounted, onUnmounted, watch } from "vue";
import { 
    MagnifyingGlassPlusIcon, 
    MagnifyingGlassMinusIcon, 
    ArrowDownTrayIcon, 
    XMarkIcon,
    MusicalNoteIcon
} from "@heroicons/vue/24/outline";

const props = defineProps({
    visible: {
        type: Boolean,
        required: true
    },
    imageUrl: {
        type: String,
        default: ""
    },
    mediaUrl: {
        type: String,
        default: ""
    },
    mediaType: {
        type: String,
        default: "Image"
    },
    contentType: {
        type: String,
        default: ""
    },
    fileName: {
        type: String,
        default: "Attachment"
    }
});

const emit = defineEmits(["close", "download"]);

const activeUrl = computed(() => props.mediaUrl || props.imageUrl || "");

// indicate the type of media
const detectedType = computed(() => {
    if (props.mediaType && props.mediaType !== "Image" && props.mediaType !== "Document") {
        return props.mediaType.toLowerCase();
    }
    const ct = props.contentType?.toLowerCase() || "";
    const fn = props.fileName?.toLowerCase() || "";
    const url = activeUrl.value?.toLowerCase() || "";
    if (ct.startsWith("video/") || fn.match(/\.(mp4|webm|mov|mkv|avi)$/) || url.match(/\.(mp4|webm|mov|mkv|avi)/)) {
        return "video";
    }
    if (ct.startsWith("audio/") || fn.match(/\.(mp3|wav|ogg|m4a|aac)$/) || url.match(/\.(mp3|wav|ogg|m4a|aac)/)) {
        return "audio";
    }
    return "image";
});

const zoomScale = ref(1);
const viewportRef = ref(null);
const imageRef = ref(null);
const isDragging = ref(false);
const startX = ref(0);
const startY = ref(0);
const startScrollLeft = ref(0);
const startScrollTop = ref(0);

// Computed style for the zoomed image, makes size responsive
const imageZoomStyle = computed(() => {
    if (zoomScale.value <= 1) {
        return {
            maxWidth: 'calc(90vw - 120px)',
            maxHeight: 'calc(82vh - 120px)',
            width: 'auto',
            height: 'auto',
            cursor: 'default',
        };
    }
    return {
        width: `${zoomScale.value * 80}vw`,
        maxWidth: 'none',
        maxHeight: 'none',
        cursor: isDragging.value ? 'grabbing' : 'grab',
    };
});

// Watch for visibility of the media lightbox
watch(() => props.visible, (val) => {
    if (val) {
        zoomScale.value = 1;
        isDragging.value = false;
    }
});

// Close button handler
const close = () => {
    zoomScale.value = 1;
    isDragging.value = false;
    emit("close");
};

// Mouse drag start handler (for dragging the image)
const onPanStart = (e) => {
    if (detectedType.value !== 'image' || zoomScale.value <= 1 || !viewportRef.value) return;
    if (e.button && e.button !== 0) return;
    isDragging.value = true;
    const clientX = e.touches ? e.touches[0].clientX : e.clientX;
    const clientY = e.touches ? e.touches[0].clientY : e.clientY;
    startX.value = clientX;
    startY.value = clientY;
    startScrollLeft.value = viewportRef.value.scrollLeft;
    startScrollTop.value = viewportRef.value.scrollTop;
};

// Mouse drag move handler
const onPanMove = (e) => {
    if (!isDragging.value || zoomScale.value <= 1 || !viewportRef.value) return;
    if (e.cancelable) e.preventDefault();
    const clientX = e.touches ? e.touches[0].clientX : e.clientX;
    const clientY = e.touches ? e.touches[0].clientY : e.clientY;
    const deltaX = clientX - startX.value;
    const deltaY = clientY - startY.value;
    viewportRef.value.scrollLeft = startScrollLeft.value - deltaX;
    viewportRef.value.scrollTop = startScrollTop.value - deltaY;
};

// Mouse drag end handler
const onPanEnd = () => {
    isDragging.value = false;
};

// Centers the zoomed image
const centerZoom = () => {
    nextTick(() => {
        if (!viewportRef.value) return;
        const maxScrollX = viewportRef.value.scrollWidth - viewportRef.value.clientWidth;
        const maxScrollY = viewportRef.value.scrollHeight - viewportRef.value.clientHeight;
        if (maxScrollX > 0) viewportRef.value.scrollLeft = maxScrollX / 2;
        if (maxScrollY > 0) viewportRef.value.scrollTop = maxScrollY / 2;
    });
};

// Zoom in button handler
const zoomIn = (e) => {
    if (e) e.stopPropagation();
    zoomScale.value = Math.min(4, Math.round((zoomScale.value + 0.5) * 10) / 10);
    centerZoom();
};

// Zoom out button handler
const zoomOut = (e) => {
    if (e) e.stopPropagation();
    const next = Math.max(1, Math.round((zoomScale.value - 0.5) * 10) / 10);
    zoomScale.value = next;
    if (next === 1 && viewportRef.value) {
        viewportRef.value.scrollLeft = 0;
        viewportRef.value.scrollTop = 0;
    } else {
        centerZoom();
    }
};

// Reset zoom to 100%
const resetZoom = (e) => {
    if (e) e.stopPropagation();
    zoomScale.value = 1;
    if (viewportRef.value) {
        viewportRef.value.scrollLeft = 0;
        viewportRef.value.scrollTop = 0;
    }
};

const handleWheelZoom = (e) => {
    // Only run for images and when elements exist
    if (detectedType.value !== 'image' || !viewportRef.value || !imageRef.value) return;

    const imgRect = imageRef.value.getBoundingClientRect();
    const normX = Math.max(0, Math.min(1, (e.clientX - imgRect.left) / imgRect.width));
    const normY = Math.max(0, Math.min(1, (e.clientY - imgRect.top) / imgRect.height));

    // scroll wheel zoom
    const delta = e.deltaY < 0 ? 0.5 : -0.5;
    const nextScale = Math.min(4, Math.max(1, Math.round((zoomScale.value + delta) * 10) / 10));
    if (nextScale === zoomScale.value) return;

    zoomScale.value = nextScale;

    // handle reset
    if (nextScale === 1) {
        viewportRef.value.scrollLeft = 0;
        viewportRef.value.scrollTop = 0;
        return;
    }

    // enables zoom towards the mouse cursor
    nextTick(() => {
        if (!viewportRef.value || !imageRef.value) return;
        const newImgRect = imageRef.value.getBoundingClientRect();
        const currentTargetX = newImgRect.left + normX * newImgRect.width;
        const currentTargetY = newImgRect.top + normY * newImgRect.height;

        viewportRef.value.scrollLeft += currentTargetX - e.clientX;
        viewportRef.value.scrollTop += currentTargetY - e.clientY;
    });
};

const handleKeydown = (e) => {
    if (e.key === "Escape" && props.visible) {
        close();
    }
};
// listen for escape key
onMounted(() => {
    window.addEventListener("keydown", handleKeydown);
});
// remove lister for escape key
onUnmounted(() => {
    window.removeEventListener("keydown", handleKeydown);
});
</script>

<template>
    <Teleport to="body">
        <Transition name="fade-lightbox">
            <div 
                v-if="visible && activeUrl" 
                class="image-lightbox-overlay"
            >
                <div class="lightbox-wrapper position-relative d-flex flex-column align-items-center">
                    <div class="lightbox-top-bar d-flex justify-content-between align-items-center w-100 mb-2 px-1">
                        <span class="text-white-50 text-truncate pe-3 small">
                            {{ fileName }}
                        </span>
                        <div class="d-flex align-items-center gap-2">
                            <!-- Zoom Controls for Images -->
                            <template v-if="detectedType === 'image'">
                                <button 
                                    type="button" 
                                    class="lightbox-btn" 
                                    @click="zoomOut" 
                                    :disabled="zoomScale <= 1" 
                                    title="Zoom out"
                                >
                                    <MagnifyingGlassMinusIcon style="width: 19px; height: 19px;" />
                                </button>
                                <button 
                                    type="button" 
                                    class="lightbox-zoom-badge" 
                                    @click="resetZoom" 
                                    title="Reset zoom (100%)"
                                >
                                    {{ Math.round(zoomScale * 100) }}%
                                </button>
                                <button 
                                    type="button" 
                                    class="lightbox-btn" 
                                    @click="zoomIn" 
                                    :disabled="zoomScale >= 4" 
                                    title="Zoom in"
                                >
                                    <MagnifyingGlassPlusIcon style="width: 19px; height: 19px;" />
                                </button>
                            </template>

                            <!-- Download -->
                            <button 
                                type="button" 
                                class="lightbox-btn ms-1" 
                                @click="emit('download')" 
                                title="Download file"
                            >
                                <ArrowDownTrayIcon style="width: 19px; height: 19px;" />
                            </button>
                            <!-- Close -->
                            <button 
                                type="button" 
                                class="lightbox-btn" 
                                @click="close" 
                                title="Close (Esc)"
                            >
                                <XMarkIcon style="width: 22px; height: 22px;" />
                            </button>
                        </div>
                    </div>

                    <!-- 1. IMAGE DISPLAY -->
                    <div 
                        v-if="detectedType === 'image'"
                        ref="viewportRef"
                        class="lightbox-stage" 
                        :class="{ 'is-zoomed': zoomScale > 1 }"
                        @wheel.prevent="handleWheelZoom"
                        @mousedown="onPanStart"
                        @mousemove="onPanMove"
                        @mouseup="onPanEnd"
                        @mouseleave="onPanEnd"
                        @touchstart="onPanStart"
                        @touchmove="onPanMove"
                        @touchend="onPanEnd"
                    >
                        <div class="lightbox-canvas">
                            <img 
                                ref="imageRef"
                                :src="activeUrl" 
                                :alt="fileName" 
                                class="lightbox-image" 
                                :class="{ 'is-dragging': isDragging }"
                                :style="imageZoomStyle"
                                draggable="false"
                            />
                        </div>
                    </div>

                    <!-- 2. VIDEO DISPLAY -->
                    <div v-else-if="detectedType === 'video'" class="lightbox-stage d-flex align-items-center justify-content-center">
                        <video controls autoplay class="lightbox-video rounded-3 shadow">
                            <source :src="activeUrl" :type="contentType || 'video/mp4'" />
                            Your browser does not support HTML5 video player.
                        </video>
                    </div>

                    <!-- 3. AUDIO DISPLAY -->
                    <div v-else-if="detectedType === 'audio'" class="lightbox-stage d-flex align-items-center justify-content-center">
                        <div class="p-4 text-center text-white" style="background: transparent; width: 360px;">
                            <div class="audio-icon-bg mx-auto mb-3 d-flex align-items-center justify-content-center rounded-circle" style="width: 64px; height: 64px; background: rgba(255, 255, 255, 0.1);">
                                <MusicalNoteIcon style="width: 36px; height: 36px;" class="text-warning" />
                            </div>
                            <h6 class="text-truncate mb-3 text-white-50">{{ fileName }}</h6>
                            <audio controls autoplay class="w-100 mb-2">
                                <source :src="activeUrl" :type="contentType || 'audio/mpeg'" />
                                Your browser does not support audio playback.
                            </audio>
                        </div>
                    </div>
                </div>
            </div>
        </Transition>
    </Teleport>
</template>

<style scoped>
.image-lightbox-overlay {
    position: fixed;
    inset: 0;
    z-index: 9999;
    background: rgba(10, 10, 10, 0.88);
    backdrop-filter: blur(10px);
    -webkit-backdrop-filter: blur(10px);
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 20px;
}

.lightbox-wrapper {
    max-width: 90vw;
    max-height: 92vh;
    cursor: default;
}

.lightbox-top-bar {
    max-width: 650px;
    width: 100%;
}

.lightbox-btn {
    width: 36px;
    height: 36px;
    border-radius: 50%;
    border: none;
    background: rgba(255, 255, 255, 0.15);
    color: #ffffff;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.2s ease;
    cursor: pointer;
}

.lightbox-btn:hover {
    background: rgba(255, 255, 255, 0.3);
    transform: scale(1.08);
}

.lightbox-btn:disabled {
    opacity: 0.35;
    cursor: not-allowed;
    transform: none;
}

.lightbox-zoom-badge {
    background: rgba(255, 255, 255, 0.15);
    color: #ffffff;
    border: none;
    border-radius: 14px;
    padding: 4px 10px;
    font-size: 0.75rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s ease;
    user-select: none;
}

.lightbox-zoom-badge:hover {
    background: rgba(255, 255, 255, 0.3);
}

.lightbox-stage {
    width: 90vw;
    height: 82vh;
    max-width: 90vw;
    max-height: 82vh;
    display: flex;
    align-items: center;
    justify-content: center;
    overflow: hidden;
    touch-action: none;
    user-select: none;
    position: relative;
    border-radius: 8px;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.45);
}

.lightbox-video {
    max-width: 100%;
    max-height: 100%;
    outline: none;
}

.lightbox-stage.is-zoomed {
    display: block;
    overflow: auto;
    scrollbar-width: thin;
    scrollbar-color: var(--accent-primary, #c9933b) transparent;
}

.lightbox-stage.is-zoomed::-webkit-scrollbar {
    width: 6px;
    height: 6px;
}

.lightbox-stage.is-zoomed::-webkit-scrollbar-thumb {
    background: var(--accent-primary, #c9933b);
    border-radius: 4px;
}

.lightbox-stage.is-zoomed::-webkit-scrollbar-thumb:hover {
    background: var(--accent-hover, #a87424);
}

.lightbox-stage.is-zoomed::-webkit-scrollbar-track {
    background: transparent;
}

.lightbox-canvas {
    margin: auto;
    display: flex;
    align-items: center;
    justify-content: center;
    min-width: 100%;
    min-height: 100%;
    width: max-content;
    height: max-content;
    box-sizing: border-box;
}

.lightbox-stage.is-zoomed .lightbox-canvas {
    padding: 60px;
}

.lightbox-image {
    object-fit: contain;
    user-select: none;
    -webkit-user-drag: none;
    transition: none;
}

.lightbox-image.is-dragging {
    transition: none !important;
}

.fade-lightbox-enter-active,
.fade-lightbox-leave-active {
    transition: opacity 0.2s ease, transform 0.2s ease;
}

.fade-lightbox-enter-from,
.fade-lightbox-leave-to {
    opacity: 0;
    transform: scale(0.96);
}
</style>
