import { useEffect, useRef, useState } from 'react';
import { getStage, LAST_STAGE_ID } from '../data/stages';
import { getStageStart, randomClearMessage, randomFailMessage } from '../data/story';
import { loadSave, recordClear, setLastStage, setSoundOn } from '../save/saveManager';
import { soundEngine } from '../audio/soundEngine';
import { useGame } from '../game/hooks/useGame';
import { HUD } from '../components/HUD';
import { GameBoard } from '../components/GameBoard';
import { ResultPopup } from '../components/ResultPopup';
import { Tutorial } from '../components/Tutorial';

interface Props {
  stageId: number;
  onNext: (nextStageId: number) => void;
  onMenu: () => void;
}

/** 게임 플레이 화면 (핵심 화면) */
export function GameScreen({ stageId, onNext, onMenu }: Props) {
  const stage = getStage(stageId) ?? getStage(1)!;
  const game = useGame(stage);

  const [soundOn, setSoundState] = useState(() => loadSave().soundOn);
  const [showStart, setShowStart] = useState(true);
  const [resultMessage, setResultMessage] = useState('');
  const recordedRef = useRef(false);

  // 소리 설정 동기화
  useEffect(() => {
    soundEngine.setEnabled(soundOn);
  }, [soundOn]);

  // 스테이지 진입 시: 마지막 스테이지 기록 + 시작 배너 자동 숨김.
  // (GameScreen 은 stageId 를 key 로 매 스테이지 새로 마운트되므로 상태는 초기값으로 시작)
  useEffect(() => {
    setLastStage(stageId);
    const t = setTimeout(() => setShowStart(false), 2200);
    return () => clearTimeout(t);
  }, [stageId]);

  // 클리어/실패 결과 처리
  useEffect(() => {
    if (game.status === 'clear' && !recordedRef.current) {
      recordedRef.current = true;
      recordClear(stageId, game.score);
      setResultMessage(stage.clearMessage || randomClearMessage());
    } else if (game.status === 'fail' && !recordedRef.current) {
      recordedRef.current = true;
      setResultMessage(randomFailMessage());
    }
  }, [game.status, game.score, stageId, stage.clearMessage]);

  const toggleSound = () => {
    const next = !soundOn;
    setSoundState(next);
    setSoundOn(next);
  };

  const hasNext = stageId < LAST_STAGE_ID;
  const goNext = () => {
    if (hasNext) onNext(stageId + 1);
    else onMenu();
  };
  const retry = () => {
    recordedRef.current = false;
    game.resetBoard();
  };

  return (
    <div style={{ minHeight: '100vh', padding: 16, boxSizing: 'border-box', display: 'flex', flexDirection: 'column', gap: 12 }}>
      <HUD
        stageName={stage.name}
        score={game.score}
        targetScore={stage.targetScore}
        movesLeft={game.movesLeft}
        soundOn={soundOn}
        onToggleSound={toggleSound}
        onHint={game.showHint}
        onMenu={onMenu}
      />

      <GameBoard board={game.board} selected={game.selected} hint={game.hint} onCellClick={game.handleCellClick} />

      {game.lastCascade >= 2 && game.status === 'playing' && (
        <p style={{ textAlign: 'center', color: 'white', fontWeight: 700, margin: 0 }}>
          {game.lastCascade}연쇄! 🔥
        </p>
      )}

      {showStart && game.status === 'playing' && (
        <div style={startBanner}>{getStageStart(stageId)}</div>
      )}

      {game.status !== 'playing' && (
        <ResultPopup
          status={game.status}
          score={game.score}
          targetScore={stage.targetScore}
          message={resultMessage}
          onRetry={retry}
          onNext={hasNext ? goNext : undefined}
          onMenu={onMenu}
        />
      )}

      <Tutorial />
    </div>
  );
}

const startBanner: React.CSSProperties = {
  position: 'fixed',
  top: '40%',
  left: '50%',
  transform: 'translate(-50%, -50%)',
  background: 'rgba(33,37,41,0.92)',
  color: 'white',
  padding: '16px 22px',
  borderRadius: 16,
  fontSize: 17,
  maxWidth: '85%',
  textAlign: 'center',
  zIndex: 30,
  pointerEvents: 'none',
};
