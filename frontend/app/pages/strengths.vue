<script setup lang="ts">
import { strengthTraitGroups } from '~/data/strengthTraits'
type StrengthEntry = { id: string; event: string; notes: string; tags: string[]; createdAt: string; updatedAt: string }
const auth = useAuthStore()
const { $api } = useNuxtApp()
const ready = ref(false)
const loading = ref(false)
const entries = ref<StrengthEntry[]>([])
const message = ref('')
const notice = ref('')
const view = ref<'events' | 'traits'>('events')
const filter = ref('')
const editing = ref(false)
const editingId = ref<string | null>(null)
const editorDialog = ref<HTMLDialogElement | null>(null)
const hasDraft = ref(false)
let previousBodyOverflow = ''
const event = ref('')
const notes = ref('')
const tags = ref<string[]>([])
const busy = ref(false)
const busyMessage = ref('')
const deletingId = ref<string | null>(null)
const traitGroup = ref<string>(strengthTraitGroups[0].id)
const traitSearch = ref('')
const traitColors: Record<string, string> = {"learning":"#526da2","thinking":"#7966a5","expression":"#a65d85","action":"#a66e40","relationships":"#408075","values":"#927c40","emotions":"#508198","care":"#52816a","practical":"#997365"}
function traitStyle(tag: string) {
  const group = strengthTraitGroups.find(group => (group.tags as readonly string[]).includes(tag))
  return { '--trait-color': traitColors[group?.id ?? ''] ?? '#6b7280' }
}
const suggestedTags = new Set<string>(strengthTraitGroups.flatMap(group => [...group.tags]))
const customTags = computed(() => [...new Set([...entries.value.flatMap(entry => entry.tags), ...tags.value])].filter(tag => !suggestedTags.has(tag)))
const visibleTraitGroups = computed(() => {
  const query = traitSearch.value.trim().toLocaleLowerCase()
  const groups = [...strengthTraitGroups.map(group => ({ id: group.id as string, name: group.name as string, tags: [...group.tags] as string[] })),
    { id: 'custom', name: '自訂與既有標籤', tags: customTags.value }]
  return groups.filter(group => query || group.id === traitGroup.value)
    .map(group => ({ ...group, tags: group.tags.filter(tag => !query || tag.toLocaleLowerCase().includes(query)) }))
    .filter(group => group.tags.length)
})
const counts = computed(() => {
  const result = new Map<string, number>()
  for (const entry of entries.value) for (const tag of entry.tags) result.set(tag, (result.get(tag) ?? 0) + 1)
  return [...result].map(([tag, count]) => ({ tag, count })).sort((a, b) => b.count - a.count || a.tag.localeCompare(b.tag, 'zh-TW'))
})
const visibleEntries = computed(() => filter.value ? entries.value.filter(entry => entry.tags.includes(filter.value)) : entries.value)
const signedIn = computed(() => Boolean(auth.user))
function errorText(error: unknown, fallback: string) {
  const failure = error as { data?: { error?: string }; response?: { status: number } }
  return failure.response?.status === 401 ? '登入已失效，請重新登入。' : failure.data?.error ?? fallback
}
async function load() {
  if (loading.value) return
  loading.value = true
  message.value = ''
  try {
    const result: StrengthEntry[] = []
    for (let page = 1; page <= 10; page++) {
      const batch = await $api<StrengthEntry[]>('/api/strength-entries', { query: { page, pageSize: 50 }, timeout: 15000 })
      result.push(...batch)
      if (batch.length < 50) break
    }
    entries.value = result
    if (filter.value && !result.some(entry => entry.tags.includes(filter.value))) filter.value = ''
  } catch (error) { message.value = errorText(error, '紀錄目前無法載入，請稍後重試。') }
  finally { loading.value = false }
}
function open(entry?: StrengthEntry) {
  if (busy.value) return
  if (hasDraft.value && editingId.value === (entry?.id ?? null)) { editing.value = true; message.value = ''; return }
  hasDraft.value = true
  editingId.value = entry?.id ?? null
  event.value = entry?.event ?? ''
  notes.value = entry?.notes ?? ''
  tags.value = [...(entry?.tags ?? [])]
  traitSearch.value = ''
  traitGroup.value = strengthTraitGroups[0].id
  message.value = ''
  notice.value = ''
  editing.value = true
  deletingId.value = null
}
function closeEditor() {
  if (busy.value) return
  editing.value = false
  message.value = ''
}
watch(editing, async (value) => {
  await nextTick()
  if (value && editing.value && editorDialog.value && !editorDialog.value.open) {
    previousBodyOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    editorDialog.value.showModal()
  } else if (!value && editorDialog.value?.open) {
    editorDialog.value.close()
    document.body.style.overflow = previousBodyOverflow
  }
})
onBeforeUnmount(() => {
  if (editorDialog.value?.open) document.body.style.overflow = previousBodyOverflow
})
function toggleTag(tag: string) {
  if (tags.value.includes(tag)) tags.value = tags.value.filter(value => value !== tag)
  else if (tags.value.length < 10) tags.value.push(tag)
  else message.value = '每筆紀錄最多選擇 10 個標籤。'
}
async function save() {
  if (busy.value || loading.value) return
  busyMessage.value = '正在儲存這筆紀錄……'
  busy.value = true
  message.value = ''
  try {
    const saved = await $api<StrengthEntry>(editingId.value ? `/api/strength-entries/${editingId.value}` : '/api/strength-entries', {
      method: editingId.value ? 'PUT' : 'POST', body: { event: event.value, notes: notes.value, tags: tags.value }, timeout: 15000
    })
    const index = entries.value.findIndex(entry => entry.id === saved.id)
    if (index >= 0) entries.value[index] = saved
    else entries.value.unshift(saved)
    filter.value = ''
    editing.value = false
    hasDraft.value = false
    notice.value = '已留下這筆值得記住的經驗。'
  } catch (error) { message.value = errorText(error, '儲存失敗，你輸入的內容仍保留在這裡。') }
  finally { busy.value = false }
}
async function remove(id: string) {
  if (busy.value) return
  busyMessage.value = '正在刪除這筆紀錄……'
  busy.value = true
  message.value = ''
  try {
    await $api(`/api/strength-entries/${id}`, { method: 'DELETE', timeout: 15000 })
    entries.value = entries.value.filter(entry => entry.id !== id)
    deletingId.value = null
    if (filter.value && !entries.value.some(entry => entry.tags.includes(filter.value))) filter.value = ''
    notice.value = '紀錄已刪除。'
  } catch (error) { message.value = errorText(error, '刪除失敗，請稍後再試。') }
  finally { busy.value = false }
}
function date(value: string) { return new Intl.DateTimeFormat('zh-TW', { dateStyle: 'medium' }).format(new Date(value)) }
onMounted(async () => {
  if (await auth.requireUser()) await load()
  ready.value = true
})
watch(signedIn, (value) => { if (!value) {
    if (editorDialog.value?.open) document.body.style.overflow = previousBodyOverflow
    entries.value = []; editing.value = false; hasDraft.value = false
  } })
</script>

<template>
  <Transition name="page" mode="out-in">
  <section v-if="!ready" key="loading" class="scene compact-scene" role="status"><span class="journal-spinner" aria-hidden="true" /><p class="page-loading-caption">載入力量紀錄</p></section>
  <LoginPrompt v-else-if="!signedIn" key="login" eyebrow="看見我的力量" description="完成登入，儲存/回顧自己成功的經驗，很小的事也是成功的證據。" />

  <section v-else key="strengths-home" class="scene strengths-scene">
    <header class="strengths-heading"><div><p class="eyebrow">看見我的力量</p><h2>這些成功經驗，也是我的一部分。</h2><p class="lead">練習每天早上寫下三件你做得好的事情，也許你擅長寫作，或是你總是守時、幽默風趣等。累積起來能拿來反駁內在批評者的材料會越來越多。</p></div></header>
    <p v-if="message && !editing" role="alert" class="strengths-message">{{ message }}</p>
    <p v-if="notice" role="status">{{ notice }}</p>
    <button v-if="message && !editing" class="text-button" :disabled="loading || busy" @click="load">{{ loading ? '載入中…' : '重新載入' }}</button>
    <dialog ref="editorDialog" class="strengths-dialog" aria-labelledby="strength-editor-title" @cancel.prevent="closeEditor">
    <form class="strengths-editor" @submit.prevent="save">
      <div class="editor-heading"><button type="button" class="editor-close" aria-label="關閉表單" :disabled="busy" autofocus @click="closeEditor">×</button><h3 id="strength-editor-title">{{ editingId ? '修改這筆經驗' : '記下一件做得好的事' }}</h3><p>留下具體經驗，再選擇這次展現的特質或能力。</p></div>
      <p v-if="message" role="alert" class="strengths-message">{{ message }}</p>
      <fieldset :disabled="busy || loading">
        <label for="strength-event">我做了什麼，讓我想肯定自己？ <small class="required-mark">必填 *</small></label>
        <textarea id="strength-event" v-model="event" required maxlength="2000" rows="4" placeholder="例如：雖然很緊張，我還是開口表達了自己的需要。" />
        <p class="field-hint event-count">{{ event.length }} / 2000 字</p>
        <section class="trait-picker" aria-labelledby="trait-picker-title">
          <div class="trait-picker-heading"><h4 id="trait-picker-title">我看見的特質與能力 <small>選填</small></h4><span class="field-hint" role="status">已選 {{ tags.length }} / 10</span></div>
          <div v-if="tags.length" class="selected-traits" aria-label="已選標籤">
            <button v-for="tag in tags" :key="tag" type="button" :style="traitStyle(tag)" :aria-label="`移除標籤：${tag}`" @click="toggleTag(tag)">{{ tag }} <span aria-hidden="true">×</span></button>
          </div>
          <div class="trait-picker-tools">
            <div><label for="trait-category">瀏覽分類</label><select id="trait-category" v-model="traitGroup"><option v-for="group in strengthTraitGroups" :key="group.id" :value="group.id">{{ group.name }}</option><option value="custom">自訂與既有標籤</option></select></div>
            <div><label for="trait-search">搜尋所有分類</label><input id="trait-search" v-model="traitSearch" type="search" placeholder="搜尋特質或能力" @keydown.enter.prevent></div>
          </div>
          <div v-for="group in visibleTraitGroups" :key="group.id" class="trait-group">
            <h5 v-if="traitSearch.trim()">{{ group.name }}</h5>
            <div class="strengths-tags"><button v-for="tag in group.tags" :key="tag" type="button" class="trait-chip" :style="traitStyle(tag)" :aria-pressed="tags.includes(tag)" :disabled="tags.length >= 10 && !tags.includes(tag)" @click="toggleTag(tag)"><span v-if="tags.includes(tag)" aria-hidden="true">✓</span>{{ tag }}</button></div>
          </div>
          <p v-if="!visibleTraitGroups.length" class="picker-hint">{{ traitSearch.trim() ? '沒有符合的標籤，試試其他關鍵字。' : '還沒有既有標籤。' }}</p>
        </section>
        <div class="strengths-actions"><button class="primary" type="submit">{{ busy ? '儲存中…' : '儲存紀錄' }}</button></div>
      </fieldset>
    </form>
    <div v-if="busy && !deletingId" class="journal-busy-overlay" role="status" aria-live="assertive" aria-busy="true">
      <div class="journal-busy-card"><span class="journal-spinner" aria-hidden="true" /><strong>{{ busyMessage }}</strong><p>請稍候，不要關閉或重新操作按鈕。</p></div>
    </div>
    </dialog>
    <div class="strengths-browser">
      <div class="strengths-tabs" role="group" aria-label="瀏覽模式">
        <button type="button" class="view-switch" :aria-pressed="view === 'events'" @click="view = 'events'">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><rect x="3" y="4" width="18" height="16" rx="2" /><path d="M3 9h18M3 14h18M9 4v16" /></svg>
          事件紀錄
        </button>
        <button type="button" class="view-switch" :aria-pressed="view === 'traits'" @click="view = 'traits'">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="9" /><path d="M12 3v9l6.4 6.4M12 12h9" /></svg>
          特質回顧
        </button>
      </div>
      <div v-if="view === 'traits'" class="traits-overview"><h3>我看見的特質</h3><p class="traits-description">數字是相關紀錄筆數，並非特質強弱或能力評分。點選特質，回看具體經驗。</p><button v-for="item in counts" :key="item.tag" class="trait-row" :style="traitStyle(item.tag)" @click="filter = item.tag; view = 'events'"><span class="trait-name"><i aria-hidden="true" />{{ item.tag }}</span><span class="trait-bar" aria-hidden="true"><span :style="{ width: `${item.count / (counts[0]?.count ?? 1) * 100}%` }" /></span><span class="trait-count">{{ item.count }} 筆</span></button><p v-if="!counts.length">還沒有特質標籤。先留下事件，之後再補也可以。</p></div>
      <template v-else>
        <div class="strengths-toolbar">
          <div v-if="counts.length" class="strengths-filter">
            <label for="trait-filter">特質</label>
            <div class="trait-select">
              <select id="trait-filter" v-model="filter"><option value="">所有特質</option><option v-for="item in counts" :key="item.tag" :value="item.tag">{{ item.tag }}（{{ item.count }} 筆）</option></select>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="m6 9 6 6 6-6" /></svg>
            </div>
            <button v-if="filter" type="button" class="clear-filter" @click="filter = ''">清除篩選</button>
          </div>
          <span class="record-count">{{ visibleEntries.length }} 筆紀錄</span>
        </div>
        <p v-if="!visibleEntries.length && !message && !loading" class="strengths-empty">還沒有紀錄。可以從一件小事開始：照顧自己、完成一次嘗試，或表達自己的需要。</p>
        <div class="strengths-list">
          <article v-for="entry in visibleEntries" :key="entry.id" class="strength-card">
            <header class="strength-card-header">
              <time :datetime="entry.createdAt">{{ date(entry.createdAt) }}</time>
              <div class="record-actions">
                <button type="button" :disabled="busy || loading" @click="open(entry)">
                  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m16 3 5 5-12 12-6 1 1-6L16 3ZM13 6l5 5" /></svg>修改
                </button>
                <button type="button" :disabled="busy || loading" @click="message = ''; deletingId = entry.id">
                  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 6h18M9 6V3h6v3M5 6l1 15h12l1-15M10 10v7M14 10v7" /></svg>刪除
                </button>
              </div>
            </header>
            <h3>{{ entry.event }}</h3>
            <p v-if="entry.notes" class="strength-notes">{{ entry.notes }}</p>
            <div class="strengths-tags"><button v-for="tag in entry.tags" :key="tag" class="trait-chip" :style="traitStyle(tag)" :aria-pressed="filter === tag" @click="filter = tag">{{ tag }}</button><span v-if="!entry.tags.length" class="field-hint">特質還可以慢慢想</span></div>
          </article>
        </div>
      </template>
    </div>
    <button v-if="!editing" type="button" class="strengths-create" :disabled="busy || loading" @click="open()"><svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"><path d="M12 5v14M5 12h14" /></svg><span>記一件事</span></button>
  </section>
  </Transition>
  <Teleport to="body">
    <div v-if="deletingId && !busy" class="journal-modal-backdrop" role="presentation">
      <section class="journal-modal" role="alertdialog" aria-modal="true" aria-labelledby="strength-delete-title" aria-describedby="strength-delete-description">
        <p class="section-kicker">刪除紀錄</p>
        <h2 id="strength-delete-title">確定要刪除這筆紀錄嗎？</h2>
        <p id="strength-delete-description">刪除後無法復原，這筆紀錄的內容將不會保留。</p>
        <p v-if="message" role="alert">{{ message }}</p>
        <div class="journal-modal-actions">
          <button type="button" class="secondary" autofocus @click="deletingId = null">先不要</button>
          <button type="button" class="modal-delete-button" @click="remove(deletingId)">確定刪除</button>
        </div>
      </section>
    </div>
    <div v-if="deletingId && busy" class="journal-busy-overlay" role="status" aria-live="assertive" aria-busy="true">
      <div class="journal-busy-card"><span class="journal-spinner" aria-hidden="true" /><strong>{{ busyMessage }}</strong><p>請稍候，不要關閉或重新操作按鈕。</p></div>
    </div>
  </Teleport>
</template>

<style scoped>
.strengths-scene { width:min(1000px,calc(100% - 36px)); padding-bottom:calc(164px + env(safe-area-inset-bottom, 0px)); align-items:stretch; text-align:left; }
.strengths-scene .primary { margin-top:0; }
.strengths-scene .lead { margin-left:0; }
.strengths-list { display:grid; gap:12px; }
.strengths-heading { display:flex; align-items:center; justify-content:space-between; gap:24px; flex-wrap:wrap; }
.strengths-heading .eyebrow { display:inline-block; width:fit-content; transform:none; }
.strengths-heading h2 { font-size:clamp(1.6rem,4vw,2.6rem); }
.strengths-heading .lead { max-width:650px; }
.traits-overview { margin-top:20px; padding:24px; border:1px solid var(--color-brand-grid); border-radius:16px; background:var(--color-brand-surface); box-shadow:0 2px 8px var(--color-brand-shadow-faint); }
.traits-overview h3 { margin:0; font-size:1.05rem; font-weight:600; }
.traits-description { margin:8px 0 20px; color:var(--color-brand-muted); font-size:.8rem; line-height:1.7; }
.strengths-editor { margin:0; padding:28px; border:0; background:var(--color-brand-surface); }
.editor-heading { position:relative; margin-bottom:24px; padding-right:44px; }
.editor-heading h3 { margin:0; font-size:1.2rem; font-weight:650; }
.editor-heading p { margin:8px 0 0; color:var(--color-brand-muted); font-size:.85rem; line-height:1.7; }
.strength-card { padding:20px 24px; border:1px solid var(--color-brand-grid); border-radius:16px; background:var(--color-brand-surface); box-shadow:0 2px 8px var(--color-brand-shadow-faint); }
.strength-card-header { display:flex; align-items:center; justify-content:space-between; gap:12px; flex-wrap:wrap; }
.strength-card time { color:var(--color-brand-muted); font-size:.8rem; opacity:1; }
.record-actions { display:flex; gap:4px; }
.record-actions button,.clear-filter { display:inline-flex; align-items:center; gap:6px; min-height:36px; padding:6px 9px; border:0; border-radius:8px; background:transparent; color:var(--color-brand-muted); font:inherit; font-size:.8rem; cursor:pointer; }
.record-actions button:hover,.clear-filter:hover { color:var(--color-brand-ink); background:var(--color-brand-shadow-soft); }
.record-actions button:focus-visible,.clear-filter:focus-visible { outline:2px solid var(--color-brand-ink); outline-offset:3px; }
.strengths-editor fieldset { border:0; padding:0; margin:0; min-width:0; }
.strengths-editor label { display:block; margin:20px 0 8px; font-size:.9rem; font-weight:600; }
.strengths-editor fieldset > label:first-child { margin-top:0; }
.strengths-editor .required-mark { color:#b42318; opacity:1; }
.strengths-editor small,.field-hint { font-size:.85rem; opacity:.65; }
.strengths-editor textarea,.strengths-editor input,.strengths-editor select { box-sizing:border-box; width:100%; border:1px solid var(--color-brand-grid); border-radius:10px; padding:11px 12px; background:var(--color-brand-surface); color:var(--color-brand-ink); font:inherit; font-size:.9rem; box-shadow:none; }
.strengths-editor textarea { resize:vertical; margin-top:0; min-height:120px; line-height:1.7; }
.strengths-editor :is(textarea,input,select):focus { outline:2px solid var(--color-brand-muted); outline-offset:2px; border-color:var(--color-brand-muted); box-shadow:none; }
.event-count { margin:7px 0 0; text-align:right; }
.trait-picker { margin-top:24px; padding:22px 0; border-block:1px solid var(--color-brand-grid); }
.trait-picker-heading { display:flex; align-items:center; justify-content:space-between; gap:12px; flex-wrap:wrap; }
.trait-picker-heading h4 { margin:0; font-size:.9rem; font-weight:600; }
.picker-hint { margin:8px 0 14px; font-size:.82rem; color:var(--color-brand-muted); line-height:1.7; }
.trait-picker-tools { display:grid; grid-template-columns:minmax(0,1fr) minmax(0,1.4fr); gap:12px; margin-bottom:12px; }
.trait-picker-tools label { margin:0 0 6px; font-size:.78rem; color:var(--color-brand-muted); }
.trait-group h5 { margin:14px 0 8px; font-size:.8rem; color:var(--color-brand-muted); }
.trait-picker .strengths-tags { gap:8px; }
.strengths-editor .trait-chip { display:inline-flex; align-items:center; gap:6px; min-height:36px; padding:6px 11px; border:1px solid transparent; border-radius:8px; background:color-mix(in srgb,var(--trait-color) 12%,var(--color-brand-surface)); color:var(--trait-color); font-size:.85rem; }
.strengths-editor .trait-chip:hover { border-color:var(--trait-color); }
.strengths-editor .trait-chip[aria-pressed=true] { background:var(--trait-color); color:var(--color-brand-surface); }
.strengths-editor .trait-chip:focus-visible { outline:2px solid var(--color-brand-ink); outline-offset:2px; }
.trait-picker .trait-chip:disabled { cursor:not-allowed; }
.selected-traits { display:flex; flex-wrap:wrap; gap:8px; margin:14px 0; }
.selected-traits button { display:inline-flex; align-items:center; gap:10px; min-height:34px; padding:5px 10px; border:1px solid var(--color-brand-grid); border-radius:8px; background:color-mix(in srgb,var(--trait-color) 12%,var(--color-brand-surface)); color:var(--trait-color); font:inherit; font-size:.82rem; cursor:pointer; }
.selected-traits button:focus-visible { outline:2px solid var(--color-brand-ink); outline-offset:2px; }
.strengths-editor .strengths-actions { justify-content:flex-end; gap:12px; }
.strengths-editor :is(.primary,.secondary) { min-height:42px; padding:0 18px; border:1px solid var(--color-brand-grid); border-radius:10px; box-shadow:none; font-size:.9rem; transform:none; }
.strengths-editor :is(.primary,.secondary):hover { box-shadow:none; transform:none; opacity:.85; }
.strengths-editor .text-button { margin:0; padding:10px 12px; border:0; border-radius:10px; text-decoration:none; color:var(--color-brand-muted); font-size:.85rem; }
.strengths-editor .text-button:hover { background:var(--color-brand-shadow-soft); color:var(--color-brand-ink); }
.strengths-tags,.strengths-actions,.strengths-tabs { display:flex; flex-wrap:wrap; gap:12px; align-items:center; }
.strengths-actions,.strengths-tabs { margin-top:24px; }
.trait-chip { border:1px solid var(--color-brand-ink); border-radius:24px; padding:6px 14px; background:var(--color-brand-surface); color:var(--color-brand-ink); cursor:pointer; font:inherit; }
.trait-chip[aria-pressed=true] { background:var(--color-brand-accent); }
.strengths-tabs { gap:4px; }
.view-switch { display:inline-flex; align-items:center; gap:8px; min-height:40px; padding:8px 14px; border:0; border-radius:22px; background:transparent; color:var(--color-brand-muted); font:inherit; font-size:.95rem; cursor:pointer; transition:background .15s,color .15s; }
.view-switch:hover { background:var(--color-brand-shadow-faint); color:var(--color-brand-ink); }
.view-switch[aria-pressed=true] { background:var(--color-brand-shadow-soft); color:var(--color-brand-ink); }
.view-switch:focus-visible { outline:2px solid var(--color-brand-ink); outline-offset:3px; }
.view-switch svg { flex-shrink:0; }
.strengths-toolbar { display:flex; justify-content:space-between; align-items:center; flex-wrap:wrap; gap:12px; margin:20px 0 16px; }
.strengths-filter { display:flex; align-items:center; flex-wrap:wrap; gap:10px; min-width:0; }
.strengths-filter label,.record-count { color:var(--color-brand-muted); font-size:.8rem; }
.trait-select { position:relative; max-width:100%; }
.trait-select select { appearance:none; box-sizing:border-box; width:200px; max-width:100%; min-height:40px; padding:8px 36px 8px 12px; border:1px solid var(--color-brand-grid); border-radius:10px; background:var(--color-brand-surface); color:var(--color-brand-ink); font:inherit; font-size:.85rem; cursor:pointer; }
.trait-select select:focus-visible { outline:2px solid var(--color-brand-ink); outline-offset:2px; }
.trait-select svg { position:absolute; right:12px; top:50%; transform:translateY(-50%); pointer-events:none; color:var(--color-brand-muted); }
.strength-card .strengths-tags { gap:6px; margin-top:14px; }
.strength-card .trait-chip { padding:5px 10px; border:0; border-radius:6px; background:color-mix(in srgb,var(--trait-color) 12%,var(--color-brand-surface)); color:var(--trait-color); font-size:.8rem; }
.strength-card .trait-chip:hover,.strength-card .trait-chip[aria-pressed=true] { background:color-mix(in srgb,var(--trait-color) 22%,var(--color-brand-surface)); color:var(--trait-color); }
.strength-card .trait-chip:focus-visible { outline:2px solid var(--color-brand-ink); outline-offset:2px; }
.strength-card h3,.strength-notes { white-space:pre-wrap; overflow-wrap:anywhere; line-height:1.8; }
.strength-card h3 { margin:10px 0 0; font-size:1.05rem; font-weight:600; line-height:1.65; }
.strength-notes { margin:8px 0 0; color:var(--color-brand-muted); font-size:.9rem; }
.strengths-empty { padding:36px 0; line-height:1.9; }
.trait-row { display:grid; grid-template-columns:140px minmax(0,1fr) 44px; gap:16px; align-items:center; width:100%; min-height:44px; padding:10px 8px; border:0; border-radius:8px; background:transparent; color:var(--color-brand-ink); text-align:left; font:inherit; font-size:.85rem; cursor:pointer; }
.trait-row:hover { background:var(--color-brand-shadow-faint); }
.trait-row:focus-visible { outline:2px solid var(--color-brand-ink); outline-offset:2px; }
.trait-name { display:flex; align-items:center; gap:8px; overflow-wrap:anywhere; }
.trait-name i { width:7px; height:7px; flex-shrink:0; border-radius:50%; background:color-mix(in srgb,var(--trait-color) 72%,var(--color-brand-surface)); }
.trait-count { color:var(--color-brand-muted); text-align:right; font-size:.8rem; font-variant-numeric:tabular-nums; }
.trait-bar { height:8px; background:var(--color-brand-shadow-faint); border-radius:8px; overflow:hidden; }
.trait-bar span { display:block; height:100%; border-radius:inherit; background:color-mix(in srgb,var(--trait-color) 72%,var(--color-brand-surface)); }
.strengths-message { padding:14px; border:1px solid var(--color-brand-ink); }
.strengths-dialog { box-sizing:border-box; width:min(720px,calc(100% - 32px)); max-height:calc(100dvh - 64px); margin:auto; padding:0; border:1px solid var(--color-brand-grid); border-radius:20px; background:var(--color-brand-surface); color:var(--color-brand-ink); text-align:left; overflow-y:auto; overscroll-behavior:contain; box-shadow:0 24px 80px color-mix(in srgb,var(--color-brand-ink) 20%,transparent); }
.strengths-dialog::backdrop { background:color-mix(in srgb,var(--color-brand-ink) 38%,transparent); backdrop-filter:blur(3px); }
.editor-close { position:absolute; top:0; right:0; display:grid; place-items:center; width:36px; height:36px; border:0; border-radius:10px; background:var(--color-brand-shadow-soft); color:var(--color-brand-ink); font:inherit; font-size:1.5rem; cursor:pointer; }
.editor-close:focus-visible { outline:2px solid var(--color-brand-ink); outline-offset:3px; }
.strengths-create { position:fixed; right:max(40px, calc((100vw - 1000px) / 2)); bottom:calc(72px + env(safe-area-inset-bottom, 0px)); z-index:20; display:inline-flex; align-items:center; justify-content:center; gap:8px; min-height:52px; padding:0 20px; border:0; border-radius:16px; background:var(--color-brand-ink); color:var(--color-brand-surface); box-shadow:0 4px 16px color-mix(in srgb,var(--color-brand-ink) 18%,transparent); font:inherit; font-size:.9rem; font-weight:600; cursor:pointer; }
.strengths-create:hover { background:color-mix(in srgb,var(--color-brand-ink) 85%,var(--color-brand-surface)); }
.strengths-create:focus-visible { outline:2px solid var(--color-brand-ink); outline-offset:4px; }
.strengths-create svg { flex-shrink:0; }
button:disabled { cursor:wait; opacity:.6; }
@media(max-width:600px) { .strengths-create { right:calc(24px + env(safe-area-inset-right, 0px)); bottom:calc(88px + env(safe-area-inset-bottom, 0px)); } .strengths-dialog { width:calc(100% - 24px); max-height:calc(100dvh - 48px); } .strengths-editor,.strength-card,.traits-overview { padding:18px; } .trait-row { grid-template-columns:96px minmax(0,1fr) 38px; gap:10px; padding-inline:0; font-size:.8rem; } }
@media(max-width:480px) { .trait-picker-tools { grid-template-columns:1fr; } .strengths-editor .strengths-actions { flex-wrap:wrap; } }
</style>
