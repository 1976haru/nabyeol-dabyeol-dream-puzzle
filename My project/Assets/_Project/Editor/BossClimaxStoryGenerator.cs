using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Story;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 보스 클라이맥스 StoryNode 생성/업데이트.
    /// - Stage 31(기억나무): 기존 노노 대사(#64)를 보존하고 기억나무 대사를 append-merge.
    ///   재실행 시 중복 append를 막기 위해 마지막 라인 marker로 검증.
    /// - Stage 32(거꾸로 시계탑): 단순 신규 생성/in-place 업데이트.
    /// TODO: Play bossDialogues before boss stage starts.
    /// TODO: Show clear/fail dialogues alongside popups.
    /// TODO: Add boss portrait sprites.
    /// TODO: Add one-time boss intro playback using PlayerPrefs.
    /// TODO: Connect bossStageType to StoryManager automatically.
    /// </summary>
    public static class BossClimaxStoryGenerator
    {
        private const string BossFolder = "Assets/_Project/Data/Story/Boss";
        private const string Stage31MemoryTreePath = "Assets/_Project/Data/Story/Story_Boss_031_MemoryTree.asset";
        private const string Stage31NonoFallbackPath = "Assets/_Project/Data/Story/Special/Story_Nono_FirstAppearance.asset";
        private const string Stage31NewPath = "Assets/_Project/Data/Story/Boss/Story_Boss_031_MemoryTree.asset";
        private const string Stage32Path = "Assets/_Project/Data/Story/Boss/Story_Boss_032_ReverseClockTower.asset";

        // 중복 append 방지용 marker: 기억나무 보스 마지막 라인.
        private const string MemoryTreeBossLastMarker = "따뜻한 마음으로 조각을 모아다오.";

        private struct DialogueRow
        {
            public string speakerId;
            public string speakerName;
            public string dialogue;
        }

        [MenuItem("Tools/NabyeolDabyeol/Generate Boss Climax Stories")]
        public static void GenerateAll()
        {
            EnsureFolder(BossFolder);

            UpdateStage31MemoryTree();
            UpdateStage32ReverseClockTower();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("BossClimaxStoryGenerator: Done. Generated/updated 2 boss climax StoryNode assets.");
        }

        // ───────── Stage 31: 기억나무 (노노 보존 + 기억나무 append) ─────────

        private static void UpdateStage31MemoryTree()
        {
            string targetPath = ResolveStage31Path(out StoryNode existing);
            bool isNew = existing == null;
            StoryNode asset = existing != null ? existing : ScriptableObject.CreateInstance<StoryNode>();

            ApplyStage31Data(asset);

            if (isNew)
            {
                AssetDatabase.CreateAsset(asset, targetPath);
                Debug.Log("BossClimaxStoryGenerator: Created " + targetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
                Debug.Log("BossClimaxStoryGenerator: Updated " + targetPath);
            }

            StoryNode loaded = AssetDatabase.LoadAssetAtPath<StoryNode>(targetPath);
            if (loaded != null)
            {
                Debug.Log($"BossClimaxStoryGenerator: Stage 31 validation done. IsValid={loaded.IsValid()}, storyId={loaded.StoryId}, linkedStageId={loaded.LinkedStageId}, title='{loaded.StoryTitle}'.");
            }
        }

        private static string ResolveStage31Path(out StoryNode existing)
        {
            // 1. #60 legacy boss 자산 우선
            existing = AssetDatabase.LoadAssetAtPath<StoryNode>(Stage31MemoryTreePath);
            if (existing != null) return Stage31MemoryTreePath;

            // 2. #64 노노 fallback 경로
            existing = AssetDatabase.LoadAssetAtPath<StoryNode>(Stage31NonoFallbackPath);
            if (existing != null) return Stage31NonoFallbackPath;

            // 3. 신규 환경: Boss/ 폴더에 새로 생성
            return Stage31NewPath;
        }

        private static void ApplyStage31Data(StoryNode asset)
        {
            SerializedObject so = new SerializedObject(asset);

            so.FindProperty("storyId").intValue = 331;
            so.FindProperty("storyTitle").stringValue = "기억나무의 빛나는 조각";
            so.FindProperty("learningTip").stringValue = "목표 블록을 모으면 잠든 기억이 깨어나요.";
            so.FindProperty("linkedStageId").intValue = 31;

            // 기존 대사를 읽어와 기억나무 대사를 append. 이미 merge되어 있으면 skip.
            AppendOrMergeBoss(so);
            AppendOrMergeStart(so);
            AppendOrMergeClear(so);
            AppendOrMergeFail(so);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AppendOrMergeBoss(SerializedObject so)
        {
            SerializedProperty list = so.FindProperty("bossDialogues");
            if (IsMemoryTreeMerged(list))
            {
                Debug.Log("BossClimaxStoryGenerator: bossDialogues already merged. Re-applying memory tree lines.");
                // 이미 merge된 경우에도 마지막 5줄을 정확한 기억나무 내용으로 보장하기 위해 잘라내고 다시 append.
                int originalSize = list.arraySize;
                int memoryTreeLinesCount = 5;
                list.arraySize = Mathf.Max(0, originalSize - memoryTreeLinesCount);
            }

            DialogueRow[] memoryLines = new[] {
                D("memory_tree", "기억나무", "작은 기억 조각들이 숲에 흩어졌구나."),
                D("nabyeol",     "나별",     "우리가 초록 기억 블록을 모아줄게!"),
                D("dabyeol",     "다별",     "목표 개수만큼 모으면 나무가 깨어날 거야."),
                D("capymong",    "카피몽",   "천천히 모아도 괜찮아. 기억은 돌아올 거야."),
                D("memory_tree", "기억나무", "따뜻한 마음으로 조각을 모아다오.")
            };
            AppendLines(list, memoryLines);
        }

        private static void AppendOrMergeStart(SerializedObject so)
        {
            SerializedProperty list = so.FindProperty("startDialogues");
            string memoryTreeStartMarker = "조급해하지 말고 하나씩 모아보자.";
            if (LastDialogueEquals(list, memoryTreeStartMarker))
            {
                list.arraySize = Mathf.Max(0, list.arraySize - 3);
            }

            DialogueRow[] memoryLines = new[] {
                D("dabyeol",  "다별",   "이번 목표는 기억 블록을 모으는 거야."),
                D("nabyeol",  "나별",   "초록 블록을 찾아 기억나무를 깨우자!"),
                D("capymong", "카피몽", "조급해하지 말고 하나씩 모아보자.")
            };
            AppendLines(list, memoryLines);
        }

        private static void AppendOrMergeClear(SerializedObject so)
        {
            SerializedProperty list = so.FindProperty("clearDialogues");
            string memoryTreeClearMarker = "흩어진 조각을 모두 잘 모았어.";
            if (LastDialogueEquals(list, memoryTreeClearMarker))
            {
                list.arraySize = Mathf.Max(0, list.arraySize - 3);
            }

            DialogueRow[] memoryLines = new[] {
                D("memory_tree", "기억나무", "고맙구나. 잊었던 빛이 다시 돌아왔어."),
                D("nabyeol",     "나별",     "기억나무가 반짝이고 있어!"),
                D("dabyeol",     "다별",     "흩어진 조각을 모두 잘 모았어.")
            };
            AppendLines(list, memoryLines);
        }

        private static void AppendOrMergeFail(SerializedObject so)
        {
            SerializedProperty list = so.FindProperty("failDialogues");
            string memoryTreeFailMarker = "다음엔 더 많은 조각을 찾을 수 있어!";
            if (LastDialogueEquals(list, memoryTreeFailMarker))
            {
                list.arraySize = Mathf.Max(0, list.arraySize - 3);
            }

            DialogueRow[] memoryLines = new[] {
                D("memory_tree", "기억나무", "괜찮단다. 기억은 천천히 돌아오는 법이지."),
                D("capymong",    "카피몽",   "다시 천천히 모아보면 돼."),
                D("nabyeol",     "나별",     "다음엔 더 많은 조각을 찾을 수 있어!")
            };
            AppendLines(list, memoryLines);
        }

        private static bool IsMemoryTreeMerged(SerializedProperty list)
        {
            if (list == null || list.arraySize == 0) return false;
            SerializedProperty last = list.GetArrayElementAtIndex(list.arraySize - 1);
            return last.FindPropertyRelative("dialogue").stringValue == MemoryTreeBossLastMarker;
        }

        private static bool LastDialogueEquals(SerializedProperty list, string marker)
        {
            if (list == null || list.arraySize == 0) return false;
            SerializedProperty last = list.GetArrayElementAtIndex(list.arraySize - 1);
            return last.FindPropertyRelative("dialogue").stringValue == marker;
        }

        // ───────── Stage 32: 거꾸로 시계탑 (신규/단순 업데이트) ─────────

        private static void UpdateStage32ReverseClockTower()
        {
            StoryNode existing = AssetDatabase.LoadAssetAtPath<StoryNode>(Stage32Path);
            bool isNew = existing == null;
            StoryNode asset = existing != null ? existing : ScriptableObject.CreateInstance<StoryNode>();

            ApplyStage32Data(asset);

            if (isNew)
            {
                AssetDatabase.CreateAsset(asset, Stage32Path);
                Debug.Log("BossClimaxStoryGenerator: Created " + Stage32Path);
            }
            else
            {
                EditorUtility.SetDirty(asset);
                Debug.Log("BossClimaxStoryGenerator: Updated " + Stage32Path);
            }

            StoryNode loaded = AssetDatabase.LoadAssetAtPath<StoryNode>(Stage32Path);
            if (loaded != null)
            {
                Debug.Log($"BossClimaxStoryGenerator: Stage 32 validation done. IsValid={loaded.IsValid()}, storyId={loaded.StoryId}, linkedStageId={loaded.LinkedStageId}, title='{loaded.StoryTitle}'.");
            }
        }

        private static void ApplyStage32Data(StoryNode asset)
        {
            SerializedObject so = new SerializedObject(asset);

            so.FindProperty("storyId").intValue = 332;
            so.FindProperty("storyTitle").stringValue = "거꾸로 시계탑의 마지막 톱니";
            so.FindProperty("learningTip").stringValue = "연쇄를 만들면 점수가 더 크게 올라요.";
            so.FindProperty("linkedStageId").intValue = 32;

            SerializedProperty boss = so.FindProperty("bossDialogues");
            boss.arraySize = 5;
            SetLine(boss, 0, "clock_tower", "거꾸로 시계탑", "째깍째깍, 시간이 거꾸로 춤추고 있구나.");
            SetLine(boss, 1, "dabyeol",     "다별",         "점수를 많이 모으면 시간이 다시 맞춰질 거야.");
            SetLine(boss, 2, "nabyeol",     "나별",         "연쇄를 만들면 별빛 점수가 커져!");
            SetLine(boss, 3, "mochirun",    "모찌룬",       "블록을 잘 정리하면 큰 점수를 만들 수 있어.");
            SetLine(boss, 4, "clock_tower", "거꾸로 시계탑", "그럼 마지막 톱니를 돌려보렴.");

            SerializedProperty start = so.FindProperty("startDialogues");
            start.arraySize = 3;
            SetLine(start, 0, "dabyeol",  "다별",   "이번엔 높은 점수를 모아야 해.");
            SetLine(start, 1, "nabyeol",  "나별",   "연쇄를 노리면 시계가 반짝일 거야!");
            SetLine(start, 2, "mochirun", "모찌룬", "차례대로 정리하면 길이 보여.");

            SerializedProperty clear = so.FindProperty("clearDialogues");
            clear.arraySize = 3;
            SetLine(clear, 0, "clock_tower", "거꾸로 시계탑", "째깍, 째깍. 시간이 다시 부드럽게 흐르는구나.");
            SetLine(clear, 1, "nabyeol",     "나별",         "해냈어! 시계탑이 빛나고 있어!");
            SetLine(clear, 2, "dabyeol",     "다별",         "연쇄와 점수를 아주 잘 활용했어.");

            SerializedProperty fail = so.FindProperty("failDialogues");
            fail.arraySize = 3;
            SetLine(fail, 0, "clock_tower", "거꾸로 시계탑", "아직 톱니가 조금 어긋나 있구나.");
            SetLine(fail, 1, "capymong",    "카피몽",       "괜찮아. 다시 맞춰보면 돼.");
            SetLine(fail, 2, "dabyeol",     "다별",         "다음엔 연쇄 자리를 먼저 찾아보자.");

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ───────── 공통 헬퍼 ─────────

        private static void AppendLines(SerializedProperty list, DialogueRow[] lines)
        {
            if (lines == null || lines.Length == 0) return;
            int startIndex = list.arraySize;
            list.arraySize = startIndex + lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(startIndex + i);
                element.FindPropertyRelative("speakerId").stringValue = lines[i].speakerId;
                element.FindPropertyRelative("speakerName").stringValue = lines[i].speakerName;
                element.FindPropertyRelative("dialogue").stringValue = lines[i].dialogue;
            }
        }

        private static void SetLine(SerializedProperty list, int index, string speakerId, string speakerName, string dialogue)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("speakerId").stringValue = speakerId;
            element.FindPropertyRelative("speakerName").stringValue = speakerName;
            element.FindPropertyRelative("dialogue").stringValue = dialogue;
        }

        private static DialogueRow D(string id, string name, string dialogue)
        {
            return new DialogueRow { speakerId = id, speakerName = name, dialogue = dialogue };
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
