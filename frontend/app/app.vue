<script setup lang="ts">
type JournalKey = 'trigger' | 'critic' | 'emotions' | 'behaviors' | 'origin' | 'reply'

const stage = ref(0)
const lessonOne = ref(0)
const lessonTwo = ref(0)
const criticName = ref('')
const confirmedName = ref('山姆')
const soundOn = ref(true)
const effectsOn = ref(true)
const soundPanel = ref(false)
const customEmotion = ref('')
const customBehavior = ref('')
const completed = ref(false)

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
let ambientGain: GainNode | null = null
let ambientNodes: OscillatorNode[] = []

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
  const ctx = ensureAudio()
  if (!ctx || ambientGain) return
  ambientGain = ctx.createGain()
  ambientGain.gain.setValueAtTime(0.0001, ctx.currentTime)
  ambientGain.gain.exponentialRampToValueAtTime(0.018, ctx.currentTime + 1.5)
  ambientGain.connect(ctx.destination)
  ambientNodes = [196, 246.94, 293.66].map((frequency, index) => {
    const osc = ctx.createOscillator()
    const noteGain = ctx.createGain()
    osc.type = 'sine'
    osc.frequency.value = frequency
    noteGain.gain.value = index === 0 ? 0.55 : 0.22
    osc.connect(noteGain).connect(ambientGain!)
    osc.start()
    return osc
  })
}

function stopAmbient() {
  if (!audioContext || !ambientGain) return
  const gain = ambientGain
  gain.gain.cancelScheduledValues(audioContext.currentTime)
  gain.gain.setValueAtTime(Math.max(gain.gain.value, 0.0001), audioContext.currentTime)
  gain.gain.exponentialRampToValueAtTime(0.0001, audioContext.currentTime + 0.6)
  window.setTimeout(() => {
    ambientNodes.forEach(node => node.stop())
    ambientNodes = []
    gain.disconnect()
  }, 650)
  ambientGain = null
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
  if (soundOn.value) startAmbient()
  playEffect()
  stage.value = 1
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

function useHardReply() {
  journal.reply = '這真的很難，我還不知道要怎麼回答。'
  playEffect('paper')
}

function finishJournal() {
  completed.value = true
  stage.value = 14
  playEffect('complete')
}

function displayAnswer(key: JournalKey) {
  const value = journal[key]
  return Array.isArray(value) ? value.map(item => `#${item}`).join('　') || '今天沒有回答' : value || '今天沒有回答'
}

onBeforeUnmount(() => stopAmbient())
</script>

<template>
  <main class="app-shell" :class="{ 'is-complete': completed }">
    <div class="mist mist-one" />
    <div class="mist mist-two" />

    <header class="topbar">
      <button class="brand" @click="stage = 0">擁抱內在批評者</button>
      <div class="sound-wrap">
        <button class="icon-button" aria-label="聲音設定" @click="soundPanel = !soundPanel">
          {{ soundOn ? '♪' : '♩' }}
        </button>
        <div v-if="soundPanel" class="sound-panel">
          <label><input v-model="soundOn" type="checkbox"> 背景聲景</label>
          <label><input v-model="effectsOn" type="checkbox"> 按鈕小音效</label>
        </div>
      </div>
    </header>

    <div class="map-path" aria-hidden="true">
      <span v-for="n in 8" :key="n" class="path-dot" :class="{ lit: stage >= n + 5 }" />
    </div>

    <Transition name="page" mode="out-in">
      <section v-if="stage === 0" key="welcome" class="scene welcome-scene">
        <StoryCharacters scene="welcome" />
        <p class="eyebrow">EMBRACE YOUR INNER CRITIC</p>
        <h1>歡迎來到<br><em>你的內在地圖。</em></h1>
        <p class="lead">這裡沒有標準答案，也不需要急著一次把所有事情想清楚。</p>
        <p class="lead">我們只從一個你最近聽見的聲音開始。</p>
        <button class="primary" @click="startJourney">開始探索 <span>→</span></button>
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
          <StoryCharacters :scene="lessonOne === 0 ? 'approach' : lessonOne === 1 ? 'talk' : 'separate'" />
          <template v-if="lessonOne === 1">
            <span class="speech s1">你怎麼又搞砸了</span><span class="speech s2">還不夠好</span>
            <span class="speech s3">不可以停下來</span><span class="speech s4">你總是讓人失望</span>
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
        <StoryCharacters scene="name" compact />
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
        <button class="primary" @click="stage = 7; playEffect()">開始吧！ <span>→</span></button>
        <button class="text-button" @click="stage = 0">今天先不寫</button>
        <button class="help-link" @click="stage = 15">我現在需要協助</button>
      </section>

      <section v-else-if="stage >= 7 && stage <= 12" :key="`question-${stage}`" class="scene journal-scene">
        <div class="journal-topline">
          <button class="back-button" @click="stage--">← 返回</button>
          <span>{{ currentQuestion.eyebrow }}</span>
          <button class="save-link" @click="stage = 0">儲存並離開</button>
        </div>
        <div v-if="stage > 8 && criticQuote" class="critic-note">
          <span>{{ confirmedName }} 剛才說</span>
          「{{ criticQuote }}」
        </div>
        <h2>{{ displayTitle }}</h2>
        <p class="lead narrow">{{ currentQuestion.help }}</p>

        <div v-if="currentQuestion.key === 'emotions' || currentQuestion.key === 'behaviors'" class="tag-area">
          <div class="tag-cloud">
            <button v-for="tag in currentQuestion.key === 'emotions' ? emotions : behaviors" :key="tag" class="tag" :class="{ selected: (journal[currentQuestion.key] as string[]).includes(tag) }" @click="toggleTag(currentQuestion.key as 'emotions' | 'behaviors', tag)">#{{ tag }}</button>
          </div>
          <form class="custom-tag" @submit.prevent="addCustomTag(currentQuestion.key as 'emotions' | 'behaviors')">
            <input v-if="currentQuestion.key === 'emotions'" v-model="customEmotion" placeholder="加入自己的感受">
            <input v-else v-model="customBehavior" placeholder="加入自己的行為">
            <button>＋ 加入</button>
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
        <div class="review-grid">
          <article v-for="question in questions" :key="question.key">
            <span>{{ question.eyebrow }}</span>
            <p>{{ displayAnswer(question.key) }}</p>
          </article>
        </div>
        <div class="review-actions">
          <button class="secondary" @click="stage = 7">返回修改</button>
          <button class="primary" @click="finishJournal">儲存這篇日記 <span>→</span></button>
        </div>
      </section>

      <section v-else-if="stage === 14" key="complete" class="scene complete-scene">
        <div class="lit-node"><span>✦</span><i/><i/><i/></div>
        <p class="eyebrow">第一個節點已被看見</p>
        <h2>你不需要一次<br>走完整張地圖。</h2>
        <p class="lead narrow">今天，你已經看見了一個原本很容易被忽略的聲音。</p>
        <button class="primary" @click="stage = 0">回到我的地圖 <span>→</span></button>
        <button class="text-button" @click="stage = 0">關閉今天的練習</button>
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
