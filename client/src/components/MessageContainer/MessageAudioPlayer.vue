
<script setup>
import { ref, computed, onMounted, onUnmounted } from "vue";
import { PlayIcon, PauseIcon, SpeakerWaveIcon, SpeakerXMarkIcon } from "@heroicons/vue/24/solid";

const props = defineProps({
    message: {
        type: Object,
        required: true
    },
    audioUrl: {
        type: String,
        required: true
    },
    fileExtension: {
        type: String,
        default: ""
    }
});

const audioRef = ref(null);
const isPlaying = ref(false);
const currentTime = ref(0);
const duration = ref(0);
const playbackRate = ref(1);
const isMuted = ref(false);

// mute/unmute audio
const toggleMute = () => {
    if (!audioRef.value) return;
    isMuted.value = !isMuted.value;
    audioRef.value.muted = isMuted.value;
};

// update audio duration
const updateDuration = () => {
    if (!audioRef.value) return;
    const d = audioRef.value.duration;
    if (d && !isNaN(d) && isFinite(d) && d > 0) {
        duration.value = d;
    } else if (audioRef.value.seekable && audioRef.value.seekable.length > 0) {
        const seekableEnd = audioRef.value.seekable.end(audioRef.value.seekable.length - 1);
        if (seekableEnd && !isNaN(seekableEnd) && isFinite(seekableEnd) && seekableEnd > 0) {
            duration.value = seekableEnd;
        }
    }
};

// toggle audio play/pause
const togglePlay = () => {
    if (!audioRef.value) return;
    if (isPlaying.value) {
        audioRef.value.pause();
    } else {
        window.dispatchEvent(new CustomEvent('app-audio-play', { detail: { messageId: props.message.id } }));
        audioRef.value.play();
    }
};

// handle other audio when interacting with another audio
const handleOtherAudioPlay = (e) => {
    if (e.detail?.messageId !== props.message.id && isPlaying.value) {
        if (audioRef.value) {
            audioRef.value.pause();
        }
        isPlaying.value = false;
    }
};

// handle audio time update
const onTimeUpdate = () => {
    if (audioRef.value) {
        currentTime.value = audioRef.value.currentTime;
        if (!duration.value || duration.value === 0) {
            updateDuration();
        }
    }
};

// handle audio loaded metadata event
const onLoadedMetadata = () => {
    updateDuration();
};

// handle audio ended event
const onEnded = () => {
    isPlaying.value = false;
    currentTime.value = 0;
};

// seek audio
const onSeek = (e) => {
    if (audioRef.value) {
        audioRef.value.currentTime = Number(e.target.value);
    }
};

// audio playback speed
const cycleSpeed = () => {
    if (!audioRef.value) return;
    const speeds = [1, 1.5, 2];
    const nextIdx = (speeds.indexOf(playbackRate.value) + 1) % speeds.length;
    playbackRate.value = speeds[nextIdx];
    audioRef.value.playbackRate = playbackRate.value;
};

// Format audio duration in the message card
const formatAudioTime = (seconds) => {
    if (!seconds || isNaN(seconds)) return "0:00";
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs < 10 ? '0' : ''}${secs}`;
};

// Format file size in the message card
const formatFileSize = (bytes) => {
    if (!bytes || bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
};

// horzontal progress bar of the audio
const progressPercent = computed(() => {
    if (!duration.value) return 0;
    return (currentTime.value / duration.value) * 100;
});

// add a listener that only 1 audio can play at a time
onMounted(() => {
    window.addEventListener("app-audio-play", handleOtherAudioPlay);
    if (audioRef.value) {
        updateDuration();
    }
});

// remove listener
onUnmounted(() => {
    window.removeEventListener("app-audio-play", handleOtherAudioPlay);
    if (audioRef.value) {
        audioRef.value.pause();
    }
});
</script>

<template>
    <div class="custom-audio-card mb-2 d-flex flex-column gap-2">
        <!-- Hidden native audio element for playback engine -->
        <audio 
            ref="audioRef"
            :src="audioUrl"
            preload="metadata"
            :muted="isMuted"
            @play="isPlaying = true; updateDuration()"
            @pause="isPlaying = false"
            @timeupdate="onTimeUpdate"
            @durationchange="updateDuration"
            @canplay="updateDuration"
            @loadedmetadata="onLoadedMetadata"
            @volumechange="isMuted = audioRef?.muted ?? false"
            @ended="onEnded"
            class="d-none"
        ></audio>

        <!-- Top Info Bar: Filename + Size Badge -->
        <div class="d-flex align-items-center justify-content-between gap-2 px-1">
            <div class="d-flex align-items-center gap-2 overflow-hidden min-w-0">
                <span class="audio-badge text-uppercase fw-bold">{{ fileExtension || 'AUDIO' }}</span>
                <span class="audio-filename text-truncate fw-semibold" :title="message.originalFileName || 'Audio'">
                    {{ message.originalFileName || 'Audio Note' }}
                </span>
            </div>
            <small class="audio-size text-muted flex-shrink-0">{{ formatFileSize(message.fileSize) }}</small>
        </div>

        <!-- Controls Row: Play/Pause button + Scrubber + Times + Mute + Speed Pill -->
        <div class="d-flex align-items-center gap-2 pt-1">
            <!-- Play / Pause Circular Accent Button -->
            <button 
                type="button" 
                class="audio-play-btn flex-shrink-0 d-flex align-items-center justify-content-center"
                @click.stop="togglePlay"
                :title="isPlaying ? 'Pause' : 'Play'"
                :aria-label="isPlaying ? 'Pause' : 'Play'"
            >
                <PauseIcon v-if="isPlaying" style="width: 18px; height: 18px;" />
                <PlayIcon v-else style="width: 18px; height: 18px; margin-left: 2px;" />
            </button>

            <!-- Scrubber + Duration Bar -->
            <div class="flex-grow-1 min-w-0 d-flex flex-column gap-1">
                <input 
                    type="range"
                    min="0"
                    :max="duration || 100"
                    step="0.1"
                    :value="currentTime"
                    @input="onSeek"
                    class="audio-slider"
                    :style="{ '--progress': `${progressPercent}%` }"
                    title="Seek audio"
                />
                <div class="d-flex justify-content-between align-items-center audio-time-row">
                    <span>{{ formatAudioTime(currentTime) }}</span>
                    <span>{{ formatAudioTime(duration) }}</span>
                </div>
            </div>

            <!-- Mute / Unmute Button -->
            <button 
                type="button" 
                class="audio-mute-btn flex-shrink-0 d-flex align-items-center justify-content-center"
                :class="{ 'is-muted': isMuted }"
                @click.stop="toggleMute"
                :title="isMuted ? 'Unmute' : 'Mute'"
                :aria-label="isMuted ? 'Unmute' : 'Mute'"
            >
                <SpeakerXMarkIcon v-if="isMuted" style="width: 15px; height: 15px;" />
                <SpeakerWaveIcon v-else style="width: 15px; height: 15px;" />
            </button>

            <button 
                type="button" 
                class="audio-speed-btn flex-shrink-0"
                @click.stop="cycleSpeed"
                title="Playback speed"
            >
                {{ playbackRate }}x
            </button>
        </div>
    </div>
</template>

<style scoped>
.custom-audio-card {
    background-color: var(--media-card-bg);
    border: var(--media-card-border);
    border-radius: var(--media-card-radius);
    padding: var(--media-card-padding);
    width: var(--media-card-width);
    max-width: 100%;
    box-shadow: var(--media-card-shadow);
    user-select: none;
}

.audio-badge {
    font-size: 0.65rem;
    font-weight: 700;
    padding: 2px 6px;
    border-radius: var(--radius-xs);
    background-color: var(--accent-tint-light);
    color: var(--accent-primary);
    letter-spacing: 0.5px;
}

.audio-filename {
    font-size: 0.85rem;
    color: var(--text-primary);
}

.audio-size {
    font-size: 0.72rem;
}

.audio-play-btn {
    width: var(--media-btn-size);
    height: var(--media-btn-size);
    border-radius: 50%;
    border: none;
    background-color: var(--accent-primary);
    color: #ffffff;
    box-shadow: var(--accent-btn-shadow);
    transition: transform 0.15s ease, background-color 0.15s ease;
    cursor: pointer;
}
.audio-play-btn:hover {
    background-color: var(--accent-hover);
    transform: scale(1.08);
}

.audio-slider {
    -webkit-appearance: none;
    appearance: none;
    width: 100%;
    height: 4px;
    border-radius: 2px;
    outline: none;
    cursor: pointer;
    background: linear-gradient(
        to right,
        var(--accent-primary) 0%,
        var(--accent-primary) var(--progress, 0%),
        var(--accent-tint-medium) var(--progress, 0%),
        var(--accent-tint-medium) 100%
    );
}

.audio-slider::-webkit-slider-thumb {
    -webkit-appearance: none;
    appearance: none;
    width: 12px;
    height: 12px;
    border-radius: 50%;
    background: var(--accent-primary);
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.25);
    transition: transform 0.15s ease;
}
.audio-slider::-webkit-slider-thumb:hover {
    transform: scale(1.25);
}

.audio-time-row {
    font-size: 0.7rem;
    color: var(--text-muted);
    font-weight: 500;
}

.audio-speed-btn {
    font-size: 0.72rem;
    font-weight: 600;
    color: var(--accent-primary);
    background-color: var(--accent-tint-subtle);
    border: 1px solid var(--accent-tint-border);
    border-radius: var(--radius-md);
    padding: 2px 7px;
    cursor: pointer;
    transition: all 0.15s ease;
}
.audio-speed-btn:hover {
    background-color: var(--accent-primary);
    color: #ffffff;
}

.audio-mute-btn {
    width: var(--media-btn-size-sm);
    height: var(--media-btn-size-sm);
    border-radius: 50%;
    border: 1px solid var(--accent-tint-border);
    background-color: var(--accent-tint-subtle);
    color: var(--accent-primary);
    cursor: pointer;
    transition: all 0.15s ease;
}

.audio-mute-btn:hover {
    background-color: var(--accent-primary);
    color: #ffffff;
    transform: scale(1.08);
}

.audio-mute-btn.is-muted {
    border-color: rgba(220, 53, 69, 0.35);
    background-color: rgba(220, 53, 69, 0.1);
    color: #dc3545;
}

.audio-mute-btn.is-muted:hover {
    background-color: #dc3545;
    color: #ffffff;
}
</style>