import type { Board, Pos } from '../types';
import { isValidSwap } from './boardActions';

/** 가능한 교환 한 쌍 */
export type Hint = [Pos, Pos];

/**
 * 유효한 교환(매치가 생기는 교환) 한 쌍을 찾아 반환한다.
 * 없으면 null. 오른쪽/아래 인접만 검사하면 모든 인접 쌍을 빠짐없이 본다.
 */
export function findHint(board: Board): Hint | null {
  const rows = board.length;
  if (rows === 0) return null;
  const cols = board[0].length;

  for (let row = 0; row < rows; row++) {
    for (let col = 0; col < cols; col++) {
      const a: Pos = { row, col };
      if (col + 1 < cols) {
        const right: Pos = { row, col: col + 1 };
        if (isValidSwap(board, a, right)) return [a, right];
      }
      if (row + 1 < rows) {
        const down: Pos = { row: row + 1, col };
        if (isValidSwap(board, a, down)) return [a, down];
      }
    }
  }
  return null;
}

/** 가능한 교환이 하나라도 있는지 */
export function hasAvailableMove(board: Board): boolean {
  return findHint(board) !== null;
}
