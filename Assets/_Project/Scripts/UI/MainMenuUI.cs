// MainMenuUI.cs
// Task 92 — Main menu screen anchored on Nabyeol / Dabyeol / Capymong.
//
// Goals:
//   - First screen the child sees: bright, simple, character-led.
//   - Five primary actions (start, continue, album, learning coach, parent, settings).
//   - Pulls character art + alias + representative dialogue from existing
//     managers when they exist, falls back to placeholder strings when they don't.
//
// Defensive design:
//   - All inspector references are nullable. NullReferenceException is never
//     allowed to occur from a missing wiring.
//   - External managers (StageManager, CharacterPackManager, AliasManager,
//     RepresentativeDialogueManager, SparkleAlbumUI, LearningCoachUI,
//     ParentModeUI) are held as MonoBehaviour fields and called via
//     reflection / SendMessage. This way MainMenuUI compiles standalone and
//     auto-wires when those classes are added in their own tasks.
//
// Out of scope (per task 92):
//   - Final illustration art (placeholders are fine).
//   - Settings panel internals (logs only).
//   - Stage select screen.
//   - Real audio playback (TODO comments only).

using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Sound;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        // ---- Panel & header ---------------------------------------------------------------

        [Header("Panel")]
        [SerializeField] private GameObject mainMenuPanel;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private string titleLabel = "말랑 트윈즈";
        [SerializeField] private TextMeshProUGUI versionText;
        [SerializeField] private string versionLabel = "v0.1";

        // ---- Character group --------------------------------------------------------------

        [Header("Character images")]
        [SerializeField] private Image nabyeolImage;
        [SerializeField] private Image dabyeolImage;
        [SerializeField] private Image capymongImage;

        [Header("Dialogue bubble")]
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;

        [Header("Dialogue rotation")]
        [SerializeField] private bool rotateDialogue = true;
        [SerializeField, Min(1f)] private float dialogueRotateSeconds = 4f;

        // ---- Buttons ----------------------------------------------------------------------

        [Header("Primary buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button albumButton;
        [SerializeField] private Button learningCoachButton;

        [Header("Secondary buttons")]
        [SerializeField] private Button parentModeButton;
        [SerializeField] private Button settingsButton;

        [Header("Optional buttons")]
        [SerializeField] private Button dailyKnowledgeButton;
        [SerializeField] private Button regionRecoveryButton;
        [SerializeField] private Button ownStoryButton;

        // ---- External manager references (loose coupling) ---------------------------------

        [Header("External managers (optional)")]
        [Tooltip("StageManager component reference. Held as MonoBehaviour so this " +
                 "UI does not hard-depend on the StageManager type at compile time.")]
        [SerializeField] private MonoBehaviour stageManager;
        [SerializeField] private MonoBehaviour characterPackManager;
        [SerializeField] private MonoBehaviour characterAliasManager;
        [SerializeField] private MonoBehaviour characterRepresentativeDialogueManager;

        [Header("External UI (optional)")]
        [SerializeField] private MonoBehaviour sparkleAlbumUI;
        [Tooltip("Task 97 — unified collection/album UI. If assigned, the album " +
                 "button opens this instead of sparkleAlbumUI. sparkleAlbumUI is " +
                 "kept as a fallback so older scenes still work.")]
        [SerializeField] private MonoBehaviour collectionAlbumUI;
        [SerializeField] private MonoBehaviour learningCoachUI;
        [SerializeField] private MonoBehaviour parentModeUI;
        [SerializeField] private MonoBehaviour settingsUI;
        [SerializeField] private MonoBehaviour stageSelectUI;

        // ---- Character ids (keep aligned with CharacterPack) ------------------------------

        private const string NabyeolId  = "nabyeol";
        private const string DabyeolId  = "dabyeol";
        private const string CapymongId = "capymong";

        private static readonly string[] RotationOrder = { NabyeolId, DabyeolId, CapymongId };

        private Coroutine rotateRoutine;
        private int currentRotationIndex;

        // ---- Lifecycle --------------------------------------------------------------------

        private void Start() => InitMainMenu();

        private void OnEnable()
        {
            BindButton(startButton,            OnStartClicked);
            BindButton(continueButton,         OnContinueClicked);
            BindButton(albumButton,            OnAlbumClicked);
            BindButton(learningCoachButton,    OnLearningCoachClicked);
            BindButton(parentModeButton,       OnParentModeClicked);
            BindButton(settingsButton,         OnSettingsClicked);
            BindButton(dailyKnowledgeButton,   OnLearningCoachClicked);
            BindButton(regionRecoveryButton,   OnRegionRecoveryClicked);
            BindButton(ownStoryButton,         OnOwnStoryClicked);
        }

        private void OnDisable()
        {
            UnbindButton(startButton,          OnStartClicked);
            UnbindButton(continueButton,       OnContinueClicked);
            UnbindButton(albumButton,          OnAlbumClicked);
            UnbindButton(learningCoachButton,  OnLearningCoachClicked);
            UnbindButton(parentModeButton,     OnParentModeClicked);
            UnbindButton(settingsButton,       OnSettingsClicked);
            UnbindButton(dailyKnowledgeButton, OnLearningCoachClicked);
            UnbindButton(regionRecoveryButton, OnRegionRecoveryClicked);
            UnbindButton(ownStoryButton,       OnOwnStoryClicked);

            StopRotation();
        }

        // ---- Public API -------------------------------------------------------------------

        /// <summary>Show the main menu and refresh all dynamic content.</summary>
        public void OpenMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            InitMainMenu();
            Debug.Log("[MainMenu] opened.");
        }

        /// <summary>Hide the main menu.</summary>
        public void CloseMainMenu()
        {
            StopRotation();
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            Debug.Log("[MainMenu] closed.");
        }

        // ---- Init -------------------------------------------------------------------------

        private void InitMainMenu()
        {
            if (titleText   != null) titleText.text   = titleLabel;
            if (versionText != null) versionText.text = versionLabel;

            ApplyCharacterProfile(NabyeolId,  nabyeolImage);
            ApplyCharacterProfile(DabyeolId,  dabyeolImage);
            ApplyCharacterProfile(CapymongId, capymongImage);

            // Initial dialogue: Nabyeol speaks first.
            currentRotationIndex = 0;
            ApplyDialogueFor(NabyeolId);

            if (rotateDialogue) StartRotation();

            WarnIfMissing(startButton,         nameof(startButton));
            WarnIfMissing(continueButton,      nameof(continueButton));
            WarnIfMissing(albumButton,         nameof(albumButton));
            WarnIfMissing(learningCoachButton, nameof(learningCoachButton));
            WarnIfMissing(parentModeButton,    nameof(parentModeButton));
            WarnIfMissing(settingsButton,      nameof(settingsButton));

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBgmWithFade(BgmType.MainMenu);
            }
            // TODO: Add button click sound.
            // TODO: Add character greeting voice.
        }

        // ---- Character profile (image + alias) --------------------------------------------

        private void ApplyCharacterProfile(string characterId, Image targetImage)
        {
            // 1) Image
            var sprite = TryGetCharacterSprite(characterId);
            if (sprite != null && targetImage != null)
                targetImage.sprite = sprite;
            // if no sprite, keep placeholder Image as-is (do not blank it out).
        }

        private Sprite TryGetCharacterSprite(string characterId)
        {
            if (characterPackManager == null || string.IsNullOrEmpty(characterId)) return null;

            try
            {
                var type = characterPackManager.GetType();

                // Convention candidates: GetCharacter(string), GetCharacterData(string)
                foreach (var methodName in new[] { "GetCharacterData", "GetCharacter", "GetById" })
                {
                    var method = type.GetMethod(methodName, new[] { typeof(string) });
                    if (method == null) continue;
                    var data = method.Invoke(characterPackManager, new object[] { characterId });
                    if (data == null) continue;

                    var fieldOrProp = data.GetType().GetField("profileSprite");
                    if (fieldOrProp != null && fieldOrProp.FieldType == typeof(Sprite))
                        return fieldOrProp.GetValue(data) as Sprite;

                    var prop = data.GetType().GetProperty("profileSprite");
                    if (prop != null && prop.PropertyType == typeof(Sprite))
                        return prop.GetValue(data, null) as Sprite;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MainMenu] character sprite lookup failed for '{characterId}': {e.Message}");
            }
            return null;
        }

        private string TryGetDisplayName(string characterId)
        {
            // Prefer alias manager (task 76) if it can resolve one.
            string alias = InvokeStringMethod(characterAliasManager, "GetAlias", characterId);
            if (!string.IsNullOrEmpty(alias)) return alias;

            // Fallback: ask CharacterPack for the base display name.
            string baseName = InvokeStringMethod(characterPackManager, "GetDisplayName", characterId);
            if (!string.IsNullOrEmpty(baseName)) return baseName;

            // Final fallback: hard-coded Korean labels.
            switch (characterId)
            {
                case NabyeolId:  return "나별";
                case DabyeolId:  return "다별";
                case CapymongId: return "카피몽";
                default:         return characterId;
            }
        }

        private string TryGetRepresentativeDialogue(string characterId)
        {
            string text = InvokeStringMethod(characterRepresentativeDialogueManager,
                                             "GetRepresentativeDialogue", characterId);
            if (!string.IsNullOrEmpty(text)) return text;

            switch (characterId)
            {
                case NabyeolId:  return "함께 반짝반짝 시작해 볼까?";
                case DabyeolId:  return "오늘도 같이 놀자!";
                case CapymongId: return "음... 천천히 가도 괜찮아요.";
                default:         return string.Empty;
            }
        }

        // ---- Dialogue rotation ------------------------------------------------------------

        private void ApplyDialogueFor(string characterId)
        {
            if (speakerNameText != null) speakerNameText.text = TryGetDisplayName(characterId);
            if (dialogueText    != null) dialogueText.text    = TryGetRepresentativeDialogue(characterId);
        }

        private void StartRotation()
        {
            StopRotation();
            if (!isActiveAndEnabled) return;
            rotateRoutine = StartCoroutine(RotateDialogueLoop());
        }

        private void StopRotation()
        {
            if (rotateRoutine != null)
            {
                StopCoroutine(rotateRoutine);
                rotateRoutine = null;
            }
        }

        private IEnumerator RotateDialogueLoop()
        {
            var wait = new WaitForSeconds(dialogueRotateSeconds);
            while (true)
            {
                yield return wait;
                currentRotationIndex = (currentRotationIndex + 1) % RotationOrder.Length;
                ApplyDialogueFor(RotationOrder[currentRotationIndex]);
            }
        }

        // ---- Button handlers --------------------------------------------------------------

        private void OnStartClicked()
        {
            Debug.Log("[MainMenu] start clicked.");

            // Preferred: hand off to StageSelectUI so the player picks a stage.
            if (stageSelectUI != null)
            {
                stageSelectUI.SendMessage("OpenStageSelect", SendMessageOptions.DontRequireReceiver);
                CloseMainMenu();
                return;
            }

            // Fallback: jump straight to stage 1 if no stage-select screen is wired.
            if (!TryInvokeStageLoad(1))
            {
                Debug.LogWarning("[MainMenu] StageManager not available — start click is a no-op for now.");
                return;
            }
            CloseMainMenu();
        }

        private void OnContinueClicked()
        {
            Debug.Log("[MainMenu] continue clicked.");
            int stageId = TryGetHighestUnlockedStageId();
            if (stageId <= 0)
            {
                Debug.LogWarning("[MainMenu] no unlocked stage info available — falling back to stage 1.");
                stageId = 1;
            }
            if (!TryInvokeStageLoad(stageId))
            {
                Debug.LogWarning("[MainMenu] StageManager not available — continue click is a no-op for now.");
                return;
            }
            CloseMainMenu();
        }

        private void OnAlbumClicked()
        {
            Debug.Log("[MainMenu] album clicked.");

            // Prefer task 97's unified collection/album UI if assigned.
            if (collectionAlbumUI != null)
            {
                collectionAlbumUI.SendMessage("OpenCollectionAlbum", SendMessageOptions.DontRequireReceiver);
                return;
            }

            // Fallback: original task 68 sparkle-album-only UI.
            if (sparkleAlbumUI == null)
            {
                Debug.LogWarning("[MainMenu] neither collectionAlbumUI nor sparkleAlbumUI is assigned.");
                return;
            }
            sparkleAlbumUI.SendMessage("OpenAlbum", SendMessageOptions.DontRequireReceiver);
        }

        private void OnLearningCoachClicked()
        {
            Debug.Log("[MainMenu] learning coach clicked.");
            if (learningCoachUI == null) { Debug.LogWarning("[MainMenu] learningCoachUI not assigned."); return; }
            learningCoachUI.SendMessage("OpenLearningCoach", SendMessageOptions.DontRequireReceiver);
        }

        private void OnParentModeClicked()
        {
            Debug.Log("[MainMenu] parent mode clicked.");
            if (parentModeUI == null) { Debug.LogWarning("[MainMenu] parentModeUI not assigned."); return; }
            parentModeUI.SendMessage("OpenParentCheck", SendMessageOptions.DontRequireReceiver);
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenu] settings clicked.");
            if (settingsUI == null)
            {
                // TODO: connect to SettingsUI when it exists.
                Debug.Log("[MainMenu] settings UI not yet implemented.");
                return;
            }
            settingsUI.SendMessage("OpenSettings", SendMessageOptions.DontRequireReceiver);
        }

        private void OnRegionRecoveryClicked()
        {
            Debug.Log("[MainMenu] region recovery clicked.");
            // TODO: connect to RegionRecoveryUI when it exists.
        }

        private void OnOwnStoryClicked()
        {
            Debug.Log("[MainMenu] own story clicked.");
            // TODO: connect to OwnStoryUI when it exists.
        }

        // ---- StageManager loose-coupled calls ---------------------------------------------

        private bool TryInvokeStageLoad(int stageId)
        {
            if (stageManager == null || stageId <= 0) return false;
            try
            {
                var method = stageManager.GetType().GetMethod("LoadStageById", new[] { typeof(int) });
                if (method == null)
                {
                    Debug.LogWarning("[MainMenu] StageManager.LoadStageById(int) not found.");
                    return false;
                }
                method.Invoke(stageManager, new object[] { stageId });
                Debug.Log($"[MainMenu] StageManager.LoadStageById({stageId}) invoked.");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MainMenu] StageManager.LoadStageById failed: {e.Message}");
                return false;
            }
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
                Debug.LogWarning($"[MainMenu] highest-unlocked lookup failed: {e.Message}");
            }
            return 0;
        }

        // ---- Reflection helpers -----------------------------------------------------------

        private static string InvokeStringMethod(MonoBehaviour target, string methodName, string arg)
        {
            if (target == null || string.IsNullOrEmpty(methodName)) return null;
            try
            {
                var method = target.GetType().GetMethod(methodName, new[] { typeof(string) });
                if (method == null || method.ReturnType != typeof(string)) return null;
                return method.Invoke(target, new object[] { arg }) as string;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MainMenu] {methodName}('{arg}') failed: {e.Message}");
                return null;
            }
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

        private static void WarnIfMissing(UnityEngine.Object reference, string fieldName)
        {
            if (reference == null)
                Debug.LogWarning($"[MainMenu] '{fieldName}' is not assigned in the inspector.");
        }
    }
}
