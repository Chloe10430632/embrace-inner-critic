<script setup lang="ts">
type JournalKey = 'trigger' | 'critic' | 'emotions' | 'behaviors' | 'origin' | 'reply'
type JournalEntry = {
  id: string
  createdAt: string
  criticName: string
  answers: Record<JournalKey, string | string[]>
  isComplete: boolean
}
type AuthUser = { id: string; email: string | null }

const stage = ref(0)
const lessonOne = ref(0)
const lessonTwo = ref(0)
const criticName = ref('')
const confirmedName = ref('山姆')
const hasCompletedOnboarding = ref(false)
const soundOn = ref(true)
const effectsOn = ref(true)
const soundPanel = ref(false)
const navigationOpen = ref(false)
const customEmotion = ref('')
const customBehavior = ref('')
const completed = ref(false)
const entries = ref<JournalEntry[]>([])
const editingEntryId = ref<string | null>(null)
const config = useRuntimeConfig()
const authUser = ref<AuthUser | null>(null)
const authChecked = ref(false)
const authMessage = ref('')
const csrfToken = ref('')
const shareMessage = ref('')

const journal = reactive<Record<JournalKey, string | string[]>>({
  trigger: '', critic: '', emotions: [], behaviors: [], origin: '', reply: ''
})

const emotions = ['羞恥', '焦慮', '害怕', '憤怒', '難過', '挫折', '孤單', '無力', '麻木']
const behaviors = ['逃避', '討好', '拖延', '反擊', '僵住', '過度工作', '反覆確認', '責怪自己', '放棄']
const questions: { key: JournalKey; eyebrow: string; title: string; help: string; placeholder?: string }[] = [
  { key: 'trigger', eyebrow: '01 · 觸發', title: '什麼情況下，{name} 出現了？', help: '寫下當時發生了什麼事，不需要先分析原因，很簡短也可以喔！', placeholder: '例如：主管問我為什麼還沒完成工作……' },
  { key: 'critic', eyebrow: '02 · 聽見', title: '那一刻，{name} 對你說了什麼？', help: '一次只抓住一句現在感受最強烈的話。照原本的樣子寫下來，不用替它修飾。', placeholder: '他說：「你時間管理真的有問題……」' },
  { key: 'emotions', eyebrow: '03 · 感受', title: '聽見這句話時，你感受到什麼？', help: '可以選很多個，也可以用自己的方式形容。' },
  { key: 'behaviors', eyebrow: '04 · 行動', title: '聽見這句話後，你的行為受到什麼影響？', help: '不需要判斷好壞，只要看看它把你帶往哪裡。' },
  { key: 'origin', eyebrow: '05 · 種子', title: '這個自我批評的種子，是怎麼形成的呢？', help: '也許來自家庭、學校、老師、同儕或其他關係；也可能暫時想不到。這裡不用一定要找到源頭。', placeholder: '它讓我想到……' },
  { key: 'reply', eyebrow: '06 · 回應', title: '如果是你的朋友遇到一樣的事，你會怎麼支持他？', help: '想不到也沒有關係。你可以承認：「這真的很難，我還不知道要怎麼回答。」', placeholder: '我想對自己說……' }
]

let audioContext: AudioContext | null = null
let ambientAudio: HTMLAudioElement | null = null
let ambientFadeTimer: number | null = null

const currentQuestion = computed(() => questions[stage.value - 7] ?? questions[0]!)
const displayTitle = computed(() => currentQuestion.value?.title.replace('{name}', confirmedName.value))
const criticQuote = computed(() => String(journal.critic || '').trim())

function ensureAudio() {
  if (!import.meta.client) return null
  audioContext ??= new AudioContext()
  if (audioContext.state === 'suspended') audioContext.resume()
  return audioContext
}

function playEffect(kind: 'tap' | 'paper' | 'complete' = 'tap') {
  if (!effectsOn.value) return
  const ctx = ensureAudio()
  if (!ctx) return
  const osc = ctx.createOscillator()
  const gain = ctx.createGain()
  const now = ctx.currentTime
  const frequencies = { tap: 520, paper: 380, complete: 660 }
  osc.frequency.setValueAtTime(frequencies[kind], now)
  if (kind === 'complete') osc.frequency.exponentialRampToValueAtTime(880, now + 0.28)
  gain.gain.setValueAtTime(0.0001, now)
  gain.gain.exponentialRampToValueAtTime(0.055, now + 0.018)
  gain.gain.exponentialRampToValueAtTime(0.0001, now + (kind === 'complete' ? 0.55 : 0.16))
  osc.connect(gain).connect(ctx.destination)
  osc.start(now)
  osc.stop(now + (kind === 'complete' ? 0.58 : 0.18))
}

function startAmbient() {
  if (!import.meta.client || ambientAudio) return
  const audio = new Audio('/audio/mindful-piano.mp3')
  audio.loop = true
  audio.volume = 0
  audio.addEventListener('timeupdate', () => {
    if (audio.currentTime >= 95) audio.currentTime = 0
  })
  ambientAudio = audio
  void audio.play().then(() => fadeAmbientTo(0.22, 1500)).catch(() => {
    ambientAudio = null
  })
}

function fadeAmbientTo(target: number, duration: number, onComplete?: () => void) {
  if (!ambientAudio) return
  if (ambientFadeTimer !== null) window.clearInterval(ambientFadeTimer)
  const audio = ambientAudio
  const startVolume = audio.volume
  const startedAt = performance.now()
  ambientFadeTimer = window.setInterval(() => {
    const progress = Math.min((performance.now() - startedAt) / duration, 1)
    audio.volume = startVolume + (target - startVolume) * progress
    if (progress === 1) {
      if (ambientFadeTimer !== null) window.clearInterval(ambientFadeTimer)
      ambientFadeTimer = null
      onComplete?.()
    }
  }, 50)
}

function stopAmbient() {
  if (!ambientAudio) return
  const audio = ambientAudio
  fadeAmbientTo(0, 600, () => {
    audio.pause()
    audio.currentTime = 0
    if (ambientAudio === audio) ambientAudio = null
  })
}

function disposeAmbient() {
  if (ambientFadeTimer !== null) {
    window.clearInterval(ambientFadeTimer)
    ambientFadeTimer = null
  }
  ambientAudio?.pause()
  ambientAudio = null
}

watch(soundOn, value => value ? startAmbient() : stopAmbient())
watch(stage, async () => {
  await nextTick()
  if (import.meta.client) window.scrollTo({ top: 0, behavior: 'smooth' })
})

function next(sound: 'tap' | 'paper' = 'tap') {
  playEffect(sound)
  stage.value++
}

function startJourney() {
  lessonOne.value = 0
  lessonTwo.value = 0
  if (soundOn.value) startAmbient()
  playEffect()
  stage.value = 1
}

async function startJournal() {
  if (import.meta.client) localStorage.setItem('embrace-inner-critic:onboarding-complete', 'true')
  hasCompletedOnboarding.value = true
  if (await requireJournalAccess()) beginNewJournal()
}

async function beginNewJournal() {
  if (!await requireJournalAccess()) return
  for (const key of Object.keys(journal) as JournalKey[]) journal[key] = key === 'emotions' || key === 'behaviors' ? [] : ''
  customEmotion.value = ''
  customBehavior.value = ''
  editingEntryId.value = null
  completed.value = false
  playEffect()
  stage.value = 7
}

function replayIntroduction() {
  lessonOne.value = 0
  lessonTwo.value = 0
  playEffect()
  stage.value = 1
}

async function shareSite() {
  if (!import.meta.client) return

  const shareData = {
    title: '內在批評者的覺察地圖',
    text: '用一步一題的方式，辨認內在批評者如何影響情緒、行為與決定。',
    url: window.location.href
  }

  try {
    if (navigator.share) {
      await navigator.share(shareData)
      shareMessage.value = '分享視窗已開啟。'
    } else {
      await navigator.clipboard.writeText(shareData.url)
      shareMessage.value = '網站連結已複製。'
    }
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') return
    shareMessage.value = '目前無法自動分享，請直接複製瀏覽器網址。'
  }
}

async function navigateTo(destination: 'home' | 'journals' | 'animation') {
  if (stage.value >= 7 && stage.value <= 13 && hasJournalContent()) void saveJournal(completed.value)
  navigationOpen.value = false

  if (destination === 'home') {
    stage.value = 0
  } else if (destination === 'journals') {
    if (await requireJournalAccess()) {
      await loadEntries()
      stage.value = 6
    }
  } else {
    replayIntroduction()
  }
}

function hasJournalContent() {
  return Object.values(journal).some(value => Array.isArray(value) ? value.length > 0 : value.trim().length > 0)
}

function confirmSound(enabled: boolean) {
  soundOn.value = enabled
  if (enabled) startAmbient()
  playEffect()
  stage.value = 2
}

function advanceLessonOne() {
  playEffect()
  if (lessonOne.value < 2) lessonOne.value++
  else stage.value = 3
}

function confirmCriticName() {
  confirmedName.value = criticName.value.trim() || '山姆'
  if (import.meta.client) localStorage.setItem('embrace-inner-critic:critic-name', confirmedName.value)
  playEffect('paper')
  stage.value = 4
}

function advanceLessonTwo() {
  playEffect()
  if (lessonTwo.value < 1) lessonTwo.value++
  else stage.value = 5
}

function toggleTag(key: 'emotions' | 'behaviors', tag: string) {
  const values = journal[key] as string[]
  const index = values.indexOf(tag)
  index >= 0 ? values.splice(index, 1) : values.push(tag)
  playEffect()
}

function addCustomTag(key: 'emotions' | 'behaviors') {
  const source = key === 'emotions' ? customEmotion : customBehavior
  const value = source.value.trim().replace(/^#/, '')
  if (!value) return
  const values = journal[key] as string[]
  if (!values.includes(value)) values.push(value)
  source.value = ''
  playEffect()
}

function availableTags(key: 'emotions' | 'behaviors') {
  const suggested = key === 'emotions' ? emotions : behaviors
  const selected = journal[key] as string[]
  return [...suggested, ...selected.filter(tag => !suggested.includes(tag))]
}

function useHardReply() {
  journal.reply = '這真的很難，我還不知道要怎麼回答。'
  playEffect('paper')
}

function finishJournal() {
  void saveJournal(true)
}

function cloneAnswers(source: Record<JournalKey, string | string[]>) {
  return Object.fromEntries(Object.entries(source).map(([key, value]) => [key, Array.isArray(value) ? [...value] : value])) as Record<JournalKey, string | string[]>
}

async function saveJournal(isComplete: boolean) {
  try {
    const entry = await api<JournalEntry>(editingEntryId.value ? '/api/diary-entries/' + editingEntryId.value : '/api/diary-entries', {
      method: editingEntryId.value ? 'PUT' : 'POST',
      body: {
        criticName: confirmedName.value,
        answers: cloneAnswers(journal),
        isComplete
      }
    })
    const existingIndex = entries.value.findIndex(item => item.id === entry.id)
    if (existingIndex >= 0) entries.value.splice(existingIndex, 1, entry)
    else entries.value.unshift(entry)
    editingEntryId.value = entry.id
    playEffect(isComplete ? 'complete' : 'paper')
    if (isComplete) {
      completed.value = true
      stage.value = 14
    } else stage.value = 6
  } catch {
    authMessage.value = '日記目前無法儲存，請確認登入與後端服務後再試一次。'
  }
}

function openEntry(entry: JournalEntry) {
  for (const key of Object.keys(journal) as JournalKey[]) journal[key] = Array.isArray(entry.answers[key]) ? [...entry.answers[key] as string[]] : entry.answers[key] as string
  confirmedName.value = entry.criticName
  criticName.value = entry.criticName
  editingEntryId.value = entry.id
  completed.value = entry.isComplete
  stage.value = 13
}

async function deleteEntry(id: string) {
  if (!import.meta.client || !window.confirm('要刪除這篇日記嗎？這個動作無法復原。')) return
  try {
    await api('/api/diary-entries/' + id, { method: 'DELETE' })
    entries.value = entries.value.filter(entry => entry.id !== id)
    playEffect('paper')
  } catch {
    authMessage.value = '日記目前無法刪除，請稍後再試。'
  }
}

function formatEntryDate(isoDate: string) {
  return new Intl.DateTimeFormat('zh-TW', { dateStyle: 'medium' }).format(new Date(isoDate))
}

function entryPreview(entry: JournalEntry) {
  return String(entry.answers.critic || entry.answers.trigger || '今天先沒有留下文字。').slice(0, 72)
}

function saveAndLeave() {
  void saveJournal(false)
}

async function api<T>(path: string, options: Record<string, unknown> = {}) {
  const method = String(options.method ?? 'GET').toUpperCase()
  const requiresCsrf = ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method)
  const headers = new Headers(options.headers as HeadersInit | undefined)

  if (requiresCsrf) {
    if (!csrfToken.value) {
      const response = await $fetch<{ requestToken: string }>(config.public.apiBase + '/api/auth/csrf', {
        credentials: 'include'
      })
      csrfToken.value = response.requestToken
    }
    headers.set('X-CSRF-TOKEN', csrfToken.value)
  }

  return await $fetch<T>(config.public.apiBase + path, {
    credentials: 'include',
    ...options,
    headers
  })
}

async function checkSession() {
  try {
    authUser.value = await api<AuthUser>('/api/auth/me')
  } catch {
    authUser.value = null
  } finally {
    authChecked.value = true
  }
}

async function requireJournalAccess() {
  if (!authChecked.value) await checkSession()
  if (authUser.value) return true
  authMessage.value = ''
  stage.value = 16
  return false
}

function startGoogleLogin() {
  if (!import.meta.client) return
  window.location.assign(config.public.apiBase + '/api/auth/google')
}

async function loadEntries() {
  try {
    const loadedEntries: JournalEntry[] = []
    const pageSize = 50
    const maxPages = 10
    for (let page = 1; page <= maxPages; page++) {
      const pageEntries = await api<JournalEntry[]>(`/api/diary-entries?page=${page}&pageSize=${pageSize}`)
      loadedEntries.push(...pageEntries)
      if (pageEntries.length < pageSize) break
    }
    entries.value = loadedEntries
  } catch {
    entries.value = []
    authMessage.value = '無法讀取日記，請確認後端服務是否已啟動。'
  }
}

async function logout() {
  try {
    await api('/api/auth/logout', { method: 'POST' })
  } finally {
    authUser.value = null
    csrfToken.value = ''
    entries.value = []
    stage.value = 0
  }
}

function displayAnswer(key: JournalKey) {
  const value = journal[key]
  return Array.isArray(value) ? value.map(item => `#${item}`).join('　') || '今天沒有回答' : value || '今天沒有回答'
}

onMounted(async () => {
  hasCompletedOnboarding.value = localStorage.getItem('embrace-inner-critic:onboarding-complete') === 'true'
  const storedCriticName = localStorage.getItem('embrace-inner-critic:critic-name')
  if (storedCriticName) {
    criticName.value = storedCriticName
    confirmedName.value = storedCriticName
  }
  await checkSession()
  if (authUser.value) await loadEntries()
  if (authUser.value && new URLSearchParams(window.location.search).get('next') === 'journals') {
    stage.value = 6
    window.history.replaceState({}, '', window.location.pathname)
  }
})

onBeforeUnmount(() => disposeAmbient())
</script>

<template>
  <main class="app-shell" :class="{ 'is-complete': completed }">
    <div class="mist mist-one" />
    <div class="mist mist-two" />

    <header class="topbar">
      <button class="brand" aria-label="回到擁抱內在批評者首頁" @click="navigateTo('home')">
        <img class="brand-mark" src="/favicon.svg" alt="" width="40" height="40">
        <span class="brand-wordmark">
          <span class="tracking-widest">擁抱內在批評者</span>
          <small>EMBRACE YOUR INNER CRITIC</small>
        </span>
      </button>
      <div class="topbar-actions">
        <div class="sound-wrap">
          <button class="icon-button" aria-label="聲音設定" @click="soundPanel = !soundPanel">
            {{ soundOn ? '♪' : '♩' }}
          </button>
          <div v-if="soundPanel" class="sound-panel">
            <label><input v-model="soundOn" type="checkbox"> 背景聲景</label>
            <label><input v-model="effectsOn" type="checkbox"> 按鈕小音效</label>
          </div>
        </div>
        <div class="navigation-wrap">
          <button class="icon-button menu-button" :aria-expanded="navigationOpen" aria-controls="main-navigation" aria-label="開啟主要導覽" @click="navigationOpen = !navigationOpen"><span /><span /><span /></button>
          <nav v-if="navigationOpen" id="main-navigation" class="main-nav" aria-label="主要導覽">
            <button type="button" @click="navigateTo('home')">首頁</button>
            <button type="button" @click="navigateTo('journals')">我的日記</button>
            <button type="button" @click="navigateTo('animation')">動畫導覽</button>
            <button v-if="authUser" type="button" @click="logout">登出</button>
          </nav>
        </div>
      </div>
    </header>

    <div class="map-path" aria-hidden="true">
      <span v-for="n in 8" :key="n" class="path-dot" :class="{ lit: stage >= n + 5 }" />
    </div>

    <Transition name="page" mode="out-in">
      <section v-if="stage === 0" key="welcome" class="scene welcome-scene">
        <div class="welcome-hero">
          <StoryCharactersImage scene="welcome" />
          <p class="eyebrow">EMBRACE YOUR INNER CRITIC</p>
          <h1>看見你的<br><em>內在批評者</em></h1>
          <p class="lead">你知道嗎？你是你想法背後的意識，你的想法不是你的身份與價值。</p>
          <p class="lead">透過覺察地圖練習，你可以慢慢分辨它的聲音。</p>
          <button class="primary" @click="hasCompletedOnboarding ? startJournal() : startJourney()">
            {{ hasCompletedOnboarding ? '直接開始寫日記' : '開始探索' }} <span>→</span>
          </button>
          <button v-if="hasCompletedOnboarding" class="text-button replay-link" @click="replayIntroduction">重新看前面的動畫</button>
          <button v-if="hasCompletedOnboarding && entries.length" class="text-button replay-link" @click="stage = 6">查看我的日記</button>
          <a class="welcome-more" href="#about-product">先了解這個網站 <span aria-hidden="true">↓</span></a>
        </div>

        <div id="about-product" class="welcome-content">
          <article class="welcome-section origin-section">
            <p class="section-kicker">為什麼做這個產品</p>
            <h2>把難以整理的內在經驗，<br>變成一步步看見的線索。</h2>
            <div class="prose-columns">
              <p>這個產品源自於自身心理諮商經驗啟悟與閱讀《自我批評也是愛，但不是我們要的愛》後的思考。一般筆記工具提供了記錄空間，卻不一定能幫助我們分開看見：發生了什麼、腦中出現什麼話，以及這些批評如何影響我們的情緒與決定。</p>
              <p>因此，我想創造一個低干擾、有清楚步驟的自我覺察空間。我們沒辦法立刻想通所有事情，而是從一次次具體的紀錄開始，再透過慢慢地檢視、辨認其中反覆出現的模式。</p>
            </div>
          </article>

          <article class="welcome-section pattern-section">
            <div class="section-heading">
              <div>
                <p class="section-kicker">自我批判的聲音是從何而來?</p>
                <h2>有些內耗，可能是過去的保護方式仍在運作。</h2>
              </div>
              <p>在原生家庭、學校、同儕或其他權威關係中，我們可能逐漸學會用監督、責備、要求完美或預演失敗來避免犯錯、衝突、拒絕與懲罰。這不是唯一的形成原因，也不能代表每個人都有相同經歷。</p>
            </div>
            <ol class="pattern-path" aria-label="內在批評者可能形成與影響的過程">
              <li><span>01</span><strong>過去經驗</strong><p>批評、權威、條件式肯定，或缺乏安全感的環境。</p></li>
              <li><span>02</span><strong>保護方式</strong><p>透過責備、催促或預想最壞情況，試著保護自己避免再次受傷。</p></li>
              <li><span>03</span><strong>聲音內化</strong><p>反覆出現的聲音變得熟悉，甚至被認成自己的判斷。</p></li>
              <li><span>04</span><strong>影響現在</strong><p>它開始參與情緒、行為、關係與日常決定。</p></li>
            </ol>
          </article>

          <article class="welcome-section method-section">
            <p class="section-kicker">這個網站會如何幫助你？</p>
            <h2>依循五個步驟，練習把每個反芻思想放回事實中檢查。</h2>
            <p class="section-intro">完整的練習方向包含五個階段。目前網站先提供「識別」與「檢視」的引導日記；其餘三個階段是後續發展方向，尚在開發流程。</p>
            <ol class="method-path">
              <li class="is-available">
                <span class="method-number">01</span>
                <div><span class="status-label">目前提供</span><h3>識別</h3><p>在生活中仔細留意內在批評者在什麼情況下會出現，並抓住他實際說出的具體內容，把它拖到陽光下，很多憑空製造的恐懼就會見光死。</p></div>
              </li>
              <li class="is-available">
                <span class="method-number">02</span>
                <div><span class="status-label">目前提供</span><h3>檢視</h3><p>分開記錄事件、批評性想法、情緒、行為，以及可能讓你聯想到的較早經驗。</p></div>
              </li>
              <li>
                <span class="method-number">03</span>
                <div><span class="status-label">尚在開發</span><h3>質問</h3><p>核對這句批評說得有道理嗎？正不正確？有什麼證據？以及它是否符合完整事實。</p></div>
              </li>
              <li>
                <span class="method-number">04</span>
                <div><span class="status-label">尚在開發</span><h3>重新定位</h3><p>改變人生唯一的方法，就是改變看待自己的方式。如果面對相同處境的是你的朋友，你會如何同理、回應並且支持他？</p></div>
              </li>
              <li>
                <span class="method-number">05</span>
                <div><span class="status-label">尚在開發</span><h3>和解</h3><p>重新理解過去形成的保護方式，逐步調整現在看待與對待自己的方法，坦然接受你的一切，好的不好的都接受。</p></div>
              </li>
            </ol>
          </article>

          <article class="welcome-section pain-section">
            <p class="section-kicker">想解決的困難</p>
            <h2>道理我都懂，真正困難的不是「沒有地方寫」，而是......</h2>
            <div class="pain-grid">
              <section><span>01</span><h3>不知道如何開始</h3><p>想整理經驗，卻不知道該問自己什麼，也很難在情緒升高時組織內容。</p></section>
              <section><span>02</span><h3>把想法當成事實</h3><p>批評性的自動想法太熟悉，容易被直接認成客觀判斷或自身價值。</p></section>
              <section><span>03</span><h3>看不見重複模式</h3><p>記錄散落在不同地方，觸發因素、情緒與保護反應之間的關係不容易被看見。</p></section>
              <section><span>04</span><h3>整理成本太高</h3><p>自行建立模板、分類、標籤與回顧方式，需要額外時間與持續維護。</p></section>
              <section><span>05</span><h3>原始感受被修飾</h3><p>生成式 AI 可以讓文字更流暢，卻也可能蓋過原本混亂、矛盾但重要的線索。</p></section>
              <section><span>06</span><h3>練習變成另一種要求</h3><p>字數、連續天數與完成率，可能讓自我覺察再次變成需要達成的標準。</p></section>
            </div>
          </article>

          <article class="welcome-section boundary-section">
            <div>
              <p class="section-kicker">使用界線</p>
              <h2>這是一項自我覺察與紀錄工具。</h2>
            </div>
            <ul>
              <li>不提供心理診斷、心理治療、醫療建議或危機介入。</li>
              <li>不替你判定童年原因、創傷類型、他人意圖或記憶真偽。</li>
              <li>每一題都可以跳過；你也可以中途停止，只保留今天能承受的部分。</li>
              <li>你的原始文字不由 AI 代寫、改寫或補完。</li>
            </ul>
          </article>

          <article class="welcome-section closing-section">
            <div>
                <p class="section-kicker">從一次次地看見開始，一起慢慢改變</p>
                <h2>你不需要一開始就寫得很完整。</h2>
            </div>

            <p>如果這個工具對你有幫助，可以把它分享給可能需要的人。如果使用後仍有餘韻，也可以支持後續開發。</p>
            <div class="closing-actions">
              <button class="primary" @click="hasCompletedOnboarding ? startJournal() : startJourney()">
                {{ hasCompletedOnboarding ? '直接開始寫日記' : '開始探索' }} <span>→</span>
              </button>
              <button class="secondary" type="button" @click="shareSite">分享這個網站</button>
              <button class="support-pending" type="button" disabled title="提供開發者的 Buy Me a Coffee 網址後即可啟用">Buy Me a Coffee · 連結準備中</button>
            </div>
            <p v-if="shareMessage" class="share-message" role="status">{{ shareMessage }}</p>
          </article>
        </div>
      </section>

      <section v-else-if="stage === 1" key="sound" class="scene compact-scene">
        <div class="sound-orbit"><span>♪</span><i /><i /><i /></div>
        <p class="eyebrow">在開始以前</p>
        <h2>想帶著一點音樂<br>一起走嗎？</h2>
        <p class="lead">你可以隨時暫停，安靜地寫也很好。</p>
        <div class="button-stack">
          <button class="primary" @click="confirmSound(true)">播放背景音樂</button>
          <button class="secondary" @click="confirmSound(false)">我想安靜地使用</button>
          <label class="effect-choice"><input v-model="effectsOn" type="checkbox"> 開啟按鈕小音效</label>
        </div>
      </section>

      <section v-else-if="stage === 2" :key="`lesson-one-${lessonOne}`" class="scene lesson-scene">
        <p class="eyebrow">認識那個熟悉的聲音 · {{ lessonOne + 1 }}/3</p>
        <div class="character-stage" :class="`lesson-${lessonOne}`">
          <StoryCharactersImage :scene="lessonOne === 0 ? 'approach' : lessonOne === 1 ? 'talk' : 'separate'" />
          <template v-if="lessonOne >= 1">
            <span class="speech s1">你怎麼又搞砸了</span><span class="speech s2">還不夠好</span>
            <span class="speech s3">不可以停下來</span><span class="speech s4">沒有人想聽你說話</span>
            <span class="speech s5">你那麼懶惰，做不到的</span><span class="speech s6">你總是讓人失望</span>
          </template>
          <div v-if="lessonOne === 2" class="breathing-space">這裡，多了一點空間</div>
        </div>
        <h2 v-if="lessonOne === 0">他總是在旁邊</h2>
        <h2 v-else-if="lessonOne === 1">他說的話，大部分都很 mean</h2>
        <h2 v-else>先把「他的聲音」和「我」分開</h2>
        <p v-if="lessonOne === 0" class="lead narrow">有時候，我們心裡會出現一個很熟悉的聲音。當我們休息、犯錯或不知道下一步時，他就急著催促我們。</p>
        <p v-else-if="lessonOne === 1" class="lead narrow">那些話很尖銳，卻熟悉得像是我們自己的聲音。</p>
        <p v-else class="lead narrow">一個想法出現在腦中，不代表它就是事實，也不代表它是完整的你。我們先練習認出：喔，原來又是他在說話。</p>
        <button class="primary" @click="advanceLessonOne">{{ lessonOne < 2 ? '繼續看看' : '替他取個名字' }} <span>→</span></button>
      </section>

      <section v-else-if="stage === 3" key="name" class="scene compact-scene">
        <StoryCharactersImage scene="name" compact />
        <p class="eyebrow">讓聲音變得可辨認</p>
        <h2>如果要替這個聲音取一個名字，<br>你想叫他什麼？</h2>
        <p class="lead narrow">替他取名，不是為了趕走他，只是幫助我們更容易知道現在是誰在說話。</p>
        <label class="name-field">
          <span>我的內在批評者叫做</span>
          <input v-model="criticName" maxlength="16" placeholder="例如：山姆" @keyup.enter="confirmCriticName">
          <small>之後可以修改。</small>
        </label>
        <button class="primary" @click="confirmCriticName">就叫這個名字 <span>→</span></button>
      </section>

      <section v-else-if="stage === 4" :key="`lesson-two-${lessonTwo}`" class="scene lesson-scene">
        <p class="eyebrow">改變從看見開始 · {{ lessonTwo + 1 }}/2</p>
        <div class="road-stage" :class="{ illuminated: lessonTwo === 1 }">
          <div class="old-road"/><div class="new-road"/><div class="lamp">✦</div>
        </div>
        <h2 v-if="lessonTwo === 0">熟悉，不等於不能改變</h2>
        <h2 v-else>今天只需要看見一次</h2>
        <p v-if="lessonTwo === 0" class="lead narrow">一個想法出現很多次，我們的身體為了節能，聰明地學會讓這條路變得很熟悉、很自動。但熟悉，不代表它永遠不能改變。</p>
        <p v-else class="lead narrow">每一次停下來，看見「他現在又說了什麼」，都在替自己多留一點選擇的空間。我們不用著急地一下子改變所有事情。</p>
        <button class="primary" @click="advanceLessonTwo">{{ lessonTwo === 0 ? '照亮另一條路' : '開始第一次覺察練習' }} <span>→</span></button>
        <button v-if="lessonTwo === 1" class="text-button" @click="stage = 0">今天先到這裡</button>
      </section>

      <section v-else-if="stage === 5" key="ready" class="scene compact-scene">
        <div class="blank-node"><span>01</span></div>
        <p class="eyebrow">第一個節點</p>
        <h2>開始前，先看看現在的自己。</h2>
        <p class="lead narrow">你可以隨時跳過、返回或停下來，已經寫下的部分仍然有意義。</p>
        <button class="primary" @click="startJournal">開始吧！ <span>→</span></button>
        <button class="text-button" @click="stage = 0">今天先不寫</button>
        <button class="help-link" @click="stage = 15">我現在需要協助</button>
      </section>

      <section v-else-if="stage === 6" key="journal-home" class="scene journal-home-scene">
        <p class="eyebrow">我的日記</p>
        <h2>從今天想看見的地方開始。</h2>
        <p class="lead narrow">每一篇都可以慢慢寫、之後再回來修改。</p>
        <p v-if="authMessage" class="auth-message">{{ authMessage }}</p>
        <button class="primary" @click="beginNewJournal">寫一篇新日記 <span>→</span></button>
        <div v-if="entries.length" class="entry-list">
          <article v-for="entry in entries" :key="entry.id" class="entry-card" @click="openEntry(entry)">
            <div>
              <span>{{ formatEntryDate(entry.createdAt) }} · {{ entry.isComplete ? '已完成' : '草稿' }}</span>
              <h3>{{ entry.criticName }} 說：「{{ entryPreview(entry) }}」</h3>
            </div>
            <div class="entry-actions">
              <button type="button" class="secondary" @click.stop="openEntry(entry)">閱讀／修改</button>
              <button type="button" class="delete-button" @click.stop="deleteEntry(entry.id)">刪除</button>
            </div>
          </article>
        </div>
        <p v-else class="empty-journals">第一篇不需要寫得完整，從一個當下的聲音開始就好。</p>
        <button class="text-button" @click="stage = 0">回到歡迎頁</button>
      </section>

      <section v-else-if="stage >= 7 && stage <= 12" :key="`question-${stage}`" class="scene journal-scene">
        <div class="journal-topline">
          <button class="back-button" @click="stage--">← 返回</button>
          <span>{{ currentQuestion.eyebrow }}</span>
          <button class="save-link" @click="saveAndLeave">儲存並離開</button>
        </div>
        <div v-if="stage > 8 && criticQuote" class="critic-note">
          <span>{{ confirmedName }} 剛才說</span>
          「{{ criticQuote }}」
        </div>
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
          <button class="primary" @click="stage === 12 ? stage = 13 : next(stage === 8 ? 'paper' : 'tap')">{{ stage === 12 ? '看看我的這段地圖' : '下一步' }} <span>→</span></button>
        </div>
        <button class="help-link inline-help" @click="stage = 15">我現在需要協助</button>
      </section>

      <section v-else-if="stage === 13" key="review" class="scene review-scene">
        <p class="eyebrow">回顧</p>
        <h2>這是你今天看見的一小段地圖。</h2>
        <p class="lead">你的原始文字會保持原樣。</p>
        <p v-if="authMessage" class="auth-message">{{ authMessage }}</p>
        <div class="review-grid">
          <article v-for="question in questions" :key="question.key">
            <span>{{ question.eyebrow }}</span>
            <p>{{ displayAnswer(question.key) }}</p>
          </article>
        </div>
        <div class="review-actions">
          <button class="secondary" @click="stage = 7">返回修改</button>
          <button class="primary" @click="finishJournal">{{ editingEntryId ? '儲存修改' : '儲存這篇日記' }} <span>→</span></button>
        </div>
      </section>

      <section v-else-if="stage === 14" key="complete" class="scene complete-scene">
        <div class="lit-node"><span>✦</span><i/><i/><i/></div>
        <p class="eyebrow">第一個節點已被看見</p>
        <h2>你不需要一次<br>走完整張地圖。</h2>
        <p class="lead narrow">今天，你已經看見了一個原本很容易被忽略的聲音。</p>
        <button class="primary" @click="stage = 6">回到我的日記 <span>→</span></button>
        <button class="text-button" @click="stage = 6">關閉今天的練習</button>
      </section>

      <section v-else-if="stage === 16" key="login" class="scene compact-scene">
        <div class="blank-node"><span>↗</span></div>
        <p class="eyebrow">寫日記前</p>
        <h2>先登入，才可以把<br>這一頁留給自己。</h2>
        <p class="lead narrow">你的日記會只屬於登入的帳號；完成登入後，就可以建立、修改、查看與刪除自己的日記。</p>
        <p v-if="authMessage" class="auth-message">{{ authMessage }}</p>
        <button class="primary" @click="startGoogleLogin">使用 Google 登入 <span>→</span></button>
        <button class="text-button" @click="stage = hasCompletedOnboarding ? 0 : 5">先回去看看</button>
      </section>

      <section v-else-if="stage === 15" key="safety" class="scene safety-scene">
        <div class="safety-mark">♡</div>
        <p class="eyebrow">先照顧現在的你</p>
        <h2>我們先停在這裡，<br>現在的安全比完成日記更重要。</h2>
        <p class="lead narrow">這個網站無法判斷你現在是否安全，也無法提供即時救援。如果你可能立刻傷害自己或別人，請先不要獨自承受。</p>
        <div class="safety-options">
          <a href="tel:119"><strong>撥打 119</strong><span>緊急救護</span></a>
          <a href="tel:110"><strong>撥打 110</strong><span>需要警方立即協助</span></a>
          <a href="tel:1925"><strong>撥打 1925</strong><span>24 小時安心專線</span></a>
        </div>
        <button class="secondary" @click="stage = 0">保存草稿並回到首頁</button>
        <p class="safety-footnote">文字提示可能誤判或漏掉真正的危險。你不需要等網站提醒，任何時候都可以主動尋求協助。</p>
      </section>
    </Transition>
  </main>
</template>
