using System.Collections.Generic;

namespace NabyeolDabyeolDreamPuzzle.QA
{
    /// <summary>전체 QA 검사 결과 집계.</summary>
    public class ContentQAReport
    {
        public List<ContentQAResult> results = new List<ContentQAResult>();
        public int errorCount;
        public int warningCount;
        public int infoCount;

        public void Add(ContentQAResult r)
        {
            if (r == null) return;
            results.Add(r);
            switch (r.severity)
            {
                case ContentQASeverity.Error:   errorCount++;   break;
                case ContentQASeverity.Warning: warningCount++; break;
                case ContentQASeverity.Info:    infoCount++;    break;
            }
        }

        public void Add(ContentQASeverity sev, string category, string assetName, string message, string assetPath = null)
        {
            Add(new ContentQAResult(sev, category, assetName, message, assetPath));
        }

        public int TotalCount => results == null ? 0 : results.Count;

        public string Summary()
        {
            return $"QA Report: Errors={errorCount}, Warnings={warningCount}, Info={infoCount}, Total={TotalCount}";
        }
    }
}
