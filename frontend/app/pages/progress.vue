<script setup lang="ts">
const ready = ref(false)
const signedIn = ref(false)
const auth = useAuthStore()
const journalStore = useJournalStore()
const { entries, message } = storeToRefs(journalStore)
const { loadEntries, formatDate } = journalStore
const { startGoogleLogin } = auth

const completedEntries = computed(() => entries.value.filter(entry => entry.isComplete))

function answer(entry: typeof entries.value[number], key: keyof typeof entry.answers) {
  const value = entry.answers[key]
  if (Array.isArray(value)) return value.length ? value.map(item => `#${item}`).join('　') : '尚未記錄'
  return typeof value === 'string' && value.trim() ? value : '尚未記錄'
}

onMounted(async () => {
  signedIn.value = await auth.requireUser()
  if (signedIn.value) await loadEntries()
  ready.value = true
})
</script>

<template>
  <section v-if="!ready" class="scene compact-scene" aria-live="polite">
    <p class="eyebrow">追蹤進展</p>
    <h2>正在整理你的進展……</h2>
  </section>

  <section v-else-if="!signedIn" class="scene compact-scene">
    <div class="blank-node"><span>↗</span></div>
    <p class="eyebrow">追蹤進展</p>
    <h2>登入後，才能看見<br>自己的轉化歷程。</h2>
    <p class="lead narrow">這裡會整理每篇日記的自我批評、感受、重新框架與行動。</p>
    <button class="primary" @click="startGoogleLogin">使用 Google 登入 <span>→</span></button>
    <NuxtLink class="text-button" to="/">先回到首頁</NuxtLink>
  </section>

  <section v-else class="scene progress-overview-scene">
    <div class="progress-overview-heading">
      <div>
        <!-- <p class="eyebrow">追蹤進展</p> -->
        <h2>把反覆出現的聲音，<br>和你走過的路放在一起看。</h2>
        <p class="lead">每一列都是一次從自我批評，到重新理解並採取行動的歷程。</p>
      </div>
      <NuxtLink class="secondary" to="/journals">回到我的日記</NuxtLink>
    </div>

    <p v-if="message" class="auth-message">{{ message }}</p>

    <div v-if="completedEntries.length" class="progress-table-wrap">
      <table class="progress-table">
        <thead>
          <tr>
            <th class="progress-date-cell">日期</th>
            <th class="progress-context-cell">當時的情境</th>
            <th class="progress-thought-cell">自我批評</th>
            <th class="progress-emotion-cell">負面情緒／行為</th>
            <th class="progress-reframe-cell">重新框架</th>
            <th class="progress-action-cell">採取的行動</th>
            <th class="progress-after-cell">行動之後的情緒</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="entry in completedEntries" :key="entry.id">
            <td class="progress-date-cell">{{ formatDate(entry.createdAt) }}</td>
            <td class="progress-context-cell">{{ answer(entry, 'trigger') }}</td>
            <td class="progress-thought-cell">「{{ answer(entry, 'critic') }}」</td>
            <td class="progress-emotion-cell">
              <strong>情緒</strong><br>{{ answer(entry, 'emotions') }}<br><br>
              <strong>行為</strong><br>{{ answer(entry, 'behaviors') }}
            </td>
            <td class="progress-reframe-cell">{{ answer(entry, 'reframedThought') }}</td>
            <td class="progress-action-cell">{{ answer(entry, 'nextAction') }}</td>
            <td class="progress-after-cell">
              {{ answer(entry, 'afterActionEmotion') }}
              <NuxtLink class="progress-row-action" :to="`/journals?progress=${entry.id}`">{{ entry.answers.afterActionEmotion ? '查看／修改' : '繼續追蹤 →' }}</NuxtLink>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-else class="progress-empty">
      <p>完成第一篇日記後，就可以從那篇日記接著記錄重新框架與小行動。</p>
      <NuxtLink class="primary" to="/journals">前往我的日記 <span>→</span></NuxtLink>
    </div>
  </section>
</template>
