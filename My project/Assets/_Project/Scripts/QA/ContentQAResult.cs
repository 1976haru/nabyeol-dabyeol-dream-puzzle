namespace NabyeolDabyeolDreamPuzzle.QA
{
    /// <summary>콘텐츠 QA 검사 결과 1건.</summary>
    public class ContentQAResult
    {
        public ContentQASeverity severity;
        public string category;
        public string assetName;
        public string message;
        public string assetPath;

        public ContentQAResult() { }

        public ContentQAResult(ContentQASeverity sev, string cat, string asset, string msg, string path = null)
        {
            severity = sev;
            category = cat ?? string.Empty;
            assetName = asset ?? string.Empty;
            message = msg ?? string.Empty;
            assetPath = path ?? string.Empty;
        }

        public override string ToString()
        {
            return $"[{severity}] {category} | {assetName} | {message}";
        }
    }
}
