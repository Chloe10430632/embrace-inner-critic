<script setup lang="ts">
const props = withDefaults(defineProps<{
  scene?: 'welcome' | 'approach' | 'talk' | 'separate' | 'name'
  compact?: boolean
}>(), {
  scene: 'welcome',
  compact: false
})

/** Returns the image URL for the current scene */
const image = computed(() => {
  if (props.scene === 'name') return '/illustrations/critic-wave.webp'
  if (props.scene === 'approach') return '/illustrations/critic-peek-wall.webp'
  if (props.scene === 'talk') return '/illustrations/critic-scene-pressure.webp'
  if (props.scene === 'separate') return '/illustrations/critic-scene-space.webp'
  return '/illustrations/critic-scene-rest.webp'
})

/** Returns the description for the current scene */
const description = computed(() => {
  if (props.scene === 'talk') return '使用者緊張地坐著，內在批評者在一旁急切說話。'
  if (props.scene === 'separate') return '使用者鬆了一口氣，與內在批評者之間留出一段空間。'
  if (props.scene === 'name') return '內在批評者面向使用者，友善地揮手打招呼。'
  if (props.scene === 'approach') return '內在批評者默默從牆後探頭。'
  return '使用者安靜坐著休息，內在批評者從遠處探頭。'
})
</script>

<template>
  <figure class="story-image" :class="[`scene-${scene}`, { compact }]">
    <img v-if="scene === 'name'" class="character-layer name-layer" :src="image" :alt="description" decoding="async">
    <img v-else-if="scene === 'approach'" class="character-layer approach-layer" :src="image" :alt="description" decoding="async">
    <template v-else>
      <img class="character-layer protagonist-layer" :src="image" :alt="description" decoding="async">
      <img class="character-layer critic-layer" :src="image" alt="" decoding="async">
    </template>
    <span v-if="scene === 'separate'" class="breath-ring ring-one" />
    <span v-if="scene === 'separate'" class="breath-ring ring-two" />
    <!-- <span v-if="scene === 'welcome'" class="sticker-label">從辨認它的聲音開始</span> -->
    <span v-if="scene === 'approach'" class="peek-speech">我還不夠好......</span>
  </figure>
</template>

<style scoped>
.story-image {
  position: relative;
  width: min(920px, 96vw);
  height: 390px;
  margin: 0;
}

.character-layer {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  display: block;
  object-fit: contain;
  pointer-events: none;
}

.protagonist-layer { clip-path: inset(0 34% 0 0); animation: person-arrive .8s cubic-bezier(.2,.8,.25,1) both; }
.critic-layer { clip-path: inset(0 0 0 66%); animation: critic-arrive .8s .12s cubic-bezier(.2,.8,.25,1) both; }

.scene-welcome .protagonist-layer { animation:person-arrive .8s both,soft-breathe 3.4s .8s ease-in-out infinite; }
.scene-welcome .critic-layer { animation:critic-peek 1s .35s cubic-bezier(.2,.9,.3,1) both,critic-hover 2.2s 1.35s ease-in-out infinite; }
.approach-layer { animation:wall-peek 1.25s cubic-bezier(.2,.85,.25,1) both,quiet-peek 3.2s 1.25s ease-in-out infinite; transform-origin:86% 55%; }
.scene-talk .protagonist-layer { clip-path:none; animation:pressure-in .55s ease-out both,tense-breathe 1.6s .55s ease-in-out infinite; transform-origin:50% 65%; }
.scene-talk .critic-layer { display:none; }
.scene-separate .protagonist-layer { clip-path:inset(0 34% 0 0); animation:person-release .8s ease-out both,soft-breathe 3.6s .8s ease-in-out infinite; transform-origin:34% 65%; }
.scene-separate .critic-layer { clip-path:inset(0 0 0 66%); animation:critic-center-in .8s ease-out both,critic-center-hover 3.2s .8s ease-in-out infinite; transform-origin:80% 58%; }
.name-layer { animation:name-arrive .8s ease-out both,hello-wave 1.8s .8s ease-in-out infinite; transform-origin:50% 54%; }

.breath-ring { position:absolute; left:52%; top:34%; width:34px; height:34px; border:3px solid var(--color-brand-accent); border-radius:50%; opacity:0; animation:breathing-space 2.2s ease-out infinite; }
.ring-two { animation-delay:.7s; }

.sticker-label,.motion-word {
  position: absolute;
  z-index: 2;
  border: 4px solid var(--color-brand-ink);
  border-radius: 999px;
  padding: 9px 15px;
  color: var(--color-brand-ink);
  background: var(--color-brand-surface);
  box-shadow: 6px 6px 0 var(--color-brand-accent);
  font-weight: 800;
  transform: rotate(-3deg);
}
.sticker-label { left: 5%; top: 10%; }
.motion-word { right: 2%; top: 8%; animation: pop .65s ease-out both; }
.peek-speech {
  position:absolute;
  z-index:2;
  right:50%;
  top:40%;
  border:4px solid var(--color-brand-ink);
  border-radius:999px;
  padding:9px 15px;
  color:var(--color-brand-ink);
  background:var(--color-brand-surface);
  box-shadow:6px 6px 0 var(--color-brand-accent);
  font-weight:800;
  animation:peek-whisper .65s .9s ease-out both;
}
.compact { height: 230px; }

@keyframes person-arrive { from { opacity:0; transform:translateY(12px) scale(.98); } to { opacity:1; transform:none; } }
@keyframes critic-arrive { from { opacity:0; transform:translateX(245px); } to { opacity:1; transform:none; } }
@keyframes soft-breathe { 50% { transform:translateY(10px); } }
@keyframes pressure-in { from { opacity:0; transform:scale(.98); } to { opacity:1; transform:none; } }
@keyframes tense-breathe { 50% { transform:scale(.992) translateY(2px); } }
@keyframes critic-peek { from { opacity:0; transform:translateX(75px) rotate(5deg); } to { opacity:1; transform:translateX(-0px); } }
@keyframes wall-peek { from { opacity:0; transform:translateX(110px); } to { opacity:1; transform:none; } }
@keyframes quiet-peek { 50% { transform:translateX(-7px) rotate(-.5deg); } }
@keyframes peek-whisper { from { opacity:0; transform:translate(12px,8px) scale(.8); } to { opacity:1; transform:none; } }
@keyframes critic-hover { 50% { transform:translateY(-3px) rotate(0.5deg) translateX(-20px); } }
@keyframes cord-sway { 50% { transform:rotate(-2deg) translateY(-4px); } }
@keyframes critic-talk { to { transform:translateX(-7px) rotate(-1.5deg); } }
@keyframes person-release { from { opacity:.55; transform:translateX(26px) scale(.98); } to { opacity:1; transform:none; } }
@keyframes critic-step-back { from { transform:translate(-230px,-45px); } to { transform:translate(-145px,-65px); } }
@keyframes critic-center-in { from { opacity:0; transform:translate(-85px,-45px) scale(.96); } to { opacity:1; transform:translate(-100px,-95px); } }
@keyframes critic-center-hover { 50% { transform:translate(-105px,-90px) rotate(.5deg); } }
@keyframes name-arrive { from { opacity:0; transform:translateY(18px) scale(1.25); } to { opacity:1; transform:translateY(-4px) scale(1.55); } }
@keyframes hello-wave { 50% { transform:translateY(-7px) scale(1.55) rotate(2deg); } }
@keyframes breathing-space { 0% { opacity:.8; transform:scale(.25); } 75%,100% { opacity:0; transform:scale(2.4); } }
@keyframes pop { from { opacity:0; transform:scale(.45) rotate(7deg); } to { opacity:1; transform:scale(1) rotate(-3deg); } }

@media(max-width:640px) {
  .story-image { width:120%; height:350px; }
  .scene-approach,.scene-talk,.scene-separate { left:-8%; width:116%; height:100%; }
  .story-image.compact { left:0; width:100%; height:190px; }
  .scene-separate .critic-layer { animation:critic-center-in-mobile .8s ease-out both,critic-center-hover-mobile 3.2s .8s ease-in-out infinite; }
  .name-layer { animation:name-arrive-mobile .8s ease-out both,hello-wave-mobile 1.8s .8s ease-in-out infinite; }
  .sticker-label { left:1%; top:3%; font-size:.7rem; }
  .motion-word { right:0; top:5%; font-size:.7rem; }
  .peek-speech { right:50%; top:40%; padding:7px 10px; border-width:3px; font-size:.7rem; }
  .compact { height:190px; }

  @keyframes critic-center-in-mobile { from { opacity:0; transform:translate(-32px,-42px) scale(.96); } to { opacity:1; transform:translate(-48px,-68px); } }
  @keyframes critic-center-hover-mobile { 50% { transform:translate(-51px,-65px) rotate(.5deg); } }
  @keyframes name-arrive-mobile { from { opacity:0; transform:translateY(10px) scale(.96); } to { opacity:1; transform:translateY(-2px) scale(1.12); } }
  @keyframes hello-wave-mobile { 50% { transform:translateY(-5px) scale(1.12) rotate(1deg); } }
}
</style>
