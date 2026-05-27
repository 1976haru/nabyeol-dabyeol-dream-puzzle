import { useCallback, useEffect, useRef, useState } from 'react';
import { getStage, LAST_STAGE_ID } from '../data/stages';
import { getStageStart, randomClearMessage, randomFailMessage } from '../data/story';
import { loadSave, recordClear, setLastStage, setSoundOn } from '../save/saveManager';
import { soundEngine } from '../audio/soundEngine';
import { getTheme } from '../themes';
import { useGame } from '../game/hooks/useGame';
import { HUD } from '../components/HUD';
import { GameBoard } from '../components/GameBoard';
import { ResultPopup } from '../components/ResultPopup';
import { Tutorial } from '../components/Tutorial';

/** 테마가 제공하는 모든 교육 메시지를 한 줄로 펼친 목록 (없으면 빈 배열) */
const EDU_MESSAGES: string[] = Object.values(getTheme().matchEducationMessages).flat();

interface Props {
  stageId: number;
  onNext: (nextStageId: number) => void;
  onMenu: () => void;
}

/** 게임 플레이 화면 (핵심 화면) */
export function GameScreen({ stageId, onNext, onMenu }: Props) {
  const stage = getStage(stageId) ?? getStage(1)!;

  const [soundOn, setSoundState] = useState(() => loadSave().soundOn);
  const [showStart, setShowStart] = useState(true);
  const [resultMessage, setResultMessage] = useState('');
  const [eduTip, setEduTip] = useState('');
  const recordedRef = useRef(false);
  const eduTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // 교육 메시지: 큰 연쇄(3+) 해소 시 일정 확률로 테마 메시지를 잠깐 표시 (justice 등).
  // 효과(effect)가 아니라 매치 해소 "이벤트"에 반응한다.
  const handleResolve = useCallback((cascade: number) => {
    if (EDU_MESSAGES.length === 0 || cascade < 3 || Math.random() > 0.5) return;
    setEduTip(EDU_MESSAGES[Math.floor(Math.random() * EDU_MESSAGES.length)]);
    if (eduTimerRef.current) clearTimeout(eduTimerRef.current);
    eduTimerRef.current = setTimeout(() => setEduTip(''), 3500);
  }, []);

  const game = useGame(stage, handleResolve);

  // 언마운트 시 교육 메시지 타이머 정리
  useEffect(() => () => {
    if (eduTimerRef.current) clearTimeout(eduTimerRef.current);
  }, []);

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

      {eduTip && game.status === 'playing' && (
        <div style={eduBox}>{eduTip}</div>
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

const eduBox: React.CSSProperties = {
  maxWidth: 480,
  width: '100%',
  margin: '0 auto',
  background: 'rgba(64,128,192,0.92)',
  color: 'white',
  padding: '12px 16px',
  borderRadius: 14,
  fontSize: 15,
  fontWeight: 600,
  textAlign: 'center',
  boxSizing: 'border-box',
};

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
