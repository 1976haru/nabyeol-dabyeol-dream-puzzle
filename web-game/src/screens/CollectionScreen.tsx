import { CHARACTERS } from '../data/characters';
import { loadSave } from '../save/saveManager';
import { getTheme } from '../themes';

interface Props {
  onBack: () => void;
}

/** 캐릭터 도감 화면 */
export function CollectionScreen({ onBack }: Props) {
  const save = loadSave();
  const theme = getTheme();
  const unlockedCount = CHARACTERS.filter((c) => save.unlockedCharacters.includes(c.id)).length;

  return (
    <div style={{ minHeight: '100vh', padding: 20, boxSizing: 'border-box' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, maxWidth: 480, margin: '0 auto' }}>
        <button type="button" onClick={onBack} style={backBtn}>
          ← 뒤로
        </button>
        <h2 style={{ color: 'white', margin: 0, fontSize: 22 }}>{theme.collectionTitle}</h2>
      </div>

      <p style={{ color: 'white', textAlign: 'center', fontSize: 16, marginTop: 12 }}>
        {unlockedCount} / {CHARACTERS.length} 수집
      </p>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(2, 1fr)',
          gap: 14,
          maxWidth: 480,
          margin: '12px auto 0',
        }}
      >
        {CHARACTERS.map((c) => {
          const unlocked = save.unlockedCharacters.includes(c.id);
          // 표시용 override (테마가 제공할 때만). 캐릭터 id/저장 데이터는 그대로.
          const override = theme.collectionCharacterOverrides?.[c.id];
          const displayName = override?.name ?? c.name;
          const displayDescription = override?.description ?? c.description;
          return (
            <div
              key={c.id}
              style={{
                background: 'white',
                borderRadius: 18,
                padding: 16,
                textAlign: 'center',
                filter: unlocked ? 'none' : 'grayscale(1)',
                opacity: unlocked ? 1 : 0.4,
              }}
            >
              <div style={{ fontSize: 48 }}>{unlocked ? c.emoji : '❓'}</div>
              <div style={{ fontSize: 17, fontWeight: 800, color: theme.primary, marginTop: 6 }}>
                {unlocked ? displayName : '???'}
              </div>
              <div style={{ fontSize: 13, color: '#868e96', marginTop: 4, minHeight: 34 }}>
                {unlocked ? displayDescription : `스테이지 ${c.unlockStageId} 클리어 시 해금`}
              </div>
              {unlocked && override?.ability && (
                <div style={abilityBadge}>{override.ability}</div>
              )}
            </div>
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

const abilityBadge: React.CSSProperties = {
  display: 'inline-block',
  marginTop: 8,
  padding: '3px 10px',
  borderRadius: 999,
  background: '#f5f0ff',
  color: '#5f3dc4',
  fontSize: 12,
  fontWeight: 700,
};
