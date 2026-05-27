import type { BlockType } from '../game/types';

/** 블록 한 종류의 테마별 외형. 6개 블록 슬롯은 고정이고 테마가 reskin 한다. */
export interface BlockVisual {
  emoji: string;
  color: string;
  /** 블록 의미 라벨 (교육용 표시 등) */
  label: string;
}

/**
 * 도감 캐릭터의 테마별 표시 정보 (표시 전용).
 * 캐릭터 id 와 저장 데이터는 그대로 두고, 화면에 보이는 문구만 바꾼다.
 */
export interface CharacterOverride {
  name: string;
  description: string;
  /** 능력 설명 문구 (override 가 있을 때만 도감에 표시) */
  ability: string;
}

/**
 * 앱 전체의 외형/문구/기능을 결정하는 테마 설정.
 * ACTIVE_THEME_ID 한 줄만 바꾸면 게임 전체가 이 설정대로 바뀐다.
 */
export interface ThemeConfig {
  id: string;

  /* ── 외형 ── */
  /** body 배경 그라데이션 (CSS 변수 --bg-gradient 로 적용) */
  bgGradient: string;
  /** 강조색 (--theme-primary) */
  primary: string;
  /** 보조 강조색 (--theme-accent) */
  accent: string;
  /** 고정 6슬롯 블록의 외형 */
  blocks: Record<BlockType, BlockVisual>;

  /* ── 문구 ── */
  appTitle: string;
  menuSubtitle: string;
  intro: string;
  stageSelectTitle: string;
  collectionTitle: string;
  hintLabel: string;

  /* ── 기능 토글 ── */
  /** 메뉴에 OX 퀴즈 진입 노출 */
  hasQuiz: boolean;
  /** 메뉴에 부모(학습) 보고서 진입 노출 */
  hasParentReport: boolean;

  /* ── 교육 ── */
  /** 큰 연쇄 시 블록 종류별로 보여줄 교육 메시지 (없으면 표시 안 함) */
  matchEducationMessages: Partial<Record<BlockType, string[]>>;

  /**
   * 도감 캐릭터의 테마별 표시 override (캐릭터 id → 표시 정보).
   * 지정하지 않으면 기본 캐릭터 데이터(data/characters.ts)를 그대로 보여준다.
   */
  collectionCharacterOverrides?: Record<string, CharacterOverride>;
}
