import type { ScreenName } from '../navigation';
import { loadSave } from '../save/saveManager';
import { STORY } from '../data/story';

interface Props {
  onNavigate: (screen: ScreenName, stageId?: number) => void;
}

/** 메인 메뉴 화면 */
export function MenuScreen({ onNavigate }: Props) {
  const save = loadSave();
  return (
    <div style={wrap}>
      <h1 style={{ fontSize: 32, color: 'white', margin: 0, textShadow: '0 2px 8px rgba(0,0,0,0.3)' }}>
        나별다별 꿈퍼즐 ✨
      </h1>
      <p style={{ color: 'rgba(255,255,255,0.9)', fontSize: 18, marginTop: 8 }}>꿈을 모아 별을 찾아요</p>
      <p style={{ color: 'rgba(255,255,255,0.85)', fontSize: 15, maxWidth: 320, textAlign: 'center', marginBottom: 8 }}>
        {STORY.intro}
      </p>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 14, width: '100%', maxWidth: 300, marginTop: 12 }}>
        <button type="button" style={primary} onClick={() => onNavigate('game', 1)}>
          🌟 시작하기
        </button>
        <button type="button" style={primary} onClick={() => onNavigate('game', save.lastStageId)}>
          ▶ 이어하기 (스테이지 {save.lastStageId})
        </button>
        <button type="button" style={secondary} onClick={() => onNavigate('stage-select')}>
          🗺️ 스테이지 선택
        </button>
        <button type="button" style={secondary} onClick={() => onNavigate('collection')}>
          📖 도감
        </button>
      </div>
    </div>
  );
}

const wrap: React.CSSProperties = {
  minHeight: '100vh',
  display: 'flex',
  flexDirection: 'column',
  alignItems: 'center',
  justifyContent: 'center',
  gap: 6,
  padding: 24,
  boxSizing: 'border-box',
};

const primary: React.CSSProperties = {
  background: 'linear-gradient(90deg,#ffe066,#ff922b)',
  color: '#5c3b00',
  border: 'none',
  borderRadius: 18,
  padding: '16px',
  fontSize: 19,
  fontWeight: 800,
  cursor: 'pointer',
  boxShadow: '0 4px 12px rgba(0,0,0,0.2)',
};

const secondary: React.CSSProperties = {
  background: 'rgba(255,255,255,0.9)',
  color: '#495057',
  border: 'none',
  borderRadius: 18,
  padding: '14px',
  fontSize: 17,
  fontWeight: 700,
  cursor: 'pointer',
};
