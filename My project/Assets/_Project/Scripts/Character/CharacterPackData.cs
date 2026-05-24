using System.Collections.Generic;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Skill;

namespace NabyeolDabyeolDreamPuzzle.Character
{
    /// <summary>캐릭터 역할 분류. UI 그룹화/필터링/통계 용도.</summary>
    public enum CharacterRole
    {
        MainHero = 0,
        MainSupport = 1,
        HelperBuff = 2,
        HelperHint = 3,
        HelperSort = 4,
        Trickster = 5
    }

    /// <summary>
    /// 한 캐릭터의 메타데이터를 보관하는 ScriptableObject.
    /// 실제 대사 문장은 DialogueDatabase가 key 기반으로 관리하고, 본 자산은 key·이름·색상·스킬명 같은
    /// 캐릭터 정체성 정보만 모은다. 캐릭터 추가/수정 시 코드 수정 없이 자산만 교체하면 된다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CharacterPack",
        menuName = "NabyeolDabyeol/Character Pack",
        order = 160)]
    public class CharacterPackData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string characterId;
        [SerializeField] private string characterName;
        [TextArea(2, 4)]
        [SerializeField] private string toneDescription;
        [SerializeField] private CharacterRole role = CharacterRole.MainHero;

        [Header("Dialogue Keys (DialogueDatabase 참조)")]
        [SerializeField] private string defaultDialogueKey;
        [SerializeField] private string representativeDialogueKey;
        [SerializeField] private string skillSuccessDialogueKey;
        [SerializeField] private string skillFailDialogueKey;

        [Header("Visual")]
        [SerializeField] private Sprite profileSprite;
        [SerializeField] private Color characterColor = Color.white;

        [Header("Skill")]
        [SerializeField] private string skillName;
        [SerializeField] private string skillTitleKey;
        [SerializeField] private string skillDescriptionKey;
        [SerializeField] private SkillType skillType = SkillType.None;

        [Header("Representative Dialogue Templates")]
        [Tooltip("부모 모드에서 사용자가 선택할 수 있는 대표 대사 후보 목록. 비어 있으면 representativeDialogueKey만 사용한다.")]
        [SerializeField] private List<CharacterDialogueTemplate> representativeDialogueTemplates = new List<CharacterDialogueTemplate>();

        public string CharacterId => characterId;
        public string CharacterName => characterName;
        public string ToneDescription => toneDescription;
        public CharacterRole Role => role;

        public string DefaultDialogueKey => defaultDialogueKey;
        public string RepresentativeDialogueKey => representativeDialogueKey;
        public string SkillSuccessDialogueKey => skillSuccessDialogueKey;
        public string SkillFailDialogueKey => skillFailDialogueKey;

        public Sprite ProfileSprite => profileSprite;
        public Color CharacterColor => characterColor;

        public string SkillName => skillName;
        public string SkillTitleKey => skillTitleKey;
        public string SkillDescriptionKey => skillDescriptionKey;
        public SkillType SkillType => skillType;

        public IReadOnlyList<CharacterDialogueTemplate> RepresentativeDialogueTemplates => representativeDialogueTemplates;
        public bool HasRepresentativeTemplates => representativeDialogueTemplates != null && representativeDialogueTemplates.Count > 0;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(characterId)) return false;
            if (string.IsNullOrWhiteSpace(characterName)) return false;
            if (string.IsNullOrWhiteSpace(skillName)) return false;
            if (string.IsNullOrWhiteSpace(defaultDialogueKey)) return false;
            // representativeDialogueKey는 대표 대사가 누락되지 않도록 비어 있으면 안 됨.
            if (string.IsNullOrWhiteSpace(representativeDialogueKey)) return false;
            // skillTitleKey/skillDescriptionKey는 노노처럼 SkillType.None인 캐릭터에서 비어 있을 수 있음.
            // 다만 둘 다 비어 있으면 안 됨 (튜토리얼이 아예 없는 캐릭터라도 최소 하나는 채워야 UI 빈 상태 방지).
            if (string.IsNullOrWhiteSpace(skillTitleKey) && string.IsNullOrWhiteSpace(skillDescriptionKey))
            {
                return false;
            }
            // 대표 대사 템플릿은 비어 있어도 OK. 단, 항목이 있다면 templateId / dialogueKey가 모두 채워져 있어야 함.
            if (representativeDialogueTemplates != null)
            {
                for (int i = 0; i < representativeDialogueTemplates.Count; i++)
                {
                    CharacterDialogueTemplate t = representativeDialogueTemplates[i];
                    if (t == null) continue;
                    if (!t.IsValid())
                    {
                        Debug.LogWarning($"CharacterPackData '{characterId}': representativeDialogueTemplates[{i}] is invalid (templateId or dialogueKey missing).");
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
