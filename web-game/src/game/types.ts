// 게임 핵심 타입 정의

/** 보드 한 변의 칸 수 */
export const BOARD_SIZE = 8;

/** 블록 종류 (꿈나라 테마) */
export const BLOCK_TYPES = ['star', 'moon', 'cloud', 'bubble', 'heart', 'rainbow'] as const;
export type BlockType = (typeof BLOCK_TYPES)[number];

/** 블록 종류별 이모지 (UI 표시용) */
export const BLOCK_EMOJI: Record<BlockType, string> = {
  star: '⭐',
  moon: '🌙',
  cloud: '☁️',
  bubble: '🫧',
  heart: '💜',
  rainbow: '🌈',
};

/** 블록 종류별 배경색 */
export const BLOCK_COLOR: Record<BlockType, string> = {
  star: '#fff3bf',
  moon: '#d0bfff',
  cloud: '#e7f5ff',
  bubble: '#c5f6fa',
  heart: '#fcc2d7',
  rainbow: '#ffd8a8',
};

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
