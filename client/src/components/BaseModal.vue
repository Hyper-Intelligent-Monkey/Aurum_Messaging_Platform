<script setup>
import { computed } from "vue";

const props = defineProps({
    isOpen: {
        type: Boolean,
        default: false,
    },
    title: {
        type: String,
        default: "",
    },
    size: {
        type: String,
        default: "md",
    },
    cancelText: {
        type: String,
        default: "Cancel",
    },
    actionText: {
        type: String,
        default: "Continue",
    },
    isLoading: {
        type: Boolean,
        default: false,
    },
    isActionDisabled: {
        type: Boolean,
        default: false,
    },
    actionVariant: {
        type: String,
        default: "accent",
    },
});

const emit = defineEmits(["close", "action"]);

const resolvedMaxWidth = computed(() => {
    if (props.size === "sm") return "430px";
    if (props.size === "lg") return "640px";
    return "520px";
});
</script>

<template>
    <Teleport to="body">
        <Transition name="modal-fade">
            <div 
                v-if="isOpen" 
                class="modal-backdrop-theme d-flex align-items-center justify-content-center p-3"
                @contextmenu.stop.prevent
            >
                <div 
                    class="modal-card-theme rounded-4 shadow-lg w-100 position-relative" 
                    :style="{ maxWidth: resolvedMaxWidth }"
                    @click.stop
                >
                    <!-- Modal Header with divider line matching Profile.vue -->
                    <div v-if="$slots.header || title" class="d-flex align-items-center justify-content-between mb-3 pb-2 border-bottom-theme">
                        <slot name="header">
                            <div class="d-flex align-items-center gap-2">
                                <slot name="icon" />
                                <h5 class="fw-bold mb-0 text-main-theme">{{ title }}</h5>
                            </div>
                        </slot>
                    </div>

                    <!-- Modal Body (arbitrary HTML block) -->
                    <div class="modal-body-content mb-4">
                        <slot />
                    </div>

                    <!-- Buttons -->
                    <div class="buttons-margin">
                        <slot name="footer">
                            <div class="d-flex gap-2 w-100">
                                <button 
                                    type="button" 
                                    class="btn btn-outline-theme w-50 fw-semibold" 
                                    @click="emit('close')" 
                                    :disabled="isLoading"
                                >
                                    {{ cancelText }}
                                </button>
                                <button 
                                    type="button" 
                                    :class="actionVariant === 'danger' ? 'btn-danger' : 'btn-accent'"
                                    class="btn w-50 fw-semibold d-flex align-items-center justify-content-center gap-1" 
                                    @click="emit('action')" 
                                    :disabled="isLoading || isActionDisabled"
                                >
                                    <span>{{ actionText }}</span>
                                </button>
                            </div>
                        </slot>
                    </div>
                </div>
            </div>
        </Transition>
    </Teleport>
</template>

<style scoped>
.buttons-margin {
    margin-top: 30px;
}

.btn-danger:hover {
    background-color: var(--status-red);
}

.modal-backdrop-theme {
    position: fixed;
    top: 0;
    left: 0;
    width: 100vw;
    height: 100vh;
    background-color: rgba(0, 0, 0, 0.65);
    backdrop-filter: blur(4px);
    z-index: 2000;
}

.modal-card-theme {
    background-color: var(--bg-modal, var(--bg-chat-header, #111b21));
    border: 1px solid var(--border-light, rgba(255, 255, 255, 0.12));
    color: var(--text-primary);
    padding: 1.5rem;
}

@media (min-width: 576px) {
    .modal-card-theme {
        padding: 2rem;
    }
}

.border-bottom-theme {
    border-bottom: 1px solid var(--border-light, rgba(255, 255, 255, 0.08));
}

.text-main-theme {
    color: var(--text-primary);
}

.btn-outline-theme {
    color: var(--text-secondary);
    border: 1px solid var(--border-light);
}

.btn-outline-theme:hover:not(:disabled), .btn-outline-theme:focus:not(:disabled) {
    color: var(--text-primary);
    background-color: rgba(255, 255, 255, 0.05);
    border: 1px solid var(--border-light);
}

.btn-accent {
    background-color: var(--accent-primary);
    color: var(--text-inversed);
    border: none;
}

.btn-accent:hover:not(:disabled), .btn-accent:focus:not(:disabled) {
    color: var(--text-inversed);
    background-color: var(--accent-hover);
}

.btn-outline-theme:disabled,
.btn-accent:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

/* Modal Fade & Scale Transition */
.modal-fade-enter-active,
.modal-fade-leave-active {
    transition: opacity 0.2s ease;
}

.modal-fade-enter-active .modal-card-theme,
.modal-fade-leave-active .modal-card-theme {
    transition: transform 0.2s ease;
}

.modal-fade-enter-from,
.modal-fade-leave-to {
    opacity: 0;
}

.modal-fade-enter-from .modal-card-theme {
    transform: scale(0.96);
}

.modal-fade-leave-to .modal-card-theme {
    transform: scale(0.96);
}
</style>