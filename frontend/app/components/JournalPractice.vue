<script setup lang="ts">
import { journalQuestions } from '~/stores/journal'

const props = defineProps<{ viewOnly?: boolean }>()
const emit = defineEmits<{ finish: [tab?: 'diary' | 'progress'] }>()
const isBusy = ref(false)
const busyMessage = ref('')
const onboarding = useOnboardingStore()
const journalStore = useJournalStore()
const {
  stage: journalStage, customEmotion, customBehavior, editingEntryId, message: authMessage,
  completed, draft: journal, currentQuestion, displayTitle, criticQuote
} = storeToRefs(journalStore)
const { confirmedName } = storeToRefs(onboarding)
const {
  toggleTag, addCustomTag, availableTags,
  save: saveJournal, beginProgress,
  hasContent: hasJournalContent, displayAnswer
} = journalStore
const { playEffect } = useAudio()

watch(journalStage, async () => {
  await nextTick()
  if (import.meta.client) window.scrollTo({ top: 0, behavior: 'smooth' })
})

function next(sound: 'tap' | 'paper' = 'tap') {
  playEffect(sound)
  journalStage.value++
}

async function handleSave(isComplete: boolean, successStage?: number, loadingMessage?: string) {
  if (isBusy.value) return

  busyMessage.value = loadingMessage ?? (isComplete ? '正在儲存這篇日記……' : '正在儲存草稿……')
  isBusy.value = true
  try {
    const saved = await saveJournal(isComplete, successStage)
    if (saved && journalStage.value === 6) emit('finish')
    return saved
  } finally {
    isBusy.value = false
  }
}

async function returnToList(tab?: 'diary' | 'progress') {
  if (isBusy.value) return
  const editing = (journalStage.value >= 7 && journalStage.value <= 13) || (journalStage.value >= 17 && journalStage.value <= 21) || journalStage.value === 23
  if (editing && !(props.viewOnly && journalStage.value === 13) && hasJournalContent()) {
    if (!await handleSave(completed.value, journalStage.value)) return
  }
  journalStage.value = 6
  emit('finish', tab)
}

async function startAction() {
  if (await handleSave(false, 23, '正在儲存小行動……')) {
    journalStage.value = 6
    emit('finish', 'progress')
  }
}
</script>

<template>
  <div class="journal-page" :inert="isBusy" :aria-busy="isBusy">
    <div class="map-path" aria-hidden="true"><span v-for="n in 8" :key="n" class="path-dot" :class="{ lit: journalStage >= n + 5 }" /></div>
    <button v-if="journalStage !== 15 && journalStage !== 23 && !(viewOnly && journalStage === 13)" type="button" class="practice-return" @click="returnToList()">← 返回列表</button>
    <p v-if="authMessage" class="auth-message practice-message" role="alert">{{ authMessage }}</p>
    <Transition name="page" mode="out-in">
    <section v-if="journalStage >= 7 && journalStage <= 11" :key="`question-${journalStage}`" class="scene journal-scene">
      <div class="journal-topline">
        <button class="back-button" @click="journalStage === 7 ? returnToList() : journalStage--">← 返回</button>
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
        <button v-if="currentQuestion.key === 'origin'" class="text-button" @click="journalStage = 13">這題先跳過</button>
        <button class="primary" @click="journalStage === 11 ? journalStage = 13 : next(journalStage === 8 ? 'paper' : 'tap')">{{ journalStage === 11 ? '完成這次紀錄' : '下一步' }} <span>→</span></button>
      </div>
      <button class="help-link inline-help" @click="journalStage = 15">我現在需要協助</button>
    </section>

    <section v-else-if="journalStage === 13" key="review" class="scene review-scene">
      <p class="eyebrow">回顧</p>
      <h2>這是你看見的一次練習。</h2>
      <p class="lead">練習思考看看，你的負面批評是如何影響你的情緒與行為</p>
      <div class="review-grid">
        <article v-for="question in journalQuestions" :key="question.key" :class="{ 'review-origin': question.key === 'origin' }"><span>{{ question.eyebrow }}</span><p>{{ displayAnswer(question.key) }}</p></article>
      </div>
      <button v-if="viewOnly" type="button" class="practice-return review-bottom-return" @click="returnToList()">← 返回列表</button>
      <div v-else class="review-actions">
        <button type="button" class="text-button review-return-link" @click="returnToList()">← 返回列表</button>
        <div class="review-action-buttons">
          <button class="secondary" @click="journalStage = 7">修改</button>
          <button class="primary" @click="handleSave(completed, 14, '正在儲存覺察紀錄……')">{{ editingEntryId ? '確認' : '儲存' }} </button>
        </div>
      </div>
    </section>

    <section v-else-if="journalStage === 14" key="complete" class="scene complete-scene">
      <div class="lit-node"><span>✦</span><i/><i/><i/></div>
      <p class="eyebrow">又有一句話被你看見了</p>
      <h2>感謝自己的紀錄和能量</h2>
      <p class="lead narrow">今天，你成功的捕捉了一個原本很容易被忽略的聲音。</p>
      <button class="primary" @click="beginProgress">繼續追蹤進展 <span>→</span></button>
      <button class="secondary completion-secondary" @click="returnToList()">返回列表</button>
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
        <button class="secondary" @click="returnToList()">先返回列表</button>
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
      <p class="lead narrow">如果你最要好的朋友聽見這句話，他會如何理解這件事、回應並支持你？</p>
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
      <p class="lead narrow">先去試試看，完成後隨時回來，記錄當時的感受。</p>
      <div class="progress-actions progress-actions-wrap">
        <button class="secondary" @click="journalStage = 19">← 返回</button>
        <button class="primary" @click="startAction">儲存，先去做這個小行動 <span>→</span></button>
      </div>
    </section>

    <section v-else-if="journalStage === 23" key="progress-action-return" class="scene progress-scene progress-return-scene">
      <p class="eyebrow">回來記錄進展</p>
      <h2>你已經試過這個小行動了嗎？</h2>
      <article class="progress-focus-card progress-return-card">
        <header class="return-card-heading"><span>你想試試看的小行動</span><button type="button" class="return-card-edit" @click="journalStage = 20"><svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m16 3 5 5-12 12-6 1 1-6L16 3ZM13 6l5 5" /></svg>修改</button></header>
        <p>{{ journal.nextAction }}</p>
      </article>
      <p class="lead narrow">照自己的步調就好。試過之後，再回來記錄真實的感受。</p>
      <div class="progress-actions progress-return-actions">
        <button class="primary" @click="journalStage = 21">我已經試過，記錄感受 <span>→</span></button>
      </div>
      <button type="button" class="practice-return return-list-link" @click="returnToList('progress')">← 返回列表</button>
    </section>

    <section v-else-if="journalStage === 21" key="progress-emotion" class="scene progress-scene">
      <p class="eyebrow">追蹤進展 · 05</p>
      <h2>做完這個小行動後，<br>你現在感受到什麼？</h2>
      <p class="lead narrow">不需要變得更開心才算有進展。請照現在真實的感受寫下來。</p>
      <textarea v-model="journal.afterActionEmotion as string" placeholder="例如：還是有點緊張，但比剛才多了一點踏實。" rows="4" />
      <div class="progress-actions">
        <button class="secondary" @click="journalStage = 23">← 返回</button>
        <button class="primary" @click="handleSave(true, 22, '正在儲存這次進展……')">儲存這次進展 <span>→</span></button>
      </div>
    </section>

    <section v-else-if="journalStage === 22" key="progress-complete" class="scene complete-scene">
      <div class="lit-node"><span>✦</span><i/><i/><i/></div>
      <p class="eyebrow">這次進展已經留下</p>
      <h2>一個很小的行動，<br>也是一條新的路。</h2>
      <p class="lead narrow">你不需要證明自己已經完全改變。願意停下來、換一個角度並做出一步，就值得被記得。</p>
      <button class="primary" @click="returnToList('progress')">回到追蹤進展 <span>→</span></button>
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
  <Teleport to="body">
    <div v-if="isBusy" class="journal-busy-overlay" role="status" aria-live="assertive" aria-busy="true"><div class="journal-busy-card"><span class="journal-spinner" aria-hidden="true" /><strong>{{ busyMessage }}</strong><p>請稍候，不要關閉或重新操作按鈕。</p></div></div>
  </Teleport>
  </div>
</template>

<style scoped>
.practice-message { position:relative; z-index:3; margin:16px auto 0; width:min(700px,calc(100% - 36px)); text-align:center; }
.practice-return { position:relative; z-index:3; display:block; margin:20px auto 0; padding:8px 14px; border:0; border-radius:10px; background:var(--color-brand-shadow-faint); color:var(--color-brand-muted); font:inherit; font-size:.85rem; cursor:pointer; }
.review-bottom-return { margin-top:28px; }
.progress-return-scene { width:min(760px,calc(100% - 36px)); justify-content:center; gap:0; }
.progress-return-scene h2 { font-size:clamp(1.7rem,4vw,2.6rem); }
.progress-return-scene .progress-return-card { box-sizing:border-box; width:100%; margin-top:28px; padding:20px 24px; border:1px solid var(--color-brand-grid); border-radius:16px; box-shadow:0 2px 8px var(--color-brand-shadow-faint); text-align:left; }
.return-card-heading { display:flex; align-items:center; justify-content:space-between; gap:16px; }
.return-card-edit { display:inline-flex; align-items:center; gap:6px; min-height:36px; padding:6px 9px; border:0; border-radius:8px; background:transparent; color:var(--color-brand-muted); font:inherit; font-size:.8rem; cursor:pointer; }
.return-card-edit:hover { background:var(--color-brand-shadow-soft); color:var(--color-brand-ink); }
.return-card-edit:focus-visible { outline:2px solid var(--color-brand-ink); outline-offset:3px; }
.progress-return-scene .lead { margin:20px 0 0; font-size:1rem; }
.progress-return-actions { width:100%; justify-content:center; margin-top:28px; }
.return-list-link { margin-top:16px; background:transparent; }
@media(max-width:600px) { .progress-return-scene .progress-return-card { padding:18px; } .progress-return-actions .primary { width:100%; justify-content:center; } }
.practice-return:focus-visible { outline:2px solid var(--color-brand-ink); outline-offset:3px; }
</style>
