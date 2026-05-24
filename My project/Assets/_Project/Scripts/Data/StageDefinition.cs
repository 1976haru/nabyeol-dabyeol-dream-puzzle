using System;
using UnityEngine;
namespace MallangTwins.Data {
    [Serializable]
    public class KnowledgeCard {
        public string cardId;
        public LearningTheme theme;
        public string title;
        [TextArea(2,4)] public string shortText;
    }
    [Serializable]
    public class StageGoal {
        public BlockType targetBlockType = BlockType.DreamBubble;
        public int targetCount = 8;
        public int targetScore = 500;
        public bool requireBlockCount = true;
        public bool requireScore = false;
    }
    [CreateAssetMenu(menuName="MallangTwins/Stage Definition")]
    public class StageDefinition : ScriptableObject {
        public int stageNumber = 1;
        public string stageTitle = "방울숲의 첫 번째 방울";
        public LearningTheme theme = LearningTheme.Animals;
        public int width = 6;
        public int height = 6;
        public int moveLimit = 25;
        public BlockType[] allowedBlocks = { BlockType.DreamBubble, BlockType.MoonRiceCake, BlockType.InkStar, BlockType.SkyWave, BlockType.HeartLight };
        public StageGoal goal = new StageGoal();
        [TextArea(2,4)] public string introLine = "같은 꿈방울을 세 개 모으면 기억이 돌아올 거야.";
        [TextArea(2,4)] public string clearLine = "반짝 조각을 되찾았어!";
        public KnowledgeCard rewardCard;
    }
}
