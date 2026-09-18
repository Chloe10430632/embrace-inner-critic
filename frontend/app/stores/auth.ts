type AuthUser = { id: string; email: string | null }

export const useAuthStore = defineStore('auth', () => {
  const user = ref<AuthUser | null>(null)
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
    window.location.assign(config.public.apiBase + '/api/auth/google')
  }

  async function logout() {
    const { $api } = useNuxtApp()
    try { await $api('/api/auth/logout', { method: 'POST' }) }
    finally {
      clearSession()
      useJournalStore().resetStore()
      await navigateTo('/')
    }
  }

  return { user, checked, clearSession, checkSession, requireUser, startGoogleLogin, logout }
})
