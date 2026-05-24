using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Character;
using NabyeolDabyeolDreamPuzzle.Dialogue;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 캐릭터별 대표 대사 템플릿(3개 × 6 캐릭터 = 18개)을 CharacterPackData asset에 자동 등록.
    /// 동시에 DefaultDialogueDatabase에 누락된 대표 대사 key가 있으면 append-merge한다.
    /// 메뉴: Tools/NabyeolDabyeol/Generate Representative Dialogue Templates
    /// 이미 등록된 templateId는 덮어쓰고, 추가 templateId는 append. 멱등.
    /// </summary>
    public static class RepresentativeDialogueTemplateGenerator
    {
        private const string CharacterRoot = "Assets/_Project/Data/Characters";
        private const string DialogueDatabasePath = "Assets/_Project/Data/Dialogue/DefaultDialogueDatabase.asset";

        private struct TemplateRow
        {
            public string templateId;
            public string dialogueKey;
            public string previewText;
            public string description;
        }

        private struct CharacterRow
        {
            public string characterId;
            public string representativeKey;   // CharacterPackData.representativeDialogueKey 동기화 (없으면 기본 첫 템플릿 key)
            public TemplateRow[] templates;
        }

        [MenuItem("Tools/NabyeolDabyeol/Generate Representative Dialogue Templates")]
        public static void GenerateAll()
        {
            CharacterRow[] rows = BuildRows();

            // 1) DialogueDatabase 누락 key 보강 (append-merge)
            int dialogueAdded = AugmentDialogueDatabase(rows);

            // 2) CharacterPackData asset 검색 후 templates 등록
            int packsUpdated = ApplyToCharacterPacks(rows);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"RepresentativeDialogueTemplateGenerator: dialogueAdded={dialogueAdded}, packsUpdated={packsUpdated}.");
        }

        private static int AugmentDialogueDatabase(CharacterRow[] rows)
        {
            DialogueDatabase dlg = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(DialogueDatabasePath);
            if (dlg == null)
            {
                Debug.LogWarning("RepresentativeDialogueTemplateGenerator: DialogueDatabase not found. Run DialogueDatabaseGenerator first.");
                return 0;
            }

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
            for (int r = 0; r < rows.Length; r++)
            {
                CharacterRow charRow = rows[r];
                if (charRow.templates == null) continue;
                for (int t = 0; t < charRow.templates.Length; t++)
                {
                    TemplateRow tmpl = charRow.templates[t];
                    if (string.IsNullOrWhiteSpace(tmpl.dialogueKey)) continue;
                    if (existingKeys.Contains(tmpl.dialogueKey)) continue;

                    int newIndex = entriesProp.arraySize;
                    entriesProp.arraySize = newIndex + 1;
                    SerializedProperty element = entriesProp.GetArrayElementAtIndex(newIndex);
                    element.FindPropertyRelative("key").stringValue = tmpl.dialogueKey;
                    element.FindPropertyRelative("category").enumValueIndex = (int)DialogueCategory.CharacterDefault;
                    element.FindPropertyRelative("text").stringValue = tmpl.previewText ?? string.Empty;
                    element.FindPropertyRelative("speakerId").stringValue = charRow.characterId ?? string.Empty;
                    existingKeys.Add(tmpl.dialogueKey);
                    added++;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dlg);
            dlg.InvalidateCache();
            return added;
        }

        private static int ApplyToCharacterPacks(CharacterRow[] rows)
        {
            int updated = 0;
            string[] roots = AssetDatabase.IsValidFolder(CharacterRoot)
                ? new[] { CharacterRoot }
                : new[] { "Assets" };
            string[] guids = AssetDatabase.FindAssets("t:CharacterPackData", roots);

            // characterId → CharacterRow 빠른 검색
            Dictionary<string, CharacterRow> byId = new Dictionary<string, CharacterRow>();
            for (int i = 0; i < rows.Length; i++)
            {
                byId[rows[i].characterId] = rows[i];
            }

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CharacterPackData pack = AssetDatabase.LoadAssetAtPath<CharacterPackData>(path);
                if (pack == null) continue;
                string id = pack.CharacterId;
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (!byId.TryGetValue(id, out CharacterRow row)) continue;

                SerializedObject so = new SerializedObject(pack);

                // representativeDialogueKey 동기화 로직:
                // - 비어 있으면 row.representativeKey 또는 templates[0]로 채움
                // - "character.<id>.default" 같은 평상시 키와 동일하면 새 형식 "character.<id>.rep.default"로 업그레이드
                // - 그 외 사용자 커스텀 값은 유지
                SerializedProperty repProp = so.FindProperty("representativeDialogueKey");
                if (repProp != null)
                {
                    string desiredRepKey = !string.IsNullOrWhiteSpace(row.representativeKey)
                        ? row.representativeKey
                        : (row.templates != null && row.templates.Length > 0 ? row.templates[0].dialogueKey : string.Empty);
                    string currentRep = repProp.stringValue;
                    string legacyKey = $"character.{id}.default";
                    if (string.IsNullOrWhiteSpace(currentRep) || currentRep == legacyKey)
                    {
                        repProp.stringValue = desiredRepKey;
                    }
                }

                // representativeDialogueTemplates 채움 — 기존 templateId가 같으면 업데이트, 없으면 append
                SerializedProperty listProp = so.FindProperty("representativeDialogueTemplates");
                if (listProp == null)
                {
                    Debug.LogWarning($"RepresentativeDialogueTemplateGenerator: representativeDialogueTemplates property not found on '{id}'. CharacterPackData might be outdated.");
                    continue;
                }

                // 기존 templateId → 인덱스 매핑
                Dictionary<string, int> existingIdx = new Dictionary<string, int>();
                for (int e = 0; e < listProp.arraySize; e++)
                {
                    SerializedProperty el = listProp.GetArrayElementAtIndex(e);
                    string tid = el.FindPropertyRelative("templateId").stringValue;
                    if (!string.IsNullOrWhiteSpace(tid)) existingIdx[tid] = e;
                }

                if (row.templates != null)
                {
                    for (int t = 0; t < row.templates.Length; t++)
                    {
                        TemplateRow tmpl = row.templates[t];
                        int targetIdx;
                        if (existingIdx.TryGetValue(tmpl.templateId, out targetIdx))
                        {
                            // in-place 업데이트
                        }
                        else
                        {
                            targetIdx = listProp.arraySize;
                            listProp.arraySize = targetIdx + 1;
                            existingIdx[tmpl.templateId] = targetIdx;
                        }
                        SerializedProperty element = listProp.GetArrayElementAtIndex(targetIdx);
                        element.FindPropertyRelative("templateId").stringValue = tmpl.templateId ?? string.Empty;
                        element.FindPropertyRelative("dialogueKey").stringValue = tmpl.dialogueKey ?? string.Empty;
                        element.FindPropertyRelative("previewText").stringValue = tmpl.previewText ?? string.Empty;
                        element.FindPropertyRelative("description").stringValue = tmpl.description ?? string.Empty;
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(pack);
                updated++;
            }

            return updated;
        }

        private static TemplateRow T(string id, string key, string preview, string desc)
        {
            return new TemplateRow { templateId = id, dialogueKey = key, previewText = preview, description = desc };
        }

        private static CharacterRow[] BuildRows()
        {
            return new CharacterRow[]
            {
                new CharacterRow {
                    characterId = "nabyeol",
                    representativeKey = "character.nabyeol.rep.default",
                    templates = new[] {
                        T("default", "character.nabyeol.rep.default", "말랑이들아, 같이 퍼즐을 풀어보자!", "밝고 진행자 톤의 기본 인사."),
                        T("brave",   "character.nabyeol.rep.brave",   "좋아! 반짝이는 길을 찾아보자!",     "용감하게 시작하는 느낌."),
                        T("cheer",   "character.nabyeol.rep.cheer",   "할 수 있어! 한 번 더 해보자!",      "응원하는 톤. 실패 후 다시 도전할 때 어울림.")
                    }
                },
                new CharacterRow {
                    characterId = "dabyeol",
                    representativeKey = "character.dabyeol.rep.default",
                    templates = new[] {
                        T("default", "character.dabyeol.rep.default", "이번엔 내가 차분히 도와줄게.",   "차분한 지원형 기본 대사."),
                        T("smart",   "character.dabyeol.rep.smart",   "차분히 보면 답이 보여.",          "관찰을 강조하는 톤."),
                        T("guide",   "character.dabyeol.rep.guide",   "순서대로 살펴보면 쉬워.",         "순서·정리를 강조하는 톤.")
                    }
                },
                new CharacterRow {
                    characterId = "capymong",
                    representativeKey = "character.capymong.rep.default",
                    templates = new[] {
                        T("default", "character.capymong.rep.default", "천천히 가도 괜찮아.",              "느긋한 기본 격려."),
                        T("relax",   "character.capymong.rep.relax",   "쉬어 가도 괜찮아. 다시 해보자.",   "휴식을 권하는 톤."),
                        T("support", "character.capymong.rep.support", "괜찮아. 같이 천천히 해보자.",      "함께한다는 느낌의 따뜻한 톤.")
                    }
                },
                new CharacterRow {
                    characterId = "poporing",
                    representativeKey = "character.poporing.rep.default",
                    templates = new[] {
                        T("default", "character.poporing.rep.default", "방울방울 떠오르는 길을 찾아볼게!", "방울 모티프의 기본 인사."),
                        T("bubble",  "character.poporing.rep.bubble",  "톡톡! 방울이 알려줄게.",           "의성어가 강한 톤."),
                        T("bright",  "character.poporing.rep.bright",  "반짝 방울을 따라가 보자!",         "방향을 안내하는 톤.")
                    }
                },
                new CharacterRow {
                    characterId = "mochirun",
                    representativeKey = "character.mochirun.rep.default",
                    templates = new[] {
                        T("default", "character.mochirun.rep.default", "숫자를 차례대로 정리해 볼게!",     "정리·차분형 기본 인사."),
                        T("order",   "character.mochirun.rep.order",   "순서대로 정리하면 쉬워.",          "순서 강조 톤."),
                        T("number",  "character.mochirun.rep.number",  "숫자처럼 차근차근 보자.",          "수학적 접근을 강조한 톤.")
                    }
                },
                new CharacterRow {
                    characterId = "nono",
                    representativeKey = "character.nono.rep.default",
                    templates = new[] {
                        T("default", "character.nono.rep.default", "히히, 퍼즐 장난은 재미있어!",        "장난기 가득한 기본 인사."),
                        T("playful", "character.nono.rep.playful", "맞춰봐! 풀면 길을 열어줄게.",         "도전형 톤."),
                        T("fun",     "character.nono.rep.fun",     "장난 퍼즐도 재미있지?",              "유쾌하게 동의를 구하는 톤.")
                    }
                }
            };
        }
    }
}
