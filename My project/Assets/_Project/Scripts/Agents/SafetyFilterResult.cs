namespace NabyeolDabyeolDreamPuzzle.Agents
{
    /// <summary>
    /// 안전 필터 검사 결과. UI는 message만 사용하고 matchedText는 디버그 용도로만 활용.
    /// </summary>
    public class SafetyFilterResult
    {
        public bool isSafe;
        public SafetyFilterReason reason;
        public string message;
        public string matchedText;

        public static SafetyFilterResult MakeSafe(string okMessage = null)
        {
            return new SafetyFilterResult
            {
                isSafe = true,
                reason = SafetyFilterReason.None,
                message = okMessage ?? "사용할 수 있는 문장이에요.",
                matchedText = string.Empty
            };
        }

        public static SafetyFilterResult Block(SafetyFilterReason r, string msg, string matched = "")
        {
            return new SafetyFilterResult
            {
                isSafe = false,
                reason = r,
                message = msg ?? string.Empty,
                matchedText = matched ?? string.Empty
            };
        }
    }
}
