using System;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Learning
{
    /// <summary>학습 목표 분류 enum. UI 필터링/통계 용도.</summary>
    public enum LearningGoalType
    {
        AnimalFact = 0,
        NumberRule = 1,
        PatternRecognition = 2,
        SpecialFact = 3,
        BossLesson = 4
    }

    /// <summary>
    /// 하나의 학습 목표. LearningPack 내부에 직렬화된다.
    /// linkedCardId는 KnowledgeCardData.cardId를 참조하고, linkedStageId는 StageData.stageId를 참조한다.
    /// </summary>
    [Serializable]
    public class LearningGoalData
    {
        [SerializeField] private string goalId;
        [SerializeField] private string goalTitle;
        [TextArea(2, 4)]
        [SerializeField] private string goalDescription;
        [SerializeField] private LearningGoalType goalType = LearningGoalType.AnimalFact;
        [SerializeField, Min(1)] private int linkedStageId = 1;
        [SerializeField] private string linkedCardId;

        public string GoalId => goalId;
        public string GoalTitle => goalTitle;
        public string GoalDescription => goalDescription;
        public LearningGoalType GoalType => goalType;
        public int LinkedStageId => linkedStageId;
        public string LinkedCardId => linkedCardId;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(goalId)) return false;
            if (string.IsNullOrWhiteSpace(goalTitle)) return false;
            if (linkedStageId <= 0) return false;
            // linkedCardId는 카드 미연결 학습 목표를 허용해 빈 값도 OK
            return true;
        }
    }
}
