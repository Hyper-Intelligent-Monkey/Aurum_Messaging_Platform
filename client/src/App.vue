<script setup>
import Navbar from './components/Navbar.vue';
import { useAuthStore } from './store/authStore';
import { ref, watchEffect } from "vue";
import Toast from './components/Toast.vue';

const authStore = useAuthStore();
const token = ref("");
watchEffect(() => {
  token.value = authStore.userData;
})
</script>
<template>
  <div class="d-flex flex-column vh-100 overflow-hidden">
    <Navbar v-if="$route.matched.length > 0 && $route.meta.showNavbar !== false" />
    <main class="flex-grow-1 overflow-hidden position-relative">
      <router-view v-slot="{ Component }">
        <component :is="Component" />
      </router-view>
    </main>
    <Toast/>
  </div>
</template>

<style scoped>
.slide-fade-enter {
  transform: translateX(10px);
  opacity: 0;
}

.slide-fade-enter-active,
.slide-fade-leave-active {
  transition: all 0.5s ease;
}

.slide-fade-leave-to {
  transform: translateX(-10px);
  opacity: 0;
}
</style>