import type { ThemeConfig } from './ThemeConfig';

/** 기본 테마 — 나별다별 꿈퍼즐 (기존 게임 외형/문구 그대로) */
export const NabyeolTheme: ThemeConfig = {
  id: 'nabyeol',

  bgGradient: 'linear-gradient(160deg, #4263eb 0%, #7048e8 55%, #9c36b5 100%)',
  primary: '#5c7cfa',
  accent: '#ff922b',

  blocks: {
    star: { emoji: '⭐', color: '#fff3bf', label: '별' },
    moon: { emoji: '🌙', color: '#d0bfff', label: '달' },
    cloud: { emoji: '☁️', color: '#e7f5ff', label: '구름' },
    bubble: { emoji: '🫧', color: '#c5f6fa', label: '꿈방울' },
    heart: { emoji: '💜', color: '#fcc2d7', label: '마음' },
    rainbow: { emoji: '🌈', color: '#ffd8a8', label: '무지개' },
  },

  appTitle: '나별다별 꿈퍼즐 ✨',
  menuSubtitle: '꿈을 모아 별을 찾아요',
  intro: '나별이와 다별이가 꿈나라의 별을 찾아 퍼즐 여행을 떠나요! ✨',
  stageSelectTitle: '스테이지 선택',
  collectionTitle: '도감',
  hintLabel: '💡 힌트',

  hasQuiz: false,
  hasParentReport: false,

  matchEducationMessages: {},
};
