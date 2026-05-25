# GitHub Pages 설정 (사람이 직접, 약 5분)

웹게임은 `.github/workflows/deploy.yml` 워크플로가 `web-game` 을 빌드해
`gh-pages` 브랜치로 자동 배포합니다. 처음 한 번만 아래 설정을 해 주세요.

## 순서
1. https://github.com/1976haru/nabyeol-dabyeol-dream-puzzle → **Settings** → **Pages**
2. **Build and deployment → Source**: `Deploy from a branch`
3. **Branch**: `gh-pages`, 폴더 `/ (root)` 선택
4. **Save**
5. 상단 **Actions** 탭에서 "Deploy web game" 워크플로가 완료(초록 체크)될 때까지 대기 (5~10분)
6. 접속 확인: https://1976haru.github.io/nabyeol-dabyeol-dream-puzzle/

## 참고
- `main`(또는 `web-migration`) 브랜치에 push 될 때마다 자동 재배포됩니다.
- 워크플로가 처음 실행되면서 `gh-pages` 브랜치가 자동 생성됩니다.
  3번 단계에서 `gh-pages` 가 안 보이면, Actions 탭에서 워크플로가 한 번
  성공한 뒤 다시 Settings → Pages 로 돌아오면 보입니다.
- Settings → Actions → General → Workflow permissions 가
  **Read and write permissions** 인지 확인하세요 (gh-pages 브랜치 생성에 필요).
