<script setup lang="ts">
const ready = ref(false)
const signedIn = ref(false)
const auth = useAuthStore()
const journalStore = useJournalStore()
const { entries, message } = storeToRefs(journalStore)
const { loadEntries, formatDate, isRecorded, isProgressComplete, entryStatus } = journalStore
const { startGoogleLogin } = auth

const recordedEntries = computed(() => entries.value.filter(isRecorded))

function answer(entry: typeof entries.value[number], key: keyof typeof entry.answers) {
  const value = entry.answers[key]
  if (Array.isArray(value)) return value.length ? value.map(item => `#${item}`).join('　') : '尚未記錄'
  return typeof value === 'string' && value.trim() ? value : '尚未記錄'
}

onMounted(async () => {
  signedIn.value = await auth.requireUser()
  if (signedIn.value) await loadEntries()
  ready.value = true
})
</script>

<template>
  <section v-if="!ready" class="scene compact-scene" role="status">
    <span class="journal-spinner" aria-hidden="true" />
    <p class="page-loading-caption">載入時光紀錄</p>
  </section>

  <section v-else-if="!signedIn" class="scene compact-scene">
    <div class="blank-node" aria-hidden="true"><svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="var(--green)" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="5" y="10" width="14" height="11" rx="2" /><path d="M8 10V7a4 4 0 0 1 8 0v3M12 14v3" /></svg></div>
    <p class="eyebrow">追蹤進展</p>
    <h2>登入後，才能看見<br>自己的轉化歷程。</h2>
    <p class="lead narrow">這裡會整理每篇日記的自我批評、感受、重新框架與行動。</p>
    <button class="primary" @click="startGoogleLogin"><img class="google-login-logo" src="/google-logo.svg" width="20" height="20" alt="" aria-hidden="true">使用 Google 登入 <span>→</span></button>
    <NuxtLink class="text-button" to="/">先回到首頁</NuxtLink>
  </section>

  <section v-else class="scene progress-overview-scene">
    <div class="progress-overview-heading">
      <div>
        <!-- <p class="eyebrow">追蹤進展</p> -->
        <h2>把反覆出現的聲音，<br>和你走過的路放在一起看。</h2>
        <p class="lead">一篇一篇看見當時的自我批評，也看見自己如何回應、試著走出一步。</p>
      </div>
      <NuxtLink class="secondary" to="/journals">回到我的日記</NuxtLink>
    </div>

    <p v-if="message" class="auth-message">{{ message }}</p>

    <div v-if="recordedEntries.length" class="progress-entry-list">
      <article v-for="entry in recordedEntries" :key="entry.id" class="progress-entry-card">
        <header class="progress-entry-header">
          <div><time :datetime="entry.createdAt">{{ formatDate(entry.createdAt) }}</time><span class="entry-status" :data-status="entryStatus(entry)">{{ entryStatus(entry) }}</span></div>
          <NuxtLink class="progress-entry-link" :to="`/journals?progress=${entry.id}`">{{ isProgressComplete(entry) ? '查看／修改' : entryStatus(entry) === '待完成行動' ? '回來記錄感受 →' : '繼續追蹤 →' }}</NuxtLink>
        </header>
        <div class="progress-entry-comparison">
          <section class="progress-entry-voice">
            <h3>當時的聲音</h3>
            <blockquote>「{{ answer(entry, 'critic') }}」</blockquote>
            <details class="progress-entry-context">
              <summary>看看當時的情境與感受</summary>
              <dl>
                <dt>觸發情境</dt><dd>{{ answer(entry, 'trigger') }}</dd>
                <dt>情緒</dt><dd>{{ answer(entry, 'emotions') }}</dd>
                <dt>行為</dt><dd>{{ answer(entry, 'behaviors') }}</dd>
              </dl>
            </details>
          </section>
          <section class="progress-entry-effort">
            <h3>我試著走過的路</h3>
            <dl>
              <dt>重新框架想法</dt><dd>{{ answer(entry, 'reframedThought') }}</dd>
              <dt>{{ isProgressComplete(entry) ? '試過的小行動' : '想試試看的小行動' }}</dt><dd>{{ answer(entry, 'nextAction') }}</dd>
              <dt>行動後的感受</dt><dd>{{ answer(entry, 'afterActionEmotion') }}</dd>
            </dl>
          </section>
        </div>
      </article>
    </div>

    <div v-else class="progress-empty">
      <p>儲存第一篇覺察紀錄後，就可以接著記錄重新框架與小行動。</p>
      <NuxtLink class="primary" to="/journals">前往我的日記 <span>→</span></NuxtLink>
    </div>
  </section>
</template>

<style scoped>
.progress-overview-scene { width:min(1120px,calc(100% - 36px)); padding-bottom:72px; }
.progress-entry-list { display:grid; gap:30px; margin-top:34px; }
.progress-entry-card { min-width:0; border:2px solid #27272a; border-radius:4px 22px 22px; background:#fff; box-shadow:7px 7px 0 #52525b; overflow:hidden; }
.progress-entry-header { display:flex; align-items:center; justify-content:space-between; flex-wrap:wrap; gap:12px; padding:18px 24px; border-bottom:1px solid #e4e4e7; }
.progress-entry-header time { font-size:.85rem; color:#52525b; }
.progress-entry-link { color:#27272a; font-size:.85rem; font-weight:700; text-underline-offset:4px; }
.progress-entry-comparison { display:grid; grid-template-columns:minmax(0,1fr) minmax(0,1.2fr); }
.progress-entry-comparison section { min-width:0; padding:28px; }
.progress-entry-voice { background:#f4f4f5; border-right:1px solid #e4e4e7; }
.progress-entry-comparison h3 { margin:0 0 20px; padding-left:12px; border-left:4px solid #3f3f46; color:#27272a; font-size:1.1rem; font-weight:800; line-height:1.5; }
.progress-entry-voice blockquote { margin:0; color:#18181b; font-size:1.15rem; font-weight:700; line-height:1.8; white-space:pre-wrap; overflow-wrap:anywhere; }
.progress-entry-card dl { margin:0; }
.progress-entry-card dt { display:table; max-width:100%; margin-top:22px; padding:4px 9px; border:2px solid var(--color-brand-ink); background:var(--color-brand-accent); color:var(--color-brand-ink); font-size:.8rem; font-weight:800; }
.progress-entry-card dt:first-child { margin-top:0; }
.progress-entry-card dd { margin:8px 0 0; color:#27272a; font-size:1rem; line-height:1.85; white-space:pre-wrap; overflow-wrap:anywhere; }
.progress-entry-context { margin-top:28px; }
.progress-entry-context summary { color:#52525b; font-size:.85rem; cursor:pointer; text-underline-offset:4px; }
.progress-entry-context[open] summary { margin-bottom:18px; }
@media (max-width:760px) {
  .progress-overview-heading { flex-direction:column; align-items:flex-start; }
  .progress-entry-comparison { grid-template-columns:minmax(0,1fr); }
  .progress-entry-voice { border-right:0; border-bottom:1px solid #e4e4e7; }
  .progress-entry-header { padding:16px 18px; }
  .progress-entry-comparison section { padding:22px 18px; }
}
</style>
