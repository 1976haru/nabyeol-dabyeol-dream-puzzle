using System;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Story
{
    /// <summary>
    /// 스토리 대사 한 줄의 override 상태.
    /// 원본 StoryNode 자산은 절대 수정하지 않고, PlayerPrefs에 본 구조와 동등한 데이터를 분산 저장한다.
    /// stageId + dialogueType + lineIndex 조합이 한 줄을 식별한다.
    /// </summary>
    [Serializable]
    public class StoryDialogueOverrideData
    {
        public int stageId;
        public StoryDialogueType dialogueType;
        public int lineIndex;
        public string originalText;
        public string proposedText;
        public string approvedText;
        public bool isApproved;

        public StoryDialogueOverrideData() { }

        public StoryDialogueOverrideData(int stageId, StoryDialogueType type, int lineIndex)
        {
            this.stageId = stageId;
            this.dialogueType = type;
            this.lineIndex = lineIndex;
        }

        /// <summary>승인된 문장이 게임에 적용 가능한 상태인지.</summary>
        public bool HasApplicableApproved()
        {
            return isApproved && !string.IsNullOrWhiteSpace(approvedText);
        }

        /// <summary>제안만 있고 아직 승인되지 않은 상태인지.</summary>
        public bool IsPendingReview()
        {
            return !isApproved && !string.IsNullOrWhiteSpace(proposedText);
        }
    }
}
