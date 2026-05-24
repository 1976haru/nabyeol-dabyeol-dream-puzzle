using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Stage
{
    /// <summary>
    /// StagePackDatabase에 대한 런타임 접근점.
    /// StageManager가 stageList에서 stageId를 못 찾을 때 fallback으로 호출한다.
    /// BoardManager의 TryApplyStageData도 보드 규칙 fallback으로 GetBoardRuleByStageId를 활용 가능.
    /// </summary>
    public class StagePackManager : MonoBehaviour
    {
        public static StagePackManager Instance { get; private set; }

        [SerializeField] private StagePackDatabase database;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("StagePackManager: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (database != null)
            {
                bool ok = database.ValidatePacks();
                Debug.Log($"StagePackManager: ValidatePacks = {ok} (count={database.Count}).");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public StagePackData GetPackById(string packId)
        {
            if (database == null) return null;
            return database.FindByPackId(packId);
        }

        public StagePackData GetPackByStageId(int stageId)
        {
            if (database == null) return null;
            return database.FindPackByStageId(stageId);
        }

        public StageData GetStageById(int stageId)
        {
            if (database == null) return null;
            return database.FindStageById(stageId);
        }

        public StageBoardRule GetBoardRuleByStageId(int stageId)
        {
            if (database == null) return null;
            return database.FindBoardRuleByStageId(stageId);
        }

        public StagePackDatabase Database => database;
    }
}
