<script setup lang="ts">
import type { JournalEntry } from '~/types/journal'
defineProps<{ disabled: boolean; entries: JournalEntry[]; filtered: boolean }>()
const emit = defineEmits<{ open: [entry: JournalEntry]; edit: [entry: JournalEntry]; progress: [entry: JournalEntry]; remove: [id: string] }>()
const store = useJournalStore()
const { formatDate, preview, isRecorded, isProgressComplete, entryStatus } = store
const { confirmedName } = storeToRefs(useOnboardingStore())
</script>

<template>
  <div>
    <p v-if="!entries.length" class="journal-empty">{{ filtered ? '沒有符合這個狀態的日記，可以調整或清除篩選。' : '第一篇不需要寫得完整，從一個當下的聲音開始就好。' }}</p>
    <div class="journal-list">
      <article v-for="entry in entries" :key="entry.id" class="journal-card">
        <header>
          <div class="journal-meta"><time :datetime="entry.createdAt">{{ formatDate(entry.createdAt) }}</time><span class="entry-status" :data-status="entryStatus(entry)">{{ entryStatus(entry) }}</span></div>
          <div class="journal-record-actions">
            <button type="button" :disabled="disabled" @click="emit('open', entry)"><svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z" /><circle cx="12" cy="12" r="3" /></svg>檢視</button>
            <button type="button" :disabled="disabled" @click="emit('edit', entry)"><svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m16 3 5 5-12 12-6 1 1-6L16 3ZM13 6l5 5" /></svg>編輯</button>
            <button type="button" :disabled="disabled" @click="emit('remove', entry.id)"><svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 6h18M9 6V3h6v3M5 6l1 15h12l1-15M10 10v7M14 10v7" /></svg>刪除</button>
          </div>
        </header>
        <dl><dt>觸發情境</dt><dd>{{ preview(entry, 'trigger') }}</dd><dt>{{ confirmedName }} 的批評</dt><dd>{{ preview(entry, 'critic') }}</dd></dl>
        <div v-if="isRecorded(entry) && !isProgressComplete(entry)" class="journal-progress-reminder">
          <button type="button" class="journal-track" :disabled="disabled" @click="emit('progress', entry)">追蹤進展 <span aria-hidden="true">→</span></button>
        </div>
      </article>
    </div>
  </div>
</template>

<style scoped>
.journal-list { display:grid; gap:20px; }
.journal-card { padding:20px 24px; border:1px solid var(--color-brand-grid); border-radius:16px; background:var(--color-brand-surface); box-shadow:0 2px 8px var(--color-brand-shadow-faint); }
.journal-card header,.journal-meta,.journal-record-actions { display:flex; align-items:center; flex-wrap:wrap; gap:12px; }
.journal-card header { justify-content:space-between; }
.journal-meta time { color:var(--color-brand-muted); font-size:.8rem; }
.journal-record-actions { gap:4px; }
.journal-record-actions button { display:inline-flex; align-items:center; gap:6px; min-height:36px; padding:6px 9px; border:0; border-radius:8px; background:transparent; color:var(--color-brand-muted); font:inherit; font-size:.8rem; cursor:pointer; }
.journal-track { display:inline-flex; align-items:center; gap:12px; min-height:36px; padding:8px 12px; border:0; border-radius:10px; background:var(--color-brand-shadow-faint); color:var(--color-brand-ink); font:inherit; font-size:.85rem; cursor:pointer; transition:background .15s; }
.journal-record-actions button:hover { background:var(--color-brand-shadow-soft); color:var(--color-brand-ink); }
.journal-track:hover { background:var(--color-brand-shadow-soft); }
button:focus-visible { outline:2px solid var(--color-brand-ink); outline-offset:3px; }
button:disabled { opacity:.6; cursor:wait; }
.journal-card dl { margin:20px 0 0; }
.journal-card dt { margin-top:16px; font-size:.8rem; color:var(--color-brand-muted); }
.journal-card dd { margin:6px 0 0; line-height:1.8; overflow-wrap:anywhere; white-space:pre-wrap; }
.journal-progress-reminder { display:flex; align-items:center; flex-wrap:wrap; gap:12px; margin-top:18px; }
.journal-empty { padding:20px 0; color:var(--color-brand-muted); line-height:1.9; }
@media(max-width:600px) { .journal-card { padding:18px; } }
</style>
