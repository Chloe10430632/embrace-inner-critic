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
  useHardReply, save: saveJournal, open: openEntry, openProgress, beginProgress, remove: deleteEntry,
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
      const progressEntryId = typeof route.query.progress === 'string' ? route.query.progress : ''
      const progressEntry = entries.value.find(entry => entry.id === progressEntryId)
      if (progressEntry) openProgress(progressEntry)
      else if (journalStage.value < 6 || journalStage.value > 16 || journalStage.value === 16) journalStage.value = 6
    } else {
      journalStage.value = 16
    }
  } finally {
    ready.value = true
  }
})

onBeforeRouteLeave(() => {
  const isWritingJournal = journalStage.value >= 7 && journalStage.value <= 13
  const isTrackingProgress = journalStage.value >= 17 && journalStage.value <= 21
  if ((isWritingJournal || isTrackingProgress) && hasJournalContent()) void saveJournal(completed.value)
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

async function handleSave(isComplete: boolean, successStage?: number, loadingMessage?: string) {
  if (isBusy.value) return

  busyMessage.value = loadingMessage ?? (isComplete ? '正在儲存這篇日記……' : '正在儲存草稿……')
  isBusy.value = true
  try {
    if (await saveJournal(isComplete, successStage)) await loadEntries()
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
      <h2>紀錄 {{ confirmedName }} 的慣性思考方式</h2>
      <p class="lead narrow">每一篇都可以慢慢寫、之後再回來修改。</p>
      <p v-if="authMessage" class="auth-message">{{ authMessage }}</p>
      <div class="journal-home-actions">
        <button class="primary" @click="beginNewJournal">寫一篇新日記 <span>→</span></button>
        <NuxtLink class="secondary" to="/progress">查看追蹤進展</NuxtLink>
      </div>
      <div v-if="entries.length" class="entry-list">
        <article v-for="entry in entries" :key="entry.id" class="entry-card" @click="openEntry(entry)">
          <div class="entry-content">
            <span>{{ formatEntryDate(entry.createdAt) }} · {{ entry.isComplete ? '已完成' : '草稿' }}</span>
            <div class="entry-preview">
              <div><span>觸發情境</span><p>{{ entryPreview(entry, 'trigger') }}</p></div>
              <div><span>{{ entry.criticName }} 的批評</span><p>{{ entryPreview(entry, 'critic') }}</p></div>
            </div>
          </div>
          <div class="entry-actions">
            <button type="button" class="secondary" @click.stop="openEntry(entry)">閱讀／修改</button>
            <button v-if="entry.isComplete" type="button" class="progress-button" @click.stop="openProgress(entry)">追蹤進展</button>
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
        <button v-if="currentQuestion.key === 'origin' || currentQuestion.key === 'reply'" class="text-button" @click="next()">這題先跳過</button>
        <button v-if="currentQuestion.key === 'reply'" class="secondary" @click="useHardReply">這真的很難，我還不知道</button>
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
      <p class="eyebrow">又有一句話被你看見了</p>
      <h2>感謝自己的紀錄和能量</h2>
      <p class="lead narrow">今天，你成功的捕捉了一個原本很容易被忽略的聲音。</p>
      <button class="primary" @click="beginProgress">繼續追蹤進展 <span>→</span></button>
      <button class="secondary completion-secondary" @click="journalStage = 6">回到我的日記</button>
      <button class="text-button" @click="journalStage = 6">關閉今天的練習</button>
    </section>

    <section v-else-if="journalStage === 17" key="progress-thought" class="scene progress-scene">
      <p class="eyebrow">追蹤進展 · 01</p>
      <h2>先帶著剛才看見的想法。</h2>
      <p class="lead narrow">不需要重新寫一次，我們先把這句自我批評放在眼前。</p>
      <article class="progress-focus-card">
        <span>{{ confirmedName }} 當時說</span>
        <p>「{{ journal.critic || '今天沒有寫下這句話' }}」</p>
      </article>
      <div class="progress-actions">
        <button class="secondary" @click="journalStage = 6">先回到日記</button>
        <button class="primary" @click="journalStage = 18">看看它帶來的影響 <span>→</span></button>
      </div>
    </section>

    <section v-else-if="journalStage === 18" key="progress-impact" class="scene progress-scene">
      <p class="eyebrow">追蹤進展 · 02</p>
      <h2>它讓你感受到什麼，<br>又把你帶往哪裡？</h2>
      <div class="progress-impact-grid">
        <article><span>感受到的情緒</span><p>{{ (journal.emotions as string[]).length ? (journal.emotions as string[]).map(item => `#${item}`).join('　') : '今天沒有記錄情緒' }}</p></article>
        <article><span>受到影響的行為</span><p>{{ (journal.behaviors as string[]).length ? (journal.behaviors as string[]).map(item => `#${item}`).join('　') : '今天沒有記錄行為' }}</p></article>
      </div>
      <div class="progress-actions">
        <button class="secondary" @click="journalStage = 17">← 返回</button>
        <button class="primary" @click="journalStage = 19">試著換一個角度 <span>→</span></button>
      </div>
    </section>

    <section v-else-if="journalStage === 19" key="progress-reframe" class="scene progress-scene">
      <p class="eyebrow">追蹤進展 · 03</p>
      <h2>如果不只聽批評的聲音，<br>這件事還能怎麼理解？</h2>
      <p class="lead narrow">重新框架不是強迫自己正向，而是找一個更貼近完整事實、也能支持現在自己的說法。</p>
      <article class="progress-focus-card progress-focus-card-compact">
        <span>{{ confirmedName }} 當時說</span>
        <p>「{{ journal.critic || '今天沒有寫下這句話' }}」</p>
      </article>
      <textarea v-model="journal.reframedThought as string" placeholder="例如：我現在遇到困難，不代表我沒有能力；我可以先完成其中一小部分。" rows="5" />
      <div class="progress-actions">
        <button class="secondary" @click="journalStage = 18">← 返回</button>
        <button class="primary" @click="journalStage = 20">想一個小行動 <span>→</span></button>
      </div>
    </section>

    <section v-else-if="journalStage === 20" key="progress-action" class="scene progress-scene">
      <p class="eyebrow">追蹤進展 · 04</p>
      <h2>現在可以馬上做的<br>最小一步是什麼？</h2>
      <p class="lead narrow">行動越小、越具體，越容易開始。它可以只是打開文件、寫下一句話，或傳出一則訊息。</p>
      <textarea v-model="journal.nextAction as string" placeholder="例如：先打開文件，寫下第一個小標題。" rows="4" />
      <div class="progress-actions progress-actions-wrap">
        <button class="secondary" @click="journalStage = 19">← 返回</button>
        <button class="text-button" @click="handleSave(true, 6, '正在保存目前的進展……')">儲存，稍後再回來</button>
        <button class="primary" @click="journalStage = 21">我去做這個小行動 <span>→</span></button>
      </div>
    </section>

    <section v-else-if="journalStage === 21" key="progress-emotion" class="scene progress-scene">
      <p class="eyebrow">追蹤進展 · 05</p>
      <h2>做完這個小行動後，<br>你現在感受到什麼？</h2>
      <p class="lead narrow">不需要變得更開心才算有進展。請照現在真實的感受寫下來。</p>
      <textarea v-model="journal.afterActionEmotion as string" placeholder="例如：還是有點緊張，但比剛才多了一點踏實。" rows="4" />
      <div class="progress-actions">
        <button class="secondary" @click="journalStage = 20">← 返回</button>
        <button class="primary" @click="handleSave(true, 22, '正在儲存這次進展……')">儲存這次進展 <span>→</span></button>
      </div>
    </section>

    <section v-else-if="journalStage === 22" key="progress-complete" class="scene complete-scene">
      <div class="lit-node"><span>✦</span><i/><i/><i/></div>
      <p class="eyebrow">這次進展已經留下</p>
      <h2>一個很小的行動，<br>也是一條新的路。</h2>
      <p class="lead narrow">你不需要證明自己已經完全改變。願意停下來、換一個角度並做出一步，就值得被記得。</p>
      <button class="primary" @click="journalStage = 6">回到我的日記 <span>→</span></button>
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
