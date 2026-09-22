<script setup lang="ts">
const onboarding = useOnboardingStore()
const journal = useJournalStore()
const { completed: hasCompletedOnboarding } = storeToRefs(onboarding)
const { entries } = storeToRefs(journal)
const shareMessage = ref('')
const { soundOn, playEffect, startAmbient } = useAudio()

async function startJourney() {
  onboarding.restart()
  if (soundOn.value) startAmbient()
  playEffect()
  await navigateTo('/introduction')
}

async function startJournal() {
  onboarding.markCompleted()
  await navigateTo('/journals')
  await journal.beginNew()
}

async function shareSite() {
  if (!import.meta.client) return
  const data = { title: '內在批評者的覺察地圖', text: '用一步一題的方式，辨認內在批評者如何影響情緒、行為與決定。', url: window.location.origin }
  try {
    if (navigator.share) {
      await navigator.share(data)
      shareMessage.value = '分享視窗已開啟。'
    } else {
      await navigator.clipboard.writeText(data.url)
      shareMessage.value = '網站連結已複製。'
    }
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') return
    shareMessage.value = '目前無法自動分享，請直接複製瀏覽器網址。'
  }
}
</script>

<template>
  <section class="scene welcome-scene">
    <div class="welcome-hero">
      <StoryCharactersImage scene="welcome" />
      <p class="eyebrow">EMBRACE YOUR INNER CRITIC</p>
      <h1>看見你的<br><em>內在批評者</em></h1>
      <p class="lead">你知道嗎？你是你想法背後的意識，你的想法不是你的身份與價值。</p>
      <p class="lead">透過覺察地圖練習，你可以慢慢分辨它的聲音。</p>
      <button class="primary" @click="hasCompletedOnboarding ? startJournal() : startJourney()">
        {{ hasCompletedOnboarding ? '記錄覺察日記' : '開始探索' }} <span>→</span>
      </button>
      <!-- <NuxtLink v-if="hasCompletedOnboarding" class="text-button replay-link" to="/introduction">重新看動畫</NuxtLink> -->
      <!-- <Nuxt。ink v-if="hasCompletedOnboarding && entries.length" class="text-button replay-link" to="/journals">查看我的日記</Nuxt。ink> -->
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
          <div><p class="section-kicker">自我批判的聲音是從何而來?</p><h2>有些內耗，可能是過去的保護方式仍在運作。</h2></div>
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
          <li class="is-available"><span class="method-number">01</span><div><span class="status-label">目前提供</span><h3>識別</h3><p>在生活中仔細留意內在批評者在什麼情況下會出現，並抓住他實際說出的具體內容，把它拖到陽光下，很多憑空製造的恐懼就會見光死。</p></div></li>
          <li class="is-available"><span class="method-number">02</span><div><span class="status-label">目前提供</span><h3>檢視</h3><p>分開記錄事件、批評性想法、情緒、行為，以及可能讓你聯想到的較早經驗。</p></div></li>
          <li><span class="method-number">03</span><div><span class="status-label">尚在開發</span><h3>質問</h3><p>核對這句批評說得有道理嗎？正不正確？有什麼證據？以及它是否符合完整事實。</p></div></li>
          <li><span class="method-number">04</span><div><span class="status-label">尚在開發</span><h3>重新定位</h3><p>改變人生唯一的方法，就是改變看待自己的方式。如果面對相同處境的是你的朋友，你會如何同理、回應並且支持他？</p></div></li>
          <li><span class="method-number">05</span><div><span class="status-label">尚在開發</span><h3>和解</h3><p>重新理解過去形成的保護方式，逐步調整現在看待與對待自己的方法，坦然接受你的一切，好的不好的都接受。</p></div></li>
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
        <div><p class="section-kicker">使用界線</p><h2>這是一項自我覺察與紀錄工具。</h2></div>
        <ul>
          <li>不提供心理診斷、心理治療、醫療建議或危機介入。</li>
          <li>不替你判定童年原因、創傷類型、他人意圖或記憶真偽。</li>
          <li>每一題都可以跳過；你也可以中途停止，只保留今天能承受的部分。</li>
          <li>你的原始文字不由 AI 代寫、改寫或補完。</li>
        </ul>
      </article>

      <article class="welcome-section closing-section">
        <div><p class="section-kicker">從一次次地看見開始，一起慢慢改變</p><h2>你不需要一開始就寫得很完整。</h2></div>
        <p>如果這個工具對你有幫助，可以把它分享給可能需要的人。如果使用後仍有餘韻，也可以支持後續開發。</p>
        <div class="closing-actions">
          <button class="primary" @click="hasCompletedOnboarding ? startJournal() : startJourney()">{{ hasCompletedOnboarding ? '直接開始寫日記' : '開始探索' }} <span>→</span></button>
          <button class="secondary" type="button" @click="shareSite">分享這個網站</button>
          <button class="support-pending" type="button" disabled title="提供開發者的 Buy Me a Coffee 網址後即可啟用">Buy Me a Coffee · 連結準備中</button>
        </div>
        <p v-if="shareMessage" class="share-message" role="status">{{ shareMessage }}</p>
      </article>
    </div>
  </section>
</template>
