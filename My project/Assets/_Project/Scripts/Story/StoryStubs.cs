using System;
using UnityEngine;
namespace NabyeolDabyeolDreamPuzzle.Story
{
    // 주의: StoryManager는 별도 파일 StoryManager.cs로 분리되었다.
    // 본 파일은 StoryTextUI 호환을 위한 한 줄 표현(StoryLineData)과 묶음 컨테이너(StageStoryEntry)만 보관한다.

    [Serializable]
    public class StoryLineData
    {
        [SerializeField] private string speakerName;
        [SerializeField, TextArea(2,4)] private string text;
        public string SpeakerName => speakerName;
        public string Text => text;
        public bool IsValid() => !string.IsNullOrWhiteSpace(text);

        /// <summary>StoryManager 호환 shim에서 사용하는 setter. StoryDialogueLine → StoryLineData 변환용.</summary>
        public void Set(string speakerName, string text)
        {
            this.speakerName = speakerName;
            this.text = text;
        }
    }

    [Serializable]
    public class StageStoryEntry
    {
        public StoryLineData startLine = new StoryLineData();
        public StoryLineData clearLine = new StoryLineData();
        public StoryLineData failLine = new StoryLineData();
    }
}
