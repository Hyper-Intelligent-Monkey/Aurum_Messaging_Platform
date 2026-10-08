<script setup>
import { reactive, computed, onMounted, onUnmounted } from "vue";
import { storeToRefs } from "pinia";
import useVuelidate from "@vuelidate/core";
import { useAuthStore } from "../store/authStore";
import { useRouter, useRoute } from "vue-router";
import { getLoginRules } from "../utils/formValidators";
import AuthForm from "../components/AuthForm.vue";
import { useToastStore } from "../store/toastStore";

// Form data for login
const formData = reactive({
    email: "",
    password: ""
});

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const toastStore = useToastStore();

const { loginLoading, loginError } = storeToRefs(authStore);

const rules = computed(() => getLoginRules());
const v$ = useVuelidate(rules, formData);

onMounted(() => {
    loginError.value = "";
    // check for confirmation from the email
    if (route.query.confirmed === "true") {
        toastStore.success("Email confirmed successfully!");
        router.replace({ query: {} });
    } else if (route.query.confirmed === "false") {
        toastStore.error("Invalid or expired confirmation link.");
        router.replace({ query: {} });
    }
});

onUnmounted(() => {
    loginError.value = "";
});

const handleSubmit = async () => {
    loginError.value = "";
    const isFormValid = await v$.value.$validate();
    if (isFormValid) {
        try {
            // login user
            await authStore.loginUser(formData.email, formData.password);
            toastStore.success("Logged in successfully!");
            await router.push({ name: "contacts", params: { username: authStore.user.username } });
        } catch (error) {
            if (error.status && error.status < 500) {
                loginError.value = error.message;
            } else {
                loginError.value = "";
            }
        }
    }
};
</script>

<template>
    <AuthForm 
        title="Login" 
        :loading="loginLoading" 
        :error="loginError" 
        :v$="v$" 
        @submit="handleSubmit" 
    />
</template>