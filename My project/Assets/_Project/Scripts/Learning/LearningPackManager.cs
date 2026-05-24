using System.Collections.Generic;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Cards;

namespace NabyeolDabyeolDreamPuzzle.Learning
{
    /// <summary>
    /// LearningPackDatabase에 대한 런타임 접근점.
    /// 반짝 앨범/카드 보상/학습 UI가 stageId·cardId·packId 기준으로 학습 데이터를 조회할 때 사용.
    /// </summary>
    public class LearningPackManager : MonoBehaviour
    {
        public static LearningPackManager Instance { get; private set; }

        [SerializeField] private LearningPackDatabase database;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("LearningPackManager: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (database != null)
            {
                bool ok = database.ValidatePacks();
                Debug.Log($"LearningPackManager: ValidatePacks = {ok} (count={database.Count}).");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public LearningPackData GetPackById(string packId)
        {
            if (database == null) return null;
            return database.FindByPackId(packId);
        }

        public LearningPackData GetPackByStageId(int stageId)
        {
            if (database == null) return null;
            return database.FindPackByStageId(stageId);
        }

        public LearningPackData GetPackByCardId(string cardId)
        {
            if (database == null) return null;
            return database.FindPackByCardId(cardId);
        }

        /// <summary>stageId에 연결된 첫 카드 반환. 반짝 앨범의 카드 스니펫 lookup에 사용 가능.</summary>
        public KnowledgeCardData GetFirstCardByStageId(int stageId)
        {
            LearningPackData pack = GetPackByStageId(stageId);
            if (pack == null) return null;
            return pack.FindCardByStageId(stageId);
        }

        /// <summary>stageId에 연결된 학습 목표 목록 반환.</summary>
        public List<LearningGoalData> GetLearningGoalsByStageId(int stageId)
        {
            LearningPackData pack = GetPackByStageId(stageId);
            if (pack == null) return new List<LearningGoalData>();
            return pack.GetLearningGoalsByStageId(stageId);
        }

        public LearningPackDatabase Database => database;
    }
}
