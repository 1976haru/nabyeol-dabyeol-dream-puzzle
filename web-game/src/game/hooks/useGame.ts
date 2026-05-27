import { useCallback, useEffect, useRef, useState } from 'react';
import type { Board, Pos } from '../types';
import { generateBoard } from '../engine/boardGenerator';
import { isValidSwap, swapCells } from '../engine/boardActions';
import { resolveBoard } from '../engine/cascadeResolver';
import { findHint, hasAvailableMove } from '../engine/hintFinder';
import type { Hint } from '../engine/hintFinder';
import { soundEngine } from '../../audio/soundEngine';
import type { Stage } from '../../data/stages';

export type GameStatus = 'playing' | 'clear' | 'fail';

export interface UseGameResult {
  board: Board;
  score: number;
  movesLeft: number;
  status: GameStatus;
  selected: Pos | null;
  hint: Hint | null;
  lastCascade: number;
  handleCellClick: (pos: Pos) => void;
  showHint: () => void;
  resetBoard: () => void;
}

/** 가능한 수가 있는 새 보드를 생성 (최대 몇 번 재시도) */
function freshBoard(): Board {
  let board = generateBoard();
  let guard = 0;
  while (!hasAvailableMove(board) && guard < 20) {
    board = generateBoard();
    guard++;
  }
  return board;
}

/**
 * 한 스테이지의 게임 상태와 조작 함수를 제공하는 훅.
 * @param onResolve 유효한 교환으로 매치가 해소될 때마다 연쇄 수와 함께 호출 (이벤트성 콜백)
 */
export function useGame(stage: Stage, onResolve?: (cascade: number) => void): UseGameResult {
  // 콜백은 ref 로 보관해 handleCellClick 의 의존성을 늘리지 않는다
  const onResolveRef = useRef(onResolve);
  useEffect(() => {
    onResolveRef.current = onResolve;
  });

  const [board, setBoard] = useState<Board>(() => freshBoard());
  const [score, setScore] = useState(0);
  const [movesLeft, setMovesLeft] = useState(stage.maxMoves);
  const [status, setStatus] = useState<GameStatus>('playing');
  const [selected, setSelected] = useState<Pos | null>(null);
  const [hint, setHint] = useState<Hint | null>(null);
  const [lastCascade, setLastCascade] = useState(0);

  const resetBoard = useCallback(() => {
    setBoard(freshBoard());
    setScore(0);
    setMovesLeft(stage.maxMoves);
    setStatus('playing');
    setSelected(null);
    setHint(null);
    setLastCascade(0);
  }, [stage.maxMoves]);

  const handleCellClick = useCallback(
    (pos: Pos) => {
      if (status !== 'playing') return;
      setHint(null);
      soundEngine.unlock();

      // 1) 선택된 셀이 없으면 선택
      if (selected === null) {
        setSelected(pos);
        soundEngine.playSelect();
        return;
      }

      // 2) 같은 셀 재클릭 → 선택 해제
      if (selected.row === pos.row && selected.col === pos.col) {
        setSelected(null);
        return;
      }

      // 3) 유효한 교환
      if (isValidSwap(board, selected, pos)) {
        soundEngine.playSwap();
        const swapped = swapCells(board, selected, pos);
        const result = resolveBoard(swapped);
        soundEngine.playMatch(result.cascadeCount);

        let nextBoard = result.board;
        // 가능한 수가 없으면 보드 재생성
        if (!hasAvailableMove(nextBoard)) nextBoard = freshBoard();

        const newScore = score + result.scoreGained;
        const newMoves = movesLeft - 1;

        setBoard(nextBoard);
        setScore(newScore);
        setMovesLeft(newMoves);
        setSelected(null);
        setLastCascade(result.cascadeCount);
        onResolveRef.current?.(result.cascadeCount);

        // 클리어 / 실패 판정
        if (newScore >= stage.targetScore) {
          setStatus('clear');
          soundEngine.playClear();
        } else if (newMoves <= 0) {
          setStatus('fail');
          soundEngine.playFail();
        }
        return;
      }

      // 4) 유효하지 않은 교환 → 선택만 새 셀로 전환
      soundEngine.playInvalid();
      setSelected(pos);
    },
    [board, score, movesLeft, status, selected, stage.targetScore],
  );

  const showHint = useCallback(() => {
    setHint(findHint(board));
  }, [board]);

  return {
    board,
    score,
    movesLeft,
    status,
    selected,
    hint,
    lastCascade,
    handleCellClick,
    showHint,
    resetBoard,
  };
}
