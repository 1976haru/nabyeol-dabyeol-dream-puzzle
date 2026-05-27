export interface Story {
  intro: string;
  /** 스테이지별 시작 메시지 (index = stageId - 1) */
  stageStart: string[];
  /** 클리어 응원 문구 */
  clearMessages: string[];
  /** 실패 위로 문구 */
  failMessages: string[];
}

export const STORY: Story = {
  intro: '나별이와 다별이가 꿈나라의 별을 찾아 퍼즐 여행을 떠나요! ✨',
  stageStart: [
    '반짝이는 첫 별을 찾으러 가요! 준비됐나요? 🌟',
    '동글동글 꿈방울을 모아 볼까요? 🫧',
    '달토끼가 달떡 파티에 초대했어요! 🐰',
    '별잉크로 밤하늘에 그림을 그려요! ✏️',
    '꿈고래가 사는 바다로 풍덩! 🐳',
    '폭신한 구름 위를 사뿐사뿐 걸어요! ☁️',
    '별여우가 사는 반짝이는 숲이에요! 🦊',
    '일곱 빛깔 무지개 다리를 건너요! 🌈',
    '저 멀리 꿈나라 성이 보여요! 🏰',
    '모두 모여 별빛 축제를 열어요! 🎉',
  ],
  clearMessages: [
    '우와! 정말 잘했어요! 🌟',
    '반짝반짝 빛나는 솜씨예요! ✨',
    '대단해요! 별을 가득 모았어요! ⭐',
    '꿈나라 친구들이 박수쳐요! 👏',
    '최고예요! 다음 별로 가 볼까요? 💫',
    '멋진 퍼즐 실력이에요! 🧩',
    '와! 꿈이 한 뼘 더 자랐어요! 🌱',
    '눈부신 활약이었어요! 🌈',
    '꿈나라가 더 환해졌어요! 🌙',
    '꿈을 이뤘어요! 정말 자랑스러워요! 🏆',
  ],
  failMessages: [
    '괜찮아요, 다시 도전하면 돼요! 💪',
    '조금만 더 하면 별을 모을 수 있어요! ⭐',
    '실수해도 괜찮아요. 천천히 해 봐요! 🌟',
    '포기하지 마요, 우리가 응원할게요! 📣',
    '다음엔 꼭 성공할 거예요! 🍀',
  ],
};

/** 스테이지 시작 메시지 */
export function getStageStart(stageId: number): string {
  return STORY.stageStart[stageId - 1] ?? STORY.intro;
}

/** 무작위 클리어 응원 문구 */
export function randomClearMessage(): string {
  return STORY.clearMessages[Math.floor(Math.random() * STORY.clearMessages.length)];
}

/** 무작위 실패 위로 문구 */
export function randomFailMessage(): string {
  return STORY.failMessages[Math.floor(Math.random() * STORY.failMessages.length)];
}
