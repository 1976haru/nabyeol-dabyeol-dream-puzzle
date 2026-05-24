using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Region
{
    /// <summary>지역 복구 단계. UI/이미지 분기의 단일 키.</summary>
    public enum RestoreStep
    {
        Percent0 = 0,
        Percent25 = 1,
        Percent50 = 2,
        Percent75 = 3,
        Percent100 = 4
    }

    /// <summary>
    /// 지역 복구 데이터. 한 월드(방울숲/달떡계단 등)의 스테이지 범위와 단계별 시각·문구를 보관한다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "RestoreRegion",
        menuName = "NabyeolDabyeol/Restore Region Data",
        order = 140)]
    public class RestoreRegionData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string regionId;
        [SerializeField] private string regionName;

        [Header("Stage Range")]
        [SerializeField, Min(1)] private int startStageId = 1;
        [SerializeField, Min(1)] private int endStageId = 15;

        [Header("Restore Sprites (선택)")]
        [SerializeField] private Sprite restore0Sprite;
        [SerializeField] private Sprite restore25Sprite;
        [SerializeField] private Sprite restore50Sprite;
        [SerializeField] private Sprite restore75Sprite;
        [SerializeField] private Sprite restore100Sprite;

        [Header("Restore Descriptions")]
        [TextArea(1, 2)]
        [SerializeField] private string desc0   = "아직 잠들어 있어요.";
        [TextArea(1, 2)]
        [SerializeField] private string desc25  = "작은 빛이 돌아왔어요.";
        [TextArea(1, 2)]
        [SerializeField] private string desc50  = "절반쯤 반짝이고 있어요.";
        [TextArea(1, 2)]
        [SerializeField] private string desc75  = "거의 다 회복됐어요.";
        [TextArea(1, 2)]
        [SerializeField] private string desc100 = "완전히 반짝이는 지역이 됐어요!";

        public string RegionId => regionId;
        public string RegionName => regionName;
        public int StartStageId => startStageId;
        public int EndStageId => endStageId;
        public int StageCount => Mathf.Max(0, endStageId - startStageId + 1);

        public Sprite GetSpriteByPercent(int percent)
        {
            if (percent >= 100) return restore100Sprite;
            if (percent >= 75)  return restore75Sprite;
            if (percent >= 50)  return restore50Sprite;
            if (percent >= 25)  return restore25Sprite;
            return restore0Sprite;
        }

        public string GetDescriptionByPercent(int percent)
        {
            if (percent >= 100) return desc100;
            if (percent >= 75)  return desc75;
            if (percent >= 50)  return desc50;
            if (percent >= 25)  return desc25;
            return desc0;
        }

        public bool ContainsStage(int stageId)
        {
            return stageId >= startStageId && stageId <= endStageId;
        }

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(regionId)) return false;
            if (string.IsNullOrWhiteSpace(regionName)) return false;
            if (startStageId <= 0) return false;
            if (endStageId < startStageId) return false;
            return true;
        }
    }
}
