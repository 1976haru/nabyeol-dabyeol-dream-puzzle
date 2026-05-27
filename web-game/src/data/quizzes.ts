/** OX 퀴즈 한 문제 */
export interface QuizQuestion {
  id: string;
  question: string;
  /** 정답이 O(맞음)인지 */
  isTrue: boolean;
  explanation: string;
}

/** 법무부 버전 OX 퀴즈 (5세트 × 3문제) */
export const QUIZZES: Record<string, QuizQuestion[]> = {
  quiz_1: [
    { id: 'q1', question: '친구의 물건을 허락 없이 쓰면 안 돼요.', isTrue: true, explanation: '친구의 물건은 친구의 것! 꼭 허락받고 써요.' },
    { id: 'q2', question: '신호등이 빨간불일 때 길을 건너도 돼요.', isTrue: false, explanation: '빨간불은 멈춤! 초록불에 건너야 안전해요.' },
    { id: 'q3', question: '학교 규칙은 모두 함께 지켜야 해요.', isTrue: true, explanation: '규칙을 지키면 학교가 즐거워져요.' },
  ],
  quiz_2: [
    { id: 'q4', question: '친구를 괴롭히는 건 법으로 금지돼 있어요.', isTrue: true, explanation: '학교폭력은 절대 안 돼요!' },
    { id: 'q5', question: '다른 사람의 비밀을 함부로 말해도 돼요.', isTrue: false, explanation: '개인정보는 소중해요.' },
    { id: 'q6', question: '어른께 인사하는 것도 예절이에요.', isTrue: true, explanation: '예절은 작은 법! 잘 지켜요.' },
  ],
  quiz_3: [
    { id: 'q7', question: '쓰레기를 길에 버려도 괜찮아요.', isTrue: false, explanation: '쓰레기는 쓰레기통에!' },
    { id: 'q8', question: '위급할 때는 119에 전화할 수 있어요.', isTrue: true, explanation: '119는 우리를 도와줘요!' },
    { id: 'q9', question: '친구의 일기를 몰래 봐도 돼요.', isTrue: false, explanation: '친구의 비밀은 존중해요.' },
  ],
  quiz_4: [
    { id: 'q10', question: '모르는 사람을 따라가도 돼요.', isTrue: false, explanation: '위험해요! 부모님께 알려요.' },
    { id: 'q11', question: '층간소음은 이웃에게 피해를 줄 수 있어요.', isTrue: true, explanation: '늦은 시간엔 조용히!' },
    { id: 'q12', question: '법은 모든 사람에게 똑같이 적용돼요.', isTrue: true, explanation: '법 앞에서는 모두 평등!' },
  ],
  quiz_5: [
    { id: 'q13', question: '경찰관께 도움을 청해도 돼요.', isTrue: true, explanation: '경찰은 우리를 도와줘요!' },
    { id: 'q14', question: '친구를 차별하는 건 옳지 않아요.', isTrue: true, explanation: '모든 친구는 소중해요.' },
    { id: 'q15', question: '약속을 어겨도 책임지지 않아도 돼요.', isTrue: false, explanation: '약속은 꼭 지켜요.' },
  ],
};

/** 퀴즈 세트 키 목록 (정의 순서) */
export const QUIZ_SET_KEYS = Object.keys(QUIZZES);

/** 모든 퀴즈 문제를 한 줄로 펼친 목록 */
export const ALL_QUIZ_QUESTIONS: QuizQuestion[] = QUIZ_SET_KEYS.flatMap((k) => QUIZZES[k]);

/** id 로 퀴즈 문제 조회 */
export function getQuizQuestion(id: string): QuizQuestion | undefined {
  return ALL_QUIZ_QUESTIONS.find((q) => q.id === id);
}
