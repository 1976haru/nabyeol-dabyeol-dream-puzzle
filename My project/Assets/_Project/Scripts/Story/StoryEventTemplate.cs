using System;
using System.Collections.Generic;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Story
{
    /// <summary>StoryEventTemplate의 사건 분류 enum.</summary>
    public enum StoryEventType
    {
        RegionIntro = 0,
        RegionOutro = 1,
        FirstMeet = 2,
        BossEntry = 3,
        SpecialEvent = 4
    }

    /// <summary>
    /// 한 사건(이벤트) 단위의 대사 묶음. StoryNode와 달리 stageId에 결합되지 않고,
    /// "방울숲 처음 입장", "노노 첫 등장" 같은 트리거에 묶여 재사용된다.
    /// </summary>
    [Serializable]
    public class StoryEventTemplate
    {
        [SerializeField] private string eventId;
        [SerializeField] private string eventName;
        [SerializeField] private StoryEventType eventType = StoryEventType.SpecialEvent;
        [TextArea(2, 4)]
        [SerializeField] private string eventDescription;
        [SerializeField] private List<StoryDialogueLine> eventDialogues = new List<StoryDialogueLine>();

        public string EventId => eventId;
        public string EventName => eventName;
        public StoryEventType EventType => eventType;
        public string EventDescription => eventDescription;
        public IReadOnlyList<StoryDialogueLine> EventDialogues => eventDialogues;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(eventId)) return false;
            if (eventDialogues == null || eventDialogues.Count == 0) return false;
            for (int i = 0; i < eventDialogues.Count; i++)
            {
                if (eventDialogues[i] != null && eventDialogues[i].IsValid()) return true;
            }
            return false;
        }
    }
}
