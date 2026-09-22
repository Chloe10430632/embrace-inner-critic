<script setup lang="ts">
const navigationOpen = ref(false)
const soundPanel = ref(false)
const editingName = ref(false)
const nameDraft = ref('')
const nameError = ref('')
const soundWrap = ref<HTMLElement | null>(null)
const navigationWrap = ref<HTMLElement | null>(null)
const route = useRoute()
const auth = useAuthStore()
const journal = useJournalStore()
const onboarding = useOnboardingStore()
const { user: authUser } = storeToRefs(auth)
const { completed } = storeToRefs(journal)
const { confirmedName } = storeToRefs(onboarding)
const { soundOn, effectsOn, startAmbient, setSound, dispose } = useAudio()

function unlockAudio() {
  if (soundOn.value) startAmbient()
}

function closeMenusOutside(event: PointerEvent) {
  const target = event.target
  if (!(target instanceof Node)) return
  if (!soundWrap.value?.contains(target)) soundPanel.value = false
  if (!navigationWrap.value?.contains(target)) navigationOpen.value = false
}

function startEditingName() {
  nameDraft.value = confirmedName.value
  nameError.value = ''
  editingName.value = true
}

function saveName() {
  if (!onboarding.setName(nameDraft.value)) {
    nameError.value = '請輸入 1 至 16 個字的名稱。'
    return
  }
  editingName.value = false
  navigationOpen.value = false
}

async function handleLogout() {
  navigationOpen.value = false
  await auth.logout()
}

onMounted(async () => {
  onboarding.initialize()
  window.addEventListener('pointerdown', unlockAudio, { once: true })
  window.addEventListener('pointerdown', closeMenusOutside)
  window.addEventListener('keydown', unlockAudio, { once: true })
  setSound(soundOn.value)
  await auth.checkSession()
})
onBeforeUnmount(() => {
  window.removeEventListener('pointerdown', unlockAudio)
  window.removeEventListener('pointerdown', closeMenusOutside)
  window.removeEventListener('keydown', unlockAudio)
  dispose()
})
watch(soundOn, setSound)
</script>

<template>
  <main class="app-shell" :class="{ 'is-complete': completed }">
    <div class="mist mist-one" />
    <div class="mist mist-two" />

    <header class="topbar">
      <NuxtLink class="brand" to="/" aria-label="回到擁抱內在批評者首頁" @click="navigationOpen = false">
        <img class="brand-mark" src="/favicon.svg" alt="" width="40" height="40">
        <span class="brand-wordmark">
          <span class="tracking-widest">擁抱內在批評者</span>
          <small>EMBRACE YOUR INNER CRITIC</small>
        </span>
      </NuxtLink>
      <div class="topbar-actions">
        <div ref="soundWrap" class="sound-wrap">
          <button class="icon-button" aria-label="聲音設定" @click="soundPanel = !soundPanel">
            {{ soundOn ? '♪' : '♩' }}
          </button>
          <div v-if="soundPanel" class="sound-panel">
            <label><input v-model="soundOn" type="checkbox"> 背景聲景</label>
            <label><input v-model="effectsOn" type="checkbox"> 按鈕音效</label>
          </div>
        </div>
        <div ref="navigationWrap" class="navigation-wrap">
          <button class="icon-button menu-button" :aria-expanded="navigationOpen" aria-controls="main-navigation" aria-label="開啟主要導覽" @click="navigationOpen = !navigationOpen"><span /><span /><span /></button>
          <nav v-if="navigationOpen" id="main-navigation" class="main-nav" :class="{ 'main-nav-editing': editingName }" aria-label="主要導覽">
            <NuxtLink to="/" :aria-current="route.path === '/' ? 'page' : undefined" @click="navigationOpen = false">首頁</NuxtLink>
            <NuxtLink to="/journals" :aria-current="route.path === '/journals' ? 'page' : undefined" @click="navigationOpen = false">我的日記</NuxtLink>
            <NuxtLink to="/progress" :aria-current="route.path === '/progress' ? 'page' : undefined" @click="navigationOpen = false">追蹤進展</NuxtLink>
            <NuxtLink to="/introduction" :aria-current="route.path === '/introduction' ? 'page' : undefined" @click="navigationOpen = false">動畫導覽</NuxtLink>
            <button v-if="!editingName" type="button" @click="startEditingName">修改批評者名稱</button>
            <form v-else class="critic-name-editor" @submit.prevent="saveName">
              <label for="critic-name-input">內在批評者的名稱</label>
              <input id="critic-name-input" v-model="nameDraft" maxlength="16" autocomplete="off" @input="nameError = ''">
              <p v-if="nameError" role="alert">{{ nameError }}</p>
              <div class="critic-name-actions">
                <button type="button" @click="editingName = false">取消</button>
                <button type="submit">儲存</button>
              </div>
            </form>
            <button v-if="authUser" type="button" @click="handleLogout">登出</button>
          </nav>
        </div>
      </div>
    </header>

    <NuxtPage />
  </main>
</template>
