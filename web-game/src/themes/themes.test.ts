import { describe, it, expect } from 'vitest';
import { THEMES, getTheme, ACTIVE_THEME_ID } from './index';
import { BLOCK_TYPES } from '../game/types';
import { ALL_QUIZ_QUESTIONS, QUIZ_SET_KEYS } from '../data/quizzes';
import { CHARACTERS } from '../data/characters';

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

describe('collection character overrides', () => {
  it('justice 테마는 6개 캐릭터 전부의 표시 이름/설명/능력을 override 한다', () => {
    const overrides = THEMES.justice.collectionCharacterOverrides;
    expect(overrides).toBeDefined();
    for (const c of CHARACTERS) {
      const o = overrides![c.id];
      expect(o, c.id).toBeDefined();
      expect(o.name.length).toBeGreaterThan(0);
      expect(o.description.length).toBeGreaterThan(0);
      expect(o.ability.length).toBeGreaterThan(0);
      // 표시 이름은 기본과 달라야 한다 (예: 나별 → 공정이)
      expect(o.name).not.toBe(c.name);
    }
  });

  it('override 키는 기존 캐릭터 id 와 정확히 일치한다 (저장 데이터 호환)', () => {
    const overrideIds = Object.keys(THEMES.justice.collectionCharacterOverrides ?? {}).sort();
    const characterIds = CHARACTERS.map((c) => c.id).sort();
    expect(overrideIds).toEqual(characterIds);
  });

  it('nabyeol 테마는 override 가 없어 기본 캐릭터 이름이 유지된다', () => {
    expect(THEMES.nabyeol.collectionCharacterOverrides).toBeUndefined();
    // 기본 데이터의 대표 이름이 그대로인지 확인
    expect(CHARACTERS.find((c) => c.id === 'nabyeol')?.name).toBe('나별');
    expect(CHARACTERS.find((c) => c.id === 'moon_rabbit')?.name).toBe('달토끼');
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
