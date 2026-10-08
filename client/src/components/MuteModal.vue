<script setup>
import { ref } from "vue";
import { useChatStore } from "../store/chatStore";
import BaseModal from "./BaseModal.vue";

const props = defineProps({
    isOpen: {
        type: Boolean,
        default: false,
    },
    conversationId: {
        type: Number,
        required: true,
    },
});

const emit = defineEmits(["close"]);

const chatStore = useChatStore();

const selectedMuteDuration = ref(30);
const isMutingAction = ref(false);

// Mute duration options
const muteDuration = [
    { label: "30 minutes", value: 30 },
    { label: "2 hours", value: 120 },
    { label: "8 hours", value: 480 },
    { label: "24 hours", value: 1440 },
    { label: "1 week", value: 10080 },
    { label: "Always", value: null },
];

// Handle mute confirmation
const handleConfirmMute = async () => {
    if (!props.conversationId) return;
    isMutingAction.value = true;
    try {
        await chatStore.muteUserConversation(props.conversationId, selectedMuteDuration.value);
        emit("close");
    } catch (err) {
        console.error("Failed to mute conversation:", err);
    } finally {
        isMutingAction.value = false;
    }
};
</script>

<template>
    <BaseModal 
        :is-open="isOpen" 
        title="Mute this chat?" 
        action-text="Mute"
        size="sm"
        :is-loading="isMutingAction"
        @close="emit('close')"
        @action="handleConfirmMute"
    >
        <!-- Duration Radio List -->
        <div class="d-flex flex-column gap-1">
            <label 
                v-for="option in muteDuration" 
                :key="option.label" 
                class="d-flex align-items-center justify-content-between px-3 py-2 rounded-3 cursor-pointer mute-option-row"
                :class="{ 'selected-option': selectedMuteDuration === option.value }"
            >
                <span :class="{ 'focused-option': selectedMuteDuration === option.value }" class="mute-option-label" >{{ option.label }}</span>
                <input 
                    type="radio" 
                    name="muteDuration" 
                    class="custom-radio-input"
                    :value="option.value" 
                    v-model="selectedMuteDuration" 
                />
            </label>
        </div>
    </BaseModal>
</template>

<style scoped>
.mute-option-row {
    transition: background-color 0.15s ease;
    user-select: none;
}

.mute-option-row:hover,
.mute-option-row.selected-option{
    background-color: rgba(0, 0, 0, 0.05);
}

.mute-option-label {
    font-size: var(--font-size-md);
    color: var(--text-secondary);
    font-weight: 500;
    text-align: end;
}

.mute-option-row.mute-option-label {
    color: var(--text-primary);
}

.focused-option {
    color: var(--text-primary);
}

/* Custom Gold Radio Button Matching Screenshot */
.custom-radio-input {
    appearance: none;
    -webkit-appearance: none;
    width: 20px;
    height: 20px;
    border: 2px solid rgba(160, 130, 80, 0.35);
    border-radius: 50%;
    outline: none;
    cursor: pointer;
    display: grid;
    place-content: center;
    margin: 0;
    transition: border-color 0.15s ease;
    background-color: transparent;
}

.custom-radio-input::before {
    content: "";
    width: 10px;
    height: 10px;
    border-radius: 50%;
    transform: scale(0);
    transition: transform 0.15s ease-in-out;
    background-color: var(--accent-primary);
}

.custom-radio-input:checked {
    border-color: var(--accent-primary);
}

.custom-radio-input:checked::before {
    transform: scale(1);
}
</style>