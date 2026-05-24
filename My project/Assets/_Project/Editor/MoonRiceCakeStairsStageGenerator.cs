using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Puzzle;
using NabyeolDabyeolDreamPuzzle.Stage;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 달떡계단 월드의 16~30 스테이지 StageData ScriptableObject 에셋을 자동 생성/업데이트한다.
    /// 모든 필드는 private SerializeField이므로 SerializedObject로 채운다.
    /// 씬에 StageManager가 있으면 방울숲(1~15)까지 포함해 stageList를 stageId 순으로 일괄 등록한다.
    /// </summary>
    public static class MoonRiceCakeStairsStageGenerator
    {
        private const string OutputFolder = "Assets/_Project/Data/Stages/MoonRiceCakeStairs";
        private const string FileNameFormat = "MoonRiceCakeStairs_Stage_{0:D3}.asset";
        private const string StagesRoot = "Assets/_Project/Data/Stages";

        private struct StageRow
        {
            public int stageId;
            public string stageName;
            public string description;
            public StageGoalType goalType;
            public int moveLimit;
            public int targetScore;
            public BlockType targetBlockType;
            public int targetBlockCount;
            public BlockType[] availableBlockTypes;
            public string rewardCardId;
            public int rewardCardAmount;
            public int rewardSparklePieces;
        }

        private static readonly BlockType[] FiveTypes =
        {
            BlockType.DreamBubble,
            BlockType.MoonRiceCake,
            BlockType.InkStar,
            BlockType.WaveCloud,
            BlockType.HeartLight
        };

        [MenuItem("Tools/NabyeolDabyeol/Generate MoonRiceCakeStairs Stages")]
        public static void GenerateAll()
        {
            EnsureFolder(OutputFolder);

            StageRow[] rows = BuildMoonRiceCakeStairsRows();
            int created = 0;
            int updated = 0;

            foreach (StageRow row in rows)
            {
                string assetPath = $"{OutputFolder}/{string.Format(FileNameFormat, row.stageId)}";
                StageData existing = AssetDatabase.LoadAssetAtPath<StageData>(assetPath);
                bool isNew = existing == null;

                StageData asset = isNew ? ScriptableObject.CreateInstance<StageData>() : existing;
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
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"MoonRiceCakeStairsStageGenerator: Generation done. Created {created}, Updated {updated} (total {created + updated}).");

            int validCount = 0;
            for (int i = 16; i <= 30; i++)
            {
                string assetPath = $"{OutputFolder}/{string.Format(FileNameFormat, i)}";
                StageData sd = AssetDatabase.LoadAssetAtPath<StageData>(assetPath);
                if (sd != null && sd.IsValid())
                {
                    validCount++;
                }
                else
                {
                    Debug.LogWarning($"MoonRiceCakeStairsStageGenerator: Stage {i} failed validation at {assetPath}.");
                }
            }
            Debug.Log($"MoonRiceCakeStairsStageGenerator: Validation done. {validCount}/15 stages passed IsValid().");

            AttachAllStagesToManager();
        }

        [MenuItem("Tools/NabyeolDabyeol/Attach All Stages to StageManager")]
        public static void AttachAllStagesToManager()
        {
            StageManager sm = Object.FindAnyObjectByType<StageManager>();
            if (sm == null)
            {
                Debug.Log("MoonRiceCakeStairsStageGenerator: No StageManager found in active scene. Skipping auto-registration.");
                return;
            }

            List<StageData> merged = LoadAllStagesSorted();
            if (merged.Count == 0)
            {
                Debug.LogWarning("MoonRiceCakeStairsStageGenerator: No StageData found under " + StagesRoot);
                return;
            }

            SerializedObject so = new SerializedObject(sm);
            SerializedProperty stageList = so.FindProperty("stageList");
            if (stageList == null)
            {
                Debug.LogWarning("MoonRiceCakeStairsStageGenerator: StageManager.stageList field not found.");
                return;
            }

            stageList.arraySize = merged.Count;
            for (int i = 0; i < merged.Count; i++)
            {
                stageList.GetArrayElementAtIndex(i).objectReferenceValue = merged[i];
            }

            SerializedProperty defaultStage = so.FindProperty("defaultStageData");
            if (defaultStage != null && defaultStage.objectReferenceValue == null)
            {
                defaultStage.objectReferenceValue = merged[0];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sm);
            AssetDatabase.SaveAssets();

            HashSet<int> seen = new HashSet<int>();
            int duplicates = 0;
            for (int i = 0; i < merged.Count; i++)
            {
                if (!seen.Add(merged[i].StageId))
                {
                    duplicates++;
                    Debug.LogWarning($"MoonRiceCakeStairsStageGenerator: Duplicate stageId {merged[i].StageId} detected at '{merged[i].name}'.");
                }
            }

            Debug.Log($"MoonRiceCakeStairsStageGenerator: Attached {merged.Count} stages to StageManager (duplicates: {duplicates}).");
        }

        private static List<StageData> LoadAllStagesSorted()
        {
            string[] guids = AssetDatabase.FindAssets("t:StageData", new[] { StagesRoot });
            List<StageData> result = new List<StageData>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                StageData sd = AssetDatabase.LoadAssetAtPath<StageData>(path);
                if (sd != null)
                {
                    result.Add(sd);
                }
            }
            result.Sort((a, b) => a.StageId.CompareTo(b.StageId));
            return result;
        }

        private static void ApplyRow(StageData asset, StageRow row)
        {
            SerializedObject so = new SerializedObject(asset);

            so.FindProperty("stageId").intValue = row.stageId;
            so.FindProperty("stageName").stringValue = row.stageName;
            so.FindProperty("description").stringValue = row.description;
            so.FindProperty("goalType").enumValueIndex = (int)row.goalType;
            so.FindProperty("targetScore").intValue = row.targetScore;
            so.FindProperty("targetBlockType").enumValueIndex = (int)row.targetBlockType;
            so.FindProperty("targetBlockCount").intValue = row.targetBlockCount;
            so.FindProperty("moveLimit").intValue = row.moveLimit;
            so.FindProperty("boardWidth").intValue = 8;
            so.FindProperty("boardHeight").intValue = 8;

            SerializedProperty list = so.FindProperty("availableBlockTypes");
            list.arraySize = row.availableBlockTypes.Length;
            for (int i = 0; i < row.availableBlockTypes.Length; i++)
            {
                list.GetArrayElementAtIndex(i).enumValueIndex = (int)row.availableBlockTypes[i];
            }

            so.FindProperty("rewardCardId").stringValue = row.rewardCardId;
            so.FindProperty("rewardCardAmount").intValue = row.rewardCardAmount;
            so.FindProperty("rewardSparklePieces").intValue = row.rewardSparklePieces;

            so.ApplyModifiedPropertiesWithoutUndo();

            asset.name = System.IO.Path.GetFileNameWithoutExtension(string.Format(FileNameFormat, row.stageId));
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

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

        // BlockType enum의 일반 스폰 가능 타입은 5종(DreamBubble~HeartLight)이므로
        // 16~30 모두 5종을 사용한다. 후반 6종 사양 의도는 enum 상한에 의해 5로 보정된다.
        private static StageRow[] BuildMoonRiceCakeStairsRows()
        {
            return new StageRow[]
            {
                new StageRow
                {
                    stageId = 16,
                    stageName = "하나둘 달계단",
                    description = "하나, 둘 순서대로 계단을 오르듯 목표 점수를 차근차근 모으는 스테이지입니다.",
                    goalType = StageGoalType.Score,
                    moveLimit = 23,
                    targetScore = 700,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 0,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_moon_step_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 17,
                    stageName = "셋째 떡발판",
                    description = "셋째 발판을 또박또박 짚듯 정확히 점수를 모아 가는 스테이지입니다.",
                    goalType = StageGoalType.Score,
                    moveLimit = 22,
                    targetScore = 800,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 0,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_moon_ricecake_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 18,
                    stageName = "차근차근 줄세기",
                    description = "줄을 세듯 달떡 블록을 차근차근 모으는 수집 스테이지입니다.",
                    goalType = StageGoalType.CollectBlock,
                    moveLimit = 22,
                    targetScore = 0,
                    targetBlockType = BlockType.MoonRiceCake,
                    targetBlockCount = 12,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_moon_count_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 19,
                    stageName = "동그란 별 다섯",
                    description = "동그란 별 다섯 개처럼 빛나는 점수를 모아 가는 스테이지입니다.",
                    goalType = StageGoalType.Score,
                    moveLimit = 20,
                    targetScore = 1000,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 0,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_number_star_005",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 20,
                    stageName = "여섯 떡 달밤",
                    description = "달밤의 떡 여섯 개를 모으며 첫 구간을 마무리하는 스테이지입니다.",
                    goalType = StageGoalType.CollectBlock,
                    moveLimit = 21,
                    targetScore = 0,
                    targetBlockType = BlockType.MoonRiceCake,
                    targetBlockCount = 15,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_moon_rabbit_count_001",
                    rewardCardAmount = 2,
                    rewardSparklePieces = 10
                },
                new StageRow
                {
                    stageId = 21,
                    stageName = "짝수 달빛길",
                    description = "짝수 달빛을 따라 균형 있게 점수를 모으는 스테이지입니다.",
                    goalType = StageGoalType.Score,
                    moveLimit = 21,
                    targetScore = 1200,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 0,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_even_moonlight_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 22,
                    stageName = "홀수 별빛돌",
                    description = "홀수 별빛돌을 또박또박 모으며 잉크별을 수집하는 스테이지입니다.",
                    goalType = StageGoalType.CollectBlock,
                    moveLimit = 20,
                    targetScore = 0,
                    targetBlockType = BlockType.InkStar,
                    targetBlockCount = 15,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_odd_starstone_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 23,
                    stageName = "더하기 한걸음",
                    description = "한 걸음 더하기처럼 점수를 한 칸씩 올려 가는 스테이지입니다.",
                    goalType = StageGoalType.Score,
                    moveLimit = 19,
                    targetScore = 1400,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 0,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_plus_step_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 24,
                    stageName = "두걸음 더하기",
                    description = "두 걸음 더하기처럼 꿈방울을 두 배로 모으는 수집 스테이지입니다.",
                    goalType = StageGoalType.CollectBlock,
                    moveLimit = 19,
                    targetScore = 0,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 16,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_plus_two_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 25,
                    stageName = "세걸음 별달기",
                    description = "세 걸음을 합쳐 별빛까지 닿는 구간 마무리 스테이지입니다.",
                    goalType = StageGoalType.Score,
                    moveLimit = 18,
                    targetScore = 1700,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 0,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_plus_three_001",
                    rewardCardAmount = 2,
                    rewardSparklePieces = 10
                },
                new StageRow
                {
                    stageId = 26,
                    stageName = "규칙 찾는 떡길",
                    description = "떡길의 규칙을 찾듯 같은 잉크별을 패턴으로 모으는 스테이지입니다.",
                    goalType = StageGoalType.CollectBlock,
                    moveLimit = 19,
                    targetScore = 0,
                    targetBlockType = BlockType.InkStar,
                    targetBlockCount = 18,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_pattern_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 27,
                    stageName = "같은 색 모으기",
                    description = "같은 색 꿈방울끼리 모아 가는 패턴 학습 스테이지입니다.",
                    goalType = StageGoalType.CollectBlock,
                    moveLimit = 18,
                    targetScore = 0,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 17,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_same_color_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 28,
                    stageName = "점점 커지는 계단",
                    description = "점점 커지는 계단처럼 마음빛을 늘려 가며 모으는 스테이지입니다.",
                    goalType = StageGoalType.CollectBlock,
                    moveLimit = 17,
                    targetScore = 0,
                    targetBlockType = BlockType.HeartLight,
                    targetBlockCount = 20,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_growing_step_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 29,
                    stageName = "별빛 더하기 합",
                    description = "별빛을 모두 더한 합처럼 점수를 한껏 끌어올리는 스테이지입니다.",
                    goalType = StageGoalType.Score,
                    moveLimit = 16,
                    targetScore = 2300,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 0,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_sum_starlight_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 30,
                    stageName = "달떡계단 마무리",
                    description = "달떡계단의 마지막 한 칸, 모든 친구가 함께하는 마무리 스테이지입니다.",
                    goalType = StageGoalType.Score,
                    moveLimit = 15,
                    targetScore = 2600,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 0,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_moon_finale_001",
                    rewardCardAmount = 2,
                    rewardSparklePieces = 10
                }
            };
        }
    }
}
