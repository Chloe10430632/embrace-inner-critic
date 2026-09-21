<script setup lang="ts">
import { journalQuestions } from '~/stores/journal'

const route = useRoute()
const ready = ref(false)
const pendingDeleteId = ref<string | null>(null)
const isBusy = ref(false)
const busyMessage = ref('')
const auth = useAuthStore()
const onboarding = useOnboardingStore()
const journalStore = useJournalStore()
const {
  stage: journalStage, customEmotion, customBehavior, entries, editingEntryId, message: authMessage,
  completed, draft: journal, currentQuestion, displayTitle, criticQuote
} = storeToRefs(journalStore)
const { confirmedName, completed: hasCompletedOnboarding } = storeToRefs(onboarding)
const {
  loadEntries, beginNew: beginNewJournal, toggleTag, addCustomTag, availableTags,
  useHardReply, save: saveJournal, open: openEntry, remove: deleteEntry,
  hasContent: hasJournalContent, formatDate: formatEntryDate, preview: entryPreview, displayAnswer
} = journalStore
const { startGoogleLogin } = auth
const { playEffect } = useAudio()

onMounted(async () => {
  try {
    if (route.query.view === 'safety') {
      journalStage.value = 15
    } else if (await auth.requireUser()) {
      await loadEntries()
      if (journalStage.value < 6 || journalStage.value > 16 || journalStage.value === 16) journalStage.value = 6
    } else {
      journalStage.value = 16
    }
  } finally {
    ready.value = true
  }
})

onBeforeRouteLeave(() => {
  if (journalStage.value >= 7 && journalStage.value <= 13 && hasJournalContent()) void saveJournal(completed.value)
})

watch(journalStage, async () => {
  await nextTick()
  if (import.meta.client) window.scrollTo({ top: 0, behavior: 'smooth' })
})

function next(sound: 'tap' | 'paper' = 'tap') {
  playEffect(sound)
  journalStage.value++
}

function requestDelete(id: string) {
  pendingDeleteId.value = id
}

async function confirmDelete() {
  const id = pendingDeleteId.value
  if (!id || isBusy.value) return

  pendingDeleteId.value = null
  busyMessage.value = '正在刪除這篇日記……'
  isBusy.value = true
  try {
    if (await deleteEntry(id)) await loadEntries()
  } finally {
    isBusy.value = false
  }
}

async function handleSave(isComplete: boolean) {
  if (isBusy.value) return

  busyMessage.value = isComplete ? '正在儲存這篇日記……' : '正在儲存草稿……'
  isBusy.value = true
  try {
    if (await saveJournal(isComplete)) await loadEntries()
  } finally {
    isBusy.value = false
  }
}
</script>

<template>
  <div class="journal-page" :inert="isBusy || pendingDeleteId !== null" :aria-busy="isBusy">
    <div class="map-path" aria-hidden="true">
      <span v-for="n in 8" :key="n" class="path-dot" :class="{ lit: journalStage >= n + 5 }" />
    </div>

    <Transition name="page" mode="out-in">
    <section v-if="!ready" key="loading" class="scene compact-scene" aria-live="polite">
      <p class="eyebrow">我的日記</p>
      <h2>正在確認登入狀態……</h2>
    </section>

    <section v-else-if="journalStage === 6" key="journal-home" class="scene journal-home-scene">
      <p class="eyebrow">我的日記</p>
      <h2>從今天想看見的地方開始。</h2>
      <p class="lead narrow">每一篇都可以慢慢寫、之後再回來修改。</p>
      <p v-if="authMessage" class="auth-message">{{ authMessage }}</p>
      <button class="primary" @click="beginNewJournal">寫一篇新日記 <span>→</span></button>
      <div v-if="entries.length" class="entry-list">
        <article v-for="entry in entries" :key="entry.id" class="entry-card" @click="openEntry(entry)">
          <div><span>{{ formatEntryDate(entry.createdAt) }} · {{ entry.isComplete ? '已完成' : '草稿' }}</span><h3>{{ entry.criticName }} 說：「{{ entryPreview(entry) }}」</h3></div>
          <div class="entry-actions">
            <button type="button" class="secondary" @click.stop="openEntry(entry)">閱讀／修改</button>
            <button type="button" class="delete-button" @click.stop="requestDelete(entry.id)">刪除</button>
          </div>
        </article>
      </div>
      <p v-else class="empty-journals">第一篇不需要寫得完整，從一個當下的聲音開始就好。</p>
      <NuxtLink class="text-button" to="/">回到歡迎頁</NuxtLink>
    </section>

    <section v-else-if="journalStage >= 7 && journalStage <= 12" :key="`question-${journalStage}`" class="scene journal-scene">
      <div class="journal-topline">
        <button class="back-button" @click="journalStage--">← 返回</button>
        <span>{{ currentQuestion.eyebrow }}</span>
        <button class="save-link" @click="handleSave(false)">儲存並離開</button>
      </div>
      <div v-if="journalStage > 8 && criticQuote" class="critic-note"><span>{{ confirmedName }} 剛才說</span>「{{ criticQuote }}」</div>
      <h2>{{ displayTitle }}</h2>
      <p class="lead narrow">{{ currentQuestion.help }}</p>

      <div v-if="currentQuestion.key === 'emotions' || currentQuestion.key === 'behaviors'" class="tag-area">
        <div class="tag-cloud">
          <button v-for="tag in availableTags(currentQuestion.key as 'emotions' | 'behaviors')" :key="tag" type="button" class="tag" :class="{ selected: (journal[currentQuestion.key] as string[]).includes(tag) }" @click="toggleTag(currentQuestion.key as 'emotions' | 'behaviors', tag)">#{{ tag }}</button>
        </div>
        <form class="custom-tag" @submit.prevent="addCustomTag(currentQuestion.key as 'emotions' | 'behaviors')">
          <input v-if="currentQuestion.key === 'emotions'" v-model="customEmotion" placeholder="加入自己的感受">
          <input v-else v-model="customBehavior" placeholder="加入自己的行為">
          <button type="submit">＋ 加入</button>
        </form>
      </div>
      <textarea v-else v-model="journal[currentQuestion.key] as string" :placeholder="currentQuestion.placeholder" rows="5" />

      <div class="journal-actions">
        <button v-if="currentQuestion.key === 'reply'" class="secondary" @click="useHardReply">這真的很難，我還不知道</button>
        <button v-if="currentQuestion.key === 'origin' || currentQuestion.key === 'reply'" class="text-button" @click="next()">這題先跳過</button>
        <button class="primary" @click="journalStage === 12 ? journalStage = 13 : next(journalStage === 8 ? 'paper' : 'tap')">{{ journalStage === 12 ? '看看我的這段地圖' : '下一步' }} <span>→</span></button>
      </div>
      <button class="help-link inline-help" @click="journalStage = 15">我現在需要協助</button>
    </section>

    <section v-else-if="journalStage === 13" key="review" class="scene review-scene">
      <p class="eyebrow">回顧</p>
      <h2>這是你今天看見的一小段地圖。</h2>
      <p class="lead">你的原始文字會保持原樣。</p>
      <p v-if="authMessage" class="auth-message">{{ authMessage }}</p>
      <div class="review-grid">
        <article v-for="question in journalQuestions" :key="question.key"><span>{{ question.eyebrow }}</span><p>{{ displayAnswer(question.key) }}</p></article>
      </div>
      <div class="review-actions">
        <button class="secondary" @click="journalStage = 7">返回修改</button>
        <button class="primary" @click="handleSave(true)">{{ editingEntryId ? '儲存修改' : '儲存這篇日記' }} <span>→</span></button>
      </div>
    </section>

    <section v-else-if="journalStage === 14" key="complete" class="scene complete-scene">
      <div class="lit-node"><span>✦</span><i/><i/><i/></div>
      <p class="eyebrow">第一個節點已被看見</p>
      <h2>你不需要一次<br>走完整張地圖。</h2>
      <p class="lead narrow">今天，你已經看見了一個原本很容易被忽略的聲音。</p>
      <button class="primary" @click="journalStage = 6">回到我的日記 <span>→</span></button>
      <button class="text-button" @click="journalStage = 6">關閉今天的練習</button>
    </section>

    <section v-else-if="journalStage === 16" key="login" class="scene compact-scene">
      <div class="blank-node"><span>↗</span></div>
      <p class="eyebrow">寫日記前</p>
      <h2>先登入，才可以把<br>這一頁留給自己。</h2>
      <p class="lead narrow">你的日記會只屬於登入的帳號；完成登入後，就可以建立、修改、查看與刪除自己的日記。</p>
      <p v-if="authMessage" class="auth-message">{{ authMessage }}</p>
      <button class="primary" @click="startGoogleLogin">使用 Google 登入 <span>→</span></button>
      <NuxtLink class="text-button" :to="hasCompletedOnboarding ? '/' : '/introduction'">先回去看看</NuxtLink>
    </section>

    <section v-else key="safety" class="scene safety-scene">
      <div class="safety-mark">♡</div>
      <p class="eyebrow">先照顧現在的你</p>
      <h2>我們先停在這裡，<br>現在的安全比完成日記更重要。</h2>
      <p class="lead narrow">這個網站無法判斷你現在是否安全，也無法提供即時救援。如果你可能立刻傷害自己或別人，請先不要獨自承受。</p>
      <div class="safety-options">
        <a href="tel:119"><strong>撥打 119</strong><span>緊急救護</span></a>
        <a href="tel:110"><strong>撥打 110</strong><span>需要警方立即協助</span></a>
        <a href="tel:1925"><strong>撥打 1925</strong><span>24 小時安心專線</span></a>
      </div>
      <NuxtLink class="secondary" to="/">保存草稿並回到首頁</NuxtLink>
      <p class="safety-footnote">文字提示可能誤判或漏掉真正的危險。你不需要等網站提醒，任何時候都可以主動尋求協助。</p>
    </section>
    </Transition>
  </div>

  <Teleport to="body">
    <div v-if="pendingDeleteId" class="journal-modal-backdrop" role="presentation">
      <section class="journal-modal" role="alertdialog" aria-modal="true" aria-labelledby="delete-dialog-title" aria-describedby="delete-dialog-description">
        <p class="section-kicker">刪除日記</p>
        <h2 id="delete-dialog-title">確定要刪除這篇日記嗎？</h2>
        <p id="delete-dialog-description">刪除後無法復原，這篇日記的內容將不會保留。</p>
        <div class="journal-modal-actions">
          <button type="button" class="secondary" autofocus @click="pendingDeleteId = null">先不要</button>
          <button type="button" class="modal-delete-button" @click="confirmDelete">確定刪除</button>
        </div>
      </section>
    </div>

    <div v-if="isBusy" class="journal-busy-overlay" role="status" aria-live="assertive" aria-busy="true">
      <div class="journal-busy-card">
        <span class="journal-spinner" aria-hidden="true" />
        <strong>{{ busyMessage }}</strong>
        <p>請稍候，不要關閉或重新操作按鈕。</p>
      </div>
    </div>
  </Teleport>
</template>
