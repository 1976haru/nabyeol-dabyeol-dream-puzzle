using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Dialogue;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// DefaultDialogueDatabase 자동 생성/업데이트.
    /// 32개 기본 entries (캐릭터 기본 5 + 스킬 성공/실패 12 + 튜토리얼 12 + 시스템 3).
    /// 씬에 DialogueManager가 있으면 database 슬롯 자동 연결.
    /// TODO: Add JSON import/export.
    /// </summary>
    public static class DialogueDatabaseGenerator
    {
        private const string Folder = "Assets/_Project/Data/Dialogue";
        private const string DatabasePath = "Assets/_Project/Data/Dialogue/DefaultDialogueDatabase.asset";

        private struct Row
        {
            public string key;
            public DialogueCategory category;
            public string text;
            public string speakerId;
        }

        [MenuItem("Tools/NabyeolDabyeol/Generate Default Dialogue Database")]
        public static void GenerateAll()
        {
            EnsureFolder(Folder);

            DialogueDatabase db = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(DatabasePath);
            bool isNew = db == null;
            if (db == null) db = ScriptableObject.CreateInstance<DialogueDatabase>();

            Row[] rows = BuildRows();
            ApplyRows(db, rows);

            if (isNew)
            {
                AssetDatabase.CreateAsset(db, DatabasePath);
                Debug.Log("DialogueDatabaseGenerator: Created " + DatabasePath);
            }
            else
            {
                EditorUtility.SetDirty(db);
                Debug.Log("DialogueDatabaseGenerator: Updated " + DatabasePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            DialogueDatabase loaded = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(DatabasePath);
            if (loaded != null)
            {
                loaded.InvalidateCache();
                bool ok = loaded.ValidateEntries();
                Debug.Log($"DialogueDatabaseGenerator: Validation done. entries={loaded.Count}, ValidateEntries={ok}.");
            }

            // 씬에 DialogueManager가 있으면 자동 attach
            DialogueManager mgr = Object.FindAnyObjectByType<DialogueManager>();
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
                    Debug.Log("DialogueDatabaseGenerator: Attached database to scene DialogueManager.");
                }
            }
            else
            {
                Debug.Log("DialogueDatabaseGenerator: No DialogueManager found in active scene. Drag the asset manually into Database slot.");
            }
        }

        private static void ApplyRows(DialogueDatabase db, Row[] rows)
        {
            SerializedObject so = new SerializedObject(db);
            SerializedProperty entriesProp = so.FindProperty("entries");
            entriesProp.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                SerializedProperty element = entriesProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("key").stringValue = rows[i].key;
                element.FindPropertyRelative("category").enumValueIndex = (int)rows[i].category;
                element.FindPropertyRelative("text").stringValue = rows[i].text;
                element.FindPropertyRelative("speakerId").stringValue = rows[i].speakerId ?? string.Empty;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Row R(string key, DialogueCategory cat, string text, string speakerId = null)
        {
            return new Row { key = key, category = cat, text = text, speakerId = speakerId };
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

        private static Row[] BuildRows()
        {
            return new Row[]
            {
                // ─── 캐릭터 기본 대사 5 ───
                R("character.nabyeol.default",  DialogueCategory.CharacterDefault, "말랑이들아, 같이 퍼즐을 풀어보자!", "nabyeol"),
                R("character.dabyeol.default",  DialogueCategory.CharacterDefault, "이번엔 내가 차분히 도와줄게.",   "dabyeol"),
                R("character.capymong.default", DialogueCategory.CharacterDefault, "천천히 가도 괜찮아. 한 번 더 해보자.", "capymong"),
                R("character.poporing.default", DialogueCategory.CharacterDefault, "방울방울 떠오르는 길을 찾아볼게!", "poporing"),
                R("character.mochirun.default", DialogueCategory.CharacterDefault, "숫자를 차례대로 정리해 볼게!",  "mochirun"),

                // ─── 스킬 성공/실패 12 ───
                R("skill.nabyeol.hint.success", DialogueCategory.SkillSuccess, "별자리가 알려줬어! 반짝이는 블록을 봐!", "nabyeol"),
                R("skill.nabyeol.hint.fail",    DialogueCategory.SkillFail,    "지금은 별자리가 잘 보이지 않아.",      "nabyeol"),
                R("skill.dabyeol.move.success", DialogueCategory.SkillSuccess, "움직일 블록을 고르고, 보내고 싶은 방향을 선택해줘.", "dabyeol"),
                R("skill.dabyeol.move.fail",    DialogueCategory.SkillFail,    "지금은 블록을 움직일 수 없어.",         "dabyeol"),
                R("skill.twinstar.pop.success.nabyeol", DialogueCategory.SkillSuccess, "반짝이는 색을 골라줘!",            "nabyeol"),
                R("skill.twinstar.pop.success.dabyeol", DialogueCategory.SkillSuccess, "같은 색 블록을 함께 정리할게.",      "dabyeol"),
                R("skill.twinstar.pop.fail.nabyeol",    DialogueCategory.SkillFail,    "지금은 트윈스타를 쓸 수 없어.",     "nabyeol"),
                R("skill.twinstar.pop.fail.dabyeol",    DialogueCategory.SkillFail,    "보드가 안정된 뒤 다시 해보자.",     "dabyeol"),
                R("skill.capymong.breath.success", DialogueCategory.SkillSuccess, "후우우… 한 번 더 움직일 수 있어.",   "capymong"),
                R("skill.capymong.breath.fail",    DialogueCategory.SkillFail,    "지금은 숨을 고를 수 없어.",          "capymong"),
                R("skill.poporing.bubble.success", DialogueCategory.SkillSuccess, "방울이 길을 알려주고 있어!",          "poporing"),
                R("skill.poporing.bubble.fail",    DialogueCategory.SkillFail,    "지금은 방울이 잘 떠오르지 않아.",      "poporing"),
                R("skill.mochirun.sort.success",   DialogueCategory.SkillSuccess, "좋아! 숫자 블록을 보기 좋게 정리했어.", "mochirun"),
                R("skill.mochirun.sort.fail",      DialogueCategory.SkillFail,    "지금은 숫자를 정리할 수 없어.",        "mochirun"),

                // ─── 스킬 튜토리얼 12 (6스킬 × 제목/설명) ───
                R("tutorial.skill.nabyeol.hint.title",       DialogueCategory.SkillTutorial, "별자리 보기"),
                R("tutorial.skill.nabyeol.hint.description", DialogueCategory.SkillTutorial, "움직이면 맞출 수 있는 블록 2개를 반짝 알려줘요."),
                R("tutorial.skill.dabyeol.move.title",       DialogueCategory.SkillTutorial, "꿈결 움직이기"),
                R("tutorial.skill.dabyeol.move.description", DialogueCategory.SkillTutorial, "블록 하나를 골라 옆 블록과 자리를 바꿀 수 있어요."),
                R("tutorial.skill.twinstar.pop.title",       DialogueCategory.SkillTutorial, "트윈스타 팡"),
                R("tutorial.skill.twinstar.pop.description", DialogueCategory.SkillTutorial, "고른 색과 같은 블록을 한 번에 팡! 지워요."),
                R("tutorial.skill.capymong.breath.title",       DialogueCategory.SkillTutorial, "느긋한 숨결"),
                R("tutorial.skill.capymong.breath.description", DialogueCategory.SkillTutorial, "남은 이동 횟수가 1번 늘어나요."),
                R("tutorial.skill.poporing.bubble.title",       DialogueCategory.SkillTutorial, "방울 힌트"),
                R("tutorial.skill.poporing.bubble.description", DialogueCategory.SkillTutorial, "방울이 움직일 곳을 톡톡 알려줘요."),
                R("tutorial.skill.mochirun.sort.title",         DialogueCategory.SkillTutorial, "숫자 블록 정렬"),
                R("tutorial.skill.mochirun.sort.description",   DialogueCategory.SkillTutorial, "같은 숫자 블록을 나란히 모아줘요."),

                // ─── 시스템 메시지 3 ───
                R("system.skill.not_ready",    DialogueCategory.SystemMessage, "아직 스킬을 쓸 준비가 안 됐어."),
                R("system.skill.already_used", DialogueCategory.SystemMessage, "이번 스테이지에서는 이미 사용했어."),
                R("system.no_hint",            DialogueCategory.SystemMessage, "지금은 알려줄 길이 보이지 않아."),

                // ─── 캐릭터 대표 대사 후보 18 (6 chars × 3) ───
                R("character.nabyeol.rep.default",  DialogueCategory.CharacterDefault, "말랑이들아, 같이 퍼즐을 풀어보자!", "nabyeol"),
                R("character.nabyeol.rep.brave",    DialogueCategory.CharacterDefault, "좋아! 반짝이는 길을 찾아보자!",     "nabyeol"),
                R("character.nabyeol.rep.cheer",    DialogueCategory.CharacterDefault, "할 수 있어! 한 번 더 해보자!",      "nabyeol"),

                R("character.dabyeol.rep.default",  DialogueCategory.CharacterDefault, "이번엔 내가 차분히 도와줄게.",     "dabyeol"),
                R("character.dabyeol.rep.smart",    DialogueCategory.CharacterDefault, "차분히 보면 답이 보여.",            "dabyeol"),
                R("character.dabyeol.rep.guide",    DialogueCategory.CharacterDefault, "순서대로 살펴보면 쉬워.",           "dabyeol"),

                R("character.capymong.rep.default", DialogueCategory.CharacterDefault, "천천히 가도 괜찮아.",                "capymong"),
                R("character.capymong.rep.relax",   DialogueCategory.CharacterDefault, "쉬어 가도 괜찮아. 다시 해보자.",     "capymong"),
                R("character.capymong.rep.support", DialogueCategory.CharacterDefault, "괜찮아. 같이 천천히 해보자.",        "capymong"),

                R("character.poporing.rep.default", DialogueCategory.CharacterDefault, "방울방울 떠오르는 길을 찾아볼게!",  "poporing"),
                R("character.poporing.rep.bubble",  DialogueCategory.CharacterDefault, "톡톡! 방울이 알려줄게.",            "poporing"),
                R("character.poporing.rep.bright",  DialogueCategory.CharacterDefault, "반짝 방울을 따라가 보자!",          "poporing"),

                R("character.mochirun.rep.default", DialogueCategory.CharacterDefault, "숫자를 차례대로 정리해 볼게!",      "mochirun"),
                R("character.mochirun.rep.order",   DialogueCategory.CharacterDefault, "순서대로 정리하면 쉬워.",            "mochirun"),
                R("character.mochirun.rep.number",  DialogueCategory.CharacterDefault, "숫자처럼 차근차근 보자.",            "mochirun"),

                R("character.nono.rep.default",     DialogueCategory.CharacterDefault, "히히, 퍼즐 장난은 재미있어!",        "nono"),
                R("character.nono.rep.playful",     DialogueCategory.CharacterDefault, "맞춰봐! 풀면 길을 열어줄게.",         "nono"),
                R("character.nono.rep.fun",         DialogueCategory.CharacterDefault, "장난 퍼즐도 재미있지?",              "nono")
            };
        }
    }
}
