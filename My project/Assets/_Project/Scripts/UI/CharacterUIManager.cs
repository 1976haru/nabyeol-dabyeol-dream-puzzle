using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Skill;
using NabyeolDabyeolDreamPuzzle.Dialogue;
using NabyeolDabyeolDreamPuzzle.Character;
using NabyeolDabyeolDreamPuzzle.Customization;
using NabyeolDabyeolDreamPuzzle.Animation;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    /// <summary>
    /// 인게임 화면에 표시되는 캐릭터(현재는 "나별") 프로필 UI를 관리한다.
    /// 프로필 이미지 / 이름 / 대사 / 스킬 버튼만 책임지며, 실제 스킬 효과는 후속 작업에서 SkillManager와 연결한다.
    /// 모든 UI 필드는 SerializeField + null 가드라 일부 누락된 상태에서도 NullReferenceException 없이 동작한다.
    /// TODO: Replace hardcoded Nabyeol data with CharacterData ScriptableObject.
    /// TODO: Connect Nabyeol skill button to SkillManager.
    /// TODO: Add skill cooldown UI.
    /// TODO: Add character dialogue by stage state (BoardManager 이벤트 또는 GoalManager 구독).
    /// </summary>
    public class CharacterUIManager : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject nabyeolPanel;

        [Header("Profile")]
        [SerializeField] private Image profileImage;
        [SerializeField] private Sprite defaultProfileSprite;
        [SerializeField] private TextMeshProUGUI nameText;

        [Header("Dialogue")]
        [SerializeField] private TextMeshProUGUI dialogueText;
        [TextArea(2, 4)]
        [SerializeField] private string defaultDialogue = "안녕! 함께 별빛을 모아 보자.";

        [Header("Skill")]
        [SerializeField] private Button skillButton;
        [SerializeField] private TextMeshProUGUI skillButtonText;

        [Header("Character Data (Hardcoded)")]
        [SerializeField] private string characterName = "나별";
        [SerializeField] private string skillName = "별자리 보기";

        [Header("Stage Dialogue Presets")]
        [TextArea(2, 4)]
        [SerializeField] private string stageStartDialogue = "이번 스테이지도 차근차근 해보자!";
        [TextArea(2, 4)]
        [SerializeField] private string stageClearDialogue = "우와, 정말 잘했어!";
        [TextArea(2, 4)]
        [SerializeField] private string stageFailDialogue = "괜찮아, 다시 하면 더 잘할 수 있어!";
        [TextArea(2, 4)]
        [SerializeField] private string skillClickDialogue = "별자리가 알려줬어! 반짝이는 블록을 봐!";
        [TextArea(2, 4)]
        [SerializeField] private string skillNoHintDialogue = "지금은 별자리가 잘 보이지 않아.";

        // ───────── 다별 ─────────
        [Header("Dabyeol Panel")]
        [SerializeField] private GameObject dabyeolPanel;

        [Header("Dabyeol Profile")]
        [SerializeField] private Image dabyeolProfileImage;
        [SerializeField] private Sprite dabyeolProfileSprite;
        [SerializeField] private TextMeshProUGUI dabyeolNameText;

        [Header("Dabyeol Dialogue")]
        [SerializeField] private TextMeshProUGUI dabyeolDialogueText;
        [TextArea(2, 4)]
        [SerializeField] private string dabyeolDefaultDialogue = "차근차근 해보자. 다별이 도와줄게.";

        [Header("Dabyeol Skill")]
        [SerializeField] private Button dabyeolSkillButton;
        [SerializeField] private TextMeshProUGUI dabyeolSkillButtonText;

        [Header("Dabyeol Character Data (Hardcoded)")]
        [SerializeField] private string dabyeolCharacterName = "다별";
        [SerializeField] private string dabyeolSkillName = "꿈결 움직이기";

        [Header("Dabyeol Stage Dialogue Presets")]
        [TextArea(2, 4)]
        [SerializeField] private string dabyeolStageStartDialogue = "순서대로 보면 답이 보여.";
        [TextArea(2, 4)]
        [SerializeField] private string dabyeolStageClearDialogue = "좋았어. 아주 깔끔한 해결이야.";
        [TextArea(2, 4)]
        [SerializeField] private string dabyeolStageFailDialogue = "괜찮아. 다시 차근차근 해보자.";
        [TextArea(2, 4)]
        [SerializeField] private string dabyeolSkillClickDialogue = "움직일 블록을 고르고, 보내고 싶은 방향을 선택해줘.";
        [TextArea(2, 4)]
        [SerializeField] private string dabyeolSkillUnavailableDialogue = "지금은 꿈결을 움직이기 어려워.";

        // ───────── 합동 스킬 ─────────
        [Header("Twin Skill UI")]
        [SerializeField] private Button twinSkillButton;
        [SerializeField] private TextMeshProUGUI twinSkillButtonText;

        [Header("Twin Skill Data (Hardcoded)")]
        [SerializeField] private string twinSkillName = "트윈스타 팡";
        [TextArea(2, 4)]
        [SerializeField] private string twinSkillClickDialogue = "둘이 함께! 같은 색 별들을 한꺼번에 반짝!";
        [TextArea(2, 4)]
        [SerializeField] private string twinSkillUnavailableDialogue = "지금은 함께 별을 만들 수 없어.";

        // ───────── 카피몽 ─────────
        [Header("Capymong Panel")]
        [SerializeField] private GameObject capymongPanel;

        [Header("Capymong Profile")]
        [SerializeField] private Image capymongProfileImage;
        [SerializeField] private Sprite capymongProfileSprite;
        [SerializeField] private TextMeshProUGUI capymongNameText;

        [Header("Capymong Dialogue")]
        [SerializeField] private TextMeshProUGUI capymongDialogueText;
        [TextArea(2, 4)]
        [SerializeField] private string capymongDefaultDialogue = "천천히 가도 괜찮아. 한 번 더 해보자.";

        [Header("Capymong Skill")]
        [SerializeField] private Button capymongSkillButton;
        [SerializeField] private TextMeshProUGUI capymongSkillButtonText;

        [Header("Capymong Data (Hardcoded)")]
        [SerializeField] private string capymongCharacterName = "카피몽";
        [SerializeField] private string capymongSkillName = "느긋한 숨결";

        [Header("Capymong Stage Dialogue Presets")]
        [TextArea(2, 4)]
        [SerializeField] private string capymongSkillClickDialogue = "후우… 이동 한 번 더 줄게.";
        [TextArea(2, 4)]
        [SerializeField] private string capymongSkillUnavailableDialogue = "지금은 숨을 고를 수 없어.";

        // ───────── 포포링 ─────────
        [Header("Poporing Panel")]
        [SerializeField] private GameObject poporingPanel;

        [Header("Poporing Profile")]
        [SerializeField] private Image poporingProfileImage;
        [SerializeField] private Sprite poporingProfileSprite;
        [SerializeField] private TextMeshProUGUI poporingNameText;

        [Header("Poporing Dialogue")]
        [SerializeField] private TextMeshProUGUI poporingDialogueText;
        [TextArea(2, 4)]
        [SerializeField] private string poporingDefaultDialogue = "방울방울 떠오르는 길을 찾아볼게!";

        [Header("Poporing Skill")]
        [SerializeField] private Button poporingSkillButton;
        [SerializeField] private TextMeshProUGUI poporingSkillButtonText;

        [Header("Poporing Data (Hardcoded)")]
        [SerializeField] private string poporingCharacterName = "포포링";
        [SerializeField] private string poporingSkillName = "방울 힌트";

        [Header("Poporing Stage Dialogue Presets")]
        [TextArea(2, 4)]
        [SerializeField] private string poporingSkillClickDialogue = "방울이 가르쳐 줄게! 통통 튀는 곳을 봐!";
        [TextArea(2, 4)]
        [SerializeField] private string poporingSkillUnavailableDialogue = "지금은 방울이 잠잠해.";

        // ───────── 모찌룬 ─────────
        [Header("Mochirun Panel")]
        [SerializeField] private GameObject mochirunPanel;

        [Header("Mochirun Profile")]
        [SerializeField] private Image mochirunProfileImage;
        [SerializeField] private Sprite mochirunProfileSprite;
        [SerializeField] private TextMeshProUGUI mochirunNameText;

        [Header("Mochirun Dialogue")]
        [SerializeField] private TextMeshProUGUI mochirunDialogueText;
        [TextArea(2, 4)]
        [SerializeField] private string mochirunDefaultDialogue = "숫자를 차례대로 정리해 볼게!";

        [Header("Mochirun Skill")]
        [SerializeField] private Button mochirunSkillButton;
        [SerializeField] private TextMeshProUGUI mochirunSkillButtonText;

        [Header("Mochirun Data (Hardcoded)")]
        [SerializeField] private string mochirunCharacterName = "모찌룬";
        [SerializeField] private string mochirunSkillName = "숫자 블록 정렬";

        [Header("Mochirun Stage Dialogue Presets")]
        [TextArea(2, 4)]
        [SerializeField] private string mochirunSkillClickDialogue = "숫자가 줄을 섰어! 매치가 보이지?";
        [TextArea(2, 4)]
        [SerializeField] private string mochirunSkillUnavailableDialogue = "지금은 숫자를 정리하기 어려워.";

        // ───────── 스킬 튜토리얼 ─────────
        [Header("Skill Tutorial UI")]
        [SerializeField] private GameObject skillTutorialPanel;
        [SerializeField] private TextMeshProUGUI skillTutorialTitleText;
        [SerializeField] private TextMeshProUGUI skillTutorialDescriptionText;
        [SerializeField] private Button skillTutorialCloseButton;
        [SerializeField, Min(0.1f)] private float skillTutorialAutoHideSeconds = 2f;

        private Coroutine skillTutorialHideCoroutine;
        // TODO: Show each skill tutorial only once using PlayerPrefs.
        // TODO: Add help button to reopen skill explanations.

        private void Awake()
        {
            InitNabyeolUI();
            InitDabyeolUI();
            InitTwinSkillUI();
            InitCapymongUI();
            InitPoporingUI();
            InitMochirunUI();
            InitSkillTutorialUI();
        }

        private void Start()
        {
            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.OnSkillUseCountChanged += HandleSkillUseCountChanged;
                RefreshAllSkillButtonLabels();
            }
            else
            {
                Debug.LogWarning("CharacterUIManager: SkillManager.Instance not found at Start. Skill use count UI will not auto-refresh.");
            }

            RefreshDefaultDialoguesFromDatabase();

            // 별칭 변경 이벤트 구독 → 즉시 모든 캐릭터 이름 라벨 재적용
            if (CharacterAliasManager.Instance != null)
            {
                CharacterAliasManager.Instance.OnAliasChanged += HandleAliasChanged;
                HandleAliasChanged();
            }

            // 대표 대사 변경 이벤트 구독 → 즉시 모든 캐릭터 평상시 대사 재적용
            if (CharacterRepresentativeDialogueManager.Instance != null)
            {
                CharacterRepresentativeDialogueManager.Instance.OnRepresentativeDialogueChanged += HandleRepresentativeDialogueChanged;
            }

            // 커스터마이징 일괄 복구 이벤트 구독 → 이름/대사 모두 한 번에 재적용
            if (CustomizationResetManager.Instance != null)
            {
                CustomizationResetManager.Instance.OnCustomizationReset += HandleCustomizationReset;
            }
        }

        /// <summary>OnCustomizationReset 콜백. 별칭과 대표 대사를 모두 다시 적용.</summary>
        private void HandleCustomizationReset()
        {
            HandleAliasChanged();
            RefreshDefaultDialoguesFromDatabase();
            Debug.Log("CharacterUIManager: Customization reset detected. All character UI refreshed.");
        }

        /// <summary>OnRepresentativeDialogueChanged 콜백. 모든 캐릭터 평상시 대사 텍스트를 재적용한다.</summary>
        private void HandleRepresentativeDialogueChanged(string characterId, string dialogueKey)
        {
            RefreshDefaultDialoguesFromDatabase();
            Debug.Log($"CharacterUIManager: Representative dialogue refreshed (changed for '{characterId}').");
        }

        /// <summary>CharacterAliasManager로 별칭을 조회해 우선 표시, 없으면 fallback 반환.</summary>
        private string ResolveDisplayName(string characterId, string fallback)
        {
            if (CharacterAliasManager.Instance == null) return fallback ?? string.Empty;
            return CharacterAliasManager.Instance.GetDisplayName(characterId, fallback);
        }

        /// <summary>OnAliasChanged 콜백. 5개 캐릭터 이름 라벨을 한 번에 재적용.</summary>
        private void HandleAliasChanged()
        {
            ApplyName();
            ApplyDabyeolName();
            ApplyCapymongName();
            ApplyPoporingName();
            ApplyMochirunName();
            Debug.Log("CharacterUIManager: Character name labels refreshed from alias change.");
        }

        /// <summary>
        /// 5개 캐릭터의 평상시 대사를 갱신.
        /// 우선순위: CharacterRepresentativeDialogueManager 선택값 → CharacterPackData.RepresentativeDialogueKey
        ///         → "character.X.default" → SerializeField 원본 fallback.
        /// 스킬 성공/실패 대사와 스토리 대사는 본 메서드 영향을 받지 않는다.
        /// </summary>
        private void RefreshDefaultDialoguesFromDatabase()
        {
            SetDialogue(ResolveCharacterDialogue("nabyeol",   "character.nabyeol.default",  defaultDialogue));
            SetDabyeolDialogue(ResolveCharacterDialogue("dabyeol",   "character.dabyeol.default",  dabyeolDefaultDialogue));
            SetCapymongDialogue(ResolveCharacterDialogue("capymong", "character.capymong.default", capymongDefaultDialogue));
            SetPoporingDialogue(ResolveCharacterDialogue("poporing", "character.poporing.default", poporingDefaultDialogue));
            SetMochirunDialogue(ResolveCharacterDialogue("mochirun", "character.mochirun.default", mochirunDefaultDialogue));
        }

        /// <summary>
        /// 캐릭터의 평상시 대사 텍스트 결정. 대표 대사 매니저가 있으면 우선 사용.
        /// 매니저/팩이 없거나 텍스트가 비면 fallbackKey → textFallback 순서로 폴백.
        /// </summary>
        private string ResolveCharacterDialogue(string characterId, string fallbackKey, string textFallback)
        {
            if (CharacterRepresentativeDialogueManager.Instance != null && CharacterPackManager.Instance != null)
            {
                CharacterPackData pack = CharacterPackManager.Get(characterId);
                if (pack != null)
                {
                    string text = CharacterRepresentativeDialogueManager.Instance.GetRepresentativeDialogueText(pack);
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
            }
            return DialogueManager.Get(fallbackKey, textFallback);
        }

        private void RefreshAllSkillButtonLabels()
        {
            if (SkillManager.Instance == null) return;
            foreach (SkillType t in System.Enum.GetValues(typeof(SkillType)))
            {
                HandleSkillUseCountChanged(t, SkillManager.Instance.GetRemainingSkillUseCount(t));
            }
        }

        private void HandleSkillUseCountChanged(SkillType skill, int remaining)
        {
            switch (skill)
            {
                case SkillType.NabyeolHint:
                    UpdateSkillButtonLabel(skillButtonText, skillButton, skillName, remaining);
                    break;
                case SkillType.DabyeolMove:
                    UpdateSkillButtonLabel(dabyeolSkillButtonText, dabyeolSkillButton, dabyeolSkillName, remaining);
                    break;
                case SkillType.TwinStarPop:
                    UpdateSkillButtonLabel(twinSkillButtonText, twinSkillButton, twinSkillName, remaining);
                    break;
                case SkillType.CapymongBreath:
                    UpdateSkillButtonLabel(capymongSkillButtonText, capymongSkillButton, capymongSkillName, remaining);
                    break;
                case SkillType.PoporingBubbleHint:
                    UpdateSkillButtonLabel(poporingSkillButtonText, poporingSkillButton, poporingSkillName, remaining);
                    break;
                case SkillType.MochirunNumberSort:
                    UpdateSkillButtonLabel(mochirunSkillButtonText, mochirunSkillButton, mochirunSkillName, remaining);
                    break;
            }
        }

        private void UpdateSkillButtonLabel(TextMeshProUGUI buttonText, Button button, string baseName, int remaining)
        {
            if (buttonText != null)
            {
                buttonText.text = $"{baseName} ({remaining})";
            }
            if (button != null)
            {
                button.interactable = remaining > 0;
            }
        }

        private void OnDestroy()
        {
            if (skillButton != null)
            {
                skillButton.onClick.RemoveListener(OnClickNabyeolSkill);
            }
            if (dabyeolSkillButton != null)
            {
                dabyeolSkillButton.onClick.RemoveListener(OnClickDabyeolSkill);
            }
            if (twinSkillButton != null)
            {
                twinSkillButton.onClick.RemoveListener(OnClickTwinSkill);
            }
            if (capymongSkillButton != null)
            {
                capymongSkillButton.onClick.RemoveListener(OnClickCapymongSkill);
            }
            if (poporingSkillButton != null)
            {
                poporingSkillButton.onClick.RemoveListener(OnClickPoporingSkill);
            }
            if (mochirunSkillButton != null)
            {
                mochirunSkillButton.onClick.RemoveListener(OnClickMochirunSkill);
            }
            if (skillTutorialCloseButton != null)
            {
                skillTutorialCloseButton.onClick.RemoveListener(HideSkillTutorial);
            }

            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.OnSkillUseCountChanged -= HandleSkillUseCountChanged;
            }

            if (CharacterAliasManager.Instance != null)
            {
                CharacterAliasManager.Instance.OnAliasChanged -= HandleAliasChanged;
            }

            if (CharacterRepresentativeDialogueManager.Instance != null)
            {
                CharacterRepresentativeDialogueManager.Instance.OnRepresentativeDialogueChanged -= HandleRepresentativeDialogueChanged;
            }

            if (CustomizationResetManager.Instance != null)
            {
                CustomizationResetManager.Instance.OnCustomizationReset -= HandleCustomizationReset;
            }
        }

        /// <summary>나별 UI를 기본 상태로 초기화한다. 패널이 비활성화 상태였다면 활성화한다.</summary>
        public void InitNabyeolUI()
        {
            ShowNabyeol();
            ApplyProfile();
            ApplyName();
            SetDialogue(defaultDialogue);
            ApplySkillButton();

            Debug.Log("CharacterUIManager: Nabyeol UI initialized.");
        }

        /// <summary>나별 패널을 화면에 표시한다.</summary>
        public void ShowNabyeol()
        {
            if (nabyeolPanel != null)
            {
                nabyeolPanel.SetActive(true);
            }
        }

        /// <summary>나별 패널을 숨긴다. 후속 작업에서 컷씬·대화창 도중 숨길 때 호출.</summary>
        public void HideNabyeol()
        {
            if (nabyeolPanel != null)
            {
                nabyeolPanel.SetActive(false);
            }
        }

        private void ApplyProfile()
        {
            if (profileImage == null)
            {
                Debug.LogWarning("CharacterUIManager: profileImage is not assigned.");
                return;
            }
            if (defaultProfileSprite != null)
            {
                profileImage.sprite = defaultProfileSprite;
            }
        }

        private void ApplyName()
        {
            if (nameText == null)
            {
                Debug.LogWarning("CharacterUIManager: nameText is not assigned.");
                return;
            }
            nameText.text = ResolveDisplayName("nabyeol", characterName);
        }

        private void ApplySkillButton()
        {
            if (skillButtonText != null)
            {
                skillButtonText.text = skillName;
            }

            if (skillButton == null)
            {
                Debug.LogWarning("CharacterUIManager: skillButton is not assigned.");
                return;
            }

            // 중복 등록 방지: Remove 후 Add.
            skillButton.onClick.RemoveListener(OnClickNabyeolSkill);
            skillButton.onClick.AddListener(OnClickNabyeolSkill);
        }

        /// <summary>대사 텍스트를 외부에서 갱신한다. 빈 문자열을 넘기면 기본 대사로 되돌린다.</summary>
        public void SetDialogue(string dialogue)
        {
            if (dialogueText == null)
            {
                Debug.LogWarning("CharacterUIManager: dialogueText is not assigned.");
                return;
            }
            dialogueText.text = string.IsNullOrWhiteSpace(dialogue) ? defaultDialogue : dialogue;
        }

        /// <summary>스킬 버튼 활성/비활성 토글. 후속 작업의 쿨타임/조건 처리용.</summary>
        public void SetSkillButtonInteractable(bool interactable)
        {
            if (skillButton != null)
            {
                skillButton.interactable = interactable;
            }
        }

        /// <summary>스테이지 시작 대사. 외부(BoardManager·GoalManager 등)에서 호출하면 된다.</summary>
        public void ShowStageStartDialogue()
        {
            SetDialogue(stageStartDialogue);
        }

        /// <summary>스테이지 클리어 대사.</summary>
        public void ShowStageClearDialogue()
        {
            SetDialogue(stageClearDialogue);
        }

        /// <summary>스테이지 실패 대사.</summary>
        public void ShowStageFailDialogue()
        {
            SetDialogue(stageFailDialogue);
        }

        private void OnClickNabyeolSkill()
        {
            Debug.Log($"CharacterUIManager: Nabyeol skill button clicked: {skillName}");
            ShowSkillTutorialByType(SkillType.NabyeolHint);

            if (SkillManager.Instance == null)
            {
                Debug.LogWarning("CharacterUIManager: SkillManager.Instance not found.");
                SetDialogue(skillNoHintDialogue);
                return;
            }

            bool used = SkillManager.Instance.UseNabyeolHintSkill();
            string text = used
                ? DialogueManager.Get("skill.nabyeol.hint.success", skillClickDialogue)
                : DialogueManager.Get("skill.nabyeol.hint.fail",    skillNoHintDialogue);
            SetDialogue(text);
            if (used) PlayNabyeolReaction();
        }

        // ───────── 다별 메서드 ─────────

        /// <summary>다별 UI를 기본 상태로 초기화한다. 패널이 비활성화 상태였다면 활성화한다.</summary>
        public void InitDabyeolUI()
        {
            ShowDabyeol();
            ApplyDabyeolProfile();
            ApplyDabyeolName();
            SetDabyeolDialogue(dabyeolDefaultDialogue);
            ApplyDabyeolSkillButton();

            Debug.Log("CharacterUIManager: Dabyeol UI initialized.");
        }

        /// <summary>다별 패널을 화면에 표시한다.</summary>
        public void ShowDabyeol()
        {
            if (dabyeolPanel != null)
            {
                dabyeolPanel.SetActive(true);
            }
        }

        /// <summary>다별 패널을 숨긴다.</summary>
        public void HideDabyeol()
        {
            if (dabyeolPanel != null)
            {
                dabyeolPanel.SetActive(false);
            }
        }

        private void ApplyDabyeolProfile()
        {
            if (dabyeolProfileImage == null)
            {
                Debug.LogWarning("CharacterUIManager: dabyeolProfileImage is not assigned.");
                return;
            }
            if (dabyeolProfileSprite != null)
            {
                dabyeolProfileImage.sprite = dabyeolProfileSprite;
            }
        }

        private void ApplyDabyeolName()
        {
            if (dabyeolNameText == null)
            {
                Debug.LogWarning("CharacterUIManager: dabyeolNameText is not assigned.");
                return;
            }
            dabyeolNameText.text = ResolveDisplayName("dabyeol", dabyeolCharacterName);
        }

        private void ApplyDabyeolSkillButton()
        {
            if (dabyeolSkillButtonText != null)
            {
                dabyeolSkillButtonText.text = dabyeolSkillName;
            }

            if (dabyeolSkillButton == null)
            {
                Debug.LogWarning("CharacterUIManager: dabyeolSkillButton is not assigned.");
                return;
            }

            // 중복 등록 방지: Remove 후 Add. 나별 버튼 리스너와 분리된 별개 핸들러를 사용한다.
            dabyeolSkillButton.onClick.RemoveListener(OnClickDabyeolSkill);
            dabyeolSkillButton.onClick.AddListener(OnClickDabyeolSkill);
        }

        /// <summary>다별 대사 텍스트를 외부에서 갱신한다. 빈 문자열이면 기본 대사로 폴백.</summary>
        public void SetDabyeolDialogue(string dialogue)
        {
            if (dabyeolDialogueText == null)
            {
                Debug.LogWarning("CharacterUIManager: dabyeolDialogueText is not assigned.");
                return;
            }
            dabyeolDialogueText.text = string.IsNullOrWhiteSpace(dialogue) ? dabyeolDefaultDialogue : dialogue;
        }

        /// <summary>다별 스킬 버튼 활성/비활성 토글.</summary>
        public void SetDabyeolSkillButtonInteractable(bool interactable)
        {
            if (dabyeolSkillButton != null)
            {
                dabyeolSkillButton.interactable = interactable;
            }
        }

        public void ShowDabyeolStageStartDialogue() { SetDabyeolDialogue(dabyeolStageStartDialogue); }
        public void ShowDabyeolStageClearDialogue() { SetDabyeolDialogue(dabyeolStageClearDialogue); }
        public void ShowDabyeolStageFailDialogue() { SetDabyeolDialogue(dabyeolStageFailDialogue); }

        private void OnClickDabyeolSkill()
        {
            Debug.Log($"CharacterUIManager: Dabyeol skill button clicked: {dabyeolSkillName}");
            ShowSkillTutorialByType(SkillType.DabyeolMove);

            if (SkillManager.Instance == null)
            {
                Debug.LogWarning("CharacterUIManager: SkillManager.Instance not found.");
                SetDabyeolDialogue(dabyeolSkillUnavailableDialogue);
                return;
            }

            bool used = SkillManager.Instance.UseDabyeolMoveSkill();
            string text = used
                ? DialogueManager.Get("skill.dabyeol.move.success", dabyeolSkillClickDialogue)
                : DialogueManager.Get("skill.dabyeol.move.fail",    dabyeolSkillUnavailableDialogue);
            SetDabyeolDialogue(text);
            if (used) PlayDabyeolReaction();
            // TODO: Add skill cooldown UI.
            // TODO: Add limited skill count per stage.
        }

        // ───────── 표시 정책 ─────────

        /// <summary>나별만 표시하고 다별은 숨긴다. 후속 단계 캐릭터 선택 흐름에서 사용.</summary>
        public void ShowOnlyNabyeol()
        {
            ShowNabyeol();
            HideDabyeol();
        }

        /// <summary>다별만 표시하고 나별은 숨긴다.</summary>
        public void ShowOnlyDabyeol()
        {
            HideNabyeol();
            ShowDabyeol();
        }

        /// <summary>두 캐릭터 패널을 동시에 표시한다(기본 정책).</summary>
        public void ShowBothCharacters()
        {
            ShowNabyeol();
            ShowDabyeol();
        }

        // ───────── 합동 스킬 메서드 ─────────

        /// <summary>합동 스킬 UI를 기본 상태로 초기화한다. 버튼 텍스트와 클릭 리스너를 적용한다.</summary>
        public void InitTwinSkillUI()
        {
            ApplyTwinSkillButton();
            Debug.Log("CharacterUIManager: Twin skill UI initialized.");
        }

        private void ApplyTwinSkillButton()
        {
            if (twinSkillButtonText != null)
            {
                twinSkillButtonText.text = twinSkillName;
            }

            if (twinSkillButton == null)
            {
                Debug.LogWarning("CharacterUIManager: twinSkillButton is not assigned.");
                return;
            }

            twinSkillButton.onClick.RemoveListener(OnClickTwinSkill);
            twinSkillButton.onClick.AddListener(OnClickTwinSkill);
        }

        /// <summary>합동 스킬 버튼 활성/비활성 토글. 향후 게이지/쿨타임 처리용.</summary>
        public void SetTwinSkillButtonInteractable(bool interactable)
        {
            if (twinSkillButton != null)
            {
                twinSkillButton.interactable = interactable;
            }
        }

        private void OnClickTwinSkill()
        {
            Debug.Log($"CharacterUIManager: Twin skill button clicked: {twinSkillName}");
            ShowSkillTutorialByType(SkillType.TwinStarPop);

            if (SkillManager.Instance == null)
            {
                Debug.LogWarning("CharacterUIManager: SkillManager.Instance not found.");
                SetDialogue(twinSkillUnavailableDialogue);
                SetDabyeolDialogue(twinSkillUnavailableDialogue);
                return;
            }

            bool used = SkillManager.Instance.UseTwinStarPopSkill();
            // 합동 스킬은 두 캐릭터의 대사를 각각 다른 키로 분리해서 표현 가능.
            string nabyeolText = used
                ? DialogueManager.Get("skill.twinstar.pop.success.nabyeol", twinSkillClickDialogue)
                : DialogueManager.Get("skill.twinstar.pop.fail.nabyeol",    twinSkillUnavailableDialogue);
            string dabyeolText = used
                ? DialogueManager.Get("skill.twinstar.pop.success.dabyeol", twinSkillClickDialogue)
                : DialogueManager.Get("skill.twinstar.pop.fail.dabyeol",    twinSkillUnavailableDialogue);
            SetDialogue(nabyeolText);
            SetDabyeolDialogue(dabyeolText);
            if (used) { PlayNabyeolReaction(); PlayDabyeolReaction(); }
            // TODO: Add twin skill gauge UI feedback.
            // TODO: Connect to twin skill cooldown indicator.
        }

        // ───────── 카피몽 메서드 ─────────

        /// <summary>카피몽 UI를 기본 상태로 초기화한다.</summary>
        public void InitCapymongUI()
        {
            ShowCapymong();
            ApplyCapymongProfile();
            ApplyCapymongName();
            SetCapymongDialogue(capymongDefaultDialogue);
            ApplyCapymongSkillButton();
            Debug.Log("CharacterUIManager: Capymong UI initialized.");
        }

        /// <summary>카피몽 패널을 표시한다.</summary>
        public void ShowCapymong()
        {
            if (capymongPanel != null)
            {
                capymongPanel.SetActive(true);
            }
        }

        /// <summary>카피몽 패널을 숨긴다.</summary>
        public void HideCapymong()
        {
            if (capymongPanel != null)
            {
                capymongPanel.SetActive(false);
            }
        }

        private void ApplyCapymongProfile()
        {
            if (capymongProfileImage == null)
            {
                Debug.LogWarning("CharacterUIManager: capymongProfileImage is not assigned.");
                return;
            }
            if (capymongProfileSprite != null)
            {
                capymongProfileImage.sprite = capymongProfileSprite;
            }
        }

        private void ApplyCapymongName()
        {
            if (capymongNameText == null)
            {
                Debug.LogWarning("CharacterUIManager: capymongNameText is not assigned.");
                return;
            }
            capymongNameText.text = ResolveDisplayName("capymong", capymongCharacterName);
        }

        private void ApplyCapymongSkillButton()
        {
            if (capymongSkillButtonText != null)
            {
                capymongSkillButtonText.text = capymongSkillName;
            }

            if (capymongSkillButton == null)
            {
                Debug.LogWarning("CharacterUIManager: capymongSkillButton is not assigned.");
                return;
            }

            capymongSkillButton.onClick.RemoveListener(OnClickCapymongSkill);
            capymongSkillButton.onClick.AddListener(OnClickCapymongSkill);
        }

        /// <summary>카피몽 대사 텍스트를 외부에서 갱신한다. 빈 문자열이면 기본 대사로 폴백.</summary>
        public void SetCapymongDialogue(string dialogue)
        {
            if (capymongDialogueText == null)
            {
                Debug.LogWarning("CharacterUIManager: capymongDialogueText is not assigned.");
                return;
            }
            capymongDialogueText.text = string.IsNullOrWhiteSpace(dialogue) ? capymongDefaultDialogue : dialogue;
        }

        /// <summary>카피몽 스킬 버튼 활성/비활성 토글. 추후 이벤트로 자동 갱신할 수 있음.</summary>
        public void SetCapymongSkillButtonInteractable(bool interactable)
        {
            if (capymongSkillButton != null)
            {
                capymongSkillButton.interactable = interactable;
            }
        }

        private void OnClickCapymongSkill()
        {
            Debug.Log($"CharacterUIManager: Capymong skill button clicked: {capymongSkillName}");
            ShowSkillTutorialByType(SkillType.CapymongBreath);

            if (SkillManager.Instance == null)
            {
                Debug.LogWarning("CharacterUIManager: SkillManager.Instance not found.");
                SetCapymongDialogue(capymongSkillUnavailableDialogue);
                return;
            }

            bool used = SkillManager.Instance.UseCapymongBreathSkill();
            string text = used
                ? DialogueManager.Get("skill.capymong.breath.success", capymongSkillClickDialogue)
                : DialogueManager.Get("skill.capymong.breath.fail",    capymongSkillUnavailableDialogue);
            SetCapymongDialogue(text);
            if (used) PlayCapymongReaction();
            // TODO: Disable button after successful use (BoardManager → 이벤트 → SetCapymongSkillButtonInteractable(false)).
        }

        // ───────── 포포링 메서드 ─────────

        /// <summary>포포링 UI를 기본 상태로 초기화한다.</summary>
        public void InitPoporingUI()
        {
            ShowPoporing();
            ApplyPoporingProfile();
            ApplyPoporingName();
            SetPoporingDialogue(poporingDefaultDialogue);
            ApplyPoporingSkillButton();
            Debug.Log("CharacterUIManager: Poporing UI initialized.");
        }

        /// <summary>포포링 패널을 표시한다.</summary>
        public void ShowPoporing()
        {
            if (poporingPanel != null)
            {
                poporingPanel.SetActive(true);
            }
        }

        /// <summary>포포링 패널을 숨긴다.</summary>
        public void HidePoporing()
        {
            if (poporingPanel != null)
            {
                poporingPanel.SetActive(false);
            }
        }

        private void ApplyPoporingProfile()
        {
            if (poporingProfileImage == null)
            {
                Debug.LogWarning("CharacterUIManager: poporingProfileImage is not assigned.");
                return;
            }
            if (poporingProfileSprite != null)
            {
                poporingProfileImage.sprite = poporingProfileSprite;
            }
        }

        private void ApplyPoporingName()
        {
            if (poporingNameText == null)
            {
                Debug.LogWarning("CharacterUIManager: poporingNameText is not assigned.");
                return;
            }
            poporingNameText.text = ResolveDisplayName("poporing", poporingCharacterName);
        }

        private void ApplyPoporingSkillButton()
        {
            if (poporingSkillButtonText != null)
            {
                poporingSkillButtonText.text = poporingSkillName;
            }

            if (poporingSkillButton == null)
            {
                Debug.LogWarning("CharacterUIManager: poporingSkillButton is not assigned.");
                return;
            }

            poporingSkillButton.onClick.RemoveListener(OnClickPoporingSkill);
            poporingSkillButton.onClick.AddListener(OnClickPoporingSkill);
        }

        /// <summary>포포링 대사 텍스트를 외부에서 갱신한다. 빈 문자열이면 기본 대사로 폴백.</summary>
        public void SetPoporingDialogue(string dialogue)
        {
            if (poporingDialogueText == null)
            {
                Debug.LogWarning("CharacterUIManager: poporingDialogueText is not assigned.");
                return;
            }
            poporingDialogueText.text = string.IsNullOrWhiteSpace(dialogue) ? poporingDefaultDialogue : dialogue;
        }

        /// <summary>포포링 스킬 버튼 활성/비활성 토글.</summary>
        public void SetPoporingSkillButtonInteractable(bool interactable)
        {
            if (poporingSkillButton != null)
            {
                poporingSkillButton.interactable = interactable;
            }
        }

        private void OnClickPoporingSkill()
        {
            Debug.Log($"CharacterUIManager: Poporing skill button clicked: {poporingSkillName}");
            ShowSkillTutorialByType(SkillType.PoporingBubbleHint);

            if (SkillManager.Instance == null)
            {
                Debug.LogWarning("CharacterUIManager: SkillManager.Instance not found.");
                SetPoporingDialogue(poporingSkillUnavailableDialogue);
                return;
            }

            bool used = SkillManager.Instance.UsePoporingBubbleHintSkill();
            string text = used
                ? DialogueManager.Get("skill.poporing.bubble.success", poporingSkillClickDialogue)
                : DialogueManager.Get("skill.poporing.bubble.fail",    poporingSkillUnavailableDialogue);
            SetPoporingDialogue(text);
            if (used) PlayPoporingReaction();
            // TODO: Disable button after successful use (BoardManager → 이벤트 → SetPoporingSkillButtonInteractable(false)).
        }

        // ───────── 모찌룬 메서드 ─────────

        /// <summary>모찌룬 UI를 기본 상태로 초기화한다.</summary>
        public void InitMochirunUI()
        {
            ShowMochirun();
            ApplyMochirunProfile();
            ApplyMochirunName();
            SetMochirunDialogue(mochirunDefaultDialogue);
            ApplyMochirunSkillButton();
            Debug.Log("CharacterUIManager: Mochirun UI initialized.");
        }

        /// <summary>모찌룬 패널을 표시한다.</summary>
        public void ShowMochirun()
        {
            if (mochirunPanel != null)
            {
                mochirunPanel.SetActive(true);
            }
        }

        /// <summary>모찌룬 패널을 숨긴다.</summary>
        public void HideMochirun()
        {
            if (mochirunPanel != null)
            {
                mochirunPanel.SetActive(false);
            }
        }

        private void ApplyMochirunProfile()
        {
            if (mochirunProfileImage == null)
            {
                Debug.LogWarning("CharacterUIManager: mochirunProfileImage is not assigned.");
                return;
            }
            if (mochirunProfileSprite != null)
            {
                mochirunProfileImage.sprite = mochirunProfileSprite;
            }
        }

        private void ApplyMochirunName()
        {
            if (mochirunNameText == null)
            {
                Debug.LogWarning("CharacterUIManager: mochirunNameText is not assigned.");
                return;
            }
            mochirunNameText.text = ResolveDisplayName("mochirun", mochirunCharacterName);
        }

        private void ApplyMochirunSkillButton()
        {
            if (mochirunSkillButtonText != null)
            {
                mochirunSkillButtonText.text = mochirunSkillName;
            }

            if (mochirunSkillButton == null)
            {
                Debug.LogWarning("CharacterUIManager: mochirunSkillButton is not assigned.");
                return;
            }

            mochirunSkillButton.onClick.RemoveListener(OnClickMochirunSkill);
            mochirunSkillButton.onClick.AddListener(OnClickMochirunSkill);
        }

        /// <summary>모찌룬 대사 텍스트를 외부에서 갱신한다. 빈 문자열이면 기본 대사로 폴백.</summary>
        public void SetMochirunDialogue(string dialogue)
        {
            if (mochirunDialogueText == null)
            {
                Debug.LogWarning("CharacterUIManager: mochirunDialogueText is not assigned.");
                return;
            }
            mochirunDialogueText.text = string.IsNullOrWhiteSpace(dialogue) ? mochirunDefaultDialogue : dialogue;
        }

        /// <summary>모찌룬 스킬 버튼 활성/비활성 토글.</summary>
        public void SetMochirunSkillButtonInteractable(bool interactable)
        {
            if (mochirunSkillButton != null)
            {
                mochirunSkillButton.interactable = interactable;
            }
        }

        private void OnClickMochirunSkill()
        {
            Debug.Log($"CharacterUIManager: Mochirun skill button clicked: {mochirunSkillName}");
            ShowSkillTutorialByType(SkillType.MochirunNumberSort);

            if (SkillManager.Instance == null)
            {
                Debug.LogWarning("CharacterUIManager: SkillManager.Instance not found.");
                SetMochirunDialogue(mochirunSkillUnavailableDialogue);
                return;
            }

            bool used = SkillManager.Instance.UseMochirunNumberSortSkill();
            string text = used
                ? DialogueManager.Get("skill.mochirun.sort.success", mochirunSkillClickDialogue)
                : DialogueManager.Get("skill.mochirun.sort.fail",    mochirunSkillUnavailableDialogue);
            SetMochirunDialogue(text);
            if (used) PlayMochirunReaction();
            // TODO: Disable button after successful use (BoardManager → 이벤트 → SetMochirunSkillButtonInteractable(false)).
        }

        // ───────── 스킬 튜토리얼 메서드 ─────────

        /// <summary>스킬 튜토리얼 UI를 비활성 상태로 초기화하고 닫기 버튼을 연결한다.</summary>
        public void InitSkillTutorialUI()
        {
            if (skillTutorialPanel != null)
            {
                skillTutorialPanel.SetActive(false);
            }
            if (skillTutorialCloseButton != null)
            {
                skillTutorialCloseButton.onClick.RemoveListener(HideSkillTutorial);
                skillTutorialCloseButton.onClick.AddListener(HideSkillTutorial);
            }
        }

        /// <summary>SkillType을 받아 미리 정의된 짧은 튜토리얼 문구를 표시한다.</summary>
        public void ShowSkillTutorialByType(SkillType skillType)
        {
            // 모든 튜토리얼 문구는 DialogueDatabase에서 조회. 누락 시 fallback 문자열 사용.
            switch (skillType)
            {
                case SkillType.NabyeolHint:
                    ShowSkillTutorial(
                        DialogueManager.Get("tutorial.skill.nabyeol.hint.title",       "별자리 보기"),
                        DialogueManager.Get("tutorial.skill.nabyeol.hint.description", "움직이면 맞출 수 있는 블록 2개를 반짝 알려줘요."));
                    break;
                case SkillType.DabyeolMove:
                    ShowSkillTutorial(
                        DialogueManager.Get("tutorial.skill.dabyeol.move.title",       "꿈결 움직이기"),
                        DialogueManager.Get("tutorial.skill.dabyeol.move.description", "블록 하나를 골라 옆 블록과 자리를 바꿀 수 있어요."));
                    break;
                case SkillType.TwinStarPop:
                    ShowSkillTutorial(
                        DialogueManager.Get("tutorial.skill.twinstar.pop.title",       "트윈스타 팡"),
                        DialogueManager.Get("tutorial.skill.twinstar.pop.description", "고른 색과 같은 블록을 한 번에 팡! 지워요."));
                    break;
                case SkillType.CapymongBreath:
                    ShowSkillTutorial(
                        DialogueManager.Get("tutorial.skill.capymong.breath.title",       "느긋한 숨결"),
                        DialogueManager.Get("tutorial.skill.capymong.breath.description", "남은 이동 횟수가 1번 늘어나요."));
                    break;
                case SkillType.PoporingBubbleHint:
                    ShowSkillTutorial(
                        DialogueManager.Get("tutorial.skill.poporing.bubble.title",       "방울 힌트"),
                        DialogueManager.Get("tutorial.skill.poporing.bubble.description", "방울이 움직일 곳을 톡톡 알려줘요."));
                    break;
                case SkillType.MochirunNumberSort:
                    ShowSkillTutorial(
                        DialogueManager.Get("tutorial.skill.mochirun.sort.title",       "숫자 블록 정렬"),
                        DialogueManager.Get("tutorial.skill.mochirun.sort.description", "같은 숫자 블록을 나란히 모아줘요."));
                    break;
                default:
                    Debug.LogWarning($"CharacterUIManager: No tutorial defined for skill type {skillType}.");
                    break;
            }
        }

        /// <summary>
        /// 임의의 제목/설명으로 스킬 튜토리얼을 표시한다. 자동 숨김 코루틴이 함께 시작된다.
        /// UI 슬롯이 비어 있어도 안전하게 무동작 + 경고 로그.
        /// </summary>
        public void ShowSkillTutorial(string title, string description)
        {
            if (skillTutorialPanel == null)
            {
                Debug.LogWarning("CharacterUIManager: skillTutorialPanel is not assigned.");
                return;
            }

            if (skillTutorialTitleText != null)
            {
                skillTutorialTitleText.text = title ?? string.Empty;
            }
            if (skillTutorialDescriptionText != null)
            {
                skillTutorialDescriptionText.text = description ?? string.Empty;
            }

            skillTutorialPanel.SetActive(true);
            Debug.Log($"CharacterUIManager: Skill tutorial shown: {title}");

            if (skillTutorialHideCoroutine != null)
            {
                StopCoroutine(skillTutorialHideCoroutine);
                skillTutorialHideCoroutine = null;
            }
            if (gameObject.activeInHierarchy)
            {
                skillTutorialHideCoroutine = StartCoroutine(HideSkillTutorialAfterDelay());
            }
        }

        /// <summary>스킬 튜토리얼 패널을 즉시 숨긴다. 닫기 버튼 OnClick에도 연결되어 있다.</summary>
        public void HideSkillTutorial()
        {
            if (skillTutorialHideCoroutine != null)
            {
                StopCoroutine(skillTutorialHideCoroutine);
                skillTutorialHideCoroutine = null;
            }
            if (skillTutorialPanel != null)
            {
                skillTutorialPanel.SetActive(false);
            }
            Debug.Log("CharacterUIManager: Skill tutorial hidden.");
        }

        private IEnumerator HideSkillTutorialAfterDelay()
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, skillTutorialAutoHideSeconds));
            HideSkillTutorial();
        }

        // ───────── Reaction Animations (Task 102) ─────────

        /// <summary>나별 프로필 이미지를 가볍게 bounce. SimpleAnimationManager가 없으면 무시.</summary>
        public void PlayNabyeolReaction()       => PlayImageReaction(profileImage);
        /// <summary>다별 프로필 이미지 bounce.</summary>
        public void PlayDabyeolReaction()       => PlayImageReaction(dabyeolProfileImage);
        /// <summary>카피몽 프로필 이미지 bounce.</summary>
        public void PlayCapymongReaction()      => PlayImageReaction(capymongProfileImage);
        /// <summary>포포링 프로필 이미지 bounce.</summary>
        public void PlayPoporingReaction()      => PlayImageReaction(poporingProfileImage);
        /// <summary>모찌룬 프로필 이미지 bounce.</summary>
        public void PlayMochirunReaction()      => PlayImageReaction(mochirunProfileImage);

        private void PlayImageReaction(Image target)
        {
            if (target == null) return;
            SimpleAnimationManager mgr = SimpleAnimationManager.Instance;
            if (mgr == null || !mgr.AnimationsEnabled) return;
            RectTransform rect = target.rectTransform;
            if (rect == null) return;
            StartCoroutine(mgr.PlayCharacterBounce(rect));
        }
    }
}
