// WebAudio 기반 간단 효과음 엔진 (외부 음원 파일 불필요)

let ctx: AudioContext | null = null;
let enabled = true;

function getCtx(): AudioContext | null {
  if (typeof window === 'undefined') return null;
  if (!ctx) {
    const Ctor = window.AudioContext ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!Ctor) return null;
    ctx = new Ctor();
  }
  return ctx;
}

/** 단일 톤 재생 */
function tone(freq: number, durationMs: number, type: OscillatorType = 'sine', volume = 0.15): void {
  if (!enabled) return;
  const audio = getCtx();
  if (!audio) return;
  if (audio.state === 'suspended') void audio.resume();

  const osc = audio.createOscillator();
  const gain = audio.createGain();
  osc.type = type;
  osc.frequency.value = freq;
  gain.gain.setValueAtTime(volume, audio.currentTime);
  gain.gain.exponentialRampToValueAtTime(0.0001, audio.currentTime + durationMs / 1000);
  osc.connect(gain);
  gain.connect(audio.destination);
  osc.start();
  osc.stop(audio.currentTime + durationMs / 1000);
}

/** 여러 톤을 순차 재생 */
function sequence(notes: { freq: number; at: number; dur: number; type?: OscillatorType }[]): void {
  notes.forEach((n) => {
    setTimeout(() => tone(n.freq, n.dur, n.type ?? 'sine'), n.at);
  });
}

export const soundEngine = {
  setEnabled(value: boolean): void {
    enabled = value;
  },
  isEnabled(): boolean {
    return enabled;
  },
  /** 일부 브라우저는 사용자 제스처 후에만 오디오 허용 */
  unlock(): void {
    const audio = getCtx();
    if (audio && audio.state === 'suspended') void audio.resume();
  },
  /** 블록 선택 */
  playSelect(): void {
    tone(520, 90, 'triangle');
  },
  /** 유효한 교환 */
  playSwap(): void {
    tone(660, 110, 'sine');
  },
  /** 매치 제거 (연쇄가 깊을수록 음 높아짐) */
  playMatch(cascade = 1): void {
    tone(600 + cascade * 80, 130, 'square', 0.12);
  },
  /** 잘못된 교환 */
  playInvalid(): void {
    tone(180, 140, 'sawtooth', 0.1);
  },
  /** 스테이지 클리어 팡파레 */
  playClear(): void {
    sequence([
      { freq: 523, at: 0, dur: 150 },
      { freq: 659, at: 140, dur: 150 },
      { freq: 784, at: 280, dur: 150 },
      { freq: 1046, at: 420, dur: 320 },
    ]);
  },
  /** 실패 */
  playFail(): void {
    sequence([
      { freq: 392, at: 0, dur: 200, type: 'sine' },
      { freq: 330, at: 180, dur: 200, type: 'sine' },
      { freq: 262, at: 360, dur: 320, type: 'sine' },
    ]);
  },
};
