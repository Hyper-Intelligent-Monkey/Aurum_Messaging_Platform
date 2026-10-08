<script setup>
defineProps({
    hasMore: {
        type: Boolean,
        default: false
    },
    loading: ({
        type: Boolean,
        default: false
    }),
    idleText: ({
        type: String,
        default: 'Load more'
    }),
    loadingText: {
        type: String,
        default: 'Loading'
    }

});

defineEmits(['click']);

</script>

<template>
<div v-if="hasMore" class="load-more-container">
    <button
        @click="$emit('click')"
        class="load-more-btn"
        :disabled="loading"
        type="button"
    >
        <span 
            v-if="loading" 
            class="spinner-border spinner-border-sm load-more-spinner"
            role="status"
            aria-hidden="true"
        ></span>
        <svg v-else xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="load-more-icon">
            <path fill-rule="evenodd" d="M5.22 8.22a.75.75 0 0 1 1.06 0L10 11.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 9.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd" />
        </svg>
        <span v-if="loading">{{ loadingText}}<span class="dots"></span></span>
        <span v-else>{{ idleText }}</span>
    </button>

</div>

</template>

<style scoped>
.load-more-container {
    display: flex;
    justify-content: center;
    padding: 1.25rem 0 1.75rem;
}

.load-more-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
    padding: 0.5rem 1.25rem;
    font-size: var(--font-size-xs, 0.8125rem);
    font-weight: 600;
    letter-spacing: 0.3px;
    color: var(--text-secondary);
    background-color: var(--bg-input);
    border: 1px solid var(--border-light);
    border-radius: var(--radius-full, 9999px);
    box-shadow: var(--shadow-sm);
    cursor: pointer;
    user-select: none;
    transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
}

.load-more-btn:hover:not(:disabled) {
    background-color: var(--accent-light);
    color: var(--accent-hover);
    border-color: var(--accent-primary);
    box-shadow: var(--shadow-md);
    transform: translateY(-1px);
}

.load-more-btn:hover:not(:disabled) .load-more-icon {
    transform: translateY(1.5px);
}

.load-more-btn:active:not(:disabled) {
    transform: translateY(0);
    box-shadow: var(--shadow-sm);
}

.load-more-btn:disabled {
    opacity: 0.65;
    cursor: not-allowed;
}

.load-more-icon {
    width: 14px;
    height: 14px;
    transition: transform 0.2s ease;
}

.load-more-spinner {
    width: 13px;
    height: 13px;
    border-width: 2px;
}

.dots::after {
    display: inline-block;
    content: '';
    width: 1.2em;
    text-align: left;
    animation: loadingDots 1.4s infinite steps(4, jump-none);
}

@keyframes loadingDots {
    0% {content: '';}
    25% {content: '.';}
    50% {content: '..';}
    75% {content: '...';}
}
</style>