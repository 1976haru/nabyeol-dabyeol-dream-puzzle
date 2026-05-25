export interface Character {
  id: string;
  name: string;
  emoji: string;
  description: string;
  /** 해당 스테이지 클리어 시 해금. null 이면 처음부터 해금 */
  unlockStageId: number | null;
}

export const CHARACTERS: Character[] = [
  { id: 'nabyeol', name: '나별', emoji: '⭐', description: '꿈나라의 별을 모으는 씩씩한 별아이', unlockStageId: null },
  { id: 'dabyeol', name: '다별', emoji: '💫', description: '나별의 단짝 친구', unlockStageId: null },
  { id: 'moon_rabbit', name: '달토끼', emoji: '🐰', description: '달나라에 사는 토끼', unlockStageId: 3 },
  { id: 'dream_whale', name: '꿈고래', emoji: '🐳', description: '꿈의 바다를 헤엄치는 고래', unlockStageId: 5 },
  { id: 'star_fox', name: '별여우', emoji: '🦊', description: '별빛을 모으는 여우', unlockStageId: 7 },
  { id: 'cloud_bear', name: '구름곰', emoji: '🐻', description: '구름 위에 사는 포근한 곰', unlockStageId: 10 },
];

/** 처음부터 해금된 캐릭터 id 목록 */
export const DEFAULT_UNLOCKED = CHARACTERS.filter((c) => c.unlockStageId === null).map((c) => c.id);

/** 특정 스테이지 클리어로 해금되는 캐릭터 목록 */
export function charactersUnlockedByStage(stageId: number): Character[] {
  return CHARACTERS.filter((c) => c.unlockStageId === stageId);
}
