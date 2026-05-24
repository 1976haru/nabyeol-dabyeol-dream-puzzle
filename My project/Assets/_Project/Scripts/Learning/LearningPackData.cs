using System.Collections.Generic;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Cards;

namespace NabyeolDabyeolDreamPuzzle.Learning
{
    /// <summary>LearningPack 분류 enum.</summary>
    public enum LearningPackType
    {
        AnimalLessons = 0,
        NumberLessons = 1,
        BossLessons = 2,
        SpecialLessons = 3
    }

    /// <summary>
    /// 여러 KnowledgeCardData와 LearningGoalData를 학습 테마/월드 단위로 묶는 상위 데이터.
    /// 카드 자체는 KnowledgeCardData/Database가 보관하고, 본 자산은 카드 참조 + 학습 목표만 보유.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LearningPack",
        menuName = "NabyeolDabyeol/Learning Pack",
        order = 180)]
    public class LearningPackData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string packId;
        [SerializeField] private string packName;
        [SerializeField] private LearningPackType packType = LearningPackType.AnimalLessons;
        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Stage Range")]
        [SerializeField, Min(1)] private int startStageId = 1;
        [SerializeField, Min(1)] private int endStageId = 15;

        [Header("Contents")]
        [SerializeField] private List<KnowledgeCardData> cards = new List<KnowledgeCardData>();
        [SerializeField] private List<LearningGoalData> learningGoals = new List<LearningGoalData>();

        public string PackId => packId;
        public string PackName => packName;
        public LearningPackType PackType => packType;
        public string Description => description;
        public int StartStageId => startStageId;
        public int EndStageId => endStageId;
        public IReadOnlyList<KnowledgeCardData> Cards => cards;
        public IReadOnlyList<LearningGoalData> LearningGoals => learningGoals;

        public bool ContainsStage(int stageId)
        {
            return stageId >= startStageId && stageId <= endStageId;
        }

        public KnowledgeCardData FindCardByCardId(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId) || cards == null) return null;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null && cards[i].CardId == cardId) return cards[i];
            }
            return null;
        }

        public KnowledgeCardData FindCardByStageId(int stageId)
        {
            if (cards == null) return null;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null && cards[i].LinkedStageId == stageId) return cards[i];
            }
            return null;
        }

        public List<LearningGoalData> GetLearningGoalsByStageId(int stageId)
        {
            List<LearningGoalData> result = new List<LearningGoalData>();
            if (learningGoals == null) return result;
            for (int i = 0; i < learningGoals.Count; i++)
            {
                LearningGoalData g = learningGoals[i];
                if (g != null && g.LinkedStageId == stageId) result.Add(g);
            }
            return result;
        }

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(packId)) return false;
            if (string.IsNullOrWhiteSpace(packName)) return false;
            if (startStageId <= 0) return false;
            if (endStageId < startStageId) return false;
            bool hasCards = cards != null && cards.Count > 0;
            bool hasGoals = learningGoals != null && learningGoals.Count > 0;
            return hasCards || hasGoals;
        }
    }
}
