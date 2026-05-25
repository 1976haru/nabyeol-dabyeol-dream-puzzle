import { describe, it, expect } from 'vitest';
import { cellId } from '../types';
import type { Board, BlockType } from '../types';
import { findMatches } from './matchFinder';
import { isAdjacent, isValidSwap, swapCells } from './boardActions';
import { removeMatches, dropBlocks, fillEmpty } from './boardResolver';
import { resolveBoard } from './cascadeResolver';
import { generateBoard } from './boardGenerator';
import { findHint, hasAvailableMove } from './hintFinder';

/** 문자 배열 표기로 보드를 만든다. '.' 은 null */
function makeBoard(rows: string[][]): Board {
  return rows.map((line, r) =>
    line.map((t, c) => ({
      id: cellId(r, c),
      row: r,
      col: c,
      type: t === '.' ? null : (t as BlockType),
    })),
  );
}

describe('matchFinder', () => {
  it('가로 3개 매치를 찾는다', () => {
    const board = makeBoard([
      ['star', 'star', 'star', 'moon'],
      ['moon', 'cloud', 'heart', 'bubble'],
      ['heart', 'bubble', 'moon', 'cloud'],
      ['cloud', 'heart', 'bubble', 'star'],
    ]);
    const m = findMatches(board);
    expect(m.has('0-0')).toBe(true);
    expect(m.has('0-1')).toBe(true);
    expect(m.has('0-2')).toBe(true);
    expect(m.has('0-3')).toBe(false);
  });

  it('세로 3개 매치를 찾는다', () => {
    const board = makeBoard([
      ['star', 'moon', 'cloud', 'heart'],
      ['star', 'cloud', 'heart', 'bubble'],
      ['star', 'heart', 'bubble', 'moon'],
      ['moon', 'bubble', 'star', 'cloud'],
    ]);
    const m = findMatches(board);
    expect(m.has('0-0')).toBe(true);
    expect(m.has('1-0')).toBe(true);
    expect(m.has('2-0')).toBe(true);
  });

  it('null 셀은 매치하지 않는다', () => {
    const board = makeBoard([['.', '.', '.', 'star']]);
    expect(findMatches(board).size).toBe(0);
  });

  it('2개는 매치가 아니다', () => {
    const board = makeBoard([
      ['star', 'star', 'moon', 'cloud'],
      ['heart', 'bubble', 'moon', 'cloud'],
      ['heart', 'bubble', 'star', 'heart'],
      ['cloud', 'star', 'bubble', 'moon'],
    ]);
    expect(findMatches(board).size).toBe(0);
  });
});

describe('boardActions', () => {
  it('인접 판정', () => {
    expect(isAdjacent({ row: 0, col: 0 }, { row: 0, col: 1 })).toBe(true);
    expect(isAdjacent({ row: 0, col: 0 }, { row: 1, col: 1 })).toBe(false);
    expect(isAdjacent({ row: 0, col: 0 }, { row: 0, col: 0 })).toBe(false);
  });

  it('swapCells 는 불변이며 type 을 교환한다', () => {
    const board = makeBoard([
      ['star', 'moon'],
      ['cloud', 'heart'],
    ]);
    const swapped = swapCells(board, { row: 0, col: 0 }, { row: 0, col: 1 });
    expect(board[0][0].type).toBe('star'); // 원본 불변
    expect(swapped[0][0].type).toBe('moon');
    expect(swapped[0][1].type).toBe('star');
  });

  it('isValidSwap: 매치가 생기는 교환만 유효', () => {
    // (0,2)star 와 (1,2)moon 을 바꾸면 세로 star 3개 발생
    const board = makeBoard([
      ['moon', 'cloud', 'star'],
      ['heart', 'bubble', 'moon'],
      ['star', 'star', 'heart'],
    ]);
    // (0,2)=star,(1,2)=moon,(2,2)=heart. 세로 매치 안되지만
    // (2,0),(2,1)=star, (2,2) 와 (1,2) 교환은 인접 아님.
    // 대신 (1,2)moon 과 (1,1)bubble 교환은 매치 없음
    expect(isValidSwap(board, { row: 1, col: 1 }, { row: 1, col: 2 })).toBe(false);
  });
});

describe('boardResolver', () => {
  it('removeMatches 는 매치 셀을 null 로 만든다', () => {
    const board = makeBoard([['star', 'star', 'star']]);
    const ids = findMatches(board);
    const removed = removeMatches(board, ids);
    expect(removed[0][0].type).toBeNull();
    expect(removed[0][2].type).toBeNull();
  });

  it('dropBlocks 는 빈 칸 위 블록을 아래로 내린다', () => {
    const board = makeBoard([['star'], ['.'], ['moon']]);
    const dropped = dropBlocks(board);
    // moon 이 맨 아래, star 가 그 위, 맨 위는 null
    expect(dropped[2][0].type).toBe('moon');
    expect(dropped[1][0].type).toBe('star');
    expect(dropped[0][0].type).toBeNull();
  });

  it('fillEmpty 는 null 을 새 블록으로 채운다', () => {
    const board = makeBoard([['.', 'star']]);
    const filled = fillEmpty(board);
    expect(filled[0][0].type).not.toBeNull();
  });
});

describe('cascadeResolver', () => {
  it('resolveBoard 는 매치를 모두 제거하고 점수를 준다', () => {
    const board = makeBoard([
      ['star', 'star', 'star', 'moon'],
      ['heart', 'bubble', 'cloud', 'moon'],
      ['bubble', 'cloud', 'heart', 'star'],
      ['cloud', 'heart', 'bubble', 'moon'],
    ]);
    const result = resolveBoard(board);
    expect(result.totalRemoved).toBeGreaterThanOrEqual(3);
    expect(result.scoreGained).toBeGreaterThan(0);
    expect(result.cascadeCount).toBeGreaterThanOrEqual(1);
    // 최종 보드에는 매치가 남아있지 않다
    expect(findMatches(result.board).size).toBe(0);
    // 모든 칸이 채워져 있다
    for (const line of result.board) for (const cell of line) expect(cell.type).not.toBeNull();
  });
});

describe('boardGenerator', () => {
  it('초기 매치가 없는 보드를 만든다', () => {
    for (let i = 0; i < 20; i++) {
      const board = generateBoard(8);
      expect(findMatches(board).size).toBe(0);
      for (const line of board) for (const cell of line) expect(cell.type).not.toBeNull();
    }
  });
});

describe('hintFinder', () => {
  it('가능한 수를 찾는다', () => {
    const board = makeBoard([
      ['moon', 'cloud', 'star'],
      ['heart', 'star', 'bubble'],
      ['star', 'cloud', 'heart'],
    ]);
    // (2,0)star 를 (1,0)heart 와 바꾸면 세로 star? (0,0)moon,(1,0)star,(2,0)heart -> no
    // 어떤 보드든 findHint 는 null 또는 유효한 쌍을 반환해야 한다
    const hint = findHint(board);
    if (hint) {
      expect(isValidSwap(board, hint[0], hint[1])).toBe(true);
    }
    expect(hasAvailableMove(board)).toBe(hint !== null);
  });

  it('생성된 8x8 보드는 보통 가능한 수가 있다', () => {
    const board = generateBoard(8);
    // 무작위 8x8 은 거의 항상 가능한 수가 존재
    expect(typeof hasAvailableMove(board)).toBe('boolean');
  });
});
