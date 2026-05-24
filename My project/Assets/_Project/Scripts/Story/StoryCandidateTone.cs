namespace NabyeolDabyeolDreamPuzzle.Story
{
    /// <summary>
    /// 스토리 대사 후보의 톤 분류. 캐릭터별/상황별 후보 선택과 필터링에 사용.
    /// 자유 입력이 아닌 템플릿 기반이므로 이 enum으로 안전한 톤만 미리 큐레이션한다.
    /// </summary>
    public enum StoryCandidateTone
    {
        Neutral = 0,        // 톤 불특정 (공통)
        Brave = 1,          // 용감/적극 (나별형)
        Cheer = 2,          // 응원
        Calm = 3,           // 차분/안내 (다별형)
        Encouraging = 4,    // 격려/위로 (카피몽형, 실패 후 회복)
        Bubble = 5,         // 톡톡/방울 (포포링형)
        Playful = 6,        // 장난기 (노노형)
        Numeric = 7,        // 순서·숫자 (모찌룬형)
        BossClimax = 8      // 보스/특수 클라이맥스 톤
    }
}
