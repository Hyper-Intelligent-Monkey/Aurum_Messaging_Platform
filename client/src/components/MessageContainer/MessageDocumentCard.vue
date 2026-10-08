<script setup>
import { DocumentTextIcon, ArrowDownTrayIcon } from "@heroicons/vue/24/outline";

defineProps({
    message: {
        type: Object,
        required: true
    },
    fileExtension: {
        type: String,
        default: ""
    }
});

const emit = defineEmits(["download"]);

// Format file size in the message card
const formatFileSize = (bytes) => {
    if (!bytes || bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
};
</script>

<template>
    <div class="document-attachment-card mb-2 d-flex align-items-center justify-content-between gap-3">
        <div class="d-flex align-items-center gap-3 overflow-hidden min-w-0">
            <div class="document-icon-badge flex-shrink-0 d-flex align-items-center justify-content-center">
                <DocumentTextIcon style="width: 20px; height: 20px;" />
            </div>
            <div class="document-info text-truncate">
                <div class="document-name text-truncate fw-semibold" :title="message.originalFileName || 'Attachment'">
                    {{ message.originalFileName || 'Attachment' }}
                </div>
                <div class="document-meta d-flex align-items-center gap-1">
                    <span>{{ formatFileSize(message.fileSize) }}</span>
                    <span v-if="fileExtension" class="meta-dot">•</span>
                    <span v-if="fileExtension">{{ fileExtension }}</span>
                </div>
            </div>
        </div>
        <button 
            @click.stop="emit('download')" 
            class="document-download-btn flex-shrink-0 d-flex align-items-center justify-content-center"
            title="Download file"
            aria-label="Download file"
        >
            <ArrowDownTrayIcon style="width: 17px; height: 17px;" />
        </button>
    </div>
</template>

<style scoped>
.document-attachment-card {
    width: var(--media-card-width);
    max-width: 100%;
    background-color: var(--media-card-bg);
    border: var(--media-card-border);
    border-radius: var(--media-card-radius);
    padding: var(--media-card-padding);
    box-shadow: var(--media-card-shadow);
    transition: all 0.2s ease;
}

.document-icon-badge {
    width: var(--media-btn-size);
    height: var(--media-btn-size);
    border-radius: var(--radius-sm);
    background-color: var(--accent-tint-light);
    color: var(--accent-primary);
}

.document-info {
    min-width: 0;
}

.document-name {
    font-size: 0.875rem;
    color: var(--text-primary);
    line-height: 1.3;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.document-meta {
    font-size: 0.72rem;
    color: var(--text-muted);
    font-weight: 500;
}

.meta-dot {
    font-size: 0.6rem;
    opacity: 0.6;
}

.document-download-btn {
    width: 34px;
    height: 34px;
    border-radius: 50%;
    border: 1.5px solid var(--border-light);
    background-color: #ffffff;
    color: var(--text-secondary);
    transition: all 0.2s ease;
    cursor: pointer;
}

.document-download-btn:hover {
    background-color: var(--accent-primary);
    border-color: var(--accent-primary);
    color: #ffffff;
    transform: scale(1.06);
    box-shadow: var(--accent-btn-shadow);
}
</style>