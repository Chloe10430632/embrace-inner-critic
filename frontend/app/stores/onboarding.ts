export const useOnboardingStore = defineStore('onboarding', () => {
  const stage = ref(1)
  const lessonOne = ref(0)
  const lessonTwo = ref(0)
  const criticName = ref('')
  const pendingName = ref('')
  const confirmedName = computed(() => useAuthStore().user?.criticName || pendingName.value || '山姆')
  const completed = ref(false)
  const initialized = ref(false)

  function initialize() {
    if (!import.meta.client || initialized.value) return
    initialized.value = true
    completed.value = localStorage.getItem('embrace-inner-critic:onboarding-complete') === 'true'
  }

  function restart() {
    lessonOne.value = 0
    lessonTwo.value = 0
    stage.value = 1
  }

  function clearPendingName() {
    pendingName.value = ''
    criticName.value = ''
  }

  async function confirmName() {
    if (await setName(criticName.value.trim() || '山姆')) stage.value = 4
  }

  async function setName(name: string) {
    const nextName = name.trim()
    if (!nextName || nextName.length > 64) return false
    const auth = useAuthStore()
    if (auth.user) await auth.updateCriticName(nextName)
    else pendingName.value = nextName
    criticName.value = nextName
    return true
  }

  function markCompleted() {
    completed.value = true
    if (import.meta.client) localStorage.setItem('embrace-inner-critic:onboarding-complete', 'true')
  }

  return { stage, lessonOne, lessonTwo, criticName, pendingName, confirmedName, completed, initialize, restart, clearPendingName, confirmName, setName, markCompleted }
})
