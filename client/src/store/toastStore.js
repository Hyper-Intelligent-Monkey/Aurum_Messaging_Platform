import { defineStore, acceptHMRUpdate } from 'pinia';   

export const ToastType = {
    Success: 'success',
    Error: 'error',
}

export const useToastStore = defineStore('toast', {
    state: () => ({
        toast: null,
        timerId: null,
    }),

    actions: {
        // show toast
        showToast(message, type = ToastType.Success, duration = 3000) {
            if (this.timerId) {
                clearTimeout(this.timerId);
                this.timerId = null;
            }

            const id = Date.now();
            this.toast = {id, message, type};

            if (duration > 0) {
                this.timerId = setTimeout(() => {
                    this.dismissToast();
                }, duration);
            }
        },

        // end toast
        dismissToast() {
            if (this.timerId) {
                clearTimeout(this.timerId);
                this.timerId = null;
            }
            this.toast = null;
        },

        // show success toast
        success(message, duration = 3000) {
            this.showToast(message, ToastType.Success, duration);
        },

        // show error toast
        error(message, duration = 3000) {
            this.showToast(message, ToastType.Error, duration);
        },
    },
});

// prevents code changes from reloading the page
if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useToastStore, import.meta.hot));
}