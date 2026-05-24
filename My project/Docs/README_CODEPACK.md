# 나별다별 꿈퍼즐 전체 코드팩

이 코드팩은 Unity 6 + C# 기반 `나별다별 꿈퍼즐` MVP용 스크립트 묶음입니다.

## 적용 원칙
1. ZIP 안의 `Assets/_Project/Scripts` 폴더를 Unity 프로젝트의 동일 경로에 복사합니다.
2. Scene, Prefab, ProjectSettings는 자동 수정하지 마세요.
3. Unity Console 빨간 에러를 먼저 확인하세요.
4. BlockPrefab에는 SpriteRenderer, Block, BlockVisual, BlockInput, BoxCollider2D가 필요합니다.
5. GameScene에는 BoardManager, ScoreManager, MoveManager, StageManager, GoalManager가 필요합니다.

## 포함 범위
- 퍼즐 핵심 코드
- 점수/이동/스테이지/목표 코드
- 저장/수집/부모설정/스토리/에이전트/UI 기본 코드

## Claude Code 적용 지시문
아래처럼 짧게 시키세요.

"이 ZIP의 파일들을 지정 경로에 반영해라. Scene/Prefab/ProjectSettings는 수정하지 마라. 컴파일 에러만 C# 코드 수준에서 최소 수정하고 변경 파일 목록을 출력해라."
