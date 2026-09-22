<script setup lang="ts">
const onboarding = useOnboardingStore()
const journal = useJournalStore()
const { stage: introductionStage, lessonOne, lessonTwo, criticName } = storeToRefs(onboarding)
const { effectsOn, setSound, playEffect } = useAudio()

function confirmSound(enabled: boolean) {
  setSound(enabled)
  playEffect()
  introductionStage.value = 2
}

function advanceLessonOne() {
  playEffect()
  if (lessonOne.value < 2) lessonOne.value++
  else introductionStage.value = 3
}

function confirmCriticName() {
  onboarding.confirmName()
  playEffect('paper')
}

function advanceLessonTwo() {
  playEffect()
  if (lessonTwo.value < 1) lessonTwo.value++
  else introductionStage.value = 5
}

async function startJournal() {
  onboarding.markCompleted()
  await navigateTo('/journals')
  await journal.beginNew()
}

onMounted(() => {
  if (introductionStage.value < 1 || introductionStage.value > 5) introductionStage.value = 1
})

watch(introductionStage, async () => {
  await nextTick()
  if (import.meta.client) window.scrollTo({ top: 0, behavior: 'smooth' })
})
</script>

<template>
  <Transition name="page" mode="out-in">
    <section v-if="introductionStage === 1" key="sound" class="scene compact-scene">
      <div class="sound-orbit"><span>♪</span><i /><i /><i /></div>
      <p class="eyebrow">在開始以前</p>
      <h2>想帶著音樂<br>一起嗎？</h2>
      <p class="lead">你也可以隨時在右上方的聲音設定調整。</p>
      <div class="button-stack">
        <button class="primary" @click="confirmSound(true)">播放背景音樂</button>
        <button class="secondary" @click="confirmSound(false)">我想安靜使用</button>
        <label class="effect-choice"><input v-model="effectsOn" type="checkbox"> 開啟按鈕音效</label>
      </div>
    </section>

    <section v-else-if="introductionStage === 2" :key="`lesson-one-${lessonOne}`" class="scene lesson-scene">
      <p class="eyebrow">認識你的內在批評者 · {{ lessonOne + 1 }}/3</p>
      <div class="character-stage" :class="`lesson-${lessonOne}`">
        <StoryCharactersImage :scene="lessonOne === 0 ? 'approach' : lessonOne === 1 ? 'talk' : 'separate'" />
        <template v-if="lessonOne >= 1">
          <span class="speech s1">我怎麼又搞砸了</span><span class="speech s2">我還不夠好</span>
          <span class="speech s3">不可以停下來</span><span class="speech s4">沒有人會喜歡我</span>
          <span class="speech s5">我很懶惰，做不到的</span><span class="speech s6">我總是讓人失望</span>
        </template>
        <div v-if="lessonOne === 2" class="breathing-space">這裡，多了一點空間</div>
      </div>
      <h2 v-if="lessonOne === 0">他總是在我們不留意的時候說話</h2>
      <h2 v-else-if="lessonOne === 1">而且他說的話，大部分都很 mean</h2>
      <h2 v-else>試著先把「批評的聲音」和「真實的我」分開</h2>
      <p v-if="lessonOne === 0" class="lead narrow">有時候，我們心裡會出現一個很熟悉的聲音。當我們休息、犯錯或不知道下一步時，他就急著催促、辱罵我們。</p>
      <p v-else-if="lessonOne === 1" class="lead narrow">那些話很尖銳，卻熟悉得像是我們自己的聲音。</p>
      <p v-else class="lead narrow">一個想法出現在腦中，不代表它就是事實，也不代表它是完整的你。我們先練習認出：喔，原來又是他在說話。</p>
      <button class="primary" @click="advanceLessonOne">{{ lessonOne < 2 ? '繼續看看' : '替他取個名字' }} <span>→</span></button>
    </section>

    <section v-else-if="introductionStage === 3" key="name" class="scene compact-scene">
      <StoryCharactersImage scene="name" compact />
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

    <section v-else-if="introductionStage === 4" :key="`lesson-two-${lessonTwo}`" class="scene lesson-scene">
      <p class="eyebrow">改變從看見開始 · {{ lessonTwo + 1 }}/2</p>
      <div class="road-stage" :class="{ illuminated: lessonTwo === 1 }"><div class="old-road"/><div class="new-road"/><div class="lamp">✦</div></div>
      <h2 v-if="lessonTwo === 0">熟悉，不等於不能改變</h2>
      <h2 v-else>今天只需要看見一次</h2>
      <p v-if="lessonTwo === 0" class="lead narrow">一個想法出現很多次，我們的身體為了節能，聰明地學會讓這條路變得很熟悉、很自動。但熟悉，不代表它永遠不能改變。</p>
      <p v-else class="lead narrow">每一次停下來，看見「他現在又說了什麼」，都在替自己多留一點選擇的空間。我們不用著急地一下子改變所有事情。</p>
      <button class="primary" @click="advanceLessonTwo">{{ lessonTwo === 0 ? '照亮另一條路' : '開始第一次覺察練習' }} <span>→</span></button>
      <NuxtLink v-if="lessonTwo === 1" class="text-button" to="/">今天先到這裡</NuxtLink>
    </section>

    <section v-else key="ready" class="scene compact-scene">
      <div class="blank-node"><span>01</span></div>
      <p class="eyebrow">第一個節點</p>
      <h2>開始前，先看看現在的自己。</h2>
      <p class="lead narrow">你可以隨時跳過、返回或停下來，已經寫下的部分仍然有意義。</p>
      <button class="primary" @click="startJournal">開始吧！ <span>→</span></button>
      <NuxtLink class="text-button" to="/">今天先不寫</NuxtLink>
      <NuxtLink class="help-link" to="/journals?view=safety">我現在需要協助</NuxtLink>
    </section>
  </Transition>
</template>
