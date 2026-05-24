using System.Collections.Generic;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Cards
{
    /// <summary>
    /// 모든 지식카드를 한 곳에서 검색·검증할 수 있는 ScriptableObject 카탈로그.
    /// 생성된 KnowledgeCardData 자산을 cards 리스트에 등록해 사용한다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "KnowledgeCardDatabase",
        menuName = "NabyeolDabyeol/Knowledge Card Database",
        order = 121)]
    public class KnowledgeCardDatabase : ScriptableObject
    {
        [SerializeField] private List<KnowledgeCardData> cards = new List<KnowledgeCardData>();

        public IReadOnlyList<KnowledgeCardData> Cards => cards;
        public int Count => cards == null ? 0 : cards.Count;

        /// <summary>cardId로 카드를 찾는다. 없으면 null.</summary>
        public KnowledgeCardData FindByCardId(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId) || cards == null) return null;
            for (int i = 0; i < cards.Count; i++)
            {
                KnowledgeCardData c = cards[i];
                if (c != null && c.CardId == cardId) return c;
            }
            return null;
        }

        /// <summary>linkedStageId로 카드를 찾는다. 같은 stage에 여러 카드가 있으면 첫 번째 반환.</summary>
        public KnowledgeCardData FindByStageId(int stageId)
        {
            if (cards == null) return null;
            for (int i = 0; i < cards.Count; i++)
            {
                KnowledgeCardData c = cards[i];
                if (c != null && c.LinkedStageId == stageId) return c;
            }
            return null;
        }

        /// <summary>
        /// 데이터베이스 일관성 검사:
        /// - null 항목 없음
        /// - 각 카드 IsValid 통과
        /// - cardId 중복 없음
        /// 어떤 항목이라도 실패하면 false. 경고 로그로 진단 정보 제공.
        /// </summary>
        public bool ValidateCards()
        {
            if (cards == null) return false;

            bool ok = true;
            HashSet<string> seenIds = new HashSet<string>();
            for (int i = 0; i < cards.Count; i++)
            {
                KnowledgeCardData c = cards[i];
                if (c == null)
                {
                    Debug.LogWarning($"KnowledgeCardDatabase: cards[{i}] is null.");
                    ok = false;
                    continue;
                }
                if (!c.IsValid())
                {
                    Debug.LogWarning($"KnowledgeCardDatabase: cards[{i}] '{c.name}' failed IsValid().");
                    ok = false;
                }
                if (!seenIds.Add(c.CardId))
                {
                    Debug.LogWarning($"KnowledgeCardDatabase: Duplicate cardId '{c.CardId}' at cards[{i}].");
                    ok = false;
                }
            }
            return ok;
        }
    }
}
