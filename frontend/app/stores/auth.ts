type AuthUser = { id: string; email: string | null; criticName: string }

export const useAuthStore = defineStore('auth', () => {
  const user = ref<AuthUser | null>(null)

  //有沒有向後端確認過目前的登入狀態？
  //還沒查過就先查；查完後，再看 user 有沒有資料。
  const checked = ref(false)

  function clearSession() {
    user.value = null
  }

  async function checkSession() {
    const { $api } = useNuxtApp()
    try { user.value = await $api<AuthUser>('/api/auth/me') }
    catch { clearSession() }
    finally { checked.value = true }
  }

  async function requireUser() {
    if (!checked.value) await checkSession()
    return Boolean(user.value)
  }

  function startGoogleLogin() {
    if (!import.meta.client) return
    const config = useRuntimeConfig()
    const pendingName = useOnboardingStore().pendingName
    const route = useRouter().currentRoute.value
    const returnUrl = route.path === '/progress' ? '/progress'
      : route.query.new === '1' ? '/journals?new=1' : '/journals'
    const query = new URLSearchParams({ returnUrl })
    if (pendingName) query.set('criticName', pendingName)
    window.location.assign(config.public.apiBase + '/api/auth/google?' + query.toString())
  }

  async function updateCriticName(name: string) {
    const { $api } = useNuxtApp()
    const result = await $api<{ criticName: string }>('/api/auth/critic-name', {
      method: 'PUT',
      body: { criticName: name }
    })
    if (user.value) user.value.criticName = result.criticName
  }

  async function logout() {
    const { $api } = useNuxtApp()
    try { await $api('/api/auth/logout', { method: 'POST' }) }
    finally {
      clearSession()
      useOnboardingStore().clearPendingName()
      useJournalStore().resetStore()
      window.location.replace('/')
    }
  }

  return { user, checked, clearSession, checkSession, requireUser, startGoogleLogin, updateCriticName, logout }
})
