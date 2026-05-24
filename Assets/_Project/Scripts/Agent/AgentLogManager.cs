// AgentLogManager.cs
// Task 91 — Agent local log storage (no personal information).
//
// Stores only numeric counts and a date string in PlayerPrefs:
//   - stage fail count (per stageId)
//   - hint use count (per stageId)
//   - assist offer count (per stageId)
//   - difficulty relief applied count (per stageId)
//   - learning coach open count (global)
//   - last updated date (yyyy-MM-dd)
//   - stage key list (pipe-separated)
//
// Forbidden (never stored here):
//   - character aliases, child names, free-text dialog
//   - proposedText / approvedText
//   - phone, email, address
//   - device id / account id / external identifiers
//
// All data is local. This file MUST NOT send anything to an external server
// or AI API. See AIIntegrationPolicy (task 90) — agent logs are local-only
// and are excluded from the customization pack export (task 81).

using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Agent
{
    /// <summary>
    /// Singleton that records lightweight, anonymous agent telemetry to
    /// PlayerPrefs. Safe to call from any agent — every method is guarded
    /// against bad input and missing prefs.
    /// </summary>
    public class AgentLogManager : MonoBehaviour
    {
        public static AgentLogManager Instance { get; private set; }

        // ---- PlayerPrefs key prefix / keys ------------------------------------------------

        public const string KeyPrefix = "AgentLog_";

        private const string StageFailCountKeyFormat        = "AgentLog_StageFailCount_{0}";
        private const string HintUseCountKeyFormat          = "AgentLog_HintUseCount_{0}";
        private const string AssistOfferCountKeyFormat      = "AgentLog_AssistOfferCount_{0}";
        private const string DifficultyReliefCountKeyFormat = "AgentLog_DifficultyReliefCount_{0}";

        private const string LearningCoachOpenCountKey = "AgentLog_LearningCoachOpenCount";
        private const string LastUpdatedDateKey        = "AgentLog_LastUpdatedDate";
        private const string StageKeyListKey           = "AgentLog_StageKeyList";

        private const char StageKeyListSeparator = '|';

        // ---- Lifecycle --------------------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---- Record APIs ------------------------------------------------------------------

        /// <summary>Stage failure count +1 for the given stage. Ignores stageId &lt;= 0.</summary>
        public void RecordStageFail(int stageId)
        {
            if (stageId <= 0) return;
            IncrementStageCounter(StageFailCountKeyFormat, stageId, "stage fail");
        }

        /// <summary>Hint successfully shown. Call only on success, not on failed hint requests.</summary>
        public void RecordHintUsed(int stageId)
        {
            if (stageId <= 0) return;
            IncrementStageCounter(HintUseCountKeyFormat, stageId, "hint used");
        }

        /// <summary>FailureAssistAgent offered help (e.g. after 3 fails).</summary>
        public void RecordAssistOffered(int stageId)
        {
            if (stageId <= 0) return;
            IncrementStageCounter(AssistOfferCountKeyFormat, stageId, "assist offered");
        }

        /// <summary>DifficultyReliefAgent actually applied relief (extraMoves &gt; 0).</summary>
        public void RecordDifficultyReliefApplied(int stageId)
        {
            if (stageId <= 0) return;
            IncrementStageCounter(DifficultyReliefCountKeyFormat, stageId, "difficulty relief");
        }

        /// <summary>Learning coach panel opened (global count, not per stage).</summary>
        public void RecordLearningCoachOpened()
        {
            int prev = PlayerPrefs.GetInt(LearningCoachOpenCountKey, 0);
            PlayerPrefs.SetInt(LearningCoachOpenCountKey, prev + 1);
            TouchDate();
            PlayerPrefs.Save();
            Debug.Log($"[AgentLog] learning coach opened -> {prev + 1}");
        }

        // ---- Getters ----------------------------------------------------------------------

        public int GetStageFailCount(int stageId) =>
            stageId <= 0 ? 0 : PlayerPrefs.GetInt(string.Format(StageFailCountKeyFormat, stageId), 0);

        public int GetHintUseCount(int stageId) =>
            stageId <= 0 ? 0 : PlayerPrefs.GetInt(string.Format(HintUseCountKeyFormat, stageId), 0);

        public int GetAssistOfferCount(int stageId) =>
            stageId <= 0 ? 0 : PlayerPrefs.GetInt(string.Format(AssistOfferCountKeyFormat, stageId), 0);

        public int GetDifficultyReliefCount(int stageId) =>
            stageId <= 0 ? 0 : PlayerPrefs.GetInt(string.Format(DifficultyReliefCountKeyFormat, stageId), 0);

        public int GetLearningCoachOpenCount() =>
            PlayerPrefs.GetInt(LearningCoachOpenCountKey, 0);

        public string GetLastUpdatedDate() =>
            PlayerPrefs.GetString(LastUpdatedDateKey, string.Empty);

        /// <summary>Returns the recorded stageId list (sorted, deduplicated).</summary>
        public List<int> GetRecordedStageIds() => ReadStageKeyList();

        // ---- Reset APIs -------------------------------------------------------------------

        /// <summary>Removes only this stage's per-stage counters. Other AgentLog_ keys are untouched.</summary>
        public void ResetStageLog(int stageId)
        {
            if (stageId <= 0) return;

            PlayerPrefs.DeleteKey(string.Format(StageFailCountKeyFormat, stageId));
            PlayerPrefs.DeleteKey(string.Format(HintUseCountKeyFormat, stageId));
            PlayerPrefs.DeleteKey(string.Format(AssistOfferCountKeyFormat, stageId));
            PlayerPrefs.DeleteKey(string.Format(DifficultyReliefCountKeyFormat, stageId));

            var ids = ReadStageKeyList();
            if (ids.Remove(stageId)) WriteStageKeyList(ids);

            TouchDate();
            PlayerPrefs.Save();
            Debug.Log($"[AgentLog] reset stage log for stageId={stageId}");
        }

        /// <summary>
        /// Removes every AgentLog_ key only. Never calls PlayerPrefs.DeleteAll —
        /// other game data (save, settings, story flags) is preserved.
        /// </summary>
        public void ResetAllAgentLogs()
        {
            var ids = ReadStageKeyList();
            foreach (int id in ids)
            {
                PlayerPrefs.DeleteKey(string.Format(StageFailCountKeyFormat, id));
                PlayerPrefs.DeleteKey(string.Format(HintUseCountKeyFormat, id));
                PlayerPrefs.DeleteKey(string.Format(AssistOfferCountKeyFormat, id));
                PlayerPrefs.DeleteKey(string.Format(DifficultyReliefCountKeyFormat, id));
            }

            PlayerPrefs.DeleteKey(LearningCoachOpenCountKey);
            PlayerPrefs.DeleteKey(LastUpdatedDateKey);
            PlayerPrefs.DeleteKey(StageKeyListKey);

            PlayerPrefs.Save();
            Debug.LogWarning("[AgentLog] all agent logs cleared (AgentLog_ keys only).");
        }

        // ---- Summaries --------------------------------------------------------------------

        /// <summary>Human-readable Korean summary, safe to show in the parent-mode UI.</summary>
        public string BuildLocalSummary()
        {
            var ids = ReadStageKeyList();

            int totalFail   = 0;
            int totalHint   = 0;
            int totalAssist = 0;
            int totalRelief = 0;

            foreach (int id in ids)
            {
                totalFail   += GetStageFailCount(id);
                totalHint   += GetHintUseCount(id);
                totalAssist += GetAssistOfferCount(id);
                totalRelief += GetDifficultyReliefCount(id);
            }

            int learningCoach  = GetLearningCoachOpenCount();
            string lastUpdated = GetLastUpdatedDate();

            var sb = new StringBuilder();
            sb.AppendLine($"기록된 스테이지: {ids.Count}개");
            sb.AppendLine($"실패 횟수: {totalFail}회");
            sb.AppendLine($"힌트 사용: {totalHint}회");
            sb.AppendLine($"도움 제공: {totalAssist}회");
            sb.AppendLine($"난이도 완화: {totalRelief}회");
            sb.AppendLine($"학습 코치 열람: {learningCoach}회");
            if (!string.IsNullOrEmpty(lastUpdated))
                sb.AppendLine($"마지막 업데이트: {lastUpdated}");

            string result = sb.ToString().TrimEnd();
            Debug.Log($"[AgentLog] summary built ({ids.Count} stages).");
            return result;
        }

        /// <summary>Privacy notice shown alongside the summary in parent mode.</summary>
        public string GetPrivacySummary()
        {
            return "이 로그는 실패 횟수와 힌트 사용 횟수 같은 숫자만 기기에 저장합니다. " +
                   "이름, 대사, 개인정보는 저장하지 않습니다.";
        }

        // ---- Internal helpers -------------------------------------------------------------

        private void IncrementStageCounter(string keyFormat, int stageId, string debugLabel)
        {
            string key = string.Format(keyFormat, stageId);
            int prev = PlayerPrefs.GetInt(key, 0);
            int next = prev + 1;
            PlayerPrefs.SetInt(key, next);
            RegisterStageId(stageId);
            TouchDate();
            PlayerPrefs.Save();
            Debug.Log($"[AgentLog] {debugLabel} stageId={stageId} -> {next}");
        }

        private void TouchDate()
        {
            string today = System.DateTime.Now.ToString("yyyy-MM-dd");
            PlayerPrefs.SetString(LastUpdatedDateKey, today);
        }

        private List<int> ReadStageKeyList()
        {
            var list = new List<int>();
            string raw = PlayerPrefs.GetString(StageKeyListKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return list;

            var parts = raw.Split(StageKeyListSeparator);
            foreach (var part in parts)
            {
                if (int.TryParse(part, out int id) && id > 0 && !list.Contains(id))
                    list.Add(id);
            }
            list.Sort();
            return list;
        }

        private void WriteStageKeyList(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                PlayerPrefs.DeleteKey(StageKeyListKey);
                return;
            }

            ids.Sort();
            var sb = new StringBuilder();
            for (int i = 0; i < ids.Count; i++)
            {
                if (i > 0) sb.Append(StageKeyListSeparator);
                sb.Append(ids[i]);
            }
            PlayerPrefs.SetString(StageKeyListKey, sb.ToString());
        }

        private void RegisterStageId(int stageId)
        {
            if (stageId <= 0) return;
            var ids = ReadStageKeyList();
            if (ids.Contains(stageId)) return;
            ids.Add(stageId);
            WriteStageKeyList(ids);
        }

        // ---- Integration notes ------------------------------------------------------------
        //
        // Hook these calls at the success path of each agent. Each call site MUST
        // null-check AgentLogManager.Instance so the game still runs if this manager
        // is not in the scene (validation criterion #14).
        //
        // FailureAssistAgent.OnStageFailed:
        //     if (AgentLogManager.Instance != null && stageId > 0)
        //         AgentLogManager.Instance.RecordStageFail(stageId);
        //
        // FailureAssistAgent.OfferAssist (when assist actually shown):
        //     if (AgentLogManager.Instance != null && stageId > 0)
        //         AgentLogManager.Instance.RecordAssistOffered(stageId);
        //
        // HintAgent.ShowHint (only on success) / Nabyeol hint skill / Poporing bubble hint:
        //     if (AgentLogManager.Instance != null
        //         && StageManager.Instance != null
        //         && StageManager.Instance.HasCurrentStage())
        //     {
        //         AgentLogManager.Instance.RecordHintUsed(
        //             StageManager.Instance.CurrentStageData.stageId);
        //     }
        //
        // DifficultyReliefAgent.ShowReliefInfoIfNeeded:
        //     if (extraMoves > 0 && AgentLogManager.Instance != null)
        //         AgentLogManager.Instance.RecordDifficultyReliefApplied(stageId);
        //
        // LearningCoachUI.OpenLearningCoach:
        //     if (AgentLogManager.Instance != null)
        //         AgentLogManager.Instance.RecordLearningCoachOpened();
        //
        // TODO: agent logs must never be transmitted to an external AI/API or
        // bundled into the customization pack export (task 81).
    }
}
