let audioContext: AudioContext | null = null
let ambientAudio: HTMLAudioElement | null = null
let ambientFadeTimer: number | null = null

export function useAudio() {
  const soundOn = useState('audio-sound-on', () => true)
  const effectsOn = useState('audio-effects-on', () => true)

  function ensureAudio() {
    if (!import.meta.client) return null
    audioContext ??= new AudioContext()
    if (audioContext.state === 'suspended') void audioContext.resume()
    return audioContext
  }

  function playEffect(kind: 'tap' | 'paper' | 'complete' = 'tap') {
    if (!effectsOn.value) return
    const ctx = ensureAudio()
    if (!ctx) return
    const osc = ctx.createOscillator()
    const gain = ctx.createGain()
    const now = ctx.currentTime
    const frequencies = { tap: 520, paper: 380, complete: 660 }
    osc.frequency.setValueAtTime(frequencies[kind], now)
    if (kind === 'complete') osc.frequency.exponentialRampToValueAtTime(880, now + 0.28)
    gain.gain.setValueAtTime(0.0001, now)
    gain.gain.exponentialRampToValueAtTime(0.055, now + 0.018)
    gain.gain.exponentialRampToValueAtTime(0.0001, now + (kind === 'complete' ? 0.55 : 0.16))
    osc.connect(gain).connect(ctx.destination)
    osc.start(now)
    osc.stop(now + (kind === 'complete' ? 0.58 : 0.18))
  }

  function fadeTo(target: number, duration: number, onComplete?: () => void) {
    if (!ambientAudio) return
    if (ambientFadeTimer !== null) window.clearInterval(ambientFadeTimer)
    const audio = ambientAudio
    const startVolume = audio.volume
    const startedAt = performance.now()
    ambientFadeTimer = window.setInterval(() => {
      const progress = Math.min((performance.now() - startedAt) / duration, 1)
      audio.volume = startVolume + (target - startVolume) * progress
      if (progress === 1) {
        if (ambientFadeTimer !== null) window.clearInterval(ambientFadeTimer)
        ambientFadeTimer = null
        onComplete?.()
      }
    }, 50)
  }

  function startAmbient() {
    if (!import.meta.client || ambientAudio) return
    const audio = new Audio('/audio/mindful-piano.mp3')
    audio.loop = true
    audio.volume = 0
    audio.addEventListener('timeupdate', () => {
      if (audio.currentTime >= 95) audio.currentTime = 0
    })
    ambientAudio = audio
    void audio.play().then(() => fadeTo(0.22, 1500)).catch(() => { ambientAudio = null })
  }

  function stopAmbient() {
    if (!ambientAudio) return
    const audio = ambientAudio
    fadeTo(0, 600, () => {
      audio.pause()
      audio.currentTime = 0
      if (ambientAudio === audio) ambientAudio = null
    })
  }

  function setSound(enabled: boolean) {
    soundOn.value = enabled
    enabled ? startAmbient() : stopAmbient()
  }

  function dispose() {
    if (ambientFadeTimer !== null) window.clearInterval(ambientFadeTimer)
    ambientFadeTimer = null
    ambientAudio?.pause()
    ambientAudio = null
  }

  return { soundOn, effectsOn, playEffect, startAmbient, setSound, dispose }
}
