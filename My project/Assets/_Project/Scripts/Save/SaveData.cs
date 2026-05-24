using System;
using System.Collections.Generic;
using MallangTwins.Data;
namespace MallangTwins.Save {
    [Serializable]
    public class SaveData {
        public int highestUnlockedStage = 1;
        public List<int> clearedStages = new List<int>();
        public List<string> unlockedKnowledgeCards = new List<string>();
        public PlayerPreset playerPreset = new PlayerPreset();
        public int failCountOnCurrentStage = 0;
        public bool bgmOn = true;
        public bool sfxOn = true;
    }
}
