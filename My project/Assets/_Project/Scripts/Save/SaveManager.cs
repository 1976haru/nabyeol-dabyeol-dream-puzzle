using System;
using System.IO;
using UnityEngine;
using MallangTwins.Data;
namespace MallangTwins.Save {
    public class SaveManager : MonoBehaviour {
        public static SaveManager Instance { get; private set; }
        public SaveData Data { get; private set; } = new SaveData();
        string SavePath => Path.Combine(Application.persistentDataPath, "mallangtwins_save.json");
        void Awake() {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject); Load();
        }
        public void Load() {
            try {
                if (!File.Exists(SavePath)) { Data = new SaveData(); Save(); return; }
                Data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath)) ?? new SaveData();
            } catch(Exception e) { Debug.LogWarning(e.Message); Data = new SaveData(); Save(); }
        }
        public void Save() => File.WriteAllText(SavePath, JsonUtility.ToJson(Data, true));
        public void MarkStageCleared(int n) {
            if (!Data.clearedStages.Contains(n)) Data.clearedStages.Add(n);
            if (Data.highestUnlockedStage <= n) Data.highestUnlockedStage = n + 1;
            Data.failCountOnCurrentStage = 0; Save();
        }
        public void AddKnowledgeCard(string id) {
            if (!string.IsNullOrWhiteSpace(id) && !Data.unlockedKnowledgeCards.Contains(id)) Data.unlockedKnowledgeCards.Add(id);
            Save();
        }
        public void RegisterFail() { Data.failCountOnCurrentStage++; Save(); }
        public void UpdatePreset(PlayerPreset p) { Data.playerPreset = p; Save(); }
    }
}
