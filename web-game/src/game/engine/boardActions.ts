import { cellId } from '../types';
import type { Board, Pos } from '../types';
import { findMatches } from './matchFinder';

/** 두 좌표가 상하좌우로 인접한지 */
export function isAdjacent(a: Pos, b: Pos): boolean {
  const dr = Math.abs(a.row - b.row);
  const dc = Math.abs(a.col - b.col);
  return dr + dc === 1;
}

/** 두 셀의 종류를 교환한 새 Board 를 반환한다 (불변). */
export function swapCells(board: Board, a: Pos, b: Pos): Board {
  const next = board.map((line) => line.map((cell) => ({ ...cell })));
  const tmp = next[a.row][a.col].type;
  next[a.row][a.col].type = next[b.row][b.col].type;
  next[b.row][b.col].type = tmp;
  // id 는 위치 기반이므로 그대로 유지
  next[a.row][a.col].id = cellId(a.row, a.col);
  next[b.row][b.col].id = cellId(b.row, b.col);
  return next;
}

/** 인접하고 교환 시 매치가 1개 이상 생기는 유효한 교환인지 */
export function isValidSwap(board: Board, a: Pos, b: Pos): boolean {
  if (!isAdjacent(a, b)) return false;
  const swapped = swapCells(board, a, b);
  return findMatches(swapped).size > 0;
}
