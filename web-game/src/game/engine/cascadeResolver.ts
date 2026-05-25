import type { Board } from '../types';
import { findMatches } from './matchFinder';
import { removeMatches, dropBlocks, fillEmpty } from './boardResolver';

/** 한 번의 교환으로 발생한 연쇄 처리 결과 */
export interface CascadeResult {
  /** 최종 안정화된 보드 */
  board: Board;
  /** 제거된 블록 총 개수 */
  totalRemoved: number;
  /** 연쇄 횟수 (매치 발생 라운드 수) */
  cascadeCount: number;
  /** 획득 점수 */
  scoreGained: number;
}

/** 블록 1개당 기본 점수 */
const BASE_SCORE = 10;

/**
 * 매치 → 제거 → 낙하 → 채우기를 매치가 없을 때까지 반복한다.
 * 연쇄가 깊어질수록 점수 배율이 커진다.
 */
export function resolveBoard(board: Board): CascadeResult {
  let current = board;
  let totalRemoved = 0;
  let cascadeCount = 0;
  let scoreGained = 0;

  let matches = findMatches(current);
  while (matches.size > 0) {
    cascadeCount++;
    const removed = matches.size;
    totalRemoved += removed;
    // 연쇄 단계가 깊을수록 배율 증가 (1배, 2배, 3배 ...)
    scoreGained += removed * BASE_SCORE * cascadeCount;

    current = removeMatches(current, matches);
    current = dropBlocks(current);
    current = fillEmpty(current);
    matches = findMatches(current);
  }

  return { board: current, totalRemoved, cascadeCount, scoreGained };
}
