using System;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Story
{
    /// <summary>
    /// 한 줄짜리 스토리 대사 후보. 부모가 자유 입력 대신 선택해서 proposedText로 저장한다.
    /// targetSpeakerId가 빈 문자열이면 모든 화자에 사용 가능한 공통 후보.
    /// </summary>
    [Serializable]
    public class StoryCandidateData
    {
        [SerializeField] private string id;
        [SerializeField] private string targetSpeakerId; // 빈 문자열이면 공통
        [SerializeField] private StoryDialogueType targetDialogueType = StoryDialogueType.StageStart;
        [SerializeField] private bool applicableToAllTypes = false;
        [SerializeField] private StoryCandidateTone tone = StoryCandidateTone.Neutral;
        [TextArea(1, 3)]
        [SerializeField] private string text;

        public string Id => id;
        public string TargetSpeakerId => targetSpeakerId;
        public StoryDialogueType TargetDialogueType => targetDialogueType;
        public bool ApplicableToAllTypes => applicableToAllTypes;
        public StoryCandidateTone Tone => tone;
        public string Text => text;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            if (string.IsNullOrWhiteSpace(text)) return false;
            return true;
        }

        /// <summary>Editor 생성기 전용 setter.</summary>
        public void Set(string id, string targetSpeakerId, StoryDialogueType targetType,
                        bool applicableToAllTypes, StoryCandidateTone tone, string text)
        {
            this.id = id;
            this.targetSpeakerId = targetSpeakerId ?? string.Empty;
            this.targetDialogueType = targetType;
            this.applicableToAllTypes = applicableToAllTypes;
            this.tone = tone;
            this.text = text;
        }
    }
}
