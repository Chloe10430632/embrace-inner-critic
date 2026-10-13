<script setup lang="ts">
import type { JournalSort } from '~/types/journal'

const status = defineModel<string>('status', { required: true })
const sort = defineModel<JournalSort>('sort', { required: true })
const openPanel = ref<'status' | 'sort' | null>(null)
const controls = ref<HTMLElement | null>(null)
const statusButton = ref<HTMLButtonElement | null>(null)
const sortButton = ref<HTMLButtonElement | null>(null)
const statuses = ['', '已完成', '已記錄', '待完成行動', '草稿']
const sortGroups: { label: string; options: { value: JournalSort; label: string }[] }[] = [
  { label: '更新時間', options: [
    { value: 'updated-desc', label: '新 → 舊' },
    { value: 'updated-asc', label: '舊 → 新' }
  ] },
  { label: '建立時間', options: [
    { value: 'created-desc', label: '新 → 舊' },
    { value: 'created-asc', label: '舊 → 新' }
  ] }
]
const sortLabel = computed(() => {
  const group = sortGroups.find(group => group.options.some(option => option.value === sort.value))
  const option = group?.options.find(option => option.value === sort.value)
  return group && option ? `${group.label} · ${option.label}` : ''
})

function closeOutside(event: PointerEvent) {
  if (event.target instanceof Node && !controls.value?.contains(event.target)) openPanel.value = null
}
function closeWithEscape() {
  const button = openPanel.value === 'status' ? statusButton.value : sortButton.value
  openPanel.value = null
  button?.focus()
}
function chooseStatus(value: string) {
  status.value = value
  openPanel.value = null
  statusButton.value?.focus()
}
function chooseSort(value: JournalSort) {
  sort.value = value
  openPanel.value = null
  sortButton.value?.focus()
}
onMounted(() => document.addEventListener('pointerdown', closeOutside))
onBeforeUnmount(() => document.removeEventListener('pointerdown', closeOutside))
</script>

<template>
  <div ref="controls" class="list-controls" @keydown.esc.stop.prevent="closeWithEscape">
    <button v-if="status" type="button" class="filter-chip" :aria-label="`清除狀態篩選：${status}`" @click="status = ''">{{ status }} <span aria-hidden="true">×</span></button>
    <div class="control-wrap">
      <button ref="statusButton" type="button" class="control-icon" :class="{ active: status }" :aria-label="`狀態篩選：${status || '全部狀態'}`" :aria-expanded="openPanel === 'status'" aria-controls="journal-status-options" @click="openPanel = openPanel === 'status' ? null : 'status'">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" aria-hidden="true"><path d="M4 6h16M7 11h10M10 16h4" /></svg>
        <span class="control-tooltip" aria-hidden="true">狀態篩選</span>
      </button>
      <div v-if="openPanel === 'status'" id="journal-status-options" class="control-panel" role="group" aria-label="狀態篩選選項">
        <p>篩選狀態</p>
        <button v-for="option in statuses" :key="option" type="button" :aria-pressed="status === option" @click="chooseStatus(option)"><span>{{ option || '全部狀態' }}</span><span v-if="status === option" aria-hidden="true">✓</span></button>
      </div>
    </div>
    <div class="control-wrap">
      <button ref="sortButton" type="button" class="control-icon" :class="{ active: sort !== 'updated-desc' }" :aria-label="`排序：${sortLabel}`" :aria-expanded="openPanel === 'sort'" aria-controls="journal-sort-options" @click="openPanel = openPanel === 'sort' ? null : 'sort'">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M8 20V4M4 8l4-4 4 4M16 4v16M12 16l4 4 4-4" /></svg>
        <span class="control-tooltip" aria-hidden="true">排序 · {{ sortLabel }}</span>
      </button>
      <div v-if="openPanel === 'sort'" id="journal-sort-options" class="control-panel" role="group" aria-label="排序選項">
        <div v-for="group in sortGroups" :key="group.label" class="sort-group" role="group" :aria-label="group.label">
          <p>{{ group.label }}</p>
          <button v-for="option in group.options" :key="option.value" type="button" :aria-label="`${group.label}，${option.label}`" :aria-pressed="sort === option.value" @click="chooseSort(option.value)"><span>{{ option.label }}</span><span v-if="sort === option.value" aria-hidden="true">✓</span></button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.list-controls { display:flex; align-items:center; flex-wrap:wrap; justify-content:flex-end; gap:4px; }
.control-wrap { position:relative; }
.control-icon { position:relative; display:grid; place-items:center; width:34px; height:34px; padding:0; border:0; border-radius:6px; background:transparent; color:var(--color-brand-muted); cursor:pointer; transition:background .15s,color .15s; }
.control-icon:hover,.control-icon[aria-expanded=true] { background:var(--color-brand-shadow-faint); color:var(--color-brand-ink); }
.control-icon.active { color:#2383e2; }
button:focus-visible { outline:2px solid var(--color-brand-ink); outline-offset:3px; }
.control-tooltip { position:absolute; bottom:calc(100% + 8px); left:50%; transform:translateX(-50%); padding:5px 8px; border-radius:6px; background:#27272a; color:#fff; font-size:.75rem; font-weight:400; white-space:nowrap; pointer-events:none; opacity:0; transition:opacity .12s; }
.control-icon:hover .control-tooltip,.control-icon:focus-visible .control-tooltip { opacity:1; }
.control-icon[aria-expanded=true] .control-tooltip { opacity:0; }
.control-panel { position:absolute; top:calc(100% + 8px); right:0; z-index:30; width:196px; padding:6px; border:1px solid var(--color-brand-grid); border-radius:10px; background:var(--color-brand-surface); box-shadow:0 6px 24px var(--color-brand-shadow-soft); }
.control-panel p { margin:0; padding:7px 10px; color:var(--color-brand-muted); font-size:.75rem; }
.control-panel button { display:flex; align-items:center; justify-content:space-between; gap:12px; width:100%; min-height:36px; padding:7px 10px; border:0; border-radius:6px; background:transparent; color:var(--color-brand-ink); font:inherit; font-size:.85rem; text-align:left; cursor:pointer; }
.control-panel button:hover { background:var(--color-brand-shadow-faint); }
.control-panel button[aria-pressed=true] { background:var(--color-brand-shadow-faint); font-weight:600; }
.sort-group + .sort-group { margin-top:6px; }
.filter-chip { display:inline-flex; align-items:center; gap:8px; min-height:28px; margin-right:6px; padding:4px 8px; border:0; border-radius:6px; background:#e8f2fc; color:#1b65ad; font:inherit; font-size:.78rem; cursor:pointer; }
@media(max-width:600px) { .control-tooltip { left:auto; right:0; transform:none; } }
</style>
