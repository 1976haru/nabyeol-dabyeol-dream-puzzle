using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Story;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 샘플 StoryNode 에셋 2개(1번 스테이지·31번 보스)를 자동 생성/업데이트한다.
    /// 60번 작업의 데이터 구조 검증 + 후속 StoryPopup 작업 테스트용.
    /// 모든 필드는 private SerializeField이므로 SerializedObject로 채운다.
    /// </summary>
    public static class SampleStoryNodeGenerator
    {
        private const string OutputFolder = "Assets/_Project/Data/Story";

        [MenuItem("Tools/NabyeolDabyeol/Generate Sample Story Nodes")]
        public static void GenerateAll()
        {
            EnsureFolder(OutputFolder);

            CreateOrUpdateStageOne();
            CreateOrUpdateBossThirtyOne();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("SampleStoryNodeGenerator: Done. Generated 2 sample StoryNode assets.");
        }

        private static void CreateOrUpdateStageOne()
        {
            string assetPath = $"{OutputFolder}/Story_Stage_001.asset";
            StoryNode existing = AssetDatabase.LoadAssetAtPath<StoryNode>(assetPath);
            StoryNode asset = existing != null ? existing : ScriptableObject.CreateInstance<StoryNode>();

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("storyId").intValue = 1;
            so.FindProperty("storyTitle").stringValue = "토끼의 첫 점프 이야기";
            so.FindProperty("linkedStageId").intValue = 1;

            SetSingleLine(so, "startDialogues", "nabyeol", "나별", "첫 번째 숲길이야. 같은 블록 3개를 맞춰보자!");
            SetSingleLine(so, "clearDialogues", "dabyeol", "다별", "좋았어! 아주 깔끔하게 성공했어.");
            SetSingleLine(so, "failDialogues", "capymong", "카피몽", "괜찮아. 천천히 다시 해보면 돼.");
            so.FindProperty("bossDialogues").arraySize = 0;

            so.ApplyModifiedPropertiesWithoutUndo();
            asset.name = "Story_Stage_001";

            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, assetPath);
                Debug.Log("SampleStoryNodeGenerator: Created " + assetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
                Debug.Log("SampleStoryNodeGenerator: Updated " + assetPath);
            }
        }

        private static void CreateOrUpdateBossThirtyOne()
        {
            string assetPath = $"{OutputFolder}/Story_Boss_031_MemoryTree.asset";
            StoryNode existing = AssetDatabase.LoadAssetAtPath<StoryNode>(assetPath);
            StoryNode asset = existing != null ? existing : ScriptableObject.CreateInstance<StoryNode>();

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("storyId").intValue = 31;
            so.FindProperty("storyTitle").stringValue = "기억나무의 속삭임";
            so.FindProperty("linkedStageId").intValue = 31;

            SetSingleLine(so, "startDialogues", "nabyeol", "나별", "기억나무가 우리를 기다리고 있어!");
            SetSingleLine(so, "clearDialogues", "dabyeol", "다별", "기억 조각이 모두 빛나기 시작했어.");
            SetSingleLine(so, "failDialogues", "capymong", "카피몽", "기억은 천천히 돌아와. 다시 해보자.");
            SetSingleLine(so, "bossDialogues", "memory_tree", "기억나무", "잃어버린 기억 조각을 모아 나를 깨워다오.");

            so.ApplyModifiedPropertiesWithoutUndo();
            asset.name = "Story_Boss_031_MemoryTree";

            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, assetPath);
                Debug.Log("SampleStoryNodeGenerator: Created " + assetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
                Debug.Log("SampleStoryNodeGenerator: Updated " + assetPath);
            }
        }

        private static void SetSingleLine(SerializedObject so, string listFieldName, string speakerId, string speakerName, string dialogue)
        {
            SerializedProperty list = so.FindProperty(listFieldName);
            list.arraySize = 1;
            SerializedProperty element = list.GetArrayElementAtIndex(0);
            element.FindPropertyRelative("speakerId").stringValue = speakerId;
            element.FindPropertyRelative("speakerName").stringValue = speakerName;
            element.FindPropertyRelative("dialogue").stringValue = dialogue;
            // portrait는 비워둔다.
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
