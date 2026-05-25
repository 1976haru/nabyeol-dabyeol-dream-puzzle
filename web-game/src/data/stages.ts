export type Difficulty = 'easy' | 'normal' | 'hard';

export interface Stage {
  id: number;
  name: string;
  targetScore: number;
  maxMoves: number;
  difficulty: Difficulty;
  clearMessage: string;
}

export const STAGES: Stage[] = [
  { id: 1, name: '별의 시작', targetScore: 300, maxMoves: 25, difficulty: 'easy', clearMessage: '첫 별을 찾았어요! 🌟' },
  { id: 2, name: '꿈방울 모으기', targetScore: 450, maxMoves: 22, difficulty: 'easy', clearMessage: '꿈방울이 가득해요! 🫧' },
  { id: 3, name: '달떡 파티', targetScore: 600, maxMoves: 20, difficulty: 'easy', clearMessage: '달토끼 친구가 나타났어요! 🐰' },
  { id: 4, name: '별잉크 여행', targetScore: 800, maxMoves: 20, difficulty: 'normal', clearMessage: '별잉크로 그림을 그렸어요! ✏️' },
  { id: 5, name: '꿈고래의 바다', targetScore: 1000, maxMoves: 18, difficulty: 'normal', clearMessage: '꿈고래가 헤엄쳐 왔어요! 🐳' },
  { id: 6, name: '구름 위 산책', targetScore: 1200, maxMoves: 18, difficulty: 'normal', clearMessage: '폭신한 구름을 모았어요! ☁️' },
  { id: 7, name: '별여우의 숲', targetScore: 1500, maxMoves: 16, difficulty: 'hard', clearMessage: '별여우가 반짝였어요! 🦊' },
  { id: 8, name: '무지개 다리', targetScore: 1800, maxMoves: 16, difficulty: 'hard', clearMessage: '무지개 다리를 건넜어요! 🌈' },
  { id: 9, name: '꿈나라 성', targetScore: 2200, maxMoves: 15, difficulty: 'hard', clearMessage: '꿈나라 성에 도착했어요! 🏰' },
  { id: 10, name: '별빛 축제', targetScore: 2600, maxMoves: 15, difficulty: 'hard', clearMessage: '구름곰과 별빛 축제를 열었어요! 🐻🎉' },
];

/** id 로 스테이지 조회 */
export function getStage(id: number): Stage | undefined {
  return STAGES.find((s) => s.id === id);
}

/** 마지막 스테이지 id */
export const LAST_STAGE_ID = STAGES[STAGES.length - 1].id;
