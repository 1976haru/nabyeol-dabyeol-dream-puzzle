import { useState } from 'react';

const KEY = 'tutorial_done';

const STEPS = [
  { icon: '👆', text: '먼저 옮기고 싶은 블록을 한 번 눌러요.' },
  { icon: '↔️', text: '바로 옆에 있는 블록을 눌러 자리를 바꿔요.' },
  { icon: '✨', text: '같은 그림 3개를 한 줄로 모으면 사라져요!' },
];

/** 첫 실행 시 보여 주는 3단계 튜토리얼 오버레이 */
export function Tutorial() {
  const [done, setDone] = useState(() => {
    try {
      return localStorage.getItem(KEY) !== null;
    } catch {
      return true;
    }
  });

  if (done) return null;

  const finish = () => {
    try {
      localStorage.setItem(KEY, '1');
    } catch {
      // ignore
    }
    setDone(true);
  };

  return (
    <div
      style={{
        position: 'fixed',
        inset: 0,
        background: 'rgba(0,0,0,0.7)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 60,
        padding: 16,
      }}
    >
      <div
        style={{
          background: 'white',
          borderRadius: 24,
          padding: 28,
          width: '100%',
          maxWidth: 360,
          textAlign: 'center',
        }}
      >
        <h2 style={{ marginTop: 0, fontSize: 22, color: '#5c7cfa' }}>게임 방법 🧩</h2>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 14, margin: '18px 0' }}>
          {STEPS.map((s, i) => (
            <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 12, textAlign: 'left' }}>
              <span style={{ fontSize: 32 }}>{s.icon}</span>
              <span style={{ fontSize: 16, color: '#495057' }}>{s.text}</span>
            </div>
          ))}
        </div>
        <button
          type="button"
          onClick={finish}
          style={{
            background: 'linear-gradient(90deg,#748ffc,#5c7cfa)',
            color: 'white',
            border: 'none',
            borderRadius: 16,
            padding: '14px',
            fontSize: 18,
            fontWeight: 700,
            cursor: 'pointer',
            width: '100%',
          }}
        >
          이해했어요!
        </button>
      </div>
    </div>
  );
}
