import type { ThemeConfig } from './ThemeConfig';
import { NabyeolTheme } from './nabyeol';
import { JusticeTheme } from './justice';

export type { ThemeConfig, BlockVisual, CharacterOverride } from './ThemeConfig';

export const THEMES: Record<string, ThemeConfig> = {
  nabyeol: NabyeolTheme,
  justice: JusticeTheme,
};

// ★ 이 한 줄로 앱 전체 테마를 바꿉니다.
//   'nabyeol' = 기본 (꿈퍼즐)  /  'justice' = 법무부 버전 (법질서 교육 + OX 퀴즈)
export const ACTIVE_THEME_ID = 'nabyeol';

/** 현재 활성 테마 설정 */
export function getTheme(): ThemeConfig {
  return THEMES[ACTIVE_THEME_ID] ?? NabyeolTheme;
}

/** 테마의 배경/강조색을 CSS 변수로 주입한다. main.tsx 에서 1회 호출 */
export function applyThemeCss(): void {
  if (typeof document === 'undefined') return;
  const theme = getTheme();
  const root = document.documentElement;
  root.style.setProperty('--bg-gradient', theme.bgGradient);
  root.style.setProperty('--theme-primary', theme.primary);
  root.style.setProperty('--theme-accent', theme.accent);
}
