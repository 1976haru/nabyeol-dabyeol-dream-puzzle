// StageSelectUI.cs
// Task 93 — Map-style stage selector for BubbleForest (1-15) and
// MoonRiceCakeStairs (16-30). Optional boss range (31-32) is also handled
// if a world configuration is added in the inspector.
//
// Design:
//   - Buttons are spawned from a prefab into stageButtonParent on world change.
//   - Existing buttons are destroyed when switching worlds — no pooling for v1.
//   - Locked stages are non-interactable; clicked buttons forward only the id.
//   - PlayButton re-checks lock state defensively before invoking StageManager.
//
// Defensive design:
//   - StageManager / StagePackManager / RegionRestoreManager / RegionRestoreUI
//     are loose-coupled via MonoBehaviour fields and called via reflection.
//   - StageData fields are also read by reflection so this UI does not need
//     to know the concrete data class.
//   - Any missing reference produces a Debug.LogWarning but never a NRE.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Sound;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    public enum StageSelectWorld
    {
        BubbleForest      = 0, // 1 - 15
        MoonRiceCakeStairs = 1, // 16 - 30
        Boss              = 2, // 31 - 32 (optional)
    }

    public class StageSelectUI : MonoBehaviour
    {
        // ---- Panel ------------------------------------------------------------------------

        [Header("Panel")]
        [SerializeField] private GameObject stageSelectPanel;

        // ---- Header / tabs ----------------------------------------------------------------

        [Header("World tabs")]
        [SerializeField] private Button bubbleForestTab;
        [SerializeField] private Button moonRiceCakeTab;
        [SerializeField] private Button bossTab;
        [SerializeField] private Button backButton;

        // ---- Stage grid -------------------------------------------------------------------

        [Header("Stage grid")]
        [SerializeField] private GameObject stageButtonPrefab;
        [SerializeField] private RectTransform stageButtonParent;

        [Tooltip("If true, lays buttons out manually with a zigzag offset so the " +
                 "grid feels like a map path. If false, leaves layout to a " +
                 "GridLayoutGroup on stageButtonParent.")]
        [SerializeField] private bool useZigzagLayout = true;

        [SerializeField] private Vector2 zigzagCellSize = new Vector2(120f, 100f);
        [SerializeField] private int zigzagColumns = 5;
        [SerializeField] private float zigzagOddRowOffsetX = 50f;

        // ---- Selected stage info ----------------------------------------------------------

        [Header("Selected stage info")]
        [SerializeField] private TextMeshProUGUI selectedStageNameText;
        [SerializeField] private TextMeshProUGUI selectedGoalText;
        [SerializeField] private TextMeshProUGUI selectedMovesText;
        [SerializeField] private TextMeshProUGUI selectedRewardText;
        [SerializeField] private TextMeshProUGUI selectedHintText;
        [SerializeField] private Button playButton;

        // ---- Restore info -----------------------------------------------------------------

        [Header("Region restore info")]
        [SerializeField] private TextMeshProUGUI regionNameText;
        [SerializeField] private TextMeshProUGUI restorePercentText;
        [SerializeField] private Button regionRestoreButton;

        // ---- External managers / UIs (loose coupling) -------------------------------------

        [Header("External managers (optional)")]
        [SerializeField] private MonoBehaviour stageManager;
        [SerializeField] private MonoBehaviour stagePackManager;
        [SerializeField] private MonoBehaviour regionRestoreManager;

        [Header("External UIs (optional)")]
        [SerializeField] private MonoBehaviour regionRestoreUI;
        [SerializeField] private MonoBehaviour mainMenuUI;

        // ---- World ranges -----------------------------------------------------------------

        [Header("World ranges")]
        [SerializeField, Min(1)] private int bubbleForestMinId = 1;
        [SerializeField, Min(1)] private int bubbleForestMaxId = 15;
        [SerializeField, Min(1)] private int moonRiceCakeMinId = 16;
        [SerializeField, Min(1)] private int moonRiceCakeMaxId = 30;
        [SerializeField, Min(1)] private int bossMinId         = 31;
        [SerializeField, Min(1)] private int bossMaxId         = 32;

        [Header("Region ids")]
        [SerializeField] private string bubbleForestRegionId   = "bubble_forest";
        [SerializeField] private string moonRiceCakeRegionId   = "moon_rice_cake";
        [SerializeField] private string bossRegionId           = "boss";

        // ---- Runtime state ----------------------------------------------------------------

        private StageSelectWorld currentWorld = StageSelectWorld.BubbleForest;
        private readonly List<StageSelectButton> spawnedButtons = new List<StageSelectButton>();
        private int selectedStageId;
        private object selectedStageData;
        private bool selectedStageLocked;

        // ---- Lifecycle --------------------------------------------------------------------

        private void Awake()
        {
            if (stageSelectPanel != null) stageSelectPanel.SetActive(false);
        }

        private void OnEnable()
        {
            BindButton(bubbleForestTab,     ShowBubbleForest);
            BindButton(moonRiceCakeTab,     ShowMoonRiceCakeStairs);
            BindButton(bossTab,             ShowBoss);
            BindButton(backButton,          CloseStageSelect);
            BindButton(playButton,          PlaySelectedStage);
            BindButton(regionRestoreButton, OpenRegionRestoreUI);
        }

        private void OnDisable()
        {
            UnbindButton(bubbleForestTab,     ShowBubbleForest);
            UnbindButton(moonRiceCakeTab,     ShowMoonRiceCakeStairs);
            UnbindButton(bossTab,             ShowBoss);
            UnbindButton(backButton,          CloseStageSelect);
            UnbindButton(playButton,          PlaySelectedStage);
            UnbindButton(regionRestoreButton, OpenRegionRestoreUI);
        }

        // ---- Public API -------------------------------------------------------------------

        public void OpenStageSelect()
        {
            if (stageSelectPanel != null) stageSelectPanel.SetActive(true);
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBgmWithFade(BgmType.StageSelect);
            }
            Debug.Log("[StageSelectUI] opened.");
            ShowWorld(StageSelectWorld.BubbleForest);
        }

        public void CloseStageSelect()
        {
            if (stageSelectPanel != null) stageSelectPanel.SetActive(false);
            Debug.Log("[StageSelectUI] closed.");
        }

        public void ShowBubbleForest()       => ShowWorld(StageSelectWorld.BubbleForest);
        public void ShowMoonRiceCakeStairs() => ShowWorld(StageSelectWorld.MoonRiceCakeStairs);
        public void ShowBoss()               => ShowWorld(StageSelectWorld.Boss);

        public void ShowWorld(StageSelectWorld world)
        {
            currentWorld = world;
            Debug.Log($"[StageSelectUI] world tab -> {world}.");
            (int min, int max) = GetWorldRange(world);
            GenerateStageButtons(min, max);
            UpdateRestoreInfo();
            ClearSelectionInfo();
        }

        // ---- Stage button generation ------------------------------------------------------

        private void GenerateStageButtons(int minId, int maxId)
        {
            DestroyExistingButtons();

            if (stageButtonPrefab == null || stageButtonParent == null)
            {
                Debug.LogWarning("[StageSelectUI] stageButtonPrefab or stageButtonParent missing — skipping button generation.");
                return;
            }

            int highestUnlocked = TryGetHighestUnlockedStageId();
            int created = 0;

            for (int stageId = minId; stageId <= maxId; stageId++)
            {
                var instance = Instantiate(stageButtonPrefab, stageButtonParent);
                var btn = instance.GetComponent<StageSelectButton>();
                if (btn == null)
                {
                    Debug.LogWarning("[StageSelectUI] stageButtonPrefab missing StageSelectButton component.");
                    Destroy(instance);
                    continue;
                }

                object data        = TryGetStageData(stageId);
                string stageName   = TryGetStringMember(data, "stageName");
                bool   locked      = !TryIsStageUnlocked(stageId, highestUnlocked);
                bool   cleared     = TryIsStageCleared(stageId);
                bool   isCurrent   = highestUnlocked > 0 && stageId == highestUnlocked;

                if (data == null)
                    Debug.LogWarning($"[StageSelectUI] StageData missing for stageId={stageId}.");

                btn.Setup(stageId, stageName, locked, cleared, isCurrent, OnStageButtonClicked);

                if (useZigzagLayout)
                    ApplyZigzagPosition(instance.transform as RectTransform, created);

                spawnedButtons.Add(btn);
                created++;
            }

            Debug.Log($"[StageSelectUI] generated {created} buttons for range {minId}-{maxId}.");
        }

        private void DestroyExistingButtons()
        {
            for (int i = spawnedButtons.Count - 1; i >= 0; i--)
            {
                var b = spawnedButtons[i];
                if (b != null) Destroy(b.gameObject);
            }
            spawnedButtons.Clear();
        }

        private void ApplyZigzagPosition(RectTransform rt, int index)
        {
            if (rt == null) return;
            int row = index / Mathf.Max(1, zigzagColumns);
            int col = index % Mathf.Max(1, zigzagColumns);
            float x = col * zigzagCellSize.x + (row % 2 == 1 ? zigzagOddRowOffsetX : 0f);
            float y = -row * zigzagCellSize.y;
            rt.anchoredPosition = new Vector2(x, y);
        }

        // ---- Selection / play -------------------------------------------------------------

        private void OnStageButtonClicked(int stageId)
        {
            object data = TryGetStageData(stageId);
            int highest = TryGetHighestUnlockedStageId();
            bool locked = !TryIsStageUnlocked(stageId, highest);

            selectedStageId     = stageId;
            selectedStageData   = data;
            selectedStageLocked = locked;

            Debug.Log($"[StageSelectUI] selected stageId={stageId} locked={locked}.");

            if (locked)
            {
                ShowLockedHint();
                if (playButton != null) playButton.interactable = false;
                return;
            }

            PopulateSelectionInfo(stageId, data);
            if (playButton != null) playButton.interactable = data != null;
        }

        private void PopulateSelectionInfo(int stageId, object data)
        {
            string stageName  = TryGetStringMember(data, "stageName");
            string goalText   = BuildGoalText(data);
            int    moveLimit  = TryGetIntMember(data, "moveLimit", -1);
            if (moveLimit < 0) moveLimit = TryGetIntMember(data, "moves", -1);
            string rewardId   = TryGetStringMember(data, "rewardCardId");

            if (selectedStageNameText != null)
                selectedStageNameText.text = string.IsNullOrEmpty(stageName) ? $"Stage {stageId}" : stageName;

            if (selectedGoalText != null)
                selectedGoalText.text = string.IsNullOrEmpty(goalText) ? "목표: -" : goalText;

            if (selectedMovesText != null)
                selectedMovesText.text = moveLimit > 0 ? $"이동 횟수: {moveLimit}" : "이동 횟수: -";

            if (selectedRewardText != null)
                selectedRewardText.text = string.IsNullOrEmpty(rewardId) ? "보상: -" : $"보상 카드: {rewardId}";

            if (selectedHintText != null)
                selectedHintText.text = string.Empty;
        }

        private void ClearSelectionInfo()
        {
            selectedStageId     = 0;
            selectedStageData   = null;
            selectedStageLocked = false;
            if (selectedStageNameText != null) selectedStageNameText.text = string.Empty;
            if (selectedGoalText      != null) selectedGoalText.text      = string.Empty;
            if (selectedMovesText     != null) selectedMovesText.text     = string.Empty;
            if (selectedRewardText    != null) selectedRewardText.text    = string.Empty;
            if (selectedHintText      != null) selectedHintText.text      = string.Empty;
            if (playButton            != null) playButton.interactable    = false;
        }

        private void ShowLockedHint()
        {
            string msg = "이전 스테이지를 먼저 클리어해 주세요.";
            if (selectedHintText != null)        selectedHintText.text        = msg;
            else if (selectedStageNameText != null) selectedStageNameText.text = msg;
            Debug.Log("[StageSelectUI] locked stage hint shown.");
        }

        private string BuildGoalText(object data)
        {
            if (data == null) return null;
            string goalType  = TryGetStringMember(data, "goalType");
            int    goalValue = TryGetIntMember(data, "goalValue", 0);
            if (string.IsNullOrEmpty(goalType) && goalValue == 0) return null;
            if (string.IsNullOrEmpty(goalType)) return $"목표: {goalValue}";
            return $"목표: {goalType} {goalValue}";
        }

        public void PlaySelectedStage()
        {
            if (selectedStageId <= 0)
            {
                Debug.LogWarning("[StageSelectUI] play clicked but no stage selected.");
                return;
            }
            if (selectedStageLocked)
            {
                Debug.LogWarning($"[StageSelectUI] play clicked but stage {selectedStageId} is locked.");
                ShowLockedHint();
                return;
            }

            if (!TryInvokeStageLoad(selectedStageId))
            {
                Debug.LogWarning($"[StageSelectUI] StageManager.LoadStageById({selectedStageId}) failed or unavailable.");
                return;
            }

            Debug.Log($"[StageSelectUI] loaded stageId={selectedStageId} — closing panel.");
            CloseStageSelect();
        }

        // ---- Restore info -----------------------------------------------------------------

        private void UpdateRestoreInfo()
        {
            string regionId   = GetCurrentRegionId();
            string regionName = GetCurrentRegionDisplayName();

            if (regionNameText != null) regionNameText.text = regionName;

            int percent = TryGetRestorePercent(regionId);
            if (restorePercentText != null)
            {
                restorePercentText.text = percent >= 0
                    ? $"복구율 {percent}%"
                    : "복구율 -";
            }
        }

        private string GetCurrentRegionId()
        {
            switch (currentWorld)
            {
                case StageSelectWorld.BubbleForest:       return bubbleForestRegionId;
                case StageSelectWorld.MoonRiceCakeStairs: return moonRiceCakeRegionId;
                case StageSelectWorld.Boss:               return bossRegionId;
                default:                                  return bubbleForestRegionId;
            }
        }

        private string GetCurrentRegionDisplayName()
        {
            switch (currentWorld)
            {
                case StageSelectWorld.BubbleForest:       return "방울숲";
                case StageSelectWorld.MoonRiceCakeStairs: return "달떡계단";
                case StageSelectWorld.Boss:               return "꿈의 정상";
                default:                                  return string.Empty;
            }
        }

        private int TryGetRestorePercent(string regionId)
        {
            if (regionRestoreManager == null || string.IsNullOrEmpty(regionId)) return -1;
            try
            {
                var type = regionRestoreManager.GetType();
                foreach (var name in new[] { "GetRestorePercentByRegionId", "GetRestorePercent", "GetRestorePercentage" })
                {
                    var method = type.GetMethod(name, new[] { typeof(string) });
                    if (method == null || method.ReturnType != typeof(int)) continue;
                    return (int)method.Invoke(regionRestoreManager, new object[] { regionId });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StageSelectUI] restore percent lookup failed: {e.Message}");
            }
            return -1;
        }

        private void OpenRegionRestoreUI()
        {
            string regionId = GetCurrentRegionId();
            if (regionRestoreUI == null)
            {
                Debug.LogWarning("[StageSelectUI] regionRestoreUI not assigned.");
                return;
            }
            try
            {
                var method = regionRestoreUI.GetType().GetMethod("OpenRegionById", new[] { typeof(string) });
                if (method != null)
                {
                    method.Invoke(regionRestoreUI, new object[] { regionId });
                    Debug.Log($"[StageSelectUI] opened RegionRestoreUI for {regionId}.");
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StageSelectUI] RegionRestoreUI.OpenRegionById failed: {e.Message}");
            }
            // Fallback: simple SendMessage.
            regionRestoreUI.SendMessage("OpenRegion", regionId, SendMessageOptions.DontRequireReceiver);
        }

        // ---- Stage data / unlock / clear lookups ------------------------------------------

        private (int min, int max) GetWorldRange(StageSelectWorld world)
        {
            switch (world)
            {
                case StageSelectWorld.BubbleForest:       return (bubbleForestMinId, bubbleForestMaxId);
                case StageSelectWorld.MoonRiceCakeStairs: return (moonRiceCakeMinId, moonRiceCakeMaxId);
                case StageSelectWorld.Boss:               return (bossMinId, bossMaxId);
                default:                                  return (1, 1);
            }
        }

        private object TryGetStageData(int stageId)
        {
            // 1) Preferred: StagePackManager.GetStageById(int)
            object data = InvokeWithInt(stagePackManager, "GetStageById", stageId)
                       ?? InvokeWithInt(stagePackManager, "GetStageData", stageId)
                       ?? InvokeWithInt(stagePackManager, "GetById",      stageId);
            if (data != null) return data;

            // 2) Fallback: ask StageManager directly.
            data = InvokeWithInt(stageManager, "GetStageData", stageId)
                ?? InvokeWithInt(stageManager, "GetStageById", stageId);
            return data;
        }

        private bool TryIsStageUnlocked(int stageId, int highestUnlocked)
        {
            if (stageManager != null)
            {
                try
                {
                    var method = stageManager.GetType().GetMethod("IsStageUnlocked", new[] { typeof(int) });
                    if (method != null && method.ReturnType == typeof(bool))
                        return (bool)method.Invoke(stageManager, new object[] { stageId });
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[StageSelectUI] IsStageUnlocked failed: {e.Message}");
                }
            }

            // Fallback A: compare to highestUnlocked.
            if (highestUnlocked > 0) return stageId <= highestUnlocked;

            // Fallback B: only stage 1 unlocked.
            return stageId == 1;
        }

        private bool TryIsStageCleared(int stageId)
        {
            if (regionRestoreManager != null)
            {
                try
                {
                    var method = regionRestoreManager.GetType().GetMethod("IsStageCleared", new[] { typeof(int) });
                    if (method != null && method.ReturnType == typeof(bool))
                        return (bool)method.Invoke(regionRestoreManager, new object[] { stageId });
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[StageSelectUI] IsStageCleared failed: {e.Message}");
                }
            }
            // Local fallback: PlayerPrefs flag set by stage clear logic.
            return PlayerPrefs.GetInt($"StageCleared_{stageId}", 0) == 1;
        }

        private int TryGetHighestUnlockedStageId()
        {
            if (stageManager == null) return 0;
            try
            {
                var type = stageManager.GetType();
                var method = type.GetMethod("GetHighestUnlockedStageId");
                if (method != null && method.ReturnType == typeof(int))
                    return (int)method.Invoke(stageManager, null);

                var prop = type.GetProperty("HighestUnlockedStageId");
                if (prop != null && prop.PropertyType == typeof(int))
                    return (int)prop.GetValue(stageManager, null);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StageSelectUI] highest-unlocked lookup failed: {e.Message}");
            }
            return 0;
        }

        private bool TryInvokeStageLoad(int stageId)
        {
            if (stageManager == null || stageId <= 0) return false;
            try
            {
                var method = stageManager.GetType().GetMethod("LoadStageById", new[] { typeof(int) });
                if (method == null)
                {
                    Debug.LogWarning("[StageSelectUI] StageManager.LoadStageById(int) not found.");
                    return false;
                }
                method.Invoke(stageManager, new object[] { stageId });
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StageSelectUI] LoadStageById failed: {e.Message}");
                return false;
            }
        }

        // ---- Reflection helpers -----------------------------------------------------------

        private static object InvokeWithInt(MonoBehaviour target, string methodName, int arg)
        {
            if (target == null) return null;
            try
            {
                var method = target.GetType().GetMethod(methodName, new[] { typeof(int) });
                if (method == null) return null;
                return method.Invoke(target, new object[] { arg });
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StageSelectUI] {methodName}({arg}) failed: {e.Message}");
                return null;
            }
        }

        private static string TryGetStringMember(object data, string memberName)
        {
            if (data == null || string.IsNullOrEmpty(memberName)) return null;
            try
            {
                var type = data.GetType();
                var field = type.GetField(memberName);
                if (field != null && field.FieldType == typeof(string))
                    return field.GetValue(data) as string;

                var prop = type.GetProperty(memberName);
                if (prop != null && prop.PropertyType == typeof(string))
                    return prop.GetValue(data, null) as string;
            }
            catch (System.Exception) { /* swallow */ }
            return null;
        }

        private static int TryGetIntMember(object data, string memberName, int fallback)
        {
            if (data == null || string.IsNullOrEmpty(memberName)) return fallback;
            try
            {
                var type = data.GetType();
                var field = type.GetField(memberName);
                if (field != null && field.FieldType == typeof(int))
                    return (int)field.GetValue(data);

                var prop = type.GetProperty(memberName);
                if (prop != null && prop.PropertyType == typeof(int))
                    return (int)prop.GetValue(data, null);
            }
            catch (System.Exception) { /* swallow */ }
            return fallback;
        }

        // ---- Tiny UI helpers --------------------------------------------------------------

        private static void BindButton(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button == null || handler == null) return;
            button.onClick.AddListener(handler);
        }

        private static void UnbindButton(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button == null || handler == null) return;
            button.onClick.RemoveListener(handler);
        }
    }
}
