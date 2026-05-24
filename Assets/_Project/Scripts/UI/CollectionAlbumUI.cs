// CollectionAlbumUI.cs
// Task 97 — Unified collection/album UI with three tabs:
//   - Characters (built from CharacterPackDatabase)
//   - Knowledge cards (built from KnowledgeCardDatabase)
//   - Sparkle album pages (built from AlbumDatabase)
//
// Design:
//   - One panel, three list views switched by tab buttons. Each tab rebuilds
//     its item list from scratch via CollectionItemButton prefab instances.
//   - Locked items are still clickable so the detail pane can show an unlock
//     hint instead of being silent.
//   - Detail pane has a single set of fields (image / name / description /
//     rarity / locked overlay) that all three tabs reuse — no per-tab prefab.
//
// Defensive design:
//   - All external managers/databases are loose-coupled MonoBehaviour fields
//     and accessed by reflection. The script compiles standalone and auto-
//     wires once the referenced classes exist.
//   - Missing item-button prefab, missing databases, missing detail fields:
//     every code path null-checks before use. NRE must never come from this UI.
//   - SparkleAlbumUI continues to exist; this UI is additive and can be wired
//     into MainMenuUI in parallel.
//
// Out of scope for v1 (per task brief):
//   - Final art / illustrations.
//   - A real "card earned" persistence layer — knowledge card unlocks fall back
//     to "stage cleared" state.
//   - Filtering / sorting / search.

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    public enum CollectionAlbumTab
    {
        Characters,
        KnowledgeCards,
        SparkleAlbum,
    }

    public class CollectionAlbumUI : MonoBehaviour
    {
        // ---- Panel ------------------------------------------------------------------------

        [Header("Panel")]
        [SerializeField] private GameObject collectionAlbumPanel;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private string panelTitle = "도감 · 앨범";

        // ---- Tabs -------------------------------------------------------------------------

        [Header("Tab buttons")]
        [SerializeField] private Button characterTabButton;
        [SerializeField] private Button knowledgeCardTabButton;
        [SerializeField] private Button sparkleAlbumTabButton;

        [Header("Tab highlight (optional)")]
        [SerializeField] private GameObject characterTabHighlight;
        [SerializeField] private GameObject knowledgeCardTabHighlight;
        [SerializeField] private GameObject sparkleAlbumTabHighlight;

        // ---- List -------------------------------------------------------------------------

        [Header("List view")]
        [SerializeField] private GameObject itemButtonPrefab;
        [SerializeField] private RectTransform itemButtonParent;

        // ---- Detail -----------------------------------------------------------------------

        [Header("Detail view")]
        [SerializeField] private Image detailImage;
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailSubText;        // 말투 / category / world name
        [SerializeField] private TextMeshProUGUI detailDescriptionText;
        [SerializeField] private TextMeshProUGUI detailRarityText;
        [SerializeField] private TextMeshProUGUI detailExtraText;      // 대표 대사 / shortText / etc.
        [SerializeField] private GameObject lockedOverlay;

        // ---- Footer -----------------------------------------------------------------------

        [Header("Footer")]
        [SerializeField] private TextMeshProUGUI countText;

        // ---- External managers / databases (loose coupling) -------------------------------

        [Header("Character data (optional)")]
        [SerializeField] private MonoBehaviour characterPackDatabase;
        [SerializeField] private MonoBehaviour characterPackManager;
        [SerializeField] private MonoBehaviour characterAliasManager;
        [SerializeField] private MonoBehaviour characterRepresentativeDialogueManager;

        [Header("Knowledge card data (optional)")]
        [SerializeField] private MonoBehaviour knowledgeCardDatabase;

        [Header("Sparkle album data (optional)")]
        [SerializeField] private MonoBehaviour albumDatabase;
        [SerializeField] private MonoBehaviour albumProgressManager;

        [Header("Clear-state lookup (optional)")]
        [SerializeField] private MonoBehaviour regionRestoreManager;

        // ---- Fallback character roster ----------------------------------------------------

        [Tooltip("Fallback character ids used when CharacterPackDatabase is missing. " +
                 "Same order as the main menu rotation.")]
        [SerializeField] private string[] fallbackCharacterIds =
        {
            "nabyeol", "dabyeol", "capymong", "poporing", "mochirun", "nono",
        };

        // ---- Runtime ----------------------------------------------------------------------

        private CollectionAlbumTab currentTab = CollectionAlbumTab.Characters;
        private readonly List<CollectionItemButton> spawnedButtons = new List<CollectionItemButton>();
        private CollectionItemButton selectedButton;

        // ---- Lifecycle --------------------------------------------------------------------

        private void Awake()
        {
            if (collectionAlbumPanel != null) collectionAlbumPanel.SetActive(false);
        }

        private void OnEnable()
        {
            BindButton(closeButton,              CloseCollectionAlbum);
            BindButton(characterTabButton,       ShowCharactersTab);
            BindButton(knowledgeCardTabButton,   ShowKnowledgeCardsTab);
            BindButton(sparkleAlbumTabButton,    ShowSparkleAlbumTab);
        }

        private void OnDisable()
        {
            UnbindButton(closeButton,            CloseCollectionAlbum);
            UnbindButton(characterTabButton,     ShowCharactersTab);
            UnbindButton(knowledgeCardTabButton, ShowKnowledgeCardsTab);
            UnbindButton(sparkleAlbumTabButton,  ShowSparkleAlbumTab);
        }

        // ---- Public API -------------------------------------------------------------------

        /// <summary>Open the collection panel on the Characters tab.</summary>
        public void OpenCollectionAlbum()
        {
            if (collectionAlbumPanel != null) collectionAlbumPanel.SetActive(true);
            if (titleText != null) titleText.text = panelTitle;
            ShowCharactersTab();
            Debug.Log("[CollectionAlbum] opened.");
        }

        /// <summary>Hide the collection panel.</summary>
        public void CloseCollectionAlbum()
        {
            if (collectionAlbumPanel != null) collectionAlbumPanel.SetActive(false);
            Debug.Log("[CollectionAlbum] closed.");
        }

        public void ShowCharactersTab()    => ShowTab(CollectionAlbumTab.Characters);
        public void ShowKnowledgeCardsTab() => ShowTab(CollectionAlbumTab.KnowledgeCards);
        public void ShowSparkleAlbumTab()  => ShowTab(CollectionAlbumTab.SparkleAlbum);

        public void ShowTab(CollectionAlbumTab tab)
        {
            currentTab = tab;
            Debug.Log($"[CollectionAlbum] tab -> {tab}.");

            DestroyExistingButtons();
            ClearDetail();
            ApplyTabHighlight();

            int total = 0, unlocked = 0;
            switch (tab)
            {
                case CollectionAlbumTab.Characters:    (total, unlocked) = BuildCharacterList(); break;
                case CollectionAlbumTab.KnowledgeCards: (total, unlocked) = BuildKnowledgeCardList(); break;
                case CollectionAlbumTab.SparkleAlbum:  (total, unlocked) = BuildSparkleAlbumList(); break;
            }

            UpdateCount(total, unlocked);
            Debug.Log($"[CollectionAlbum] tab {tab} list: total={total} unlocked={unlocked}.");
        }

        // ---- List building: characters ----------------------------------------------------

        private (int total, int unlocked) BuildCharacterList()
        {
            if (!CanSpawnButtons()) return (0, 0);

            List<string> ids = TryGetCharacterIds();
            int total = ids.Count;
            int unlocked = total; // v1: every character defined in the pack is visible/unlocked.

            foreach (var id in ids)
            {
                string label = TryGetCharacterDisplayName(id);
                string sub   = TryGetCharacterToneSummary(id);
                Sprite icon  = TryGetCharacterSprite(id);

                SpawnItemButton(id, label, sub, icon, locked: false);
            }
            return (total, unlocked);
        }

        private List<string> TryGetCharacterIds()
        {
            var ids = new List<string>();
            if (characterPackDatabase != null)
            {
                try
                {
                    var listObj = TryGetMember(characterPackDatabase, "characters")
                                ?? TryGetMember(characterPackDatabase, "Characters")
                                ?? TryGetMember(characterPackDatabase, "all");
                    if (listObj is IEnumerable<object> typedEnum)
                    {
                        foreach (var entry in typedEnum)
                            AddIdFrom(ids, entry);
                    }
                    else if (listObj is System.Collections.IEnumerable rawEnum)
                    {
                        foreach (var entry in rawEnum)
                            AddIdFrom(ids, entry);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CollectionAlbum] CharacterPackDatabase.characters lookup failed: {e.Message}");
                }
            }

            if (ids.Count == 0)
            {
                Debug.LogWarning("[CollectionAlbum] CharacterPackDatabase unavailable — using fallback id list.");
                if (fallbackCharacterIds != null)
                    foreach (var id in fallbackCharacterIds)
                        if (!string.IsNullOrEmpty(id)) ids.Add(id);
            }
            return ids;
        }

        private static void AddIdFrom(List<string> ids, object entry)
        {
            if (entry == null) return;
            string id = TryGetStringMember(entry, "characterId")
                     ?? TryGetStringMember(entry, "id")
                     ?? TryGetStringMember(entry, "key");
            if (!string.IsNullOrEmpty(id) && !ids.Contains(id)) ids.Add(id);
        }

        private string TryGetCharacterDisplayName(string characterId)
        {
            string alias = InvokeStringMethodOneArg(characterAliasManager, "GetAlias", characterId);
            if (!string.IsNullOrEmpty(alias)) return alias;

            string fromManager = InvokeStringMethodOneArg(characterPackManager, "GetDisplayName", characterId);
            if (!string.IsNullOrEmpty(fromManager)) return fromManager;

            object data = TryGetCharacterData(characterId);
            string fromData = TryGetStringMember(data, "displayName")
                           ?? TryGetStringMember(data, "characterName")
                           ?? TryGetStringMember(data, "name");
            if (!string.IsNullOrEmpty(fromData)) return fromData;

            switch (characterId)
            {
                case "nabyeol":  return "나별";
                case "dabyeol":  return "다별";
                case "capymong": return "카피몽";
                case "poporing": return "포포링";
                case "mochirun": return "모찌룬";
                case "nono":     return "노노";
                default:         return characterId;
            }
        }

        private string TryGetCharacterToneSummary(string characterId)
        {
            object data = TryGetCharacterData(characterId);
            return TryGetStringMember(data, "toneSummary")
                ?? TryGetStringMember(data, "tone")
                ?? TryGetStringMember(data, "personality");
        }

        private string TryGetCharacterRepresentativeDialogue(string characterId)
        {
            string fromManager = InvokeStringMethodOneArg(
                characterRepresentativeDialogueManager, "GetRepresentativeDialogue", characterId);
            if (!string.IsNullOrEmpty(fromManager)) return fromManager;

            object data = TryGetCharacterData(characterId);
            string key  = TryGetStringMember(data, "representativeDialogueKey");
            if (!string.IsNullOrEmpty(key))
            {
                // No DialogueManager dependency in v1; surface the key so the
                // designer can spot wiring gaps in inspector logs.
                return key;
            }
            return TryGetStringMember(data, "representativeDialogue");
        }

        private string TryGetCharacterSkillName(string characterId)
        {
            object data = TryGetCharacterData(characterId);
            return TryGetStringMember(data, "skillName")
                ?? TryGetStringMember(data, "skill");
        }

        private Sprite TryGetCharacterSprite(string characterId)
        {
            object data = TryGetCharacterData(characterId);
            if (data == null) return null;
            foreach (var name in new[] { "profileSprite", "portrait", "image", "characterSprite", "sprite" })
            {
                var sprite = TryGetMember(data, name) as Sprite;
                if (sprite != null) return sprite;
            }
            return null;
        }

        private object TryGetCharacterData(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            // Prefer manager (knows alias overrides), fall back to raw database.
            object data = TryInvokeOneArg(characterPackManager, "GetCharacterData", characterId)
                       ?? TryInvokeOneArg(characterPackManager, "GetCharacter", characterId)
                       ?? TryInvokeOneArg(characterPackManager, "GetById", characterId);
            if (data != null) return data;

            data = TryInvokeOneArg(characterPackDatabase, "GetCharacterData", characterId)
                ?? TryInvokeOneArg(characterPackDatabase, "GetCharacter", characterId)
                ?? TryInvokeOneArg(characterPackDatabase, "GetById", characterId);
            return data;
        }

        // ---- List building: knowledge cards -----------------------------------------------

        private (int total, int unlocked) BuildKnowledgeCardList()
        {
            if (!CanSpawnButtons()) return (0, 0);

            var cards = TryGetCardList();
            int total = cards.Count;
            int unlocked = 0;

            foreach (var card in cards)
            {
                string id     = TryGetStringMember(card, "cardId")
                             ?? TryGetStringMember(card, "id");
                if (string.IsNullOrEmpty(id)) continue;

                string label  = TryGetStringMember(card, "cardName") ?? id;
                int    stage  = TryGetIntMember(card, "linkedStageId", 0);
                bool   locked = !IsStageCleared(stage);
                if (!locked) unlocked++;

                Sprite icon  = TryGetMember(card, "image") as Sprite
                            ?? TryGetMember(card, "cardImage") as Sprite
                            ?? TryGetMember(card, "sprite") as Sprite;

                string sub = locked
                    ? (stage > 0 ? $"Stage {stage}" : "잠긴 카드")
                    : (TryGetStringMember(card, "category") ?? string.Empty);

                SpawnItemButton(id, locked ? "??? " : label, sub, locked ? null : icon, locked);
            }

            if (total == 0)
                Debug.LogWarning("[CollectionAlbum] KnowledgeCardDatabase empty or unavailable.");
            return (total, unlocked);
        }

        private List<object> TryGetCardList()
        {
            var result = new List<object>();
            if (knowledgeCardDatabase == null) return result;
            try
            {
                var listObj = TryGetMember(knowledgeCardDatabase, "cards")
                            ?? TryGetMember(knowledgeCardDatabase, "Cards")
                            ?? TryGetMember(knowledgeCardDatabase, "all");
                if (listObj is System.Collections.IEnumerable raw)
                    foreach (var entry in raw)
                        if (entry != null) result.Add(entry);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CollectionAlbum] KnowledgeCardDatabase.cards lookup failed: {e.Message}");
            }
            return result;
        }

        // ---- List building: sparkle album -------------------------------------------------

        private (int total, int unlocked) BuildSparkleAlbumList()
        {
            if (!CanSpawnButtons()) return (0, 0);

            var pages = TryGetAlbumPages();
            int total = pages.Count;
            int unlocked = 0;

            foreach (var page in pages)
            {
                string id    = TryGetStringMember(page, "pageId")
                            ?? TryGetStringMember(page, "id");
                if (string.IsNullOrEmpty(id))
                    id = $"page_{total - pages.Count + 1}";

                string title = TryGetStringMember(page, "pageTitle")
                            ?? TryGetStringMember(page, "title")
                            ?? id;
                int stageId  = TryGetIntMember(page, "linkedStageId", 0);
                bool locked  = !IsAlbumPageUnlocked(stageId);
                if (!locked) unlocked++;

                Sprite icon = TryGetMember(page, "pageImage") as Sprite
                           ?? TryGetMember(page, "image") as Sprite
                           ?? TryGetMember(page, "sprite") as Sprite;

                string sub = locked
                    ? (stageId > 0 ? $"Stage {stageId}" : "잠긴 장면")
                    : (TryGetStringMember(page, "worldName") ?? string.Empty);

                SpawnItemButton(id, locked ? "???" : title, sub, locked ? null : icon, locked);
            }

            if (total == 0)
                Debug.LogWarning("[CollectionAlbum] AlbumDatabase empty or unavailable.");
            return (total, unlocked);
        }

        private List<object> TryGetAlbumPages()
        {
            var result = new List<object>();
            if (albumDatabase == null) return result;
            try
            {
                var listObj = TryGetMember(albumDatabase, "pages")
                            ?? TryGetMember(albumDatabase, "Pages")
                            ?? TryGetMember(albumDatabase, "all");
                if (listObj is System.Collections.IEnumerable raw)
                    foreach (var entry in raw)
                        if (entry != null) result.Add(entry);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CollectionAlbum] AlbumDatabase.pages lookup failed: {e.Message}");
            }
            return result;
        }

        // ---- Unlock checks ----------------------------------------------------------------

        private bool IsStageCleared(int stageId)
        {
            if (stageId <= 0) return false;

            if (regionRestoreManager != null)
            {
                try
                {
                    var method = regionRestoreManager.GetType().GetMethod("IsStageCleared", new[] { typeof(int) });
                    if (method != null && method.ReturnType == typeof(bool))
                        return (bool)method.Invoke(regionRestoreManager, new object[] { stageId });
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CollectionAlbum] IsStageCleared lookup failed: {e.Message}");
                }
            }
            return PlayerPrefs.GetInt($"StageCleared_{stageId}", 0) == 1;
        }

        private bool IsAlbumPageUnlocked(int linkedStageId)
        {
            if (albumProgressManager != null && linkedStageId > 0)
            {
                try
                {
                    var method = albumProgressManager.GetType().GetMethod("IsPageUnlocked", new[] { typeof(int) });
                    if (method != null && method.ReturnType == typeof(bool))
                        return (bool)method.Invoke(albumProgressManager, new object[] { linkedStageId });
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CollectionAlbum] IsPageUnlocked lookup failed: {e.Message}");
                }
            }
            // Fallback: same rule as stage clear.
            return IsStageCleared(linkedStageId);
        }

        // ---- Detail rendering -------------------------------------------------------------

        private void OnItemSelected(string itemId)
        {
            UpdateSelectedHighlight(itemId);

            switch (currentTab)
            {
                case CollectionAlbumTab.Characters:    ShowCharacterDetail(itemId); break;
                case CollectionAlbumTab.KnowledgeCards: ShowKnowledgeCardDetail(itemId); break;
                case CollectionAlbumTab.SparkleAlbum:  ShowSparkleAlbumDetail(itemId); break;
            }
        }

        private void UpdateSelectedHighlight(string itemId)
        {
            foreach (var b in spawnedButtons)
            {
                if (b == null) continue;
                bool isMe = b.ItemId == itemId;
                b.SetSelected(isMe);
                if (isMe) selectedButton = b;
            }
        }

        private void ShowCharacterDetail(string characterId)
        {
            SetLockedOverlay(false);

            string displayName = TryGetCharacterDisplayName(characterId);
            string tone        = TryGetCharacterToneSummary(characterId);
            string repDialogue = TryGetCharacterRepresentativeDialogue(characterId);
            string skillName   = TryGetCharacterSkillName(characterId);
            Sprite portrait    = TryGetCharacterSprite(characterId);

            if (detailNameText != null)
                detailNameText.text = string.IsNullOrEmpty(displayName) ? characterId : displayName;
            if (detailSubText != null)
                detailSubText.text = string.IsNullOrEmpty(tone) ? string.Empty : tone;
            if (detailDescriptionText != null)
                detailDescriptionText.text = string.IsNullOrEmpty(skillName) ? string.Empty : $"스킬: {skillName}";
            if (detailRarityText != null)
                detailRarityText.text = string.Empty;
            if (detailExtraText != null)
                detailExtraText.text = string.IsNullOrEmpty(repDialogue) ? string.Empty : $"\"{repDialogue}\"";
            ApplyDetailImage(portrait);
        }

        private void ShowKnowledgeCardDetail(string cardId)
        {
            object card = TryInvokeOneArg(knowledgeCardDatabase, "GetCardById", cardId)
                       ?? TryInvokeOneArg(knowledgeCardDatabase, "GetCard", cardId)
                       ?? TryInvokeOneArg(knowledgeCardDatabase, "GetCardData", cardId)
                       ?? TryInvokeOneArg(knowledgeCardDatabase, "FindCard", cardId);

            if (card == null)
            {
                Debug.LogWarning($"[CollectionAlbum] knowledge card not found for id='{cardId}' — showing lock hint.");
                ShowLockedCardHint(0);
                return;
            }

            int linkedStage = TryGetIntMember(card, "linkedStageId", 0);
            if (!IsStageCleared(linkedStage))
            {
                ShowLockedCardHint(linkedStage);
                return;
            }

            SetLockedOverlay(false);
            string cardName  = TryGetStringMember(card, "cardName") ?? cardId;
            string shortText = TryGetStringMember(card, "shortText")
                            ?? TryGetStringMember(card, "description")
                            ?? string.Empty;
            string category  = TryGetStringMember(card, "category") ?? string.Empty;
            object rarityObj = TryGetMember(card, "rarity");
            Sprite img       = TryGetMember(card, "image") as Sprite
                            ?? TryGetMember(card, "cardImage") as Sprite
                            ?? TryGetMember(card, "sprite") as Sprite;

            if (detailNameText        != null) detailNameText.text        = cardName;
            if (detailSubText         != null) detailSubText.text         = category;
            if (detailDescriptionText != null) detailDescriptionText.text = shortText;
            if (detailRarityText      != null) detailRarityText.text      = GetRarityText(rarityObj);
            if (detailExtraText       != null) detailExtraText.text       = linkedStage > 0 ? $"Stage {linkedStage}" : string.Empty;
            ApplyDetailImage(img);
        }

        private void ShowSparkleAlbumDetail(string pageId)
        {
            object page = null;
            try
            {
                page = TryInvokeOneArg(albumDatabase, "GetPageById", pageId)
                    ?? TryInvokeOneArg(albumDatabase, "GetPage", pageId)
                    ?? TryInvokeOneArg(albumDatabase, "FindPage", pageId);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CollectionAlbum] AlbumDatabase page lookup failed: {e.Message}");
            }

            if (page == null)
            {
                // Fall back to scanning the list for a matching pageId.
                foreach (var p in TryGetAlbumPages())
                {
                    string id = TryGetStringMember(p, "pageId") ?? TryGetStringMember(p, "id");
                    if (id == pageId) { page = p; break; }
                }
            }

            if (page == null)
            {
                Debug.LogWarning($"[CollectionAlbum] album page not found for id='{pageId}'.");
                ShowLockedAlbumHint(0);
                return;
            }

            int linkedStage = TryGetIntMember(page, "linkedStageId", 0);
            if (!IsAlbumPageUnlocked(linkedStage))
            {
                ShowLockedAlbumHint(linkedStage);
                return;
            }

            SetLockedOverlay(false);
            string title = TryGetStringMember(page, "pageTitle") ?? TryGetStringMember(page, "title") ?? pageId;
            string desc  = TryGetStringMember(page, "pageDescription") ?? TryGetStringMember(page, "description") ?? string.Empty;
            string world = TryGetStringMember(page, "worldName") ?? string.Empty;
            string linkedCardId = TryGetStringMember(page, "linkedCardId");
            Sprite img   = TryGetMember(page, "pageImage") as Sprite
                        ?? TryGetMember(page, "image") as Sprite
                        ?? TryGetMember(page, "sprite") as Sprite;

            string cardShort = ResolveLinkedCardShortText(linkedCardId);
            if (string.IsNullOrEmpty(cardShort) && !string.IsNullOrEmpty(linkedCardId))
                Debug.LogWarning($"[CollectionAlbum] album page '{pageId}' linkedCardId='{linkedCardId}' could not be resolved.");

            if (detailNameText        != null) detailNameText.text        = title;
            if (detailSubText         != null) detailSubText.text         = string.IsNullOrEmpty(world)
                                                                              ? (linkedStage > 0 ? $"Stage {linkedStage}" : string.Empty)
                                                                              : (linkedStage > 0 ? $"{world} · Stage {linkedStage}" : world);
            if (detailDescriptionText != null) detailDescriptionText.text = desc;
            if (detailRarityText      != null) detailRarityText.text      = string.Empty;
            if (detailExtraText       != null) detailExtraText.text       = cardShort ?? string.Empty;
            ApplyDetailImage(img);
        }

        private string ResolveLinkedCardShortText(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;
            object card = TryInvokeOneArg(knowledgeCardDatabase, "GetCardById", cardId)
                       ?? TryInvokeOneArg(knowledgeCardDatabase, "GetCard", cardId)
                       ?? TryInvokeOneArg(knowledgeCardDatabase, "GetCardData", cardId)
                       ?? TryInvokeOneArg(knowledgeCardDatabase, "FindCard", cardId);
            return TryGetStringMember(card, "shortText")
                ?? TryGetStringMember(card, "description");
        }

        private void ShowLockedCardHint(int linkedStageId)
        {
            SetLockedOverlay(true);
            if (detailNameText        != null) detailNameText.text        = "아직 잠긴 카드";
            if (detailSubText         != null) detailSubText.text         = linkedStageId > 0 ? $"Stage {linkedStageId}" : string.Empty;
            if (detailDescriptionText != null) detailDescriptionText.text = "스테이지를 클리어하면 열려요.";
            if (detailRarityText      != null) detailRarityText.text      = string.Empty;
            if (detailExtraText       != null) detailExtraText.text       = string.Empty;
            ApplyDetailImage(null);
        }

        private void ShowLockedAlbumHint(int linkedStageId)
        {
            SetLockedOverlay(true);
            if (detailNameText        != null) detailNameText.text        = "아직 잠긴 장면";
            if (detailSubText         != null) detailSubText.text         = linkedStageId > 0 ? $"Stage {linkedStageId}" : string.Empty;
            if (detailDescriptionText != null) detailDescriptionText.text = "스테이지를 클리어하면 그림책에 저장돼요.";
            if (detailRarityText      != null) detailRarityText.text      = string.Empty;
            if (detailExtraText       != null) detailExtraText.text       = string.Empty;
            ApplyDetailImage(null);
        }

        private void ApplyDetailImage(Sprite sprite)
        {
            if (detailImage == null) return;
            if (sprite != null)
            {
                detailImage.sprite = sprite;
                detailImage.enabled = true;
            }
            else
            {
                // Leave the placeholder visible — do not blank to magenta.
                detailImage.enabled = detailImage.sprite != null;
            }
        }

        private void ClearDetail()
        {
            SetLockedOverlay(false);
            if (detailNameText        != null) detailNameText.text        = string.Empty;
            if (detailSubText         != null) detailSubText.text         = string.Empty;
            if (detailDescriptionText != null) detailDescriptionText.text = string.Empty;
            if (detailRarityText      != null) detailRarityText.text      = string.Empty;
            if (detailExtraText       != null) detailExtraText.text       = string.Empty;
            ApplyDetailImage(null);
            selectedButton = null;
        }

        private void SetLockedOverlay(bool on)
        {
            if (lockedOverlay != null) lockedOverlay.SetActive(on);
        }

        private string GetRarityText(object rarity)
        {
            if (rarity == null) return string.Empty;
            string s = rarity.ToString();
            switch (s)
            {
                case "Common": return "기본 카드";
                case "Rare":   return "반짝 카드";
                case "Epic":   return "특별 카드";
                default:       return s;
            }
        }

        // ---- Count footer / tab highlight -------------------------------------------------

        private void UpdateCount(int total, int unlocked)
        {
            if (countText == null) return;
            string prefix;
            switch (currentTab)
            {
                case CollectionAlbumTab.Characters:    prefix = "캐릭터";    break;
                case CollectionAlbumTab.KnowledgeCards: prefix = "지식카드"; break;
                case CollectionAlbumTab.SparkleAlbum:  prefix = "반짝 앨범"; break;
                default:                               prefix = string.Empty; break;
            }
            countText.text = $"{prefix} {unlocked}/{total}";
        }

        private void ApplyTabHighlight()
        {
            if (characterTabHighlight     != null) characterTabHighlight.SetActive(currentTab == CollectionAlbumTab.Characters);
            if (knowledgeCardTabHighlight != null) knowledgeCardTabHighlight.SetActive(currentTab == CollectionAlbumTab.KnowledgeCards);
            if (sparkleAlbumTabHighlight  != null) sparkleAlbumTabHighlight.SetActive(currentTab == CollectionAlbumTab.SparkleAlbum);
        }

        // ---- Item button spawn / destroy --------------------------------------------------

        private bool CanSpawnButtons()
        {
            if (itemButtonPrefab == null)
            {
                Debug.LogWarning("[CollectionAlbum] itemButtonPrefab not assigned — list cannot be built.");
                return false;
            }
            if (itemButtonParent == null)
            {
                Debug.LogWarning("[CollectionAlbum] itemButtonParent not assigned — list cannot be built.");
                return false;
            }
            return true;
        }

        private void SpawnItemButton(string itemId, string label, string sub, Sprite icon, bool locked)
        {
            var instance = Instantiate(itemButtonPrefab, itemButtonParent);
            var btn = instance.GetComponent<CollectionItemButton>();
            if (btn == null)
            {
                Debug.LogWarning("[CollectionAlbum] itemButtonPrefab missing CollectionItemButton component.");
                Destroy(instance);
                return;
            }
            btn.Setup(itemId, label, sub, icon, locked, OnItemSelected);
            spawnedButtons.Add(btn);
        }

        private void DestroyExistingButtons()
        {
            for (int i = spawnedButtons.Count - 1; i >= 0; i--)
            {
                var b = spawnedButtons[i];
                if (b != null) Destroy(b.gameObject);
            }
            spawnedButtons.Clear();
            selectedButton = null;
        }

        // ---- Reflection helpers -----------------------------------------------------------

        private static object TryGetMember(object data, string memberName)
        {
            if (data == null || string.IsNullOrEmpty(memberName)) return null;
            try
            {
                var type = data.GetType();
                var field = type.GetField(memberName);
                if (field != null) return field.GetValue(data);
                var prop = type.GetProperty(memberName);
                if (prop != null) return prop.GetValue(data, null);
            }
            catch (Exception) { /* swallow */ }
            return null;
        }

        private static string TryGetStringMember(object data, string memberName)
        {
            return TryGetMember(data, memberName) as string;
        }

        private static int TryGetIntMember(object data, string memberName, int fallback)
        {
            object value = TryGetMember(data, memberName);
            if (value is int i) return i;
            return fallback;
        }

        private static object TryInvokeOneArg(MonoBehaviour target, string methodName, string arg)
        {
            if (target == null || string.IsNullOrEmpty(methodName) || string.IsNullOrEmpty(arg)) return null;
            try
            {
                var method = target.GetType().GetMethod(methodName, new[] { typeof(string) });
                if (method == null) return null;
                return method.Invoke(target, new object[] { arg });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CollectionAlbum] {methodName}('{arg}') failed: {e.Message}");
                return null;
            }
        }

        private static string InvokeStringMethodOneArg(MonoBehaviour target, string methodName, string arg)
        {
            object result = TryInvokeOneArg(target, methodName, arg);
            return result as string;
        }

        // ---- Tiny UI helpers --------------------------------------------------------------

        private static void BindButton(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button == null || handler == null) return;
            button.onClick.RemoveListener(handler);
            button.onClick.AddListener(handler);
        }

        private static void UnbindButton(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button == null || handler == null) return;
            button.onClick.RemoveListener(handler);
        }
    }
}
