import { DEFAULT_UNLOCKED, charactersUnlockedByStage } from '../data/characters';

const KEY = 'nabyeol_save_v1';

export interface SaveData {
  /** 마지막으로 플레이한 스테이지 */
  lastStageId: number;
  /** 클리어한 스테이지 id 목록 */
  clearedStages: number[];
  /** 스테이지별 최고 점수 */
  highScores: Record<number, number>;
  /** 해금된 캐릭터 id 목록 */
  unlockedCharacters: string[];
  /** 효과음 on/off */
  soundOn: boolean;
  /** 맞힌 교육 퀴즈 문제 id 목록 (법무부 버전 학습 진도) */
  educationProgress: string[];
}

export const DEFAULT_SAVE: SaveData = {
  lastStageId: 1,
  clearedStages: [],
  highScores: {},
  unlockedCharacters: [...DEFAULT_UNLOCKED],
  soundOn: true,
  educationProgress: [],
};

/** 저장 데이터 불러오기. 손상/없음 시 기본값 반환 */
export function loadSave(): SaveData {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) return { ...DEFAULT_SAVE };
    const parsed = JSON.parse(raw) as Partial<SaveData>;
    return {
      ...DEFAULT_SAVE,
      ...parsed,
      // 누락 방지를 위해 기본 해금 캐릭터는 항상 포함
      unlockedCharacters: Array.from(
        new Set([...DEFAULT_UNLOCKED, ...(parsed.unlockedCharacters ?? [])]),
      ),
      clearedStages: parsed.clearedStages ?? [],
      highScores: parsed.highScores ?? {},
      educationProgress: parsed.educationProgress ?? [],
    };
  } catch {
    return { ...DEFAULT_SAVE };
  }
}

/** 저장 데이터 쓰기 */
export function writeSave(data: SaveData): void {
  try {
    localStorage.setItem(KEY, JSON.stringify(data));
  } catch {
    // 저장 실패는 무시 (시크릿 모드 등)
  }
}

/**
 * 스테이지 클리어 결과를 반영해 저장하고, 갱신된 데이터를 반환한다.
 * - 클리어 목록 추가, 최고 점수 갱신, 캐릭터 해금, lastStageId 갱신
 */
export function recordClear(stageId: number, score: number): SaveData {
  const data = loadSave();
  if (!data.clearedStages.includes(stageId)) {
    data.clearedStages = [...data.clearedStages, stageId];
  }
  if (score > (data.highScores[stageId] ?? 0)) {
    data.highScores[stageId] = score;
  }
  for (const c of charactersUnlockedByStage(stageId)) {
    if (!data.unlockedCharacters.includes(c.id)) {
      data.unlockedCharacters.push(c.id);
    }
  }
  data.lastStageId = stageId;
  writeSave(data);
  return data;
}

/** 마지막 플레이 스테이지 갱신 */
export function setLastStage(stageId: number): SaveData {
  const data = loadSave();
  data.lastStageId = stageId;
  writeSave(data);
  return data;
}

/** 효과음 on/off 토글 후 저장 */
export function setSoundOn(soundOn: boolean): SaveData {
  const data = loadSave();
  data.soundOn = soundOn;
  writeSave(data);
  return data;
}

/** 교육 퀴즈를 맞히면 학습 진도에 기록 (중복 제외) */
export function markEducation(quizId: string): SaveData {
  const data = loadSave();
  if (!data.educationProgress.includes(quizId)) {
    data.educationProgress = [...data.educationProgress, quizId];
    writeSave(data);
  }
  return data;
}

/** 스테이지 잠금 여부: 1번은 항상 열림, 이전 스테이지 클리어 시 열림 */
export function isStageLocked(stageId: number, cleared: number[]): boolean {
  if (stageId <= 1) return false;
  return !cleared.includes(stageId - 1);
}
