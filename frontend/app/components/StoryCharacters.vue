<script setup lang="ts">
withDefaults(defineProps<{ scene?: 'welcome' | 'approach' | 'talk' | 'separate' | 'name'; compact?: boolean }>(), {
  scene: 'welcome',
  compact: false
})
</script>

<template>
  <div class="story-art" :class="[`scene-${scene}`, { compact }]" aria-hidden="true">
    <svg viewBox="0 0 760 360" role="img">
      <path class="ground" d="M30 304c115-18 207-14 308 2s211 15 392-6" />
      <path class="green-path" d="M455 336c38-42 70-60 119-79 48-18 92-44 137-99" />

      <g class="person-sticker">
        <path class="white-fill ink" d="M228 174c-15-30-4-73 31-91 38-19 84 4 87 45 2 23-10 39-23 53 35 19 52 55 47 100H185c-6-47 9-82 43-107z" />
        <path class="hair" d="M224 132c-8-42 17-72 57-72 43 0 71 30 63 72-7-18-19-25-34-33-17 19-44 31-86 33z" />
        <path class="face" d="M258 145q10 8 20 0M313 145q-10 8-20 0M280 163q8 8 16 0" />
        <path class="green-fill ink" d="M205 210c15-35 47-51 81-48 40 3 69 24 81 61l-29 17-106-3z" />
        <path class="ink line" d="M237 217c34 22 67 24 101 3M246 240l-24 60M326 240l36 60" />
        <path class="white-fill ink shoe" d="M196 286h66v30h-81c-3-13 2-23 15-30zM337 286h61c15 8 20 18 17 30h-79z" />
      </g>

      <g class="critic-sticker">
        <path class="black-fill ink" d="M491 148c-14-35 8-69 44-73 39-5 71 19 76 59 6 45-22 78-65 80-38 2-62-22-55-66z" />
        <path class="black-fill ink" d="M511 85l-6-35 31 24M575 79l18-31 12 39" />
        <circle class="eye" cx="533" cy="137" r="7" /><circle class="eye" cx="571" cy="137" r="7" />
        <path class="white-line" d="M538 164q15 10 29 0" />
        <path class="black-fill ink" d="M512 206l-7 45M588 204l15 45M494 241h28M589 241h29" />
        <path class="tail-line" d="M606 179c38 4 33 47 7 54" />
        <g class="clipboard">
          <path class="white-fill ink" d="M615 113h78v105h-78z" />
          <path class="green-fill ink" d="M637 103h34v18h-34z" />
          <path class="ink line" d="M629 144h13M651 144h26M629 168h13M651 168h26M629 192h13M651 192h26" />
        </g>
      </g>

      <g class="story-notes">
        <path class="spark green-fill ink" d="M455 75l10 20 22 3-16 15 4 22-20-10-20 10 4-22-16-15 22-3z" />
        <path class="leaf green-fill ink" d="M75 270c38-45 74-37 80-35-11 28-38 49-75 45z" />
        <path class="ink line" d="M80 278l62-35" />
      </g>
    </svg>
    <span v-if="scene === 'welcome'" class="sticker-label">從一個聲音開始</span>
    <span v-if="scene === 'approach'" class="motion-word">快一點！</span>
    <span v-if="scene === 'separate'" class="space-label">這裡，多了一點空間</span>
  </div>
</template>

<style scoped>
.story-art { width: min(760px, 96vw); height: 360px; position: relative; overflow: visible; }
svg { width: 100%; height: 100%; overflow: visible; }
.ink { stroke: #000; stroke-width: 5; stroke-linecap: round; stroke-linejoin: round; }
.line { fill: none; }.white-fill { fill: #fff; }.black-fill,.hair { fill: #000; }.green-fill { fill: #39ff14; }
.ground { fill:none; stroke:#000; stroke-width:5; stroke-dasharray:7 11; }
.green-path { fill:none; stroke:#39ff14; stroke-width:17; stroke-linecap:round; stroke-dasharray:1 25; }
.face { fill:none; stroke:#000; stroke-width:4; stroke-linecap:round; }.eye { fill:#fff; }.white-line { fill:none; stroke:#fff; stroke-width:4; stroke-linecap:round; }.tail-line { fill:none; stroke:#000; stroke-width:7; stroke-linecap:round; }
.person-sticker { transform-origin: 280px 210px; animation: person-bob 3s ease-in-out infinite; }
.critic-sticker { transform-origin: 550px 160px; animation: critic-bob 1.8s ease-in-out infinite; }
.clipboard { transform-origin: 615px 180px; animation: clipboard-tap 1.4s ease-in-out infinite; }
.spark { animation: sparkle 1.6s steps(2) infinite; transform-origin:455px 105px; }
.sticker-label,.motion-word,.space-label { position:absolute; border:4px solid #000; background:#fff; color:#000; padding:9px 15px; border-radius:999px; font-weight:800; box-shadow:6px 6px 0 #39ff14; transform:rotate(-3deg); }
.sticker-label { left:8%; top:12%; }.motion-word { right:2%; top:7%; animation: pop .8s ease-out both; }.space-label { left:48%; top:42%; }
.scene-approach .critic-sticker { animation: approach 1.1s cubic-bezier(.2,.9,.3,1) both, critic-bob 1.8s 1.1s ease-in-out infinite; }
.scene-talk .critic-sticker { transform:translateX(-45px); }
.scene-separate .critic-sticker { animation: separate 1.4s ease-out both; }.scene-separate .green-path { animation: path-draw 1.6s ease-out both; }
.scene-name .person-sticker,.scene-name .ground,.scene-name .green-path,.scene-name .story-notes { opacity:.12; }.scene-name .critic-sticker { transform:translate(-160px,42px) scale(1.35); }
.compact { height:220px; }.compact svg { transform:translateY(-68px) scale(.82); }
@keyframes person-bob {50%{transform:translateY(4px)}} @keyframes critic-bob {50%{transform:translateY(-7px) rotate(2deg)}} @keyframes clipboard-tap {50%{transform:rotate(6deg)}}
@keyframes sparkle {50%{transform:scale(.75) rotate(18deg)}} @keyframes pop {from{opacity:0;transform:scale(.4) rotate(8deg)}to{opacity:1;transform:scale(1) rotate(-3deg)}}
@keyframes approach {from{opacity:0;transform:translateX(120px) rotate(12deg)}to{opacity:1;transform:none}} @keyframes separate {from{transform:translateX(-95px)}to{transform:translateX(35px)}} @keyframes path-draw {from{stroke-dashoffset:220}to{stroke-dashoffset:0}}
@media(max-width:640px){.story-art{height:270px}.sticker-label{left:2%;top:4%;font-size:.72rem}.motion-word{right:0;font-size:.75rem}.space-label{left:38%;top:48%;font-size:.68rem}.compact{height:180px}.compact svg{transform:translateY(-50px) scale(.75)}}
</style>
