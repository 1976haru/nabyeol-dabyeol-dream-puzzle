using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Agents
{
    /// <summary>
    /// 안전 필터 v1 — 로컬 규칙 기반 차단.
    /// - 외부 AI/서버 호출 없음.
    /// - 금칙어/무서운 표현/부정 표현/개인정보 패턴/길이를 순차 검사.
    /// - 완벽한 필터가 아니며 v1 기본 방어선. 추후 부모 설정/ScriptableObject로 확장.
    /// TODO: Move safety word lists to SafetyFilterConfig ScriptableObject.
    /// TODO: Add parent override list (specific words allowed by parent mode).
    /// TODO: Add stricter / lenient profile selection (Strict/Default/Lenient).
    /// </summary>
    public class SafetyFilterAgent : MonoBehaviour
    {
        public static SafetyFilterAgent Instance { get; private set; }

        [Header("Length Limits")]
        [SerializeField, Min(1)] private int maxAliasLength = 8;
        [SerializeField, Min(1)] private int maxDialogueLength = 50;
        [SerializeField, Min(1)] private int maxGeneralTextLength = 60;

        /// <summary>최근 검사 결과. UI가 차단 시 message를 읽을 때 사용한다.</summary>
        public SafetyFilterResult LastResult { get; private set; } = SafetyFilterResult.MakeSafe();

        public int MaxAliasLength => maxAliasLength;
        public int MaxDialogueLength => maxDialogueLength;
        public int MaxGeneralTextLength => maxGeneralTextLength;

        // ───────── 규칙 데이터 (v1 코드 내 리스트) ─────────

        private readonly List<string> blockedWords = new List<string>
        {
            "죽어", "죽일", "꺼져", "바보", "멍청", "싫어", "때려", "저주"
        };

        private readonly List<string> scaryWords = new List<string>
        {
            "무서워", "공포", "악몽", "피", "유령", "귀신", "괴물", "잡아간다", "사라져"
        };

        private readonly List<string> negativeWords = new List<string>
        {
            "못했어", "실패자", "틀렸어", "형편없어", "쓸모없어", "안 돼"
        };

        // 개인정보 정규식
        private static readonly Regex PhoneRegex     = new Regex(@"01[016789]-?\d{3,4}-?\d{4}", RegexOptions.Compiled);
        private static readonly Regex EmailRegex     = new Regex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.Compiled);
        private static readonly Regex ResidentRegex  = new Regex(@"\d{6}-\d{7}", RegexOptions.Compiled);
        // 주소 형태: "<숫자>동 <숫자>호", "<숫자>번지", "<숫자>층" 같은 패턴
        private static readonly Regex AddressRegex   = new Regex(@"\d+\s*(동|호|번지|층)\b", RegexOptions.Compiled);
        // 일반 숫자열 + 동/호/번지 조합 (한국어 단위만 잡음)

        // ───────── 메시지 ─────────

        [Header("Messages")]
        [SerializeField] private string msgEmpty = "문장을 입력해 주세요.";
        [SerializeField] private string msgTooLongFormat = "문장이 너무 길어요. {0}자 이하로 바꿔 주세요.";
        [SerializeField] private string msgBlocked = "아이에게 맞지 않는 말이 들어 있어요.";
        [SerializeField] private string msgPersonalInfo = "전화번호나 주소 같은 개인정보는 넣을 수 없어요.";
        [SerializeField] private string msgScary = "무서울 수 있는 표현은 사용할 수 없어요.";
        [SerializeField] private string msgNegative = "속상할 수 있는 표현은 다른 말로 바꿔 주세요.";

        // ───────── Lifecycle ─────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("SafetyFilterAgent: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ───────── 외부 진입점 ─────────

        public SafetyFilterResult CheckAlias(string alias) => CheckTextInternal(alias, maxAliasLength);
        public SafetyFilterResult CheckDialogue(string dialogue) => CheckTextInternal(dialogue, maxDialogueLength);
        public SafetyFilterResult CheckGeneralText(string text) => CheckTextInternal(text, maxGeneralTextLength);

        /// <summary>임의 길이 제한으로 검사.</summary>
        public SafetyFilterResult CheckText(string text)
        {
            return CheckTextInternal(text, maxGeneralTextLength);
        }

        // ───────── 내부 검사 ─────────

        private SafetyFilterResult CheckTextInternal(string text, int maxLength)
        {
            string cleaned = Sanitize(text);

            // 1) Empty
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                LastResult = SafetyFilterResult.Block(SafetyFilterReason.EmptyText, msgEmpty);
                return LastResult;
            }

            // 2) Length
            if (cleaned.Length > maxLength)
            {
                LastResult = SafetyFilterResult.Block(SafetyFilterReason.TooLong, string.Format(msgTooLongFormat, maxLength));
                return LastResult;
            }

            string lowered = cleaned.ToLowerInvariant();

            // 3) Personal Info (가장 우선 — 가장 민감)
            string matched;
            if (HasPersonalInfo(cleaned, lowered, out matched))
            {
                Debug.Log($"SafetyFilterAgent: Personal info blocked. (matched length={matched?.Length ?? 0})");
                LastResult = SafetyFilterResult.Block(SafetyFilterReason.PersonalInfo, msgPersonalInfo, matched);
                return LastResult;
            }

            // 4) Blocked words
            if (ContainsAny(cleaned, blockedWords, out matched))
            {
                Debug.Log($"SafetyFilterAgent: Blocked word matched: '{matched}'.");
                LastResult = SafetyFilterResult.Block(SafetyFilterReason.BlockedWord, msgBlocked, matched);
                return LastResult;
            }

            // 5) Scary expressions
            if (ContainsAny(cleaned, scaryWords, out matched))
            {
                Debug.Log($"SafetyFilterAgent: Scary word matched: '{matched}'.");
                LastResult = SafetyFilterResult.Block(SafetyFilterReason.ScaryExpression, msgScary, matched);
                return LastResult;
            }

            // 6) Negative expressions
            if (ContainsAny(cleaned, negativeWords, out matched))
            {
                Debug.Log($"SafetyFilterAgent: Negative word matched: '{matched}'.");
                LastResult = SafetyFilterResult.Block(SafetyFilterReason.NegativeExpression, msgNegative, matched);
                return LastResult;
            }

            LastResult = SafetyFilterResult.MakeSafe();
            return LastResult;
        }

        private static string Sanitize(string text)
        {
            if (text == null) return string.Empty;
            string s = text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim();
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            return s;
        }

        private static bool ContainsAny(string text, List<string> needles, out string matched)
        {
            matched = null;
            if (string.IsNullOrEmpty(text) || needles == null) return false;
            for (int i = 0; i < needles.Count; i++)
            {
                string w = needles[i];
                if (string.IsNullOrEmpty(w)) continue;
                if (text.Contains(w))
                {
                    matched = w;
                    return true;
                }
            }
            return false;
        }

        private static bool HasPersonalInfo(string cleaned, string lowered, out string matched)
        {
            Match m = PhoneRegex.Match(cleaned);
            if (m.Success) { matched = m.Value; return true; }
            m = EmailRegex.Match(lowered);
            if (m.Success) { matched = m.Value; return true; }
            m = ResidentRegex.Match(cleaned);
            if (m.Success) { matched = m.Value; return true; }
            m = AddressRegex.Match(cleaned);
            if (m.Success) { matched = m.Value; return true; }
            matched = null;
            return false;
        }

        // ───────── Debug ─────────

        [ContextMenu("Debug Test Safety Filter")]
        public void DebugTestSafetyFilter()
        {
            string[] samples = {
                "같이 다시 해보자",
                "010-1234-5678",
                "user@example.com",
                "001234-1234567",
                "서울 시 강남구 123동 456호",
                "귀신이 잡아간다",
                "너는 실패자야",
                "꺼져 바보야",
                "                                                                          매우긴문장매우긴문장매우긴문장매우긴문장매우긴문장"
            };
            for (int i = 0; i < samples.Length; i++)
            {
                SafetyFilterResult r = CheckGeneralText(samples[i]);
                Debug.Log($"[SafetyFilterTest] '{samples[i]}' → safe={r.isSafe}, reason={r.reason}, msg='{r.message}'.");
            }
        }
    }
}
