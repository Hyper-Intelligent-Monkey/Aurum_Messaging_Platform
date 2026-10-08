<script setup>
import { ref, reactive, onMounted, onUnmounted, nextTick } from "vue";
import { useRouter } from "vue-router";
import { forgotPassword, resetPassword } from "../services/authService";
import { useToastStore } from "../store/toastStore";
import Logo from "../assets/MessagingPlatformLogo.png";
import { EyeIcon, EyeSlashIcon } from "@heroicons/vue/24/outline";

const router = useRouter();
const toastStore = useToastStore();

const email = ref("");
const otpDigits = ref(["", "", "", "", "", ""]);
const otpInputRefs = ref([]);
const newPassword = ref("");
const confirmPassword = ref("");
const showNewPassword = ref(false);
const showConfirmPassword = ref(false);
const isSubmitting = ref(false);
const isResending = ref(false);

const errors = reactive({
    otp: "",
    newPassword: "",
    confirmPassword: ""
});

const clearErrors = () => {
    errors.otp = "";
    errors.newPassword = "";
    errors.confirmPassword = "";
};

const resendCooldown = ref(0);
let cooldownTimer = null;

// start cooldown timer for resend otp code
const startCooldown = (seconds = 60) => {
    resendCooldown.value = seconds;
    sessionStorage.setItem("forgot_cooldown_end", String(Date.now() + seconds * 1000));
    if (cooldownTimer) clearInterval(cooldownTimer);
    cooldownTimer = setInterval(() => {
        resendCooldown.value--;
        if (resendCooldown.value <= 0) {
            clearInterval(cooldownTimer);
            sessionStorage.removeItem("forgot_cooldown_end");
        }
    }, 1000);
};

// get email from forgot password page and start cooldown timer
onMounted(() => {
    const savedEmail = sessionStorage.getItem("reset_email");
    if (!savedEmail) {
        router.replace({ name: "forgot-password" });
        return;
    }
    email.value = savedEmail;

    const savedCooldownEnd = sessionStorage.getItem("forgot_cooldown_end");
    if (savedCooldownEnd) {
        const remaining = Math.round((Number(savedCooldownEnd) - Date.now()) / 1000);
        if (remaining > 0) {
            startCooldown(remaining);
        }
    }

    nextTick(() => {
        otpInputRefs.value[0]?.focus();
    });
});

onUnmounted(() => {
    if (cooldownTimer) clearInterval(cooldownTimer);
});

// Move cursor to the end of the digit
const setCursorAtEnd = (el) => {
    if (!el) return;
    const len = el.value?.length || 0;
    el.setSelectionRange(len, len);
    requestAnimationFrame(() => {
        el.setSelectionRange(len, len);
    });
};

const handleOtpFocus = (event) => {
    setCursorAtEnd(event.target);
};

const handleOtpInput = (index, event) => {
    errors.otp = "";
    // input must be a number
    const val = event.target.value.replace(/[^0-9]/g, "");
    // populates the input field
    otpDigits.value[index] = val ? val.slice(-1) : "";
    // matches the coresponding digit
    event.target.value = otpDigits.value[index];

    // if the input is a number, move to the next input field if it exists
    if (val && index < 5) {
        const nextInput = otpInputRefs.value[index + 1];
        nextInput?.focus();
        setCursorAtEnd(nextInput);
    }
};

const handleOtpKeydown = (index, event) => {
    // block space
    if (event.key === " " || event.code === "Space") {
        event.preventDefault();
        return;
    }

    // typing a digit into a box that already has a number
    if (/^[0-9]$/.test(event.key)) {
        if (otpDigits.value[index]) {
            // find the next empty slot to the right
            const nextEmptyIndex = otpDigits.value.findIndex((d, i) => i > index && !d);
            if (nextEmptyIndex !== -1) {
                event.preventDefault();
                errors.otp = "";
                var digit = event.key;
                // updates the reactive state
                otpDigits.value[nextEmptyIndex] = digit;
                // populating with the DOM element
                const targetInput = otpInputRefs.value[nextEmptyIndex];
                if (targetInput) {
                    // selects the digit
                    targetInput.value = digit;
                    // put focus on the digit
                    targetInput.focus();
                    // put the cursor at the end of the digit
                    setCursorAtEnd(targetInput);
                }
                return;
            } else {
                // If all slots are full, overwrite current box
                event.preventDefault();
                errors.otp = "";
                otpDigits.value[index] = event.key;
                event.target.value = event.key;
                setCursorAtEnd(event.target);
                return;
            }
        }
    }

    // Navigate backwards on backspace if current box is empty
    if (event.key === "Backspace") {
        if (!otpDigits.value[index] && index > 0) {
            event.preventDefault();
            const prevInput = otpInputRefs.value[index - 1];
            prevInput?.focus();
            setCursorAtEnd(prevInput);
        }
        return;
    }

    // move to previous box and place cursor to the right
    if (event.key === "ArrowLeft" && index > 0) {
        event.preventDefault();
        const prevInput = otpInputRefs.value[index - 1];
        prevInput?.focus();
        setCursorAtEnd(prevInput);
        return;
    }

    // move to next box and place cursor to the right
    if (event.key === "ArrowRight" && index < 5) {
        event.preventDefault();
        const nextInput = otpInputRefs.value[index + 1];
        nextInput?.focus();
        setCursorAtEnd(nextInput);
        return;
    }
};

// paste otp code logic
const handleOtpPaste = (event) => {
    event.preventDefault();
    const pasted = event.clipboardData.getData("text").replace(/[^0-9]/g, "").slice(0, 6);
    if (!pasted) return;

    for (let i = 0; i < 6; i++) {
        otpDigits.value[i] = pasted[i] || "";
    }
    const focusIndex = Math.min(pasted.length, 5);
    otpInputRefs.value[focusIndex]?.focus();
};

// when switching to a page and clear session storage
const handleSwitchTo = () => {
    sessionStorage.removeItem("reset_email");
    sessionStorage.removeItem("forgot_cooldown_end");
};

// resend otp code
const handleResendOtp = async () => {
    if (resendCooldown.value > 0 || isResending.value || isSubmitting.value) return;
    clearErrors();
    isResending.value = true;
    try {
        await forgotPassword(email.value.trim());
        toastStore.success("A new 6-digit code has been sent to your email.");
        startCooldown(60);
    } catch (err) {
        errors.otp = err.message || "Failed to resend code.";
    } finally {
        isResending.value = false;
    }
};

// reset password using otp and new password
const handleResetPassword = async () => {
    clearErrors();

    const fullOtp = otpDigits.value.join("").trim();
    if (fullOtp.length !== 6) {
        errors.otp = "Please enter the complete 6-digit code.";
        return;
    }
    if (!newPassword.value) {
        errors.newPassword = "New password is required.";
        return;
    }
    if (newPassword.value.length < 8) {
        errors.newPassword = "Password must be at least 8 characters.";
        return;
    }
    if (!confirmPassword.value) {
        errors.confirmPassword = "Confirm password is required.";
        return;
    }
    if (newPassword.value !== confirmPassword.value) {
        errors.confirmPassword = "Passwords do not match.";
        return;
    }

    isSubmitting.value = true;
    try {
        await resetPassword(email.value.trim(), fullOtp, newPassword.value);
        toastStore.success("Password reset successfully!");
        sessionStorage.removeItem("reset_email");
        sessionStorage.removeItem("forgot_cooldown_end");
        await router.push({ name: "login" });
    } catch (err) {
        const msg = err.message || "Failed to reset password.";
        if (msg.toLowerCase().includes("code") || msg.toLowerCase().includes("otp")) {
            errors.otp = msg;
        } else {
            errors.confirmPassword = msg;
        }
    } finally {
        isSubmitting.value = false;
    }
};
</script>

<template>
    <div class="form-grid">
        <div class="form-background"></div>
        <div class="form-container">
            <form @submit.prevent="handleResetPassword" class="d-grid row-gap-1" novalidate>
                <div class="d-flex justify-content-center mb-2">
                    <img :src="Logo" alt="Logo" style="width: 2.5rem; height: 2.5rem;">
                </div>

                <!-- Digit Inputs -->
                <div>
                    <label class="form-label">6-Digit Code</label>
                    <div class="otp-boxes">
                        <input 
                            v-for="(_, index) in 6" 
                            :key="index"
                            :ref="el => { if (el) otpInputRefs[index] = el }"
                            type="text" 
                            inputmode="numeric"
                            maxlength="1" 
                            class="form-input otp-digit-box" 
                            :value="otpDigits[index]"
                            @input="handleOtpInput(index, $event)"
                            @keydown="handleOtpKeydown(index, $event)"
                            @focus="handleOtpFocus($event)"
                            @paste="handleOtpPaste"
                        />
                    </div>
                    <Transition name="slide-fade">
                        <p class="error" v-if="errors.otp">{{ errors.otp }}</p>
                    </Transition>
                </div>

                <div>
                    <label for="resetNewPassword" class="form-label">New Password</label>
                    <div class="password-wrapper">
                        <input
                            :type="showNewPassword ? 'text' : 'password'"
                            id="resetNewPassword"
                            name="newPassword"
                            placeholder="********" 
                            class="form-input password-input" 
                            v-model="newPassword"
                            autocomplete="new-password"
                        />
                        <button
                            type="button"
                            class="toggle-password-btn"
                            @click="showNewPassword = !showNewPassword"
                            :aria-label="showNewPassword ? 'Hide password' : 'Show password'"
                            tabindex="-1"
                        >
                            <EyeSlashIcon v-if="showNewPassword" class="icon" />
                            <EyeIcon v-else class="icon" />
                        </button>
                    </div>
                    <Transition name="slide-fade">
                        <p class="error" v-if="errors.newPassword">{{ errors.newPassword }}</p>
                    </Transition>
                </div>

                <div>
                    <label for="resetConfirmPassword" class="form-label">Confirm New Password</label>
                    <div class="password-wrapper">
                        <input
                            :type="showConfirmPassword ? 'text' : 'password'"
                            id="resetConfirmPassword"
                            name="confirmPassword"
                            placeholder="********" 
                            class="form-input password-input" 
                            v-model="confirmPassword"
                            autocomplete="new-password"
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
                        <p class="error" v-if="errors.confirmPassword">{{ errors.confirmPassword }}</p>
                    </Transition>
                </div>

                <!-- Resend / Change Email actions -->
                <div class="d-flex justify-content-between align-items-center mt-1">
                    <router-link to="/forgot-password" class="btn-subtle" @click="handleSwitchTo">
                        Change email
                    </router-link>
                    <button 
                        type="button" 
                        class="btn-subtle" 
                        :disabled="resendCooldown > 0 || isResending || isSubmitting"
                        @click="handleResendOtp"
                    >
                        {{ isResending ? "Resending..." : (resendCooldown > 0 ? `Resend in ${resendCooldown}s` : "Resend code") }}
                    </button>
                </div>

                <!-- Submit Button -->
                <div class="mt-4">
                    <button type="submit" class="form-btn" :disabled="isSubmitting || isResending">
                        <span v-if="isSubmitting">
                            Resetting Password<span class="dots"></span>
                        </span>
                        <span v-else>
                            Reset Password
                        </span>
                    </button>
                </div>

                <!-- Toggle Link -->
                <div class="mt-2">
                    <p style="color: var(--text-inversed);" class="mb-0">
                        Return to
                        <router-link to="/login" class="form-link" @click="handleSwitchTo">Login</router-link>
                    </p>
                </div>
            </form>
        </div>
    </div>
</template>

<style scoped>
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

.otp-boxes {
    display: flex;
    justify-content: space-between;
    gap: 0.5rem;
}

.otp-digit-box {
    width: 2.75rem;
    height: 3rem;
    text-align: center;
    font-size: 1.25rem;
    font-weight: 700;
    padding: 0;
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

.btn-subtle {
    background: none;
    border: none;
    color: var(--accent-primary);
    font-size: var(--font-size-xs);
    cursor: pointer;
    padding: 0;
    text-decoration: none;
}

.btn-subtle:hover:not(:disabled) {
    color: var(--accent-hover);
    text-decoration: underline;
}

.btn-subtle:disabled {
    color: var(--text-muted);
    cursor: not-allowed;
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
    .form-container {
        width: 100%;
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
</style>
