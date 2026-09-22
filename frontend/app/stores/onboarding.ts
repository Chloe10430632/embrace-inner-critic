export const useOnboardingStore = defineStore('onboarding', () => {
  const stage = ref(1)
  const lessonOne = ref(0)
  const lessonTwo = ref(0)
  const criticName = ref('')
  const confirmedName = ref('山姆')
  const completed = ref(false)
  const initialized = ref(false)

  function initialize() {
    if (!import.meta.client || initialized.value) return
    initialized.value = true
    completed.value = localStorage.getItem('embrace-inner-critic:onboarding-complete') === 'true'
    const storedName = localStorage.getItem('embrace-inner-critic:critic-name')
    if (storedName) {
      criticName.value = storedName
      confirmedName.value = storedName
    }
  }

  function restart() {
    lessonOne.value = 0
    lessonTwo.value = 0
    stage.value = 1
  }

  function confirmName() {
    setName(criticName.value.trim() || '山姆')
    stage.value = 4
  }

  function setName(name: string) {
    const nextName = name.trim()
    if (!nextName || nextName.length > 16) return false
    confirmedName.value = nextName
    criticName.value = nextName
    if (import.meta.client) localStorage.setItem('embrace-inner-critic:critic-name', nextName)
    return true
  }

  function markCompleted() {
    completed.value = true
    if (import.meta.client) localStorage.setItem('embrace-inner-critic:onboarding-complete', 'true')
  }

  return { stage, lessonOne, lessonTwo, criticName, confirmedName, completed, initialize, restart, confirmName, setName, markCompleted }
})
