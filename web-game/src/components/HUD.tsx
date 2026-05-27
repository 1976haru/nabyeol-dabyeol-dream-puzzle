import { getTheme } from '../themes';

const theme = getTheme();

interface Props {
  stageName: string;
  score: number;
  targetScore: number;
  movesLeft: number;
  soundOn: boolean;
  onToggleSound: () => void;
  onHint: () => void;
  onMenu: () => void;
}

/** 게임 상단 정보 표시 (점수/이동/진행바/버튼) */
export function HUD({
  stageName,
  score,
  targetScore,
  movesLeft,
  soundOn,
  onToggleSound,
  onHint,
  onMenu,
}: Props) {
  const progress = Math.min(100, Math.round((score / targetScore) * 100));
  return (
    <div style={{ width: '100%', maxWidth: 480, margin: '0 auto', color: 'white' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 8 }}>
        <button type="button" onClick={onMenu} style={btnStyle}>
          ← 메뉴
        </button>
        <span style={{ fontSize: 18, fontWeight: 700 }}>{stageName}</span>
        <button type="button" onClick={onToggleSound} style={btnStyle} aria-label="소리">
          {soundOn ? '🔈' : '🔇'}
        </button>
      </div>

      <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 8, fontSize: 16 }}>
        <span>⭐ {score} / {targetScore}</span>
        <span>👣 {movesLeft}</span>
        <button type="button" onClick={onHint} style={{ ...btnStyle, padding: '4px 10px' }}>
          {theme.hintLabel}
        </button>
      </div>

      <div
        style={{
          marginTop: 8,
          height: 14,
          borderRadius: 7,
          background: 'rgba(0,0,0,0.25)',
          overflow: 'hidden',
        }}
      >
        <div
          style={{
            width: `${progress}%`,
            height: '100%',
            background: 'linear-gradient(90deg,#ffe066,#ff922b)',
            transition: 'width 0.3s ease',
          }}
        />
      </div>
    </div>
  );
}

const btnStyle: React.CSSProperties = {
  background: 'rgba(255,255,255,0.22)',
  color: 'white',
  border: 'none',
  borderRadius: 10,
  padding: '6px 12px',
  fontSize: 16,
  cursor: 'pointer',
  fontWeight: 600,
};
