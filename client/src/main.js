import { createApp } from 'vue';
import { createPinia } from "pinia";
import App from './App.vue';
import router from './router';
import timeago from 'vue-timeago3';

import 'bootstrap/dist/css/bootstrap.min.css';
import 'bootstrap/dist/js/bootstrap.min.js';

import './assets/styles/main.css';
const pinia = createPinia();
const app = createApp(App);
app.use(pinia);
app.use(timeago);
app.use(router);

// Wait for the initial route to resolve before mounting to prevent layout/navbar flicker
router.isReady().then(() => {
  app.mount('#app');
});
