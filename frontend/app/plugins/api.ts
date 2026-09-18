let csrfRequest: Promise<string> | null = null

export default defineNuxtPlugin(() => {
  const config = useRuntimeConfig()
  let csrfToken = ''

  async function getCsrfToken() {
    if (csrfToken) return csrfToken

    csrfRequest ??= $fetch<{ requestToken: string }>('/api/auth/csrf', {
      baseURL: config.public.apiBase,
      credentials: 'include'
    }).then(response => response.requestToken)

    try {
      csrfToken = await csrfRequest
      return csrfToken
    } finally {
      csrfRequest = null
    }
  }

  const api = $fetch.create({
    baseURL: config.public.apiBase,
    credentials: 'include',
    async onRequest({ options }) {
      const method = String(options.method ?? 'GET').toUpperCase()
      if (!['POST', 'PUT', 'PATCH', 'DELETE'].includes(method)) return

      const headers = new Headers(options.headers)
      headers.set('X-CSRF-TOKEN', await getCsrfToken())
      options.headers = headers
    },
    onResponseError({ response }) {
      if (response.status === 401) {
        csrfToken = ''
        useAuthStore().clearSession()
      }
    }
  })

  return { provide: { api } }
})
