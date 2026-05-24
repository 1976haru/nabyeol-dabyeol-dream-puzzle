using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Cards;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 지식카드 검수 도구. 30장 자산을 스캔해서 다음을 진단한다:
    ///  - 빈 cardId / cardName / shortText
    ///  - linkedStageId <= 0
    ///  - shortText 길이 (45자 초과 = 다소 김, 60자 초과 = 경고)
    ///  - 어려운 단어 후보 (습성, 활발하게, 표현하기도, 또렷해져요, 차례대로, ...)
    ///  - cardId 중복
    ///  - KnowledgeCardData.IsValid / KnowledgeCardDatabase.ValidateCards
    /// 실제 데이터 변경은 KnowledgeCardGenerator가 담당하며, 본 도구는 read-only 진단.
    /// </summary>
    public static class KnowledgeCardReviewTool
    {
        private const string DatabasePath = "Assets/_Project/Data/Cards/KnowledgeCardDatabase.asset";
        private const int ShortTextWarnLength = 45;
        private const int ShortTextErrorLength = 60;

        // 어려운 표현 후보 → 권장 쉬운 표현. 검사 시 키워드 발견하면 경고.
        private static readonly Dictionary<string, string> HardWordSuggestions = new Dictionary<string, string>
        {
            { "습성",         "버릇" },
            { "활발하게",     "많이 (움직이는)" },
            { "표현하기도",   "알려주기도" },
            { "또렷해져요",   "더 잘 떠올라요" },
            { "차례대로",     "순서대로" },
            { "규칙을 알 수", "규칙이 보여요" },
            { "가장 편안해요","편안해요" }
        };

        [MenuItem("Tools/NabyeolDabyeol/Review Knowledge Cards")]
        public static void ReviewAll()
        {
            KnowledgeCardDatabase db = AssetDatabase.LoadAssetAtPath<KnowledgeCardDatabase>(DatabasePath);
            if (db == null)
            {
                Debug.LogWarning("KnowledgeCardReviewTool: Database not found at " + DatabasePath);
                return;
            }

            int total = db.Count;
            Debug.Log($"KnowledgeCardReviewTool: Reviewing {total} cards...");

            int problemCards = 0;
            int hardWordFlagged = 0;
            int tooLongFlagged = 0;
            int slightlyLongFlagged = 0;
            HashSet<string> seenIds = new HashSet<string>();

            for (int i = 0; i < total; i++)
            {
                KnowledgeCardData c = db.Cards[i];
                if (c == null)
                {
                    Debug.LogWarning($"KnowledgeCardReviewTool: cards[{i}] is null.");
                    problemCards++;
                    continue;
                }

                bool hasIssue = false;

                // 빈 값 검사
                if (string.IsNullOrWhiteSpace(c.CardId))
                {
                    Debug.LogWarning($"KnowledgeCardReviewTool: cards[{i}] '{c.name}' empty cardId.");
                    hasIssue = true;
                }
                if (string.IsNullOrWhiteSpace(c.CardName))
                {
                    Debug.LogWarning($"KnowledgeCardReviewTool: cards[{i}] '{c.name}' empty cardName.");
                    hasIssue = true;
                }
                if (string.IsNullOrWhiteSpace(c.ShortText))
                {
                    Debug.LogWarning($"KnowledgeCardReviewTool: cards[{i}] '{c.name}' empty shortText.");
                    hasIssue = true;
                }
                if (c.LinkedStageId <= 0)
                {
                    Debug.LogWarning($"KnowledgeCardReviewTool: cards[{i}] '{c.name}' invalid linkedStageId {c.LinkedStageId}.");
                    hasIssue = true;
                }

                // 중복 cardId
                if (!string.IsNullOrWhiteSpace(c.CardId) && !seenIds.Add(c.CardId))
                {
                    Debug.LogWarning($"KnowledgeCardReviewTool: Duplicate cardId '{c.CardId}' at cards[{i}].");
                    hasIssue = true;
                }

                // 길이 검사
                int len = c.ShortText == null ? 0 : c.ShortText.Length;
                if (len > ShortTextErrorLength)
                {
                    Debug.LogWarning($"KnowledgeCardReviewTool: cards[{i}] '{c.CardId}' shortText length {len} > {ShortTextErrorLength} (too long).");
                    tooLongFlagged++;
                    hasIssue = true;
                }
                else if (len > ShortTextWarnLength)
                {
                    Debug.Log($"KnowledgeCardReviewTool: cards[{i}] '{c.CardId}' shortText length {len} > {ShortTextWarnLength} (slightly long).");
                    slightlyLongFlagged++;
                }

                // 어려운 단어 후보 검사
                if (!string.IsNullOrWhiteSpace(c.ShortText))
                {
                    foreach (KeyValuePair<string, string> kv in HardWordSuggestions)
                    {
                        if (c.ShortText.Contains(kv.Key))
                        {
                            Debug.LogWarning($"KnowledgeCardReviewTool: cards[{i}] '{c.CardId}' contains hard word '{kv.Key}'. Suggest: '{kv.Value}'.");
                            hardWordFlagged++;
                            hasIssue = true;
                        }
                    }
                }

                // IsValid 종합 검사
                if (!c.IsValid())
                {
                    Debug.LogWarning($"KnowledgeCardReviewTool: cards[{i}] '{c.CardId}' failed IsValid().");
                    hasIssue = true;
                }

                if (hasIssue) problemCards++;
            }

            bool dbOk = db.ValidateCards();

            Debug.Log("───── KnowledgeCardReviewTool: 종합 검수 결과 ─────");
            Debug.Log($"  총 카드: {total}");
            Debug.Log($"  문제 발견 카드: {problemCards}");
            Debug.Log($"  shortText 60자 초과: {tooLongFlagged}");
            Debug.Log($"  shortText 45자 초과(다소 김): {slightlyLongFlagged}");
            Debug.Log($"  어려운 단어 후보 검출: {hardWordFlagged}");
            Debug.Log($"  Database.ValidateCards: {dbOk}");
            Debug.Log("─────────────────────────────────────────");

            if (problemCards == 0 && dbOk)
            {
                Debug.Log("KnowledgeCardReviewTool: ✓ 모든 카드가 초등 저학년 기준 검수를 통과했습니다.");
            }
            else
            {
                Debug.LogWarning("KnowledgeCardReviewTool: 위 경고를 확인하고 KnowledgeCardGenerator의 BuildRows에서 shortText를 보정한 뒤 다시 메뉴를 실행하세요.");
            }
        }
    }
}
