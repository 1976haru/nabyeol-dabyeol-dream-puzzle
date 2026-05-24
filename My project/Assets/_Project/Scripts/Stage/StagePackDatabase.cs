using System.Collections.Generic;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Stage
{
    /// <summary>
    /// 모든 StagePackData를 모아 보관/검색하는 ScriptableObject.
    /// StageManager가 stageList에서 못 찾은 stageId를 fallback으로 본 데이터베이스에서 검색.
    /// BoardManager는 GetBoardRuleByStageId로 공통 보드 규칙을 참조할 수 있다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "StagePackDatabase",
        menuName = "NabyeolDabyeol/Stage Pack Database",
        order = 191)]
    public class StagePackDatabase : ScriptableObject
    {
        [SerializeField] private List<StagePackData> stagePacks = new List<StagePackData>();

        public IReadOnlyList<StagePackData> StagePacks => stagePacks;
        public int Count => stagePacks == null ? 0 : stagePacks.Count;

        public StagePackData FindByPackId(string packId)
        {
            if (string.IsNullOrWhiteSpace(packId) || stagePacks == null) return null;
            for (int i = 0; i < stagePacks.Count; i++)
            {
                if (stagePacks[i] != null && stagePacks[i].PackId == packId) return stagePacks[i];
            }
            return null;
        }

        public StagePackData FindPackByStageId(int stageId)
        {
            if (stagePacks == null) return null;
            for (int i = 0; i < stagePacks.Count; i++)
            {
                if (stagePacks[i] != null && stagePacks[i].ContainsStage(stageId)) return stagePacks[i];
            }
            return null;
        }

        public StageData FindStageById(int stageId)
        {
            if (stagePacks == null) return null;
            for (int i = 0; i < stagePacks.Count; i++)
            {
                if (stagePacks[i] == null) continue;
                StageData s = stagePacks[i].FindStageById(stageId);
                if (s != null) return s;
            }
            return null;
        }

        public StageBoardRule FindBoardRuleByStageId(int stageId)
        {
            StagePackData pack = FindPackByStageId(stageId);
            return pack != null ? pack.DefaultBoardRule : null;
        }

        public bool ValidatePacks()
        {
            if (stagePacks == null) return false;
            bool ok = true;
            HashSet<string> seenPackIds = new HashSet<string>();
            Dictionary<int, List<string>> stageIdToPackIds = new Dictionary<int, List<string>>();

            for (int p = 0; p < stagePacks.Count; p++)
            {
                StagePackData pack = stagePacks[p];
                if (pack == null)
                {
                    Debug.LogWarning($"StagePackDatabase: stagePacks[{p}] is null.");
                    ok = false; continue;
                }
                if (!pack.IsValid())
                {
                    Debug.LogWarning($"StagePackDatabase: stagePacks[{p}] '{pack.name}' failed IsValid().");
                    ok = false;
                }
                if (!string.IsNullOrWhiteSpace(pack.PackId) && !seenPackIds.Add(pack.PackId))
                {
                    Debug.LogWarning($"StagePackDatabase: Duplicate packId '{pack.PackId}' at stagePacks[{p}].");
                    ok = false;
                }
                if (pack.DefaultBoardRule == null || !pack.DefaultBoardRule.IsValid())
                {
                    Debug.LogWarning($"StagePackDatabase: pack '{pack.PackId}' DefaultBoardRule is invalid.");
                    ok = false;
                }

                if (pack.Stages != null)
                {
                    for (int s = 0; s < pack.Stages.Count; s++)
                    {
                        StageData stage = pack.Stages[s];
                        if (stage == null)
                        {
                            Debug.LogWarning($"StagePackDatabase: pack '{pack.PackId}' stages[{s}] is null.");
                            ok = false; continue;
                        }
                        if (!stage.IsValid())
                        {
                            Debug.LogWarning($"StagePackDatabase: pack '{pack.PackId}' stage '{stage.name}' failed IsValid().");
                            ok = false;
                        }
                        if (!pack.ContainsStage(stage.StageId))
                        {
                            Debug.LogWarning($"StagePackDatabase: pack '{pack.PackId}' stage '{stage.StageName}' (id={stage.StageId}) outside pack range [{pack.StartStageId}..{pack.EndStageId}].");
                            ok = false;
                        }
                        if (!stageIdToPackIds.TryGetValue(stage.StageId, out List<string> list))
                        {
                            list = new List<string>();
                            stageIdToPackIds[stage.StageId] = list;
                        }
                        list.Add(pack.PackId);
                    }
                }
            }

            foreach (KeyValuePair<int, List<string>> kv in stageIdToPackIds)
            {
                if (kv.Value.Count > 1)
                {
                    Debug.LogWarning($"StagePackDatabase: stageId {kv.Key} appears in multiple packs [{string.Join(", ", kv.Value)}]. (정책상 stageList 우선이지만 데이터 중복은 피하는 것을 권장)");
                }
            }

            return ok;
        }
    }
}
