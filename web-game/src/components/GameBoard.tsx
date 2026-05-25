import { BlockCell } from './BlockCell';
import type { Board, Pos } from '../game/types';
import type { Hint } from '../game/engine/hintFinder';

interface Props {
  board: Board;
  selected: Pos | null;
  hint: Hint | null;
  onCellClick: (pos: Pos) => void;
}

function samePos(a: Pos | null, b: Pos): boolean {
  return a !== null && a.row === b.row && a.col === b.col;
}

function inHint(hint: Hint | null, pos: Pos): boolean {
  if (!hint) return false;
  return samePos(hint[0], pos) || samePos(hint[1], pos);
}

/** 게임 보드 그리드 */
export function GameBoard({ board, selected, hint, onCellClick }: Props) {
  const cols = board[0]?.length ?? 0;
  return (
    <div
      style={{
        display: 'grid',
        gridTemplateColumns: `repeat(${cols}, 1fr)`,
        gap: 4,
        width: '100%',
        maxWidth: 480,
        margin: '0 auto',
        padding: 8,
        background: 'rgba(255,255,255,0.18)',
        borderRadius: 16,
        boxSizing: 'border-box',
      }}
    >
      {board.map((line) =>
        line.map((cell) => {
          const pos: Pos = { row: cell.row, col: cell.col };
          return (
            <BlockCell
              key={cell.id}
              cell={cell}
              isSelected={samePos(selected, pos)}
              isHint={inHint(hint, pos)}
              onClick={() => onCellClick(pos)}
            />
          );
        }),
      )}
    </div>
  );
}
