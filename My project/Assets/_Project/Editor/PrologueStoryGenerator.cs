using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Story;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 프롤로그 StoryNode("별빛 숲의 첫 만남")를 생성/업데이트한다.
    /// 60번에서 만든 Story_Stage_001.asset이 이미 있으면 in-place로 덮어써서
    /// linkedStageId=1 중복을 방지한다. 없으면 Prologue/ 폴더에 새로 생성한다.
    /// TODO: Add prologue-only dialogue type if needed.
    /// TODO: Connect StoryNode to StoryPopup UI.
    /// TODO: Link speakerId to CharacterData automatically.
    /// TODO: Show prologue only once using PlayerPrefs.
    /// TODO: Add skip button for repeated play.
    /// </summary>
    public static class PrologueStoryGenerator
    {
        private const string LegacyStagePath = "Assets/_Project/Data/Story/Story_Stage_001.asset";
        private const string PrologueFolder = "Assets/_Project/Data/Story/Prologue";
        private const string PrologueAssetPath = "Assets/_Project/Data/Story/Prologue/Story_Prologue_001.asset";

        [MenuItem("Tools/NabyeolDabyeol/Generate Prologue Story")]
        public static void GenerateAll()
        {
            // 60번에서 만든 stage 1 asset이 있으면 그것을 업데이트 (중복 linkedStageId=1 회피).
            string targetPath = LegacyStagePath;
            StoryNode existing = AssetDatabase.LoadAssetAtPath<StoryNode>(LegacyStagePath);
            if (existing == null)
            {
                EnsureFolder(PrologueFolder);
                targetPath = PrologueAssetPath;
                existing = AssetDatabase.LoadAssetAtPath<StoryNode>(PrologueAssetPath);
            }

            bool isNew = existing == null;
            StoryNode asset = existing != null ? existing : ScriptableObject.CreateInstance<StoryNode>();

            ApplyPrologueData(asset);

            if (isNew)
            {
                AssetDatabase.CreateAsset(asset, targetPath);
                Debug.Log("PrologueStoryGenerator: Created " + targetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
                Debug.Log("PrologueStoryGenerator: Updated " + targetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 검증
            StoryNode loaded = AssetDatabase.LoadAssetAtPath<StoryNode>(targetPath);
            if (loaded != null)
            {
                bool valid = loaded.IsValid();
                Debug.Log($"PrologueStoryGenerator: Validation done. IsValid={valid}, storyId={loaded.StoryId}, linkedStageId={loaded.LinkedStageId}, title='{loaded.StoryTitle}'.");
            }
        }

        private static void ApplyPrologueData(StoryNode asset)
        {
            SerializedObject so = new SerializedObject(asset);

            so.FindProperty("storyId").intValue = 1;
            so.FindProperty("storyTitle").stringValue = "별빛 숲의 첫 만남";
            so.FindProperty("learningTip").stringValue = "같은 블록 3개를 맞추면 길이 열려요.";
            so.FindProperty("linkedStageId").intValue = 1;

            SerializedProperty start = so.FindProperty("startDialogues");
            start.arraySize = 10;
            SetLine(start, 0, "nabyeol",  "나별",   "어? 여긴 반짝반짝한 숲이야!");
            SetLine(start, 1, "dabyeol",  "다별",   "나별, 조심해. 길이 퍼즐로 막혀 있어.");
            SetLine(start, 2, "capymong", "카피몽", "안녕… 나는 카피몽이야. 이 숲에 살고 있어.");
            SetLine(start, 3, "nabyeol",  "나별",   "우와! 너도 별빛을 따라온 거야?");
            SetLine(start, 4, "capymong", "카피몽", "응. 그런데 숲길이 잠들어 버렸어.");
            SetLine(start, 5, "dabyeol",  "다별",   "같은 블록 3개를 맞추면 길이 열릴지도 몰라.");
            SetLine(start, 6, "nabyeol",  "나별",   "좋아! 우리가 함께 풀어보자!");
            SetLine(start, 7, "capymong", "카피몽", "천천히 해도 괜찮아. 같이 가면 돼.");
            SetLine(start, 8, "dabyeol",  "다별",   "첫 번째 목표는 점수를 모으는 거야.");
            SetLine(start, 9, "nabyeol",  "나별",   "말랑이들아, 첫 퍼즐을 시작해 보자!");

            SerializedProperty clear = so.FindProperty("clearDialogues");
            clear.arraySize = 3;
            SetLine(clear, 0, "nabyeol",  "나별",   "해냈어! 숲길이 반짝 열렸어!");
            SetLine(clear, 1, "dabyeol",  "다별",   "좋았어. 같은 블록을 잘 찾았어.");
            SetLine(clear, 2, "capymong", "카피몽", "역시 함께하니까 더 쉬웠어.");

            SerializedProperty fail = so.FindProperty("failDialogues");
            fail.arraySize = 3;
            SetLine(fail, 0, "capymong", "카피몽", "괜찮아. 천천히 다시 해보자.");
            SetLine(fail, 1, "dabyeol",  "다별",   "이번엔 같은 블록을 먼저 찾아보면 좋아.");
            SetLine(fail, 2, "nabyeol",  "나별",   "다시 하면 분명 길이 열릴 거야!");

            // 프롤로그는 보스 스테이지가 아니므로 bossDialogues는 비워둔다.
            so.FindProperty("bossDialogues").arraySize = 0;

            so.ApplyModifiedPropertiesWithoutUndo();

            // asset 이름은 파일명에 맞춰 자동 동기화 (파일 경로 변경 없이 ScriptableObject 인스턴스 이름만 설정)
            // 기존 파일을 업데이트하는 경우 파일명은 유지됨.
        }

        private static void SetLine(SerializedProperty list, int index, string speakerId, string speakerName, string dialogue)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("speakerId").stringValue = speakerId;
            element.FindPropertyRelative("speakerName").stringValue = speakerName;
            element.FindPropertyRelative("dialogue").stringValue = dialogue;
            // portrait는 비워둔다. TODO: Link speakerId to CharacterData portrait automatically.
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
