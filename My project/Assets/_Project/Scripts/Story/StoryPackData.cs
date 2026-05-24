using System.Collections.Generic;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Story
{
    /// <summary>StoryPack의 분류. UI 그룹화/필터링에 활용.</summary>
    public enum StoryPackType
    {
        Region = 0,
        Boss = 1,
        SpecialEvent = 2,
        Prologue = 3,
        Tutorial = 4
    }

    /// <summary>
    /// 여러 StoryNode와 StoryEventTemplate을 지역/사건 단위로 묶는 상위 데이터.
    /// StoryNode는 stageId별 실제 대사 데이터, StoryPack은 그 묶음 + 재사용 가능한 사건 템플릿.
    /// 외부 JSON/다국어 확장도 본 구조 위에 얹기 쉽도록 설계.
    /// TODO: Add StoryPack JSON import/export.
    /// TODO: Add downloadable story packs.
    /// TODO: Add language-specific StoryPack variants.
    /// TODO: Add community-created StoryPack validation.
    /// </summary>
    [CreateAssetMenu(
        fileName = "StoryPack",
        menuName = "NabyeolDabyeol/Story Pack",
        order = 170)]
    public class StoryPackData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string packId;
        [SerializeField] private string packName;
        [SerializeField] private StoryPackType packType = StoryPackType.Region;
        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Stage Range")]
        [SerializeField, Min(1)] private int startStageId = 1;
        [SerializeField, Min(1)] private int endStageId = 1;

        [Header("Contents")]
        [SerializeField] private List<StoryNode> storyNodes = new List<StoryNode>();
        [SerializeField] private List<StoryEventTemplate> eventTemplates = new List<StoryEventTemplate>();

        public string PackId => packId;
        public string PackName => packName;
        public StoryPackType PackType => packType;
        public string Description => description;
        public int StartStageId => startStageId;
        public int EndStageId => endStageId;
        public IReadOnlyList<StoryNode> StoryNodes => storyNodes;
        public IReadOnlyList<StoryEventTemplate> EventTemplates => eventTemplates;

        public bool ContainsStage(int stageId)
        {
            return stageId >= startStageId && stageId <= endStageId;
        }

        public StoryNode FindStoryNodeByStageId(int stageId)
        {
            if (storyNodes == null) return null;
            for (int i = 0; i < storyNodes.Count; i++)
            {
                StoryNode n = storyNodes[i];
                if (n != null && n.LinkedStageId == stageId) return n;
            }
            return null;
        }

        public StoryEventTemplate FindEventByEventId(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId) || eventTemplates == null) return null;
            for (int i = 0; i < eventTemplates.Count; i++)
            {
                StoryEventTemplate t = eventTemplates[i];
                if (t != null && t.EventId == eventId) return t;
            }
            return null;
        }

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(packId)) return false;
            if (string.IsNullOrWhiteSpace(packName)) return false;
            if (startStageId <= 0) return false;
            if (endStageId < startStageId) return false;
            // storyNodes나 eventTemplates 중 최소 한 쪽에 내용이 있어야 의미 있는 팩.
            bool hasContent = (storyNodes != null && storyNodes.Count > 0)
                              || (eventTemplates != null && eventTemplates.Count > 0);
            return hasContent;
        }
    }
}
