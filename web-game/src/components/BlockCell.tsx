import type { Cell } from '../game/types';
import { getTheme } from '../themes';

const theme = getTheme();

interface Props {
  cell: Cell;
  isSelected: boolean;
  isHint: boolean;
  onClick: () => void;
}

/** 보드의 한 칸 */
export function BlockCell({ cell, isSelected, isHint, onClick }: Props) {
  const visual = cell.type ? theme.blocks[cell.type] : null;
  const bg = visual ? visual.color : 'transparent';
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={cell.type ?? 'empty'}
      style={{
        width: '100%',
        aspectRatio: '1 / 1',
        border: isSelected ? `3px solid ${theme.primary}` : '2px solid rgba(255,255,255,0.5)',
        borderRadius: 12,
        background: bg,
        fontSize: 'clamp(18px, 6vw, 30px)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        cursor: 'pointer',
        padding: 0,
        boxShadow: isHint
          ? '0 0 0 3px #ffd43b, 0 0 12px #ffd43b'
          : isSelected
            ? '0 0 10px rgba(92,124,250,0.8)'
            : 'inset 0 -2px 4px rgba(0,0,0,0.08)',
        transform: isSelected ? 'scale(1.08)' : 'scale(1)',
        transition: 'transform 0.12s ease, box-shadow 0.15s ease',
        touchAction: 'manipulation',
      }}
    >
      {visual ? visual.emoji : ''}
    </button>
  );
}
