using System;
using System.Collections.Generic;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Region
{
    /// <summary>
    /// 지역(월드)별 클리어 진행도와 복구율을 관리하는 매니저.
    /// 스테이지 해금(#48)과 별개로 "실제 클리어된 스테이지"만을 PlayerPrefs에 기록한다.
    /// 0/25/50/75/100 단계 변환은 GetRestoreStepByPercent로 수행.
    /// </summary>
    public class RegionRestoreManager : MonoBehaviour
    {
        public static RegionRestoreManager Instance { get; private set; }

        [Header("Regions")]
        [SerializeField] private List<RestoreRegionData> regions = new List<RestoreRegionData>();

        // 클리어 저장 키 (해금 키와 분리): PlayerPrefs "StageCleared_<stageId>".
        private const string StageClearedKeyPrefix = "StageCleared_";

        /// <summary>(regionId, beforePercent, afterPercent) — 단계 상승 감지에 사용.</summary>
        public event Action<string, int, int> OnRegionRestoreUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("RegionRestoreManager: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ───────── 클리어 마킹/검사 ─────────

        public bool IsStageCleared(int stageId)
        {
            if (stageId <= 0) return false;
            return PlayerPrefs.GetInt(StageClearedKeyPrefix + stageId, 0) == 1;
        }

        /// <summary>
        /// BoardManager.CheckStageClear에서 호출. 해당 stageId를 클리어로 마킹하고
        /// 소속 지역의 before/after 복구율을 비교해 단계 상승 시 이벤트 발행.
        /// </summary>
        public void MarkStageCleared(int stageId)
        {
            if (stageId <= 0) return;

            RestoreRegionData region = GetRegionByStageId(stageId);
            int beforePercent = region != null ? GetSteppedPercent(region) : 0;

            if (!IsStageCleared(stageId))
            {
                PlayerPrefs.SetInt(StageClearedKeyPrefix + stageId, 1);
                PlayerPrefs.Save();
                Debug.Log($"RegionRestoreManager: Stage cleared marked. stageId={stageId}");
            }
            else
            {
                Debug.Log($"RegionRestoreManager: Stage already marked cleared. stageId={stageId}");
            }

            if (region != null)
            {
                int afterPercent = GetSteppedPercent(region);
                if (afterPercent != beforePercent)
                {
                    Debug.Log($"RegionRestoreManager: Region '{region.RegionId}' restore step changed: {beforePercent}% → {afterPercent}%.");
                    OnRegionRestoreUpdated?.Invoke(region.RegionId, beforePercent, afterPercent);
                }
            }
        }

        // ───────── 지역 검색 ─────────

        public RestoreRegionData GetRegionById(string regionId)
        {
            if (string.IsNullOrWhiteSpace(regionId) || regions == null) return null;
            for (int i = 0; i < regions.Count; i++)
            {
                if (regions[i] != null && regions[i].RegionId == regionId) return regions[i];
            }
            return null;
        }

        public RestoreRegionData GetRegionByStageId(int stageId)
        {
            if (regions == null) return null;
            for (int i = 0; i < regions.Count; i++)
            {
                if (regions[i] != null && regions[i].ContainsStage(stageId)) return regions[i];
            }
            return null;
        }

        public IReadOnlyList<RestoreRegionData> Regions => regions;

        // ───────── 복구율 계산 ─────────

        public int GetClearedStageCount(RestoreRegionData region)
        {
            if (region == null) return 0;
            int count = 0;
            for (int s = region.StartStageId; s <= region.EndStageId; s++)
            {
                if (IsStageCleared(s)) count++;
            }
            return count;
        }

        /// <summary>실제 비율(0~100, 반올림). 단계로 스냅하지 않음.</summary>
        public int GetRawPercent(RestoreRegionData region)
        {
            if (region == null) return 0;
            int total = region.StageCount;
            if (total <= 0) return 0;
            int cleared = GetClearedStageCount(region);
            return Mathf.RoundToInt(cleared * 100f / total);
        }

        public int GetRawPercentByRegionId(string regionId)
        {
            return GetRawPercent(GetRegionById(regionId));
        }

        /// <summary>0/25/50/75/100 5단계로 스냅된 퍼센트. UI 표시용.</summary>
        public int GetSteppedPercent(RestoreRegionData region)
        {
            int raw = GetRawPercent(region);
            if (raw >= 100) return 100;
            if (raw >= 75)  return 75;
            if (raw >= 50)  return 50;
            if (raw >= 25)  return 25;
            return 0;
        }

        public int GetSteppedPercentByRegionId(string regionId)
        {
            return GetSteppedPercent(GetRegionById(regionId));
        }

        public static RestoreStep GetRestoreStepByPercent(int percent)
        {
            if (percent >= 100) return RestoreStep.Percent100;
            if (percent >= 75)  return RestoreStep.Percent75;
            if (percent >= 50)  return RestoreStep.Percent50;
            if (percent >= 25)  return RestoreStep.Percent25;
            return RestoreStep.Percent0;
        }

        // ───────── 디버그 ─────────

        [ContextMenu("Reset Region Restore Progress")]
        public void ResetRegionRestoreProgress()
        {
            if (regions == null) return;
            int cleared = 0;
            for (int r = 0; r < regions.Count; r++)
            {
                RestoreRegionData region = regions[r];
                if (region == null) continue;
                for (int s = region.StartStageId; s <= region.EndStageId; s++)
                {
                    if (PlayerPrefs.HasKey(StageClearedKeyPrefix + s))
                    {
                        PlayerPrefs.DeleteKey(StageClearedKeyPrefix + s);
                        cleared++;
                    }
                }
            }
            PlayerPrefs.Save();
            Debug.Log($"RegionRestoreManager: Reset region restore progress. Cleared {cleared} entries.");
        }

        [ContextMenu("Debug Restore All Regions")]
        public void DebugRestoreAllRegions()
        {
            if (regions == null) return;
            int marked = 0;
            for (int r = 0; r < regions.Count; r++)
            {
                RestoreRegionData region = regions[r];
                if (region == null) continue;
                for (int s = region.StartStageId; s <= region.EndStageId; s++)
                {
                    if (!IsStageCleared(s))
                    {
                        PlayerPrefs.SetInt(StageClearedKeyPrefix + s, 1);
                        marked++;
                    }
                }
                int p = GetSteppedPercent(region);
                OnRegionRestoreUpdated?.Invoke(region.RegionId, 0, p);
            }
            PlayerPrefs.Save();
            Debug.Log($"RegionRestoreManager: Debug restored all regions. New marks: {marked}.");
        }
    }
}
