# 나별다별 꿈퍼즐 🌟

나별이와 다별이가 꿈나라의 별을 찾는 3-매치 퍼즐 게임입니다.
초등학생도 쉽게 즐길 수 있도록 밝고 따뜻한 톤으로 만들었어요.

🎮 **플레이:** https://1976haru.github.io/nabyeol-dabyeol-dream-puzzle/

## 특징
- 10개의 스테이지 (난이도 easy → hard)
- 연쇄(cascade) 보너스 점수
- 캐릭터 도감 6종 (스테이지 클리어로 해금)
- 진행도 자동 저장 (localStorage)
- 힌트 / 효과음 / 튜토리얼
- PWA 설치 지원 (홈화면 추가 → 앱처럼 실행)
- Android(Capacitor) 빌드 지원
- **테마 시스템**: 한 줄 설정으로 게임 전체(블록·문구·배경) 전환. 법무부 버전(법질서 교육 + OX 퀴즈 + 부모 보고서) 포함

## 테마 전환
`web-game/src/themes/index.ts` 의 `ACTIVE_THEME_ID` 한 줄만 바꾸면 됩니다.
```ts
export const ACTIVE_THEME_ID = 'nabyeol'; // 기본 (꿈퍼즐)
// export const ACTIVE_THEME_ID = 'justice'; // 법무부 버전 (법질서 교육 + OX 퀴즈)
```
- `nabyeol` — 기본 꿈퍼즐
- `justice` — 6종 블록을 법 상징(⚖️🔨📜🤝🛡️🏛️)으로 reskin, 큰 연쇄 시 교육 메시지, 메뉴에 **법 퀴즈**·**부모 학습 보고서** 추가
- 새 테마 추가: `src/themes/justice.ts` 를 참고해 새 파일을 만들고 `index.ts` 의 `THEMES` 에 등록

## 로컬 실행
```bash
cd web-game
npm install
npm run dev
```

## 빌드 / 테스트
```bash
cd web-game
npm run build   # 프로덕션 빌드 (dist/)
npm test        # 게임 엔진 단위 테스트 (Vitest)
```

## 기술 스택
React + Vite + TypeScript + PWA + Capacitor(Android)

## 프로젝트 구조
```
web-game/
  src/
    game/        # 게임 엔진 (매치·낙하·연쇄)·타입·훅
    themes/      # 테마 시스템 (nabyeol/justice, ACTIVE_THEME_ID 한 줄 전환)
    components/  # 보드, 셀, HUD, 팝업 등 UI
    screens/     # 메뉴/스테이지선택/도감/게임/퀴즈/부모보고서 화면
    data/        # 스테이지·캐릭터·스토리·퀴즈 데이터
    save/        # localStorage 저장 관리 (학습 진도 포함)
    audio/       # WebAudio 효과음
  public/        # manifest, 서비스워커, 아이콘, 개인정보처리방침
  android/       # Capacitor Android 프로젝트
docs/            # 배포·테스트·스토어 등록 체크리스트
```

> 참고: 저장소 루트에는 초기 Unity 프로토타입(`Assets/`, `ProjectSettings/` 등)이
> 함께 보관되어 있으며, 실제 서비스는 `web-game/` 웹 버전으로 진행합니다.

## 배포 / 스토어
- GitHub Pages 설정: [docs/github_pages_setup.md](docs/github_pages_setup.md)
- 직접 테스트 체크리스트: [docs/test_checklist.md](docs/test_checklist.md)
- Google Play 등록: [docs/google_play_checklist.md](docs/google_play_checklist.md)
