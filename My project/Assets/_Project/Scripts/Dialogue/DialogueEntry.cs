using System;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Dialogue
{
    /// <summary>대사 분류 enum. 디버그/필터링/통계 용도.</summary>
    public enum DialogueCategory
    {
        CharacterDefault = 0,
        SkillSuccess = 1,
        SkillFail = 2,
        SkillTutorial = 3,
        StageStart = 4,
        StageClear = 5,
        StageFail = 6,
        BossIntro = 7,
        SystemMessage = 8
    }

    /// <summary>
    /// 한 줄 대사 데이터. key로 코드에서 참조되고, text가 실제 표시 문구.
    /// speakerId는 선택 (UI 표시용). [Serializable]이므로 DialogueDatabase의 List에 직렬화된다.
    /// </summary>
    [Serializable]
    public class DialogueEntry
    {
        [SerializeField] private string key;
        [SerializeField] private DialogueCategory category = DialogueCategory.SystemMessage;
        [TextArea(1, 3)]
        [SerializeField] private string text;
        [SerializeField] private string speakerId;

        public string Key => key;
        public DialogueCategory Category => category;
        public string Text => text;
        public string SpeakerId => speakerId;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            if (string.IsNullOrWhiteSpace(text)) return false;
            return true;
        }

        /// <summary>Editor 생성기에서 사용하는 생성자 대체 setter.</summary>
        public void Set(string key, DialogueCategory category, string text, string speakerId = null)
        {
            this.key = key;
            this.category = category;
            this.text = text;
            this.speakerId = speakerId;
        }
    }
}
