import { cellId } from '../types';
import type { Board, BlockType } from '../types';
import { randomBlockType } from './boardGenerator';

/** 매치된 셀의 type 을 null 로 바꾼 새 Board 를 반환한다. */
export function removeMatches(board: Board, matchedIds: Set<string>): Board {
  return board.map((line) =>
    line.map((cell) => (matchedIds.has(cell.id) ? { ...cell, type: null } : { ...cell })),
  );
}

/**
 * 각 열에서 null(빈 칸) 위에 있는 블록을 아래로 떨어뜨린다.
 * row 값과 id 를 재정렬한다. (빈 칸은 위쪽에 모인다)
 */
export function dropBlocks(board: Board): Board {
  const rows = board.length;
  if (rows === 0) return board;
  const cols = board[0].length;
  const next: Board = board.map((line) => line.map((cell) => ({ ...cell })));

  for (let col = 0; col < cols; col++) {
    // 아래에서부터 채워 넣는다.
    const types: BlockType[] = [];
    for (let row = rows - 1; row >= 0; row--) {
      const t = next[row][col].type;
      if (t !== null) types.push(t);
    }
    let writeRow = rows - 1;
    for (const t of types) {
      next[writeRow][col] = { id: cellId(writeRow, col), row: writeRow, col, type: t };
      writeRow--;
    }
    for (let row = writeRow; row >= 0; row--) {
      next[row][col] = { id: cellId(row, col), row, col, type: null };
    }
  }
  return next;
}

/** null 인 칸을 새 무작위 블록으로 채운 새 Board 를 반환한다. */
export function fillEmpty(board: Board): Board {
  return board.map((line) =>
    line.map((cell) =>
      cell.type === null ? { ...cell, type: randomBlockType() } : { ...cell },
    ),
  );
}
