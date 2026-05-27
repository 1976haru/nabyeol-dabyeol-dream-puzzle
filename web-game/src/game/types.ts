// 게임 핵심 타입 정의

/** 보드 한 변의 칸 수 */
export const BOARD_SIZE = 8;

/**
 * 블록 종류 (고정 6슬롯).
 * 엔진은 이 키로만 동작하고, 각 슬롯의 이모지/색은 테마가 결정한다 (src/themes).
 */
export const BLOCK_TYPES = ['star', 'moon', 'cloud', 'bubble', 'heart', 'rainbow'] as const;
export type BlockType = (typeof BLOCK_TYPES)[number];

/** 보드의 한 칸 */
export interface Cell {
  /** 위치 기반 고유 id ("row-col") */
  id: string;
  row: number;
  col: number;
  /** 블록 종류. 비어 있으면 null */
  type: BlockType | null;
}

/** 2차원 보드. board[row][col] */
export type Board = Cell[][];

/** 보드 좌표 */
export interface Pos {
  row: number;
  col: number;
}

/** 위치 기반 cell id 생성 */
export function cellId(row: number, col: number): string {
  return `${row}-${col}`;
}
