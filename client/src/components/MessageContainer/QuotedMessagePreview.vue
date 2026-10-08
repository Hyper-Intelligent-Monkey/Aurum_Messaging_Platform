<script setup>
defineProps({
    parentMessage: {
        type: Object,
        required: true
    },
    parentAuthorName: {
        type: String,
        default: ""
    }
});

const emit = defineEmits(["jump"]);
</script>

<template>
    <div 
        class="quoted-preview mb-2 p-2 rounded cursor-pointer"
        @click.stop="emit('jump', parentMessage.id)"
        title="Click to view parent message"
    >
        <div class="fw-bold small text-accent-theme">{{ parentAuthorName }}</div>
        <div class="text-truncate small text-secondary">
            {{ parentMessage.content || (parentMessage.storedFileName ? '[Media Attachment]' : '') }}
        </div>
    </div>
</template>

<style scoped>
.quoted-preview {
    background-color: rgba(0, 0, 0, 0.06);
    border-left: 3.5px solid var(--accent-primary);
    padding: 5px 10px;
    border-radius: 4px;
    margin-bottom: 6px;
    font-size: 0.8rem;
    transition: background-color 0.2s ease;
}
.quoted-preview:hover {
    background-color: rgba(0, 0, 0, 0.12);
}
.text-accent-theme {
    color: var(--accent-primary);
}
.cursor-pointer {
    cursor: pointer;
}
</style>