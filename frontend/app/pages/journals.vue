<script setup lang="ts">
import type { JournalEntry, JournalSort } from '~/types/journal'

type JournalTab = 'diary' | 'progress'
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const store = useJournalStore()
const { stage, message, entries } = storeToRefs(store)
const { confirmedName } = storeToRefs(useOnboardingStore())
const viewOnly = ref(false)
const ready = ref(false)
const loading = ref(false)
const pendingDeleteId = ref<string | null>(null)
const deleting = ref(false)
const statusFilter = ref('')
const sortOrder = ref<JournalSort>('updated-desc')
const visibleEntries = computed(() => store.listEntries(statusFilter.value, sortOrder.value, activeTab.value === 'progress'))
const activeTab = computed<JournalTab>(() => route.query.tab === 'progress' || typeof route.query.progress === 'string' ? 'progress' : 'diary')
const isEditing = () => !(viewOnly.value && stage.value === 13) && ((stage.value >= 7 && stage.value <= 13) || (stage.value >= 17 && stage.value <= 21) || stage.value === 23)

async function load() {
  if (loading.value) return
  loading.value = true
  try { await store.loadEntries() }
  finally { loading.value = false }
}

async function applyRoute() {
  viewOnly.value = false
  if (route.query.view === 'safety') { stage.value = 15; return }
  if (!auth.user) { stage.value = 16; return }
  if (route.query.new === '1') { await store.beginNew(); return }
  const entry = entries.value.find(item => item.id === route.query.progress)
  if (entry) store.openProgress(entry)
  else stage.value = 6
}

onMounted(async () => {
  try {
    await auth.requireUser()
    if (auth.user) await load()
    await applyRoute()
  } finally { ready.value = true }
})

watch(() => [route.query.tab, route.query.progress, route.query.view, route.query.new], () => {
  if (ready.value) void applyRoute()
})

onBeforeRouteUpdate(async (to) => {
  if (isEditing() && store.hasContent() && (to.query.tab !== route.query.tab || to.query.progress !== route.query.progress || to.query.view !== route.query.view || to.query.new !== route.query.new)) {
    if (!await store.save(store.completed, stage.value)) return false
  }
})
onBeforeRouteLeave(async () => {
  if (isEditing() && store.hasContent()) return await store.save(store.completed, stage.value)
})

async function selectTab(tab: JournalTab, replace = false) {
  const target = { path: '/journals', query: tab === 'progress' ? { tab: 'progress' } : {} }
  if (replace) await router.replace(target)
  else await router.push(target)
}
async function finish(tab: JournalTab = activeTab.value) {
  await selectTab(tab, true)
}
async function beginNew() {
  viewOnly.value = false
  await selectTab('diary', true)
  await store.beginNew()
}
async function openProgress(entry: JournalEntry) {
  await router.push({ path: '/journals', query: { tab: 'progress', progress: entry.id } })
}
function viewEntry(entry: JournalEntry) {
  viewOnly.value = true
  store.open(entry)
}
function editEntry(entry: JournalEntry) {
  viewOnly.value = false
  store.open(entry)
  stage.value = 7
}
async function confirmDelete() {
  if (!pendingDeleteId.value || deleting.value) return
  deleting.value = true
  try {
    if (await store.remove(pendingDeleteId.value)) pendingDeleteId.value = null
  } finally { deleting.value = false }
}
</script>

<template>
  <div>
    <Transition name="page" mode="out-in">
      <section v-if="!ready" key="loading" class="scene compact-scene" role="status"><span class="journal-spinner" aria-hidden="true" /><p class="page-loading-caption">載入時光紀錄</p></section>
      <LoginPrompt v-else-if="stage === 16" key="login" eyebrow="覺察日記" description="完成登入，建立自己的日記，並回顧重新框架與小行動的進展。" :message="message" />
      <section v-else-if="stage === 6" key="workspace" class="scene journal-workspace" :inert="pendingDeleteId !== null">
        <header class="workspace-heading"><p class="eyebrow">覺察日記</p><h2>記下當時的聲音，<br>也看見自己走過的路。</h2><p class="lead">紀錄 {{ confirmedName }} 的慣性思考方式，再慢慢練習回應。每一篇都可以之後再回來修改。</p></header>
        <div class="workspace-navigation">
        <div class="workspace-tabs" role="group" aria-label="日記瀏覽模式">
          <button type="button" :aria-pressed="activeTab === 'diary'" aria-controls="diary-panel" @click="selectTab('diary')"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><rect x="4" y="3" width="16" height="18" rx="2" /><path d="M8 8h8M8 12h8M8 16h5" /></svg>我的日記</button>
          <button type="button" :aria-pressed="activeTab === 'progress'" aria-controls="progress-panel" @click="selectTab('progress')"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 4v16h16M8 15l4-5 4 2 4-7" /></svg>追蹤進展</button>
        </div>
        <div class="workspace-list-tools"><span class="record-count" role="status">{{ visibleEntries.length }} {{ activeTab === 'diary' ? '篇日記' : '筆進展紀錄' }}</span><JournalListControls v-model:status="statusFilter" v-model:sort="sortOrder" /></div>
        </div>
        <p v-if="message" class="workspace-message" role="alert">{{ message }} <button type="button" :disabled="loading" @click="load">{{ loading ? '載入中…' : '重新載入' }}</button></p>
        <div v-if="loading" class="workspace-loading" role="status"><span class="journal-spinner" aria-hidden="true" /><p>載入時光紀錄</p></div>
        <Transition v-else name="page" mode="out-in">
          <JournalEntryList v-if="activeTab === 'diary'" id="diary-panel" key="diary" :entries="visibleEntries" :filtered="Boolean(statusFilter)" :disabled="deleting" @open="viewEntry" @edit="editEntry" @progress="openProgress" @remove="pendingDeleteId = $event" />
          <JournalProgressList v-else id="progress-panel" key="progress" :entries="visibleEntries" :filtered="Boolean(statusFilter)" @open="openProgress" @diary="selectTab('diary')" />
        </Transition>
        <button v-if="activeTab === 'diary'" type="button" class="journal-create" @click="beginNew"><span aria-hidden="true">＋</span>新增覺察日記</button>
      </section>
      <JournalPractice v-else key="practice" :view-only="viewOnly" @finish="finish" />
    </Transition>
    <Teleport to="body">
      <div v-if="pendingDeleteId" class="journal-modal-backdrop" role="presentation"><section class="journal-modal" role="alertdialog" aria-modal="true" aria-labelledby="delete-dialog-title" aria-describedby="delete-dialog-description" :aria-busy="deleting"><p class="section-kicker">刪除日記</p><h2 id="delete-dialog-title">確定要刪除這篇日記嗎？</h2><p id="delete-dialog-description">刪除後無法復原，這篇日記的內容將不會保留。</p><p v-if="message" role="alert">{{ message }}</p><div class="journal-modal-actions"><button type="button" class="secondary" autofocus :disabled="deleting" @click="pendingDeleteId = null">先不要</button><button type="button" class="modal-delete-button" :disabled="deleting" @click="confirmDelete">{{ deleting ? '正在刪除…' : '確定刪除' }}</button></div></section></div>
    </Teleport>
  </div>
</template>

<style scoped>
.journal-workspace { width:min(1000px,calc(100% - 36px)); align-items:stretch; text-align:left; padding-bottom:calc(164px + env(safe-area-inset-bottom,0px)); }
.workspace-heading .eyebrow { display:inline-block; width:fit-content; transform:none; }
.workspace-heading h2 { font-size:clamp(1.6rem,4vw,2.6rem); }
.workspace-heading .lead { max-width:650px; margin-left:0; margin-right:0; }
.workspace-tabs { display:flex; flex-wrap:wrap; gap:4px; margin-top:0; }
.workspace-tabs button { display:inline-flex; align-items:center; gap:8px; min-height:40px; padding:8px 14px; border:0; border-radius:22px; background:transparent; color:var(--color-brand-muted); font:inherit; font-size:.95rem; cursor:pointer; transition:background .15s,color .15s; }
.workspace-tabs button:hover { background:var(--color-brand-shadow-faint); color:var(--color-brand-ink); }
.workspace-tabs button[aria-pressed=true] { background:var(--color-brand-shadow-soft); color:var(--color-brand-ink); }
button:focus-visible { outline:2px solid var(--color-brand-ink); outline-offset:3px; }
.workspace-navigation { display:flex; align-items:center; justify-content:space-between; flex-wrap:wrap; gap:12px; margin-top:24px; margin-bottom:20px; }
.workspace-list-tools { display:flex; align-items:center; justify-content:flex-end; gap:14px; margin-left:auto; }
.record-count { color:var(--color-brand-muted); font-size:.8rem; white-space:nowrap; }
.workspace-message { padding:14px; border:1px solid var(--color-brand-grid); border-radius:12px; }
.workspace-message button { border:0; background:transparent; font:inherit; text-decoration:underline; cursor:pointer; }
.workspace-loading { display:grid; justify-items:center; padding:40px 0; }
.journal-create { position:fixed; right:max(40px,calc((100vw - 1000px) / 2)); bottom:calc(72px + env(safe-area-inset-bottom,0px)); z-index:20; display:inline-flex; align-items:center; gap:8px; min-height:52px; padding:0 20px; border:0; border-radius:16px; background:var(--color-brand-ink); color:var(--color-brand-surface); box-shadow:0 4px 16px var(--color-brand-shadow-soft); font:inherit; font-size:.9rem; font-weight:600; cursor:pointer; }
.journal-create:hover { background:color-mix(in srgb,var(--color-brand-ink) 85%,var(--color-brand-surface)); }
@media(max-width:600px) { .journal-create { right:calc(24px + env(safe-area-inset-right,0px)); bottom:calc(88px + env(safe-area-inset-bottom,0px)); } }
</style>
