using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Story;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 노노 첫 등장 StoryNode 자동 생성/업데이트.
    /// #60에서 생성된 Story_Boss_031_MemoryTree.asset이 이미 linkedStageId=31을 점유 중이면
    /// 그 자산을 in-place 업데이트해 중복을 회피한다. 없으면 Special/ 폴더에 새로 생성한다.
    /// 노노 캐릭터 톤: 장난스럽고 밝지만 절대 무섭지 않게.
    /// TODO: Add Nono CharacterData.
    /// TODO: Add Nono portrait sprite.
    /// TODO: Play bossDialogues before boss stage starts.
    /// TODO: Show Nono dialogue only once before first boss.
    /// TODO: Add playful puzzle shuffle effect for Nono stages.
    /// </summary>
    public static class NonoFirstAppearanceStoryGenerator
    {
        private const string LegacyBossPath = "Assets/_Project/Data/Story/Story_Boss_031_MemoryTree.asset";
        private const string SpecialFolder = "Assets/_Project/Data/Story/Special";
        private const string NonoAssetPath = "Assets/_Project/Data/Story/Special/Story_Nono_FirstAppearance.asset";

        private struct DialogueRow
        {
            public string speakerId;
            public string speakerName;
            public string dialogue;
        }

        [MenuItem("Tools/NabyeolDabyeol/Generate Nono First Appearance Story")]
        public static void GenerateAll()
        {
            // 60번에서 만든 boss 31 asset이 있으면 그것을 in-place 업데이트해 중복 회피.
            string targetPath = LegacyBossPath;
            StoryNode existing = AssetDatabase.LoadAssetAtPath<StoryNode>(LegacyBossPath);
            if (existing == null)
            {
                EnsureFolder(SpecialFolder);
                targetPath = NonoAssetPath;
                existing = AssetDatabase.LoadAssetAtPath<StoryNode>(NonoAssetPath);
            }

            bool isNew = existing == null;
            StoryNode asset = existing != null ? existing : ScriptableObject.CreateInstance<StoryNode>();

            ApplyNonoData(asset);

            if (isNew)
            {
                AssetDatabase.CreateAsset(asset, targetPath);
                Debug.Log("NonoFirstAppearanceStoryGenerator: Created " + targetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
                Debug.Log("NonoFirstAppearanceStoryGenerator: Updated " + targetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            StoryNode loaded = AssetDatabase.LoadAssetAtPath<StoryNode>(targetPath);
            if (loaded != null)
            {
                bool valid = loaded.IsValid();
                Debug.Log($"NonoFirstAppearanceStoryGenerator: Validation done. IsValid={valid}, storyId={loaded.StoryId}, linkedStageId={loaded.LinkedStageId}, title='{loaded.StoryTitle}'.");
            }
        }

        private static void ApplyNonoData(StoryNode asset)
        {
            SerializedObject so = new SerializedObject(asset);

            so.FindProperty("storyId").intValue = 301;
            so.FindProperty("storyTitle").stringValue = "노노의 첫 장난";
            so.FindProperty("learningTip").stringValue = "장난스러운 길도 차근차근 보면 풀 수 있어요.";
            so.FindProperty("linkedStageId").intValue = 31;

            // bossDialogues: 노노 첫 등장 핵심 8줄.
            SerializedProperty boss = so.FindProperty("bossDialogues");
            boss.arraySize = 8;
            SetLine(boss, 0, "nono",     "노노",   "히히, 여긴 내가 살짝 바꿔놨어!");
            SetLine(boss, 1, "nabyeol",  "나별",   "어? 너는 누구야?");
            SetLine(boss, 2, "nono",     "노노",   "나는 노노! 퍼즐 장난을 좋아해.");
            SetLine(boss, 3, "dabyeol",  "다별",   "장난이라도 길을 막으면 곤란해.");
            SetLine(boss, 4, "nono",     "노노",   "그럼 맞춰봐! 풀면 길을 열어줄게.");
            SetLine(boss, 5, "capymong", "카피몽", "무서운 친구는 아닌 것 같아.");
            SetLine(boss, 6, "nabyeol",  "나별",   "좋아, 장난 퍼즐도 풀어보자!");
            SetLine(boss, 7, "nono",     "노노",   "히히, 어디 한번 해봐!");

            // startDialogues: 31번 스테이지 시작 시 짧은 안내 3줄.
            SerializedProperty start = so.FindProperty("startDialogues");
            start.arraySize = 3;
            SetLine(start, 0, "dabyeol",  "다별",   "노노가 퍼즐을 살짝 섞어둔 것 같아.");
            SetLine(start, 1, "nabyeol",  "나별",   "차근차근 풀면 문제없어!");
            SetLine(start, 2, "capymong", "카피몽", "장난길도 천천히 보면 보여.");

            // clearDialogues: 노노가 적대적이지 않은 반응 3줄.
            SerializedProperty clear = so.FindProperty("clearDialogues");
            clear.arraySize = 3;
            SetLine(clear, 0, "nono",    "노노",   "우와, 진짜 풀었네? 재밌다!");
            SetLine(clear, 1, "nabyeol", "나별",   "장난 퍼즐도 우리가 해결했어!");
            SetLine(clear, 2, "dabyeol", "다별",   "다음엔 함께 길을 열어보자.");

            // failDialogues: 놀리거나 겁주지 않는 재도전 격려 3줄.
            SerializedProperty fail = so.FindProperty("failDialogues");
            fail.arraySize = 3;
            SetLine(fail, 0, "nono",     "노노",   "히히, 이번엔 조금 헷갈렸지?");
            SetLine(fail, 1, "capymong", "카피몽", "괜찮아. 천천히 다시 보면 돼.");
            SetLine(fail, 2, "nabyeol",  "나별",   "다시 하면 노노 장난도 풀 수 있어!");

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLine(SerializedProperty list, int index, string speakerId, string speakerName, string dialogue)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("speakerId").stringValue = speakerId;
            element.FindPropertyRelative("speakerName").stringValue = speakerName;
            element.FindPropertyRelative("dialogue").stringValue = dialogue;
            // portrait는 비워둔다. TODO: Link Nono portrait when character art is ready.
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
    }
}
