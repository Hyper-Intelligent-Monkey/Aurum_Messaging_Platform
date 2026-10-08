<script setup>
import { PlayIcon } from "@heroicons/vue/24/solid";

defineProps({
    videoUrl: {
        type: String,
        required: true
    },
    contentType: {
        type: String,
        default: "video/mp4"
    },
    alt: {
        type: String,
        default: "Video Attachment"
    }
});

const emit = defineEmits(["open"]);
</script>

<template>
    <div 
        class="mb-2 position-relative cursor-pointer video-attachment-wrapper overflow-hidden rounded border border-secondary border-opacity-25" 
        @click.stop="emit('open')"
    >
        <!-- Video Frame Preview -->
        <video 
            :src="videoUrl" 
            class="video-preview-thumb img-fluid w-100" 
            style="max-height: 280px; object-fit: cover; display: block;"
            preload="metadata"
        ></video>
        
        <!-- Centered Play Icon Overlay -->
        <div class="position-absolute top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center video-play-overlay">
            <div class="play-btn-circle d-flex align-items-center justify-content-center rounded-circle shadow-lg">
                <PlayIcon style="width: 28px; height: 28px; margin-left: 3px;" class="text-white" />
            </div>
        </div>
    </div>
</template>

<style scoped>
.video-attachment-wrapper {
    background-color: #000000;
    cursor: pointer;
}

.video-play-overlay {
    background: rgba(0, 0, 0, 0.25);
    transition: background 0.2s ease;
}

.video-attachment-wrapper:hover .video-play-overlay {
    background: rgba(0, 0, 0, 0.4);
}

.play-btn-circle {
    width: 52px;
    height: 52px;
    background: rgba(0, 0, 0, 0.65);
    backdrop-filter: blur(4px);
    border: 2px solid rgba(255, 255, 255, 0.85);
    transition: transform 0.2s ease, background-color 0.2s ease;
}

.video-attachment-wrapper:hover .play-btn-circle {
    transform: scale(1.1);
    background: rgba(0, 0, 0, 0.85);
    border-color: var(--accent-primary, #d1a153);
}
</style>
