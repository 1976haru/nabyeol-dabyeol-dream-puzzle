using System;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Character
{
    /// <summary>
    /// 캐릭터 대표 대사 템플릿 한 줄.
    /// 사용자는 직접 문장을 입력하지 않고 이 템플릿 목록 중에서 선택한다.
    /// 실제 문장은 DialogueDatabase에서 dialogueKey로 조회된다.
    /// </summary>
    [Serializable]
    public class CharacterDialogueTemplate
    {
        [SerializeField] private string templateId;
        [SerializeField] private string dialogueKey;
        [TextArea(1, 3)]
        [SerializeField] private string previewText;
        [TextArea(1, 2)]
        [SerializeField] private string description;

        public string TemplateId => templateId;
        public string DialogueKey => dialogueKey;
        public string PreviewText => previewText;
        public string Description => description;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(templateId)) return false;
            if (string.IsNullOrWhiteSpace(dialogueKey)) return false;
            return true;
        }

        /// <summary>Editor 생성기 전용 setter.</summary>
        public void Set(string templateId, string dialogueKey, string previewText, string description)
        {
            this.templateId = templateId;
            this.dialogueKey = dialogueKey;
            this.previewText = previewText;
            this.description = description;
        }
    }
}
