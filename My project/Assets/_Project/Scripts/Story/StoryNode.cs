using System;
using System.Collections.Generic;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Story
{
    /// <summary>스테이지 스토리 호출 시 어떤 묶음을 가져올지 구분하는 enum.</summary>
    public enum StoryDialogueType
    {
        StageStart = 0,
        StageClear = 1,
        StageFail = 2,
        BossIntro = 3
    }

    /// <summary>한 줄의 대사. 화자 ID/이름/문장/포트레이트(선택) 정보를 보관.</summary>
    [Serializable]
    public class StoryDialogueLine
    {
        [SerializeField] private string speakerId;
        [SerializeField] private string speakerName;
        [TextArea(2, 5)]
        [SerializeField] private string dialogue;
        [SerializeField] private Sprite portrait;

        public string SpeakerId => speakerId;
        public string SpeakerName => speakerName;
        public string Dialogue => dialogue;
        public Sprite Portrait => portrait;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(dialogue);
        }

        /// <summary>
        /// 본 라인을 복사한 새 인스턴스를 반환하되 dialogue를 newDialogue로 교체.
        /// 원본 StoryNode asset의 직렬화 필드는 건드리지 않는다.
        /// StoryDialogueOverrideManager가 표시용 라인을 만들 때 사용.
        /// </summary>
        public StoryDialogueLine CloneWithDialogue(string newDialogue)
        {
            StoryDialogueLine clone = new StoryDialogueLine();
            clone.speakerId = this.speakerId;
            clone.speakerName = this.speakerName;
            clone.dialogue = newDialogue;
            clone.portrait = this.portrait;
            return clone;
        }
    }

    /// <summary>
    /// 한 스테이지 분량의 스토리 대사를 묶어 보관하는 ScriptableObject.
    /// linkedStageId로 StageData.stageId와 1:1 연결되며, 종류별 대사 리스트(시작/클리어/실패/보스)를 분리해 둔다.
    /// 실제 UI 재생은 후속 단계에서 StoryPopup 등이 GetDialogues를 호출해 사용한다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "StoryNode",
        menuName = "NabyeolDabyeol/Story Node",
        order = 110)]
    public class StoryNode : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Min(1)] private int storyId = 1;
        [SerializeField] private string storyTitle;

        [Header("Learning")]
        [TextArea(1, 3)]
        [SerializeField] private string learningTip;

        [Header("Link")]
        [SerializeField, Min(1)] private int linkedStageId = 1;

        [Header("Dialogues")]
        [SerializeField] private List<StoryDialogueLine> startDialogues = new List<StoryDialogueLine>();
        [SerializeField] private List<StoryDialogueLine> clearDialogues = new List<StoryDialogueLine>();
        [SerializeField] private List<StoryDialogueLine> failDialogues = new List<StoryDialogueLine>();
        [SerializeField] private List<StoryDialogueLine> bossDialogues = new List<StoryDialogueLine>();

        public int StoryId => storyId;
        public string StoryTitle => storyTitle;
        public string LearningTip => learningTip;
        public int LinkedStageId => linkedStageId;

        public IReadOnlyList<StoryDialogueLine> StartDialogues => startDialogues;
        public IReadOnlyList<StoryDialogueLine> ClearDialogues => clearDialogues;
        public IReadOnlyList<StoryDialogueLine> FailDialogues => failDialogues;
        public IReadOnlyList<StoryDialogueLine> BossDialogues => bossDialogues;

        /// <summary>지정된 타입의 대사 리스트를 반환한다. 없으면 빈 리스트.</summary>
        public List<StoryDialogueLine> GetDialogues(StoryDialogueType type)
        {
            switch (type)
            {
                case StoryDialogueType.StageStart:
                    return startDialogues ?? new List<StoryDialogueLine>();
                case StoryDialogueType.StageClear:
                    return clearDialogues ?? new List<StoryDialogueLine>();
                case StoryDialogueType.StageFail:
                    return failDialogues ?? new List<StoryDialogueLine>();
                case StoryDialogueType.BossIntro:
                    return bossDialogues ?? new List<StoryDialogueLine>();
                default:
                    return new List<StoryDialogueLine>();
            }
        }

        /// <summary>
        /// 기본 유효성 검사. storyId·linkedStageId 양수, 최소 하나의 대사 리스트에 유효한 라인 1개 이상.
        /// </summary>
        public bool IsValid()
        {
            if (storyId <= 0) return false;
            if (linkedStageId <= 0) return false;

            return HasAnyValidLine(startDialogues)
                || HasAnyValidLine(clearDialogues)
                || HasAnyValidLine(failDialogues)
                || HasAnyValidLine(bossDialogues);
        }

        private bool HasAnyValidLine(List<StoryDialogueLine> list)
        {
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].IsValid()) return true;
            }
            return false;
        }
    }
}
