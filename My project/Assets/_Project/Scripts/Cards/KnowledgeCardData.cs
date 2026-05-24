using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Cards
{
    /// <summary>지식카드 주제 분류.</summary>
    public enum KnowledgeCardCategory
    {
        Animal = 0,
        Nature = 1,
        Number = 2,
        Puzzle = 3,
        Boss = 4,
        Life = 5
    }

    /// <summary>지식카드 희귀도. 추후 보상 연출에 활용.</summary>
    public enum KnowledgeCardRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2
    }

    /// <summary>
    /// 한 장의 지식카드 데이터. 어린이 친화 한 문장 중심.
    /// 어느 스테이지 보상으로 등장하는지를 linkedStageId로 연결한다.
    /// 실제 보상 지급/도감 UI는 후속 단계 담당.
    /// </summary>
    [CreateAssetMenu(
        fileName = "KnowledgeCard",
        menuName = "NabyeolDabyeol/Knowledge Card",
        order = 120)]
    public class KnowledgeCardData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string cardId;
        [SerializeField] private string cardName;

        [Header("Classification")]
        [SerializeField] private KnowledgeCardCategory category = KnowledgeCardCategory.Animal;
        [SerializeField] private KnowledgeCardRarity rarity = KnowledgeCardRarity.Common;

        [Header("Link")]
        [SerializeField, Min(1)] private int linkedStageId = 1;

        [Header("Content")]
        [TextArea(1, 3)]
        [SerializeField] private string shortText;
        [SerializeField] private Sprite image;

        [Header("Limits")]
        [SerializeField, Min(20)] private int shortTextWarningLength = 60;

        public string CardId => cardId;
        public string CardName => cardName;
        public KnowledgeCardCategory Category => category;
        public KnowledgeCardRarity Rarity => rarity;
        public int LinkedStageId => linkedStageId;
        public string ShortText => shortText;
        public Sprite Image => image;

        /// <summary>
        /// 카드 데이터 유효성 검사. cardId/cardName/shortText 비어 있지 않음 + linkedStageId 양수.
        /// shortText가 너무 길면 경고 로그를 출력하지만 IsValid 자체는 통과시킨다.
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(cardId)) return false;
            if (string.IsNullOrWhiteSpace(cardName)) return false;
            if (string.IsNullOrWhiteSpace(shortText)) return false;
            if (linkedStageId <= 0) return false;

            if (shortText.Length > shortTextWarningLength)
            {
                Debug.LogWarning($"KnowledgeCardData '{cardId}': shortText length {shortText.Length} exceeds warning threshold {shortTextWarningLength}.");
            }
            return true;
        }
    }
}
