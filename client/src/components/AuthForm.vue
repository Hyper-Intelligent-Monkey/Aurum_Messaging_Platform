<script setup>
import { ref, watch, onMounted, onUnmounted } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "../store/authStore";
import { useToastStore } from "../store/toastStore";
import Logo from "../assets/MessagingPlatformLogo.png";
import { EyeIcon, EyeSlashIcon } from "@heroicons/vue/24/outline";
import { getUsernameRules } from "../utils/formValidators";
import BaseModal from "./BaseModal.vue";

const props = defineProps({
    title: {
        type: String,
        required: true
    },
    loading: {
        type: Boolean,
        default: false
    },
    error: {
        type: String,
        default: ""
    },
    v$: {
        type: Object,
        required: true
    },
    isRegister: {
        type: Boolean,
        default: false
    }
});

const emit = defineEmits(["submit", "forgot-password"]);
const isSubmitted = ref(false);

const showPassword = ref(false);
const showConfirmPassword = ref(false);

const router = useRouter();
const authStore = useAuthStore();
const toastStore = useToastStore();

const savedGoogleToken = ref("");
const showUsernameModal = ref(false);
const promptUsername = ref("");
const modalValidationError = ref("");
const modalLoading = ref(false);

const usernameRules = getUsernameRules();
// Validate username
const validateUsername = (val) => {
    for (const rule of Object.values(usernameRules)) {
        if (!rule.$validator(val)) {
            return typeof rule.$message === "function" ? rule.$message({ $model: val }) : rule.$message;
        }
    }
    return "";
};

// Watch for username changes
watch(promptUsername, (newVal) => {
    if (modalValidationError.value || authStore.googleError) {
        modalValidationError.value = validateUsername(newVal);
        authStore.googleError = "";
    }
});

// Initialize Google Sign-In and Sign-Up button
const isGoogleLoaded = ref(false);
let isGoogleRendered = false;

const initGoogleButton = () => {
    if (isGoogleRendered) return;
    const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;
    if (!window.google?.accounts?.id || !clientId) return;

    const btnSlot = document.getElementById("google-btn-slot");
    if (!btnSlot) return;

    isGoogleRendered = true;

    window.google.accounts.id.initialize({
        client_id: clientId,
        callback: handleGoogleResponse,
    });

    // Wait for the iframe DOM element and its real load event
    const observer = new MutationObserver(() => {
        const iframe = btnSlot.querySelector("iframe");
        if (iframe) {
            observer.disconnect();
            iframe.addEventListener("load", () => {
                // Ensure internal paint completes before revealing
                setTimeout(() => {
                    isGoogleLoaded.value = true;
                }, 50);
            });
        }
    });
    observer.observe(btnSlot, { childList: true, subtree: true });

    window.google.accounts.id.renderButton(btnSlot, {
        theme: "outline",
        size: "medium",
        width: 320,
        text: props.isRegister ? "signup_with" : "signin_with",
        shape: "rectangular",
        logo_alignment: "left"
    });
};

const handleKeyDown = (e) => {
    if (e.key === "Escape" && showUsernameModal.value) {
        closeUsernameModal();
    }
};

onMounted(() => {
    window.addEventListener("keydown", handleKeyDown);

    if (window.google?.accounts?.id) {
        initGoogleButton();
    } else {
        const timer = setInterval(() => {
            if (window.google?.accounts?.id) {
                clearInterval(timer);
                initGoogleButton();
            }
        }, 30);
        setTimeout(() => clearInterval(timer), 4000);
    }
});

onUnmounted(() => {
    window.removeEventListener("keydown", handleKeyDown);
});

// Handle Google Sign-In
const handleGoogleResponse = async (response) => {
    try {
        const idToken = response.credential;
        savedGoogleToken.value = idToken;

        const res = await authStore.loginWithGoogle(idToken);
        if (res && res.requiresUsername) {
            promptUsername.value = res.username || "";
            showUsernameModal.value = true;
        } else {
            toastStore.success(props.isRegister ? "Logged in to your existing account!" : "Logged in successfully!");
            await router.push({ name: "contacts", params: { username: authStore.user.username } });
        }
    } catch {
    }
};

// Handle Google Sign-Up
const handleCompleteGoogleRegistration = async () => {
    const errorMsg = validateUsername(promptUsername.value);
    if (errorMsg) {
        modalValidationError.value = errorMsg;
        return;
    }
    modalValidationError.value = "";
    const clean = promptUsername.value.trim().replace(/^_+|_+$/g, '');

    modalLoading.value = true;
    try {
        await authStore.loginWithGoogle(savedGoogleToken.value, clean);
        showUsernameModal.value = false;
        toastStore.success("Account created successfully!");
        await router.push({ name: "contacts", params: { username: authStore.user.username } });
    } catch (err) {
        modalValidationError.value = err.message || "Username is already taken.";
    } finally {
        modalLoading.value = false;
    }
};

// Close popup modal for username when registering account
const closeUsernameModal = () => {
    showUsernameModal.value = false;
    savedGoogleToken.value = "";
    modalValidationError.value = "";
    authStore.googleError = "";
};

const handleFormSubmit = () => {
    isSubmitted.value = true;
    emit('submit');
};
</script>

<template>
    <div class="form-grid">
        <div class="form-background"></div>
        <div class="form-container">
            <form @submit.prevent="handleFormSubmit" :class="['d-grid', 'row-gap-1', { 'form-register': isRegister }]" autocomplete="on">
                
                <!-- Hidden decoy inputs to absorb browser form autofill to on the email and password fields.
                     Using (v-if="!isRegister") prevents autofill in the Username field during registration.
                -->
                <div v-if="!isRegister" style="position: absolute; opacity: 0; pointer-events: none; height: 0; width: 0; overflow: hidden;" tabindex="-1" aria-hidden="true">
                    <input type="text" name="fake_email" tabindex="-1" autocomplete="username" />
                    <input type="password" name="fake_password" tabindex="-1" autocomplete="current-password" />
                </div>

                <div class="d-flex justify-content-center mb-2">
                    <img :src="Logo" alt="Logo" style="width: 2.5rem; height: 2.5rem;">
                </div>

                <!-- Username (Register only) -->
                <div v-if="isRegister">
                    <label for="nickname" class="form-label">Username</label>
                    <input 
                        type="text" 
                        id="nickname" 
                        name="nickname" 
                        placeholder="John Doe" 
                        class="form-input" 
                        v-model="v$.username.$model"
                        autocomplete="nickname"
                        maxlength="20"
                    />

                    <Transition name="slide-fade">
                        <p class="error" v-if="isSubmitted && v$.username.$error">
                            {{ v$.username.$errors[0].$message }}
                        </p>
                        <p class="error" v-else-if="error && error.toLowerCase().includes('username')">
                            {{ error }}
                        </p>
                    </Transition>
                </div>

                <!-- Autocomplete for email on registration has to be set to "username" 
                     to store account credential to password manager vault. Assigning
                     "username email" to autocomplete on login prevents auto fill on
                     email field.
                -->
                <div>
                    <label for="email" class="form-label">Email</label>
                    <input 
                        type="email" 
                        id="email"
                        name="email"
                        placeholder="example@example.com" 
                        class="form-input" 
                        v-model="v$.email.$model"
                        :autocomplete="isRegister ? 'username' : 'username email'"
                    />
                    <Transition name="slide-fade">
                        <p class="error" v-if="isSubmitted && v$.email.$error">
                            {{ v$.email.$errors[0].$message }}
                        </p>
                        <p class="error" v-else-if="error && isRegister && error.toLowerCase().includes('email')">
                            {{ error }}
                        </p>
                    </Transition>
                </div>

                <div>
                    <label for="password" class="form-label">Password</label>
                    <div class="password-wrapper">
                        <input
                            :type="showPassword ? 'text' : 'password'"
                            id="password"
                            name="password"
                            placeholder="********" 
                            class="form-input password-input" 
                            v-model="v$.password.$model"
                            :autocomplete="isRegister ? 'new-password' : 'current-password'"
                        />
                        <button
                            type="button"
                            class="toggle-password-btn"
                            @click="showPassword = !showPassword"
                            :aria-label="showPassword ? 'Hide password' : 'Show password'"
                            tabindex="-1"
                        >
                            <EyeSlashIcon v-if="showPassword" class="icon" />
                            <EyeIcon v-else class="icon" />
                        </button>
                    </div>
                    
                    <Transition name="slide-fade">
                        <p class="error" v-if="v$.password.$dirty && v$.password.$error">
                            {{ v$.password.$errors[0].$message }}
                        </p>
                        <p class="error" v-else-if="error && (!isRegister || (!error.toLowerCase().includes('username') && !error.toLowerCase().includes('email')))">
                            {{ error }}
                        </p>
                    </Transition>
                </div>

                <div v-if="!isRegister" class="d-flex justify-content-end mt-1">
                    <router-link to="/forgot-password" class="forgot-link">
                        Forgot Password?
                    </router-link>
                </div>

                <div v-if="isRegister">

                    <label for="confirmPassword" class="form-label">Confirm Password</label>
                    <div class="password-wrapper">
                        <input
                            :type="showConfirmPassword ? 'text' : 'password'" 
                            id="confirmPassword"
                            name="confirmPassword"
                            placeholder="********" 
                            class="form-input password-input" 
                            v-model="v$.confirmPassword.$model"
                            :autocomplete="isRegister ? 'new-password' : 'current-password'"
                        />
                        <button
                            type="button"
                            class="toggle-password-btn"
                            @click="showConfirmPassword = !showConfirmPassword"
                            :aria-label="showConfirmPassword ? 'Hide password' : 'Show password'"
                            tabindex="-1"
                        >
                            <EyeSlashIcon v-if="showConfirmPassword" class="icon" />
                            <EyeIcon v-else class="icon" />
                        </button>
                    </div>
                    
                    <Transition name="slide-fade">
                        <p class="error" v-if="v$.confirmPassword.$dirty && v$.confirmPassword.$error">{{ v$.confirmPassword.$errors[0].$message }}</p>
                    </Transition>
                </div>

                <!-- Toggle Link -->
                <div class="mt-2">
                    <p style="color: var(--text-inversed);" class="mb-0" v-if="!isRegister">
                        Don't have an account? 
                        <router-link to="/register" class="form-link">Register</router-link>
                    </p>
                    <p style="color: var(--text-inversed);" class="mb-0" v-else>
                        Already have an account? 
                        <router-link to="/login" class="form-link">Login</router-link>
                    </p>
                </div>

                <!-- Submit Button -->
                <div class="mt-3">
                    <button type="submit" class="form-btn" :disabled="loading">
                        <span v-if="loading">
                            {{ isRegister ? "Signing Up" : "Signing In" }}<span class="dots"></span>
                        </span>
                        <span v-else>
                            {{ isRegister ? "Sign Up" : "Sign In" }}
                        </span>
                    </button>
                </div>

                <!-- Divider -->
                <div class="auth-divider">
                    <span class="divider-line"></span>
                    <span class="divider-text">OR</span>
                    <span class="divider-line"></span>
                </div>

                <!-- Google Sign-In Button Slot -->
                <div class="google-btn-wrapper">
                    <div class="google-btn-container">
                        <!-- Permanent Base Button: Rendered synchronously at Frame 0, matching Google's left-aligned layout -->
                        <div class="google-btn-skeleton" aria-hidden="true">
                            <div class="google-skeleton-content">
                                <svg class="google-icon" viewBox="0 0 24 24" width="14" height="14">
                                    <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
                                    <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
                                    <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z"/>
                                    <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z"/>
                                </svg>
                                <span class="google-skeleton-text">{{ isRegister ? "Sign up with Google" : "Sign in with Google" }}</span>
                            </div>
                        </div>

                        <!-- Google Iframe Slot: Overlays on top and reveals only when loaded -->
                        <div 
                            id="google-btn-slot" 
                            :class="{ 'visible': isGoogleLoaded }"
                        ></div>
                    </div>
                </div>
                <!-- Google Auth Error Message -->
            </form>
        </div>

        <!-- Choose Username Modal -->
        <BaseModal
            :is-open="showUsernameModal"
            title="Choose Your Username"
            size="sm"
            action-text="Continue"
            :is-loading="modalLoading"
            @close="closeUsernameModal"
            @action="handleCompleteGoogleRegistration"
        >
            <p class="modal-desc">
                Please choose a unique username to complete your Google registration.
            </p>

            <div class="modal-input-group mb-3">
                <label for="prompt-username" class="modal-input-label">Username</label>
                    <input
                        type="text"
                        id="prompt-username"
                        class="form-input"
                        v-model="promptUsername"
                        placeholder="your_username"
                        maxlength="20"
                        autocomplete="off"
                        autofocus
                        @keyup.enter="handleCompleteGoogleRegistration"
                    />
                
                <Transition name="slide-fade">
                    <p class="error mt-1" v-if="modalValidationError || authStore.googleError">
                        {{ modalValidationError || authStore.googleError }}
                    </p>
                </Transition>
            </div>
        </BaseModal>
    </div>
</template>

<style scoped>
.border-bottom-theme {
    border-bottom: 1px solid var(--border-light, rgba(255, 255, 255, 0.08));
}


.forgot-link {
    color: var(--accent-primary);
    font-size: var(--font-size-xs);
    font-weight: 500;
    text-decoration: none;
    transition: color 0.2s ease;
}
.forgot-link:hover {
    color: var(--accent-hover);
    text-decoration: underline;
}

.password-wrapper {
    position: relative;
    display: flex;
    align-items: center;
}

.password-input {
    padding-right: 2.5rem;
}

.toggle-password-btn {
    position: absolute;
    right: 0.625rem;
    background: transparent;
    border: none;
    padding: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    color: var(--text-muted);
    transition: color 0.2s ease;
}

.toggle-password-btn:hover {
    color: var(--text-primary);
}

.toggle-password-btn .icon {
    width: 1.25rem;
    height: 1.25rem;
}

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

    .form-grid .form-register {
        margin-top: 0;
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

.form-container {
    opacity: 0; 
    animation: slideInRight 0.5s ease-out forwards;
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

.auth-divider {
    display: flex;
    align-items: center;
    margin: 0.5rem 0;
    gap: 0.75rem;
}
.divider-line {
    flex: 1;
    height: 1px;
    background-color: var(--border-light, #e2e8f0);
}
.divider-text {
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--text-muted, #94a3b8);
    text-transform: uppercase;
}
.google-btn-wrapper {
    display: flex;
    justify-content: center;
    align-items: center;
    width: 100%;
    margin-bottom: 10px;
}

.google-btn-container {
    position: relative;
    width: 320px;
    height: 32px;
}

.google-btn-skeleton {
    position: absolute;
    inset: 0;
    width: 320px;
    height: 32px;
    box-sizing: border-box;
    background-color: #ffffff;
    border: 1px solid #dadce0;
    border-radius: 4px;
    padding: 0 10px;
    display: flex;
    align-items: center;
    user-select: none;
    box-shadow: 0 1px 2px rgba(60, 64, 67, 0.08);
    z-index: 1;
}

.google-skeleton-content {
    display: flex;
    align-items: center;
    width: 100%;
    height: 100%;
}

.google-skeleton-content .google-icon {
    width: 14px;
    height: 14px;
    min-width: 14px;
    margin-right: 8px;
    flex-shrink: 0;
}

.google-skeleton-text {
    flex-grow: 1;
    text-align: center;
    font-size: 14px;
    font-weight: 500;
    letter-spacing: 0.25px;
    color: #1f1f1f;
    font-family: "Google Sans", Roboto, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
    margin-right: 22px;
}

#google-btn-slot {
    position: absolute;
    inset: 0;
    width: 320px;
    height: 32px;
    opacity: 0;
    pointer-events: none;
    transition: opacity 0.2s ease-in;
    z-index: 2;
}

#google-btn-slot.visible {
    opacity: 1;
    pointer-events: auto;
}

/* Enhanced Elegant Modal */
.modal-overlay {
    position: fixed;
    inset: 0;
    background: rgba(15, 10, 5, 0.65);
    backdrop-filter: blur(8px);
    -webkit-backdrop-filter: blur(8px);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 9999;
    padding: 1.25rem;
}

.modal-card {
    background-color: var(--bg-modal);
    border: 1px solid hsla(var(--hue-warm-gold), 50%, 92%, 0.5);
    color: var(--text-primary);
    padding: 1.75rem;
    border-radius: 1.25rem;
    width: 100%;
    max-width: 25rem;
    box-shadow: 0 25px 50px -12px rgba(35, 20, 5, 0.35), 0 0 0 1px hsla(38, 55%, 50%, 0.15);
    transform: scale(1);
    transition: transform 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

.modal-icon-badge {
    width: 36px;
    height: 36px;
    border-radius: 50%;
    background: hsla(38, 55%, 50%, 0.18);
    color: var(--accent-hover);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
}

.modal-title {
    font-size: 1.2rem;
    font-weight: 700;
    letter-spacing: -0.01em;
    color: var(--text-primary);
}

.modal-desc {
    font-size: 0.85rem;
    color: var(--text-secondary);
    margin-bottom: 1.25rem;
    line-height: 1.45;
}

.modal-input-label {
    display: block;
    font-size: 0.8rem;
    font-weight: 600;
    color: var(--text-primary);
    margin-bottom: 0.35rem;
}

.modal-hint {
    display: block;
    font-size: 0.725rem;
    color: var(--text-muted);
    margin-top: 0.35rem;
}

.btn-modal-cancel {
    color: var(--text-secondary, #8696a0);
    border: 1px solid var(--border-light, rgba(255, 255, 255, 0.15));
}

.btn-modal-cancel:hover:not(:disabled) {
    color: var(--text-primary, #e9edef);
    background-color: rgba(255, 255, 255, 0.05);
    border: 1px solid var(--border-light, rgba(255, 255, 255, 0.15));
}


.btn-modal-confirm {
    background-color: var(--accent-primary);
    color: var(--text-inversed);
    border: none;
}

.btn-modal-confirm:hover:not(:disabled) {
    color: var(--text-inversed);
    background-color: var(--accent-hover);
}


.btn-modal-confirm:disabled,
.btn-modal-cancel:disabled {
    opacity: 0.6;
    cursor: not-allowed;
    transform: none;
}

/* Modal Transition */
.modal-fade-enter-active,
.modal-fade-leave-active {
    transition: opacity 0.2s ease;
}

.modal-fade-enter-active .modal-card,
.modal-fade-leave-active .modal-card {
    transition: transform 0.2s cubic-bezier(0.16, 1, 0.3, 1), opacity 0.2s ease;
}

.modal-fade-enter-from,
.modal-fade-leave-to {
    opacity: 0;
}

.modal-fade-enter-from .modal-card {
    transform: scale(0.94);
    opacity: 0;
}

.modal-fade-leave-to .modal-card {
    transform: scale(0.96);
    opacity: 0;
}
</style>