import { describe, it, expect } from 'vitest';
import { THEMES, getTheme, ACTIVE_THEME_ID } from './index';
import { BLOCK_TYPES } from '../game/types';
import { ALL_QUIZ_QUESTIONS, QUIZ_SET_KEYS } from '../data/quizzes';

describe('theme system', () => {
  it('ACTIVE_THEME_ID 는 등록된 테마를 가리킨다', () => {
    expect(THEMES[ACTIVE_THEME_ID]).toBeDefined();
    expect(getTheme().id).toBe(ACTIVE_THEME_ID);
  });

  it('모든 테마는 6개 블록 슬롯 전부에 외형을 제공한다', () => {
    for (const theme of Object.values(THEMES)) {
      for (const type of BLOCK_TYPES) {
        const v = theme.blocks[type];
        expect(v, `${theme.id}.${type}`).toBeDefined();
        expect(v.emoji.length).toBeGreaterThan(0);
        expect(v.color).toMatch(/^#|rgb/);
        expect(v.label.length).toBeGreaterThan(0);
      }
    }
  });

  it('justice 테마는 퀴즈/보고서가 켜져 있고 nabyeol 은 꺼져 있다', () => {
    expect(THEMES.justice.hasQuiz).toBe(true);
    expect(THEMES.justice.hasParentReport).toBe(true);
    expect(THEMES.nabyeol.hasQuiz).toBe(false);
    expect(THEMES.nabyeol.hasParentReport).toBe(false);
  });
});

describe('quiz data', () => {
  it('5세트 × 3문제 = 15문제', () => {
    expect(QUIZ_SET_KEYS).toHaveLength(5);
    expect(ALL_QUIZ_QUESTIONS).toHaveLength(15);
  });

  it('문제 id 는 중복되지 않고 모두 설명을 가진다', () => {
    const ids = new Set(ALL_QUIZ_QUESTIONS.map((q) => q.id));
    expect(ids.size).toBe(ALL_QUIZ_QUESTIONS.length);
    for (const q of ALL_QUIZ_QUESTIONS) {
      expect(q.explanation.length).toBeGreaterThan(0);
    }
  });
});
