using System;
namespace MallangTwins.Data {
    [Serializable]
    public class PlayerPreset {
        public string firstTwinName = "나별";
        public string secondTwinName = "다별";
        public string firstTwinPersonality = "차분하고 규칙을 잘 찾는 아이";
        public string secondTwinPersonality = "밝고 도전을 좋아하는 아이";
        public LearningTheme preferredTheme = LearningTheme.Animals;
        public bool parentApprovedCustomStory = true;
        public bool useSafeTextFilter = true;
    }
}
