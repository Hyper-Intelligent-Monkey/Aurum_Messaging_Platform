<script setup>
import { reactive, computed, onMounted, onUnmounted } from "vue";
import { storeToRefs } from "pinia";
import useVuelidate from "@vuelidate/core";
import { useAuthStore } from "../store/authStore";
import { useRouter } from "vue-router";
import { getRegisterRules } from "../utils/formValidators";
import AuthForm from "../components/AuthForm.vue";
import { useToastStore } from "../store/toastStore";

// Form data for register
const formData = reactive({
    email: "",
    username: "",
    password: "",
    confirmPassword: ""
});

const router = useRouter();
const authStore = useAuthStore();
const toastStore = useToastStore();

const { registerLoading, registerError } = storeToRefs(authStore);

const rules = computed(() => getRegisterRules(formData));
const v$ = useVuelidate(rules, formData);

onMounted(() => {
    registerError.value = "";
});

onUnmounted(() => {
    registerError.value = "";
});

const handleSubmit = async () => {
    registerError.value = "";
    // validate form data
    const isFormValid = await v$.value.$validate();
    if (isFormValid) {
        try {
            const cleanUsername = formData.username.trim().replace(/^_+|_+$/g, '');
            // register user
            await authStore.registerUser(cleanUsername, formData.email, formData.password);
            toastStore.success("Check your email to confirm your account.", 3500);
            await router.push({ name: "login" });
        } catch (error) {
            if (error.status && error.status < 500) {
                registerError.value = error.message;
            } else {
                registerError.value = "";
            }
        }
    }
};
</script>

<template>
    <AuthForm 
        title="Register" 
        :loading="registerLoading" 
        :error="registerError" 
        :v$="v$" 
        :isRegister="true"
        @submit="handleSubmit" 
    />
</template>