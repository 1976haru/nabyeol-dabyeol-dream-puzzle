using UnityEngine;
using MallangTwins.Data;
namespace MallangTwins.Story {
    [CreateAssetMenu(menuName="MallangTwins/Story Pack")]
    public class StoryPack : ScriptableObject {
        public string packId="bubble_forest";
        public string packTitle="방울숲 이야기";
        public LearningTheme theme=LearningTheme.Animals;
        [TextArea(2,5)] public string opening;
        [TextArea(2,5)] public string ending;
        public StageDefinition[] stages;
    }
}
