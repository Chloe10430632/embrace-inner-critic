<script setup lang="ts">
defineProps<{ eyebrow: string; description: string; message?: string }>()
const auth = useAuthStore()
</script>

<template>
  <section class="scene compact-scene">
    <div class="blank-node" aria-hidden="true"><svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="var(--green)" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="5" y="10" width="14" height="11" rx="2" /><path d="M8 10V7a4 4 0 0 1 8 0v3M12 14v3" /></svg></div>
    <p class="eyebrow">{{ eyebrow }}</p>
    <h2>您目前尚未登入</h2>
    <p class="lead narrow">{{ description }}</p>
    <p v-if="message" class="auth-message">{{ message }}</p>
    <button class="primary" @click="auth.startGoogleLogin"><img class="google-login-logo" src="/google-logo.svg" width="20" height="20" alt="" aria-hidden="true">使用 Google 登入 <span>→</span></button>
  </section>
</template>
