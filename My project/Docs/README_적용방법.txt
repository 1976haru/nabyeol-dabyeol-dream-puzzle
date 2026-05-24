나별다별 꿈퍼즐 Unity MVP 전체 코드 세트

적용 순서
1. Unity 6 LTS에서 2D 프로젝트 생성
2. Assets/_Project/Scripts 폴더를 프로젝트에 복사
3. GameScene 생성
4. 빈 오브젝트 GameManager 생성 후 GameManager.cs 연결
5. 빈 오브젝트 BoardManager 생성 후 BoardManager.cs 연결
6. Block 프리팹 생성: SpriteRenderer + BoxCollider2D + Block.cs
7. BoardManager의 blockPrefab에 Block 프리팹 연결
8. BlockSpriteSet 오브젝트 생성 후 블록 타입별 Sprite 연결
9. StageDefinition 에셋을 30개 생성해 GameManager.stages에 연결
10. Canvas에 GameHud와 StoryPanel 구성 후 Text 연결
11. Play 실행

포함 기능
- 6x6 3매치 퍼즐
- 매치 판정/제거/낙하/재생성
- 점수/이동 횟수/스테이지 목표
- 나별/다별 이름 기반 스토리 에이전트
- 실패 횟수 기반 난이도 에이전트
- 교육 힌트 에이전트
- 안전 텍스트 필터
- 로컬 저장
- StoryPack 기반 모듈식 스토리 구조

v1 권장 제외
- 실시간 AI 자유 생성
- 채팅
- 로그인
- 결제
- 광고
- 서버 저장
