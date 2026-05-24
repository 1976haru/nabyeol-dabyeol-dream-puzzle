using System.Collections.Generic;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Stage
{
    /// <summary>Stage Pack 분류 enum.</summary>
    public enum StagePackType
    {
        Region = 0,
        Boss = 1,
        SpecialEvent = 2
    }

    /// <summary>
    /// 여러 StageData와 공통 StageBoardRule을 묶는 상위 데이터.
    /// 방울숲/달떡계단/보스 같은 월드 단위 묶음이며, 새 월드 추가 시 본 자산만 생성하면 확장된다.
    /// 기존 StageManager.stageList와 병행 운영 (stageList 우선, StagePack fallback).
    /// </summary>
    [CreateAssetMenu(
        fileName = "StagePack",
        menuName = "NabyeolDabyeol/Stage Pack",
        order = 190)]
    public class StagePackData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string packId;
        [SerializeField] private string packName;
        [SerializeField] private StagePackType packType = StagePackType.Region;
        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Stage Range")]
        [SerializeField, Min(1)] private int startStageId = 1;
        [SerializeField, Min(1)] private int endStageId = 15;

        [Header("Contents")]
        [SerializeField] private List<StageData> stages = new List<StageData>();
        [SerializeField] private StageBoardRule defaultBoardRule = new StageBoardRule();

        public string PackId => packId;
        public string PackName => packName;
        public StagePackType PackType => packType;
        public string Description => description;
        public int StartStageId => startStageId;
        public int EndStageId => endStageId;
        public IReadOnlyList<StageData> Stages => stages;
        public StageBoardRule DefaultBoardRule => defaultBoardRule;

        public bool ContainsStage(int stageId)
        {
            return stageId >= startStageId && stageId <= endStageId;
        }

        public StageData FindStageById(int stageId)
        {
            if (stages == null) return null;
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i] != null && stages[i].StageId == stageId) return stages[i];
            }
            return null;
        }

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(packId)) return false;
            if (string.IsNullOrWhiteSpace(packName)) return false;
            if (startStageId <= 0) return false;
            if (endStageId < startStageId) return false;
            bool hasStages = stages != null && stages.Count > 0;
            bool ruleValid = defaultBoardRule != null && defaultBoardRule.IsValid();
            return hasStages && ruleValid;
        }
    }
}
