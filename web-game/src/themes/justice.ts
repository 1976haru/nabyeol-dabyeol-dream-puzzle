import type { ThemeConfig } from './ThemeConfig';

/**
 * 법무부 버전 테마 — 법질서 교육.
 * 6개 블록 슬롯을 법 관련 상징으로 reskin 하고, OX 퀴즈/부모 보고서를 켠다.
 */
export const JusticeTheme: ThemeConfig = {
  id: 'justice',

  bgGradient: 'linear-gradient(160deg, #1c2b4a 0%, #2c3e66 55%, #41507a 100%)',
  primary: '#c9a227',
  accent: '#4080c0',

  blocks: {
    star: { emoji: '⚖️', color: '#f3e9c8', label: '저울 (공정)' },
    moon: { emoji: '🔨', color: '#e6cfa3', label: '의사봉 (집행)' },
    cloud: { emoji: '📜', color: '#efe2c6', label: '법률 (규칙)' },
    bubble: { emoji: '🤝', color: '#cfe3f5', label: '악수 (존중)' },
    heart: { emoji: '🛡️', color: '#d6ecd6', label: '방패 (권리)' },
    rainbow: { emoji: '🏛️', color: '#e3d6f0', label: '법원 (정의)' },
  },

  appTitle: '⚖️ 법이 친구!',
  menuSubtitle: '법질서 퍼즐로 배우는 올바른 생활',
  intro: '⚖️ 퍼즐을 맞추며 법질서를 배워봐요!',
  stageSelectTitle: '법 탐험 지도',
  collectionTitle: '법 친구 도감',
  hintLabel: '⚖️ 법 힌트',

  hasQuiz: true,
  hasParentReport: true,

  matchEducationMessages: {
    star: [
      '⚖️ 저울처럼 공평하게! 법은 모두에게 똑같이 적용돼요.',
      '⚖️ 친구를 차별하지 않는 것도 법의 정신이에요.',
    ],
    moon: ['🔨 약속을 어기면 책임을 져요.', '🔨 규칙을 지키면 모두가 행복해요.'],
    cloud: ['📜 학교 규칙도 법이에요. 작은 약속부터!', '📜 신호등 지키기도 법이에요.'],
    bubble: ['🤝 친구와 약속한 건 꼭 지켜요.', '🤝 서로 존중하면 다툴 일이 없어요.'],
    heart: ['🛡️ 내 권리도 친구의 권리도 소중해요.', '🛡️ 어려운 친구를 도와요.'],
    rainbow: ['🏛️ 법원은 정의를 세우는 곳이에요!', '🏛️ 판사는 공정한 결정을 내려요.'],
  },
};
