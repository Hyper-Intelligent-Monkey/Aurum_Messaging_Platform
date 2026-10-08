<script setup>
import { ref, reactive} from "vue";
import { useRouter } from "vue-router";
import { forgotPassword } from "../services/authService";
import { useToastStore } from "../store/toastStore";
import Logo from "../assets/MessagingPlatformLogo.png";

const router = useRouter();
const toastStore = useToastStore();

const email = ref("");
const isSubmitting = ref(false);

const errors = reactive({
    email: ""
});

const clearErrors = () => {
    errors.email = "";
};

const handleRequestOtp = async () => {
    clearErrors();

    if (!email.value.trim()) {
        errors.email = "Email is required.";
        return;
    }
    // validate email
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email.value.trim())) {
        errors.email = "Please enter a valid email address.";
        return;
    }

    isSubmitting.value = true;
    try {
        // send verification code
        await forgotPassword(email.value.trim());
        toastStore.success("A 6-digit code has been sent to your email.");
        sessionStorage.setItem("reset_email", email.value.trim());
        sessionStorage.setItem("forgot_cooldown_end", String(Date.now() + 60 * 1000));
        await router.push({ name: "reset-password" });
    } catch (err) {
        errors.email = err.message || "Failed to send verification code.";
    } finally {
        isSubmitting.value = false;
    }
};
</script>

<template>
    <div class="form-grid">
        <div class="form-background"></div>
        <div class="form-container">
            <form @submit.prevent="handleRequestOtp" class="d-grid row-gap-1" novalidate>
                <div class="d-flex justify-content-center mb-2">
                    <img :src="Logo" alt="Logo" style="width: 2.5rem; height: 2.5rem;">
                </div>

                <div>
                    <label for="resetEmail" class="form-label">Email</label>
                    <input 
                        type="email" 
                        id="resetEmail"
                        name="email"
                        placeholder="example@gmail.com" 
                        class="form-input" 
                        v-model="email"
                        autocomplete="email"
                    />
                    <Transition name="slide-fade">
                        <p class="error" v-if="errors.email">{{ errors.email }}</p>
                    </Transition>
                </div>

                <!-- Submit Button -->
                <div class="mt-4">
                    <button type="submit" class="form-btn" :disabled="isSubmitting">
                        <span v-if="isSubmitting">
                            Sending Code<span class="dots"></span>
                        </span>
                        <span v-else>
                            Send Code
                        </span>
                    </button>
                </div>

                <!-- Toggle Link -->
                <div class="mt-2">
                    <p style="color: var(--text-inversed);" class="mb-0">
                        Return to
                        <router-link to="/login" class="form-link">Login</router-link>
                    </p>
                </div>
            </form>
        </div>
    </div>
</template>

<style scoped>
.form-grid {
    display: flex;
    justify-content: flex-end;
    flex: 1;
    position: relative;
    height: 100vh;
    overflow: hidden;
}

.form-grid form {
    margin-top: 1rem;
    margin-bottom: 2rem;
    width: 20rem;
}

.form-container {
    align-items: center;
    background-color: var(--bg-modal);
    border-left: 1px solid var(--border-light);
    padding: 2rem 1.5rem;
    width: 30%;
    min-width: fit-content;
    height: 100%;
    max-height: 100vh;  
    overflow-y: auto;
    display: flex;
    flex-direction: column;
    border-radius: 25px 0 0 25px;
    box-shadow: var(--shadow-lg);

    opacity: 0; 
    animation: slideInRight 0.5s ease-out forwards;
}

.form-background {
    background-image: url("../assets/background_image/warm_gold_background.jpeg");
    background-size: cover;
    display: flex;
    justify-content: flex-end;
    flex: 1;
    width: 100%;
    margin-right: -21px;
}

.form-input {
    appearance: none;
    padding: 0.625rem 0.875rem;
    width: 100%;
    border: 1px solid var(--border-light);
    border-radius: var(--radius-sm);
    background-color: var(--bg-input);
    color: var(--text-primary);
    outline: none;
    font-size: var(--font-size-sm);
    transition: border-color 0.2s, box-shadow 0.2s;
}

.form-input:focus {
    border-color: var(--accent-primary);
    box-shadow: 0 0 0 2px var(--accent-light);
}

.form-input::placeholder {
    color: var(--text-muted);
}

.form-label {
    display: block;
    margin-top: 0.4rem;
    font-weight: 600;
    font-size: var(--font-size-sm);
    color: var(--text-primary);
}

.form-btn {
    padding: 0.625rem 1.5rem;
    width: 100%;
    background-color: var(--accent-primary);
    border: none;
    border-radius: var(--radius-sm);
    color: var(--text-inversed);
    font-weight: 600;
    transition: all 0.2s ease-in-out;
    cursor: pointer;
}

.form-btn:hover {
    background-color: var(--accent-hover);
}

.form-btn:active {
    background-color: var(--accent-active);
}

.form-btn:disabled {
    opacity: 0.7;
    cursor: not-allowed;
}

.form-link {
    color: var(--accent-primary);
    text-decoration: none;
    font-weight: 600;
}

.form-link:hover {
    color: var(--accent-hover);
    text-decoration: underline;
}

.error {
    font-size: var(--font-size-xs);
    color: var(--status-red);
    margin-top: 0.2rem;
    margin-bottom: 0;
    padding: 0;
}

.dots::after {
    display: inline-block;
    content: '';
    animation: loadingDots 1.5s infinite steps(4, jump-none);
    width: 1.2em;
    text-align: left;
}

@keyframes loadingDots {
    0%   { content: ''; }
    25%  { content: '.'; }
    50%  { content: '..'; }
    75%  { content: '...'; }
}

@media (max-width: 1000px) {
    .form-grid {
        justify-content: center;
        align-items: center;
    }

    .form-container {
        width: fit-content;
        max-width: 100%;
        padding: 2rem 2rem;
        border-radius: var(--radius-lg);
        background-color: color-mix(in srgb, var(--bg-modal) 85%, transparent);
        box-shadow: var(--shadow-lg);
        z-index: 1;
    }

    .form-background {
        position: absolute;
        inset: 0;
        width: 100%;
        height: 100%;
        margin-right: 0;
    }
}

@media (max-width: 600px) {
    .form-grid form{
        width: 100%;
        max-width: 20rem;
    }

    .form-container {
        width: 100%;
        padding: 2rem 1rem;
        border-radius: 0;
    }
}

.form-background {
    opacity: 0;
    animation: fadeIn 1s ease-in-out 0.2s forwards;
}

@keyframes fadeIn {
    from { opacity: 0; }
    to { opacity: 1; }
}

@keyframes slideInRight {
    from {
        transform: translateX(50%);
        opacity: 0;
    }
    to {
        transform: translateX(0);
        opacity: 1;
    }
}

@media (max-width: 1000px) {
    .form-background {
        opacity: 1;
        animation: none;
    }
}
</style>
