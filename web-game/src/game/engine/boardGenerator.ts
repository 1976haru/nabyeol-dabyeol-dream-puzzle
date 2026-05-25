import { BLOCK_TYPES, BOARD_SIZE, cellId } from '../types';
import type { Board, BlockType, Cell } from '../types';
import { findMatches } from './matchFinder';

/** 무작위 블록 종류 반환 */
export function randomBlockType(): BlockType {
  return BLOCK_TYPES[Math.floor(Math.random() * BLOCK_TYPES.length)];
}

/** 모든 칸을 무작위 블록으로 채운 보드 생성 */
function createRandomBoard(size: number): Board {
  const board: Board = [];
  for (let row = 0; row < size; row++) {
    const line: Cell[] = [];
    for (let col = 0; col < size; col++) {
      line.push({ id: cellId(row, col), row, col, type: randomBlockType() });
    }
    board.push(line);
  }
  return board;
}

/** 매치된 셀의 종류를 다른 종류로 강제 교체해 초기 매치를 제거 */
function forceNoMatch(board: Board): Board {
  let guard = 0;
  let matches = findMatches(board);
  while (matches.size > 0 && guard < 200) {
    for (const id of matches) {
      const [r, c] = id.split('-').map(Number);
      let next = randomBlockType();
      // 인접/현재와 다른 종류 우선 선택
      while (next === board[r][c].type) next = randomBlockType();
      board[r][c] = { ...board[r][c], type: next };
    }
    matches = findMatches(board);
    guard++;
  }
  return board;
}

/**
 * 초기 매치가 없는 보드를 생성한다.
 * - 최대 20번 재시도하여 매치 없는 보드를 찾고,
 * - 실패 시 강제 교체로 매치를 제거한다.
 * - 모든 셀의 type 은 null 이 아니다.
 */
export function generateBoard(size: number = BOARD_SIZE): Board {
  for (let attempt = 0; attempt < 20; attempt++) {
    const board = createRandomBoard(size);
    if (findMatches(board).size === 0) return board;
  }
  return forceNoMatch(createRandomBoard(size));
}
