<script setup lang="ts">
import type { JournalEntry } from '~/types/journal'
defineProps<{ entries: JournalEntry[]; filtered: boolean }>()
const emit = defineEmits<{ open: [entry: JournalEntry]; diary: [] }>()
const journalStore = useJournalStore()
const { formatDate, isProgressComplete, entryStatus } = journalStore
function answer(entry: JournalEntry, key: keyof typeof entry.answers) {
  const value = entry.answers[key]
  if (Array.isArray(value)) return value.length ? value.map(item => `#${item}`).join('　') : '尚未記錄'
  return typeof value === 'string' && value.trim() ? value : '尚未記錄'
}

</script>

<template>
  <div>
    <div v-if="entries.length" class="progress-entry-list">
      <article v-for="entry in entries" :key="entry.id" class="progress-entry-card">
        <header class="progress-entry-header">
          <div><time :datetime="entry.createdAt">{{ formatDate(entry.createdAt) }}</time><span class="entry-status" :data-status="entryStatus(entry)">{{ entryStatus(entry) }}</span></div>
          <button type="button" class="progress-entry-link" @click="emit('open', entry)">{{ isProgressComplete(entry) ? '查看／修改' : entryStatus(entry) === '待完成行動' ? '回來記錄感受 →' : '繼續追蹤 →' }}</button>
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
      <p>{{ filtered ? '沒有符合這個狀態的進展紀錄，可以調整或清除篩選。' : '儲存第一篇覺察紀錄後，就可以接著記錄重新框架與小行動。' }}</p>
      <button type="button" class="progress-entry-link" @click="emit('diary')">前往我的日記 →</button>
    </div>
  </div>
</template>

<style scoped>
.progress-entry-list { display:grid; gap:20px; }
.progress-entry-card { min-width:0; border:1px solid var(--color-brand-grid); border-radius:16px; background:var(--color-brand-surface); box-shadow:0 2px 8px var(--color-brand-shadow-faint); overflow:hidden; }
.progress-entry-header { display:flex; align-items:center; justify-content:space-between; flex-wrap:wrap; gap:12px; padding:18px 24px; border-bottom:1px solid var(--color-brand-grid); }
.progress-entry-header>div { display:flex; align-items:center; flex-wrap:wrap; gap:12px; }
.progress-entry-header time { font-size:.8rem; color:var(--color-brand-muted); }
.progress-entry-link { padding:8px 12px; border:0; border-radius:10px; background:var(--color-brand-shadow-faint); color:var(--color-brand-ink); font:inherit; font-size:.85rem; cursor:pointer; }
.progress-entry-link:hover { background:var(--color-brand-shadow-soft); }
.progress-entry-link:focus-visible { outline:2px solid var(--color-brand-ink); outline-offset:3px; }
.progress-entry-comparison { display:grid; grid-template-columns:minmax(0,1fr) minmax(0,1.2fr); }
.progress-entry-comparison section { min-width:0; padding:24px; }
.progress-entry-voice { background:var(--color-brand-shadow-faint); border-right:1px solid var(--color-brand-grid); }
.progress-entry-comparison h3 { margin:0 0 16px; font-size:1rem; font-weight:600; }
.progress-entry-voice blockquote { margin:0; font-size:1.05rem; line-height:1.8; white-space:pre-wrap; overflow-wrap:anywhere; }
.progress-entry-card dl { margin:0; }
.progress-entry-card dt { margin-top:20px; color:var(--color-brand-muted); font-size:.8rem; }
.progress-entry-card dt:first-child { margin-top:0; }
.progress-entry-card dd { margin:8px 0 0; font-size:.95rem; line-height:1.85; white-space:pre-wrap; overflow-wrap:anywhere; }
.progress-entry-context { margin-top:24px; }
.progress-entry-context summary { color:var(--color-brand-muted); font-size:.85rem; cursor:pointer; }
.progress-entry-context[open] summary { margin-bottom:18px; }
.progress-empty { padding:20px 0; color:var(--color-brand-muted); line-height:1.9; }
@media(max-width:760px) { .progress-entry-comparison { grid-template-columns:minmax(0,1fr); } .progress-entry-voice { border-right:0; border-bottom:1px solid var(--color-brand-grid); } .progress-entry-header,.progress-entry-comparison section { padding:18px; } }
</style>
