<script setup>
import { useToastStore, ToastType } from '../store/toastStore';
import { storeToRefs } from 'pinia';

const toastStore = useToastStore();

// monitor toast changes
const { toast } = storeToRefs(toastStore);

// toast color objects
const TOAST_COLOR = {
  [ToastType.Success]: 'success-toast',
  [ToastType.Error]: 'error-toast',
};
</script>

<template>
  <div class="toast-container position-fixed bottom-0 end-0 p-3">
    <Transition name="toast" appear>
      <div 
        v-if="toast"
        :key="toast.id"
        class="toast-card d-flex align-items-center justify-content-between p-3 mb-2 rounded-3 shadow"
        :class="TOAST_COLOR[toast.type] || 'default-toast'"
      >
        <div class="d-flex align-items-center">
          <span class="small fw-semibold text-white">{{ toast.message }}</span>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.toast-container {
  max-width: 360px;
  width: fit-content;
  z-index: 9999;
  pointer-events: none;
}
.toast-card {
  pointer-events: auto;
}
.success-toast {
  background-color: var(--status-green, #28a745);
}

.error-toast {
  background-color: var(--status-red, #dc3545);
}

.default-toast {
  background-color: #6c757d;
}

.toast-enter-active {
  transition: all 0.35s cubic-bezier(0.34, 1.56, 0.64, 1);
}
.toast-leave-active {
  transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
}
.toast-enter-from,
.toast-leave-to {
  opacity: 0;
  transform: translateX(40px) scale(0.95);
}
</style>