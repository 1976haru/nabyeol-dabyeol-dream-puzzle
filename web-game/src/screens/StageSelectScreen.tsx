import { STAGES } from '../data/stages';
import { loadSave, isStageLocked } from '../save/saveManager';
import { getTheme } from '../themes';

interface Props {
  onSelect: (stageId: number) => void;
  onBack: () => void;
}

/** 스테이지 선택 화면 */
export function StageSelectScreen({ onSelect, onBack }: Props) {
  const save = loadSave();
  const theme = getTheme();

  return (
    <div style={{ minHeight: '100vh', padding: 20, boxSizing: 'border-box' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, maxWidth: 480, margin: '0 auto' }}>
        <button type="button" onClick={onBack} style={backBtn}>
          ← 뒤로
        </button>
        <h2 style={{ color: 'white', margin: 0, fontSize: 22 }}>{theme.stageSelectTitle}</h2>
      </div>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(3, 1fr)',
          gap: 12,
          maxWidth: 480,
          margin: '20px auto 0',
        }}
      >
        {STAGES.map((stage) => {
          const locked = isStageLocked(stage.id, save.clearedStages);
          const cleared = save.clearedStages.includes(stage.id);
          const high = save.highScores[stage.id];
          return (
            <button
              key={stage.id}
              type="button"
              disabled={locked}
              onClick={() => onSelect(stage.id)}
              style={{
                background: locked ? 'rgba(255,255,255,0.35)' : 'white',
                border: 'none',
                borderRadius: 16,
                padding: 12,
                minHeight: 96,
                cursor: locked ? 'not-allowed' : 'pointer',
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                justifyContent: 'center',
                gap: 4,
                opacity: locked ? 0.7 : 1,
              }}
            >
              {locked ? (
                <span style={{ fontSize: 28 }}>🔒</span>
              ) : (
                <>
                  <span style={{ fontSize: 18, fontWeight: 800, color: '#5c7cfa' }}>{stage.id}</span>
                  <span style={{ fontSize: 12, color: '#495057', textAlign: 'center' }}>{stage.name}</span>
                  {cleared && <span style={{ fontSize: 14 }}>⭐</span>}
                  {high !== undefined && <span style={{ fontSize: 11, color: '#868e96' }}>최고 {high}</span>}
                </>
              )}
            </button>
          );
        })}
      </div>
    </div>
  );
}

const backBtn: React.CSSProperties = {
  background: 'rgba(255,255,255,0.22)',
  color: 'white',
  border: 'none',
  borderRadius: 10,
  padding: '8px 14px',
  fontSize: 15,
  cursor: 'pointer',
  fontWeight: 600,
};
