export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: false },
  css: ['~/assets/css/main.css'],
  app: {
    head: {
      htmlAttrs: { lang: 'zh-Hant' },
      title: '擁抱內在批評者',
      meta: [
        { name: 'description', content: '一段溫柔、低干擾的內在批評者覺察練習。' },
        { name: 'theme-color', content: '#f6faf4' }
      ],
      link: [
        { rel: 'preconnect', href: 'https://fonts.googleapis.com' },
        { rel: 'preconnect', href: 'https://fonts.gstatic.com', crossorigin: '' },
        { rel: 'stylesheet', href: 'https://fonts.googleapis.com/css2?family=Noto+Sans+TC:wght@400;500;600;700&display=swap' }
      ]
    }
  }
})
