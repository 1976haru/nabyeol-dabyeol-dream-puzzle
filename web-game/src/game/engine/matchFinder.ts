import type { Board } from '../types';

/**
 * 매치된 셀 id 집합을 반환한다.
 * - 가로/세로 각각 같은 type 이 3개 이상 연속된 구간을 매치로 본다.
 * - type 이 null 인 셀은 매치 대상에서 제외한다.
 * - 가로/세로 결과의 합집합을 반환한다.
 */
export function findMatches(board: Board): Set<string> {
  const matched = new Set<string>();
  const rows = board.length;
  if (rows === 0) return matched;
  const cols = board[0].length;

  // 가로 스캔
  for (let r = 0; r < rows; r++) {
    let runStart = 0;
    for (let c = 1; c <= cols; c++) {
      const prevType = board[r][c - 1].type;
      const sameAsPrev = c < cols && board[r][c].type !== null && board[r][c].type === prevType;
      if (!sameAsPrev) {
        const runLen = c - runStart;
        if (prevType !== null && runLen >= 3) {
          for (let k = runStart; k < c; k++) matched.add(board[r][k].id);
        }
        runStart = c;
      }
    }
  }

  // 세로 스캔
  for (let c = 0; c < cols; c++) {
    let runStart = 0;
    for (let r = 1; r <= rows; r++) {
      const prevType = board[r - 1][c].type;
      const sameAsPrev = r < rows && board[r][c].type !== null && board[r][c].type === prevType;
      if (!sameAsPrev) {
        const runLen = r - runStart;
        if (prevType !== null && runLen >= 3) {
          for (let k = runStart; k < r; k++) matched.add(board[k][c].id);
        }
        runStart = r;
      }
    }
  }

  return matched;
}
