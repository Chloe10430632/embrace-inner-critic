import type { JournalAnswers, JournalEntry, JournalKey, JournalQuestion } from '~/types/journal'

const emotions = ['羞恥', '焦慮', '害怕', '憤怒', '難過', '挫折', '孤單', '無力', '麻木']
const behaviors = ['逃避', '討好', '拖延', '反擊', '僵住', '過度工作', '反覆確認', '責怪自己', '放棄']

export const journalQuestions: JournalQuestion[] = [
  { key: 'trigger', eyebrow: '01 · 情境', title: '什麼情況下，{name} 出現了？', help: '寫下是甚麼情境觸發了內在批評者，不需要分析原因，很簡短也可以！', placeholder: '例如：主管問我為什麼還沒完成工作……' },
  { key: 'critic', eyebrow: '02 · 自我批評', title: '寫下 {name} 對你說的具體內容', help: '一次只寫一句最常在你腦中響起的自我批判的想法。 照原本的樣子寫下來，不用替它修飾。', placeholder: '他說：「你時間管理真的有問題……」' },
  { key: 'emotions', eyebrow: '03 · 情緒', title: '當這句話出現，你感受到什麼？', help: '可以選很多個，最好可以用自己的方式形容。' },
  { key: 'behaviors', eyebrow: '04 · 行為', title: '把這個想法所引發「最重大」的行為記錄下來', help: '不需要判斷好壞，我們要看看這個想法如何影響你的行為決策。' },
  { key: 'origin', eyebrow: '05 · 種子', title: '這個種子，是怎麼形成的呢？', help: '回想你第一次遇到這個自我批評的想法的時候，是什麼樣的情況導致你有這些想法？也許來自家庭、學校、老師、同儕或其他關係。也可能暫時想不到。', placeholder: '它讓我想到……' },
  { key: 'reply', eyebrow: '06 · 重新框架', title: '假設你最要好的朋友聽見 {name} 說的話，想想他們會怎麼說？', help: '一個真正關心你的好朋友，聽見那些錯誤的評價，會如何提供你善意和支持。', placeholder: '我想對自己說……' }
]

export const useJournalStore = defineStore('journal', () => {
  const stage = ref(6)
  const entries = ref<JournalEntry[]>([])
  const editingEntryId = ref<string | null>(null)
  const completed = ref(false)
  const message = ref('')
  const customEmotion = ref('')
  const customBehavior = ref('')
  const draft = reactive<JournalAnswers>({
    trigger: '',
    critic: '',
    emotions: [],
    behaviors: [],
    origin: '',
    reply: '',
    reframedThought: '',
    nextAction: '',
    afterActionEmotion: ''
  })
  const onboarding = useOnboardingStore()
  const auth = useAuthStore()
  const { playEffect } = useAudio()

  const currentQuestion = computed(() => journalQuestions[stage.value - 7] ?? journalQuestions[0]!)
  const displayTitle = computed(() => currentQuestion.value.title.replace('{name}', onboarding.confirmedName))
  const criticQuote = computed(() => String(draft.critic || '').trim())

  function resetStore() {
    entries.value = []
    editingEntryId.value = null
    completed.value = false
    stage.value = 6
    message.value = ''
  }

  function resetDraft() {
    for (const key of Object.keys(draft) as JournalKey[]) draft[key] = key === 'emotions' || key === 'behaviors' ? [] : ''
    customEmotion.value = ''
    customBehavior.value = ''
    editingEntryId.value = null
    completed.value = false
  }

  async function loadEntries() {
    const { $api } = useNuxtApp()
    try {
      const loaded: JournalEntry[] = []
      for (let page = 1; page <= 10; page++) {
        const pageEntries = await $api<JournalEntry[]>(`/api/diary-entries?page=${page}&pageSize=50`)
        loaded.push(...pageEntries)
        if (pageEntries.length < 50) break
      }
      entries.value = loaded
    } catch {
      entries.value = []
      message.value = '無法讀取日記，請確認後端服務是否已啟動。'
    }
  }

  async function requireAccess() {
    if (await auth.requireUser()) return true
    message.value = ''
    stage.value = 16
    return false
  }

  async function beginNew() {
    if (!await requireAccess()) return
    resetDraft()
    playEffect()
    stage.value = 7
  }

  function cloneAnswers() {
    return Object.fromEntries(Object.entries(draft).map(([key, value]) => [key, Array.isArray(value) ? [...value] : value])) as JournalAnswers
  }

  async function save(isComplete: boolean, successStage = isComplete ? 14 : 6) {
    const { $api } = useNuxtApp()
    message.value = ''
    try {
      const entry = await $api<JournalEntry>(editingEntryId.value ? `/api/diary-entries/${editingEntryId.value}` : '/api/diary-entries', {
        method: editingEntryId.value ? 'PUT' : 'POST',
        body: { criticName: onboarding.confirmedName, answers: cloneAnswers(), isComplete }
      })
      const index = entries.value.findIndex(item => item.id === entry.id)
      index >= 0 ? entries.value.splice(index, 1, entry) : entries.value.unshift(entry)
      editingEntryId.value = entry.id
      completed.value = isComplete
      playEffect(isComplete ? 'complete' : 'paper')
      stage.value = successStage
      return true
    } catch {
      message.value = '日記目前無法儲存，請確認登入與後端服務後再試一次。'
      return false
    }
  }

  function open(entry: JournalEntry) {
    for (const key of Object.keys(draft) as JournalKey[]) {
      const answer = entry.answers[key]
      draft[key] = Array.isArray(answer) ? [...answer] : typeof answer === 'string' ? answer : key === 'emotions' || key === 'behaviors' ? [] : ''
    }
    onboarding.confirmedName = entry.criticName
    onboarding.criticName = entry.criticName
    editingEntryId.value = entry.id
    completed.value = entry.isComplete
    stage.value = 13
  }

  function openProgress(entry: JournalEntry) {
    open(entry)
    stage.value = 17
  }

  function beginProgress() {
    if (!editingEntryId.value) return
    stage.value = 17
  }

  async function remove(id: string) {
    const { $api } = useNuxtApp()
    message.value = ''
    try {
      await $api(`/api/diary-entries/${id}`, { method: 'DELETE' })
      entries.value = entries.value.filter(entry => entry.id !== id)
      playEffect('paper')
      return true
    } catch {
      message.value = '日記目前無法刪除，請稍後再試。'
      return false
    }
  }

  function toggleTag(key: 'emotions' | 'behaviors', tag: string) {
    const values = draft[key] as string[]
    const index = values.indexOf(tag)
    index >= 0 ? values.splice(index, 1) : values.push(tag)
    playEffect()
  }

  function addCustomTag(key: 'emotions' | 'behaviors') {
    const source = key === 'emotions' ? customEmotion : customBehavior
    const value = source.value.trim().replace(/^#/, '')
    if (!value) return
    const values = draft[key] as string[]
    if (!values.includes(value)) values.push(value)
    source.value = ''
    playEffect()
  }

  function availableTags(key: 'emotions' | 'behaviors') {
    const suggested = key === 'emotions' ? emotions : behaviors
    const selected = draft[key] as string[]
    return [...suggested, ...selected.filter(tag => !suggested.includes(tag))]
  }

  function useHardReply() {
    draft.reply = '這真的很難，我還不知道要怎麼回答。'
    playEffect('paper')
  }

  function hasContent() {
    return Object.values(draft).some(value => Array.isArray(value) ? value.length > 0 : value.trim().length > 0)
  }

  function formatDate(isoDate: string) {
    return new Intl.DateTimeFormat('zh-TW', { dateStyle: 'medium' }).format(new Date(isoDate))
  }

  function preview(entry: JournalEntry, key: 'trigger' | 'critic') {
    const text = String(entry.answers[key] || '').trim()
    return text ? `${text.slice(0, 30)}${text.length > 30 ? '…' : ''}` : '尚未記錄'
  }

  function displayAnswer(key: JournalKey) {
    const value = draft[key]
    return Array.isArray(value) ? value.map(item => `#${item}`).join('　') || '今天沒有回答' : value || '今天沒有回答'
  }

  return {
    stage, entries, editingEntryId, completed, message, customEmotion, customBehavior,
    draft, currentQuestion, displayTitle, criticQuote, resetStore, loadEntries, requireAccess,
    beginNew, save, open, openProgress, beginProgress, remove, toggleTag, addCustomTag, availableTags, useHardReply,
    hasContent, formatDate, preview, displayAnswer
  }
})
