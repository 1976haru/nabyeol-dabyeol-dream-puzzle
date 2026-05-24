using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Character;
using NabyeolDabyeolDreamPuzzle.Skill;
using NabyeolDabyeolDreamPuzzle.Dialogue;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 6명 캐릭터(나별/다별/카피몽/포포링/모찌룬/노노)의 CharacterPackData 자산과
    /// 통합 CharacterPackDatabase 자산을 자동 생성/업데이트.
    /// 추가로 DefaultDialogueDatabase에 노노 관련 keys가 없으면 append한다.
    /// 씬에 CharacterPackManager가 있으면 database 슬롯 자동 연결.
    /// </summary>
    public static class CharacterPackGenerator
    {
        private const string CharacterFolder = "Assets/_Project/Data/Characters";
        private const string DatabasePath = "Assets/_Project/Data/Characters/CharacterPackDatabase.asset";
        private const string DialogueDatabasePath = "Assets/_Project/Data/Dialogue/DefaultDialogueDatabase.asset";

        private struct PackRow
        {
            public string fileTag;
            public string characterId;
            public string characterName;
            public string toneDescription;
            public CharacterRole role;

            public string defaultDialogueKey;
            public string representativeDialogueKey;
            public string skillSuccessDialogueKey;
            public string skillFailDialogueKey;

            public Color characterColor;

            public string skillName;
            public string skillTitleKey;
            public string skillDescriptionKey;
            public SkillType skillType;
        }

        [MenuItem("Tools/NabyeolDabyeol/Generate Character Packs")]
        public static void GenerateAll()
        {
            EnsureFolder(CharacterFolder);

            PackRow[] rows = BuildRows();
            int created = 0, updated = 0;
            CharacterPackData[] generated = new CharacterPackData[rows.Length];

            for (int i = 0; i < rows.Length; i++)
            {
                PackRow row = rows[i];
                string assetPath = $"{CharacterFolder}/CharacterPack_{row.fileTag}.asset";
                CharacterPackData existing = AssetDatabase.LoadAssetAtPath<CharacterPackData>(assetPath);
                bool isNew = existing == null;
                CharacterPackData asset = existing != null ? existing : ScriptableObject.CreateInstance<CharacterPackData>();

                ApplyRow(asset, row);

                if (isNew)
                {
                    AssetDatabase.CreateAsset(asset, assetPath);
                    created++;
                }
                else
                {
                    EditorUtility.SetDirty(asset);
                    updated++;
                }
                generated[i] = asset;
            }

            // Database
            CharacterPackDatabase db = AssetDatabase.LoadAssetAtPath<CharacterPackDatabase>(DatabasePath);
            bool dbIsNew = db == null;
            if (db == null) db = ScriptableObject.CreateInstance<CharacterPackDatabase>();

            SerializedObject dbSo = new SerializedObject(db);
            SerializedProperty chars = dbSo.FindProperty("characters");
            chars.arraySize = generated.Length;
            for (int i = 0; i < generated.Length; i++)
            {
                chars.GetArrayElementAtIndex(i).objectReferenceValue = generated[i];
            }
            dbSo.ApplyModifiedPropertiesWithoutUndo();

            if (dbIsNew) AssetDatabase.CreateAsset(db, DatabasePath);
            else EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"CharacterPackGenerator: Packs Created {created}, Updated {updated} (total {created + updated}).");
            Debug.Log($"CharacterPackGenerator: Database {(dbIsNew ? "created" : "updated")} at {DatabasePath}.");

            CharacterPackDatabase loaded = AssetDatabase.LoadAssetAtPath<CharacterPackDatabase>(DatabasePath);
            if (loaded != null)
            {
                bool ok = loaded.ValidateCharacters();
                Debug.Log($"CharacterPackGenerator: ValidateCharacters = {ok} (count={loaded.Count}).");
            }

            // DialogueDatabase에 노노 관련 keys 보강
            AugmentDialogueDatabaseForNono();

            // 씬의 CharacterPackManager 자동 attach
            CharacterPackManager mgr = Object.FindAnyObjectByType<CharacterPackManager>();
            if (mgr != null)
            {
                SerializedObject so = new SerializedObject(mgr);
                SerializedProperty dbProp = so.FindProperty("database");
                if (dbProp != null)
                {
                    dbProp.objectReferenceValue = loaded;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(mgr);
                    AssetDatabase.SaveAssets();
                    Debug.Log("CharacterPackGenerator: Attached database to scene CharacterPackManager.");
                }
            }
            else
            {
                Debug.Log("CharacterPackGenerator: No CharacterPackManager found in active scene. Drag the asset manually into Database slot.");
            }
        }

        private static void AugmentDialogueDatabaseForNono()
        {
            DialogueDatabase dlg = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(DialogueDatabasePath);
            if (dlg == null)
            {
                Debug.LogWarning("CharacterPackGenerator: DialogueDatabase not found at " + DialogueDatabasePath + ". Skipping Nono dialogue augmentation. Run DialogueDatabaseGenerator first.");
                return;
            }

            // 추가 후보 entries
            (string key, DialogueCategory cat, string text, string speaker)[] nonoEntries = new[]
            {
                ("character.nono.default",                  DialogueCategory.CharacterDefault, "히히, 퍼즐 장난은 재미있어!", "nono"),
                ("tutorial.skill.nono.trick.title",         DialogueCategory.SkillTutorial,    "장난 섞기",                 (string)null),
                ("tutorial.skill.nono.trick.description",   DialogueCategory.SkillTutorial,    "노노가 퍼즐을 살짝 섞어 놓아요.", (string)null)
            };

            SerializedObject so = new SerializedObject(dlg);
            SerializedProperty entriesProp = so.FindProperty("entries");

            HashSet<string> existingKeys = new HashSet<string>();
            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                SerializedProperty el = entriesProp.GetArrayElementAtIndex(i);
                string k = el.FindPropertyRelative("key").stringValue;
                if (!string.IsNullOrWhiteSpace(k)) existingKeys.Add(k);
            }

            int added = 0;
            for (int i = 0; i < nonoEntries.Length; i++)
            {
                var e = nonoEntries[i];
                if (existingKeys.Contains(e.key)) continue;

                int newIndex = entriesProp.arraySize;
                entriesProp.arraySize = newIndex + 1;
                SerializedProperty element = entriesProp.GetArrayElementAtIndex(newIndex);
                element.FindPropertyRelative("key").stringValue = e.key;
                element.FindPropertyRelative("category").enumValueIndex = (int)e.cat;
                element.FindPropertyRelative("text").stringValue = e.text;
                element.FindPropertyRelative("speakerId").stringValue = e.speaker ?? string.Empty;
                added++;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dlg);
            dlg.InvalidateCache();
            AssetDatabase.SaveAssets();

            Debug.Log($"CharacterPackGenerator: DialogueDatabase augmented with {added} new Nono entries (total entries now {dlg.Count}).");
        }

        private static void ApplyRow(CharacterPackData asset, PackRow row)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("characterId").stringValue = row.characterId;
            so.FindProperty("characterName").stringValue = row.characterName;
            so.FindProperty("toneDescription").stringValue = row.toneDescription;
            so.FindProperty("role").enumValueIndex = (int)row.role;

            so.FindProperty("defaultDialogueKey").stringValue = row.defaultDialogueKey;
            so.FindProperty("representativeDialogueKey").stringValue = row.representativeDialogueKey ?? string.Empty;
            so.FindProperty("skillSuccessDialogueKey").stringValue = row.skillSuccessDialogueKey ?? string.Empty;
            so.FindProperty("skillFailDialogueKey").stringValue = row.skillFailDialogueKey ?? string.Empty;

            SerializedProperty color = so.FindProperty("characterColor");
            color.colorValue = row.characterColor;

            so.FindProperty("skillName").stringValue = row.skillName;
            so.FindProperty("skillTitleKey").stringValue = row.skillTitleKey ?? string.Empty;
            so.FindProperty("skillDescriptionKey").stringValue = row.skillDescriptionKey ?? string.Empty;
            so.FindProperty("skillType").enumValueIndex = (int)row.skillType;

            so.ApplyModifiedPropertiesWithoutUndo();
            asset.name = "CharacterPack_" + row.fileTag;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static PackRow[] BuildRows()
        {
            return new PackRow[]
            {
                new PackRow {
                    fileTag = "Nabyeol", characterId = "nabyeol", characterName = "나별",
                    toneDescription = "밝고 용감한 진행자. '~해 보자!' 같은 권유형 어미를 자주 쓴다.",
                    role = CharacterRole.MainHero,
                    defaultDialogueKey = "character.nabyeol.default",
                    representativeDialogueKey = "character.nabyeol.default",
                    skillSuccessDialogueKey = "skill.nabyeol.hint.success",
                    skillFailDialogueKey = "skill.nabyeol.hint.fail",
                    characterColor = new Color(1.00f, 0.48f, 0.71f, 1f),       // 핑크 #FF7AB6
                    skillName = "별자리 보기",
                    skillTitleKey = "tutorial.skill.nabyeol.hint.title",
                    skillDescriptionKey = "tutorial.skill.nabyeol.hint.description",
                    skillType = SkillType.NabyeolHint
                },
                new PackRow {
                    fileTag = "Dabyeol", characterId = "dabyeol", characterName = "다별",
                    toneDescription = "차분하고 설명형. '~할 수 있어' '~해 보자' 같은 안정적 톤.",
                    role = CharacterRole.MainSupport,
                    defaultDialogueKey = "character.dabyeol.default",
                    representativeDialogueKey = "character.dabyeol.default",
                    skillSuccessDialogueKey = "skill.dabyeol.move.success",
                    skillFailDialogueKey = "skill.dabyeol.move.fail",
                    characterColor = new Color(0.44f, 0.69f, 0.90f, 1f),       // 스카이블루 #6FAFE6
                    skillName = "꿈결 움직이기",
                    skillTitleKey = "tutorial.skill.dabyeol.move.title",
                    skillDescriptionKey = "tutorial.skill.dabyeol.move.description",
                    skillType = SkillType.DabyeolMove
                },
                new PackRow {
                    fileTag = "Capymong", characterId = "capymong", characterName = "카피몽",
                    toneDescription = "느긋하고 다정. '괜찮아', '천천히 가도 돼' 같은 격려가 많다.",
                    role = CharacterRole.HelperBuff,
                    defaultDialogueKey = "character.capymong.default",
                    representativeDialogueKey = "character.capymong.default",
                    skillSuccessDialogueKey = "skill.capymong.breath.success",
                    skillFailDialogueKey = "skill.capymong.breath.fail",
                    characterColor = new Color(1.00f, 0.80f, 0.44f, 1f),       // 따뜻한 노랑 #FFCB6F
                    skillName = "느긋한 숨결",
                    skillTitleKey = "tutorial.skill.capymong.breath.title",
                    skillDescriptionKey = "tutorial.skill.capymong.breath.description",
                    skillType = SkillType.CapymongBreath
                },
                new PackRow {
                    fileTag = "Poporing", characterId = "poporing", characterName = "포포링",
                    toneDescription = "방울처럼 톡톡 튀는 명랑함. '톡톡', '방울방울' 같은 의성어를 자주 쓴다.",
                    role = CharacterRole.HelperHint,
                    defaultDialogueKey = "character.poporing.default",
                    representativeDialogueKey = "character.poporing.default",
                    skillSuccessDialogueKey = "skill.poporing.bubble.success",
                    skillFailDialogueKey = "skill.poporing.bubble.fail",
                    characterColor = new Color(0.72f, 0.62f, 0.90f, 1f),       // 라벤더 #B89EE6
                    skillName = "방울 힌트",
                    skillTitleKey = "tutorial.skill.poporing.bubble.title",
                    skillDescriptionKey = "tutorial.skill.poporing.bubble.description",
                    skillType = SkillType.PoporingBubbleHint
                },
                new PackRow {
                    fileTag = "Mochirun", characterId = "mochirun", characterName = "모찌룬",
                    toneDescription = "숫자와 정리를 좋아함. '차근차근', '순서대로' 같은 표현을 자주 쓴다.",
                    role = CharacterRole.HelperSort,
                    defaultDialogueKey = "character.mochirun.default",
                    representativeDialogueKey = "character.mochirun.default",
                    skillSuccessDialogueKey = "skill.mochirun.sort.success",
                    skillFailDialogueKey = "skill.mochirun.sort.fail",
                    characterColor = new Color(0.44f, 0.88f, 0.71f, 1f),       // 민트 #6FE0B5
                    skillName = "숫자 블록 정렬",
                    skillTitleKey = "tutorial.skill.mochirun.sort.title",
                    skillDescriptionKey = "tutorial.skill.mochirun.sort.description",
                    skillType = SkillType.MochirunNumberSort
                },
                new PackRow {
                    fileTag = "Nono", characterId = "nono", characterName = "노노",
                    toneDescription = "장난꾸러기. '히히', '재밌다', '맞춰봐' 같은 밝은 장난기 표현.",
                    role = CharacterRole.Trickster,
                    defaultDialogueKey = "character.nono.default",
                    representativeDialogueKey = "character.nono.default",
                    skillSuccessDialogueKey = "",   // 플레이어 스킬 없음
                    skillFailDialogueKey = "",
                    characterColor = new Color(0.54f, 0.36f, 0.82f, 1f),       // 짙은 보라 #8A5BD0
                    skillName = "장난 섞기",
                    skillTitleKey = "tutorial.skill.nono.trick.title",
                    skillDescriptionKey = "tutorial.skill.nono.trick.description",
                    skillType = SkillType.None
                }
            };
        }
    }
}
