<script setup lang="ts">
const navigationOpen = ref(false)
const openingJournalList = ref(false)
const soundPanel = ref(false)
const editingName = ref(false)
const nameDraft = ref('')
const nameError = ref('')
const savingName = ref(false)
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

async function saveName() {
  if (savingName.value) return
  if (!authUser.value) {
    nameError.value = '請先登入，才能儲存名稱。'
    return
  }
  savingName.value = true
  try {
    if (!await onboarding.setName(nameDraft.value)) {
      nameError.value = '請輸入 1 至 64 個字的名稱。'
      return
    }
    editingName.value = false
    navigationOpen.value = false
  } catch {
    nameError.value = '名稱目前無法儲存，請稍後再試。'
  } finally {
    savingName.value = false
  }
}

async function handleLogout() {
  navigationOpen.value = false
  await auth.logout()
}

async function openJournalList() {
  if (openingJournalList.value) return
  openingJournalList.value = true
  navigationOpen.value = false
  try {
    const editing = (journal.stage >= 7 && journal.stage <= 13) || (journal.stage >= 17 && journal.stage <= 21) || journal.stage === 23
    if (route.path === '/journals' && editing && journal.hasContent()) {
      if (!await journal.save(journal.completed, 6)) return
    }
    journal.stage = auth.user ? 6 : 16
    await navigateTo('/journals')
  } finally {
    openingJournalList.value = false
  }
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
watch(authUser, (user) => {
  if (!user) editingName.value = false
})
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
          <nav v-if="navigationOpen" id="main-navigation" class="main-nav" :class="{ 'main-nav-editing': authUser && editingName }" aria-label="主要導覽">
            <NuxtLink to="/" :aria-current="route.path === '/' ? 'page' : undefined" @click="navigationOpen = false"><svg class="nav-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m3 10 9-7 9 7v10H3zM9 20v-7h6v7" /></svg><span>首頁</span></NuxtLink>
            <NuxtLink to="/introduction" :aria-current="route.path === '/introduction' ? 'page' : undefined" @click="onboarding.restart(); navigationOpen = false"><svg class="nav-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="3" y="4" width="18" height="16" rx="2" /><path d="m10 8 6 4-6 4z" /></svg><span>動畫導覽</span></NuxtLink>
            <NuxtLink to="/journals" :aria-current="route.path === '/journals' ? 'page' : undefined" @click.prevent="openJournalList"><svg class="nav-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 4h6a3 3 0 0 1 3 3v14a3 3 0 0 0-3-3H4zM13 7a3 3 0 0 1 3-3h4v14h-4a3 3 0 0 0-3 3" /></svg><span>覺察日記</span></NuxtLink>
            <NuxtLink to="/strengths" :aria-current="route.path === '/strengths' ? 'page' : undefined" @click="navigationOpen = false"><svg class="nav-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m12 3 2.5 6.5L21 12l-6.5 2.5L12 21l-2.5-6.5L3 12l6.5-2.5z" /></svg><span>看見我的力量</span></NuxtLink>
            <button v-if="authUser && !editingName" type="button" @click="startEditingName"><svg class="nav-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m15 4 5 5M4 20l5-1L20 8a2 2 0 0 0-5-5L4 14z" /></svg><span>修改批評者名稱</span></button>
            <form v-else-if="authUser && editingName" class="critic-name-editor" @submit.prevent="saveName">
              <label for="critic-name-input">內在批評者的名稱</label>
              <input id="critic-name-input" v-model="nameDraft" maxlength="64" autocomplete="off" :disabled="savingName" @input="nameError = ''">
              <p v-if="nameError" role="alert">{{ nameError }}</p>
              <div class="critic-name-actions">
                <button type="button" :disabled="savingName" @click="editingName = false">取消</button>
                <button type="submit" :disabled="savingName">{{ savingName ? '儲存中…' : '儲存' }}</button>
              </div>
            </form>
            <button v-if="authUser" type="button" @click="handleLogout"><svg class="nav-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M9 4H4v16h5M9 12h12m-4-4 4 4-4 4" /></svg><span>登出</span></button>
          </nav>
        </div>
      </div>
    </header>

    <NuxtPage />
  </main>
</template>

<style scoped>
.main-nav { width:180px; max-width:calc(100vw - 24px); }
.main-nav.main-nav-editing { width:min(250px,calc(100vw - 24px)); }
.main-nav > :is(a,button) { display:flex; align-items:center; gap:10px; }
.main-nav .nav-icon { flex-shrink:0; }
.main-nav > :is(a,button) span { white-space:nowrap; }
</style>
