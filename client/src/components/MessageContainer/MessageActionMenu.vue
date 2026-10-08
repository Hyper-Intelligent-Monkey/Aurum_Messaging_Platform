<script setup>
import { 
    TrashIcon, 
    ArrowDownTrayIcon, 
    ArrowUturnLeftIcon, 
    PencilSquareIcon 
} from "@heroicons/vue/24/outline";

defineProps({
    isOwner: {
        type: Boolean,
        required: true
    },
    isMedia: {
        type: Boolean,
        required: true
    },
    openUpward: {
        type: Boolean,
        default: false
    }
});

const emit = defineEmits(["reply", "edit", "download", "delete"]);
</script>

<template>
    <div 
        :class="[
            'message-action-menu shadow-lg', 
            isOwner ? 'menu-owner' : 'menu-partner',
            openUpward ? 'menu-upward' : 'menu-downward'
        ]"
    >
        <button @click.stop="emit('reply')" class="menu-action-item" title="Reply">
            <ArrowUturnLeftIcon class="action-icon text-accent" />
            <span class="action-label">Reply</span>
        </button>
        <button v-if="isOwner && !isMedia" @click.stop="emit('edit')" class="menu-action-item" title="Edit">
            <PencilSquareIcon class="action-icon text-warning" />
            <span class="action-label">Edit</span>
        </button>
        <button v-if="isMedia" @click.stop="emit('download')" class="menu-action-item" title="Save Media">
            <ArrowDownTrayIcon class="action-icon text-info" />
            <span class="action-label">Save</span>
        </button>
        <div v-if="isOwner" class="menu-divider"></div>
        <button v-if="isOwner" @click.stop="emit('delete')" class="menu-action-item text-danger" title="Delete">
            <TrashIcon class="action-icon text-danger" />
            <span class="action-label">Delete</span>
        </button>
    </div>
</template>

<style scoped>
.message-action-menu {
    position: absolute;
    z-index: 1050;
    min-width: 140px;
    background-color: #ffffff;
    border: 1px solid var(--border-light);
    border-radius: 8px;
    padding: 4px 0;
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12), 0 2px 6px rgba(0, 0, 0, 0.08);
    display: flex;
    flex-direction: column;
    animation: fadeInScale 0.15s ease-out;
}

.menu-downward {
    top: calc(100% + 4px);
    bottom: auto !important;
}

.menu-upward {
    bottom: calc(100% + 4px);
    top: auto !important;
}

.menu-owner {
    right: 0;
}

.menu-partner {
    left: 0;
}

.menu-action-item {
    display: flex;
    align-items: center;
    gap: 10px;
    width: 100%;
    padding: 8px 14px;
    background: transparent;
    border: none;
    cursor: pointer;
    font-size: 0.85rem;
    color: var(--text-primary);
    transition: background-color 0.15s ease;
    text-align: left;
}

.menu-action-item:hover {
    background-color: rgba(0, 0, 0, 0.05);
}

.action-icon {
    width: 18px;
    height: 18px;
    flex-shrink: 0;
}

.action-label {
    font-weight: 500;
}

.menu-divider {
    height: 1px;
    background-color: var(--border-light);
    margin: 4px 0;
}

@keyframes fadeInScale {
    from {
        opacity: 0;
        transform: translateY(-4px) scale(0.96);
    }
    to {
        opacity: 1;
        transform: translateY(0) scale(1);
    }
}
</style>