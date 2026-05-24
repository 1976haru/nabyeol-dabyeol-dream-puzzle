namespace NabyeolDabyeolDreamPuzzle.Agents
{
    /// <summary>안전 필터 차단 사유 분류.</summary>
    public enum SafetyFilterReason
    {
        None = 0,
        EmptyText = 1,
        TooLong = 2,
        BlockedWord = 3,
        PersonalInfo = 4,
        ScaryExpression = 5,
        NegativeExpression = 6,
        Unknown = 99
    }
}
