using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Puzzle;
using NabyeolDabyeolDreamPuzzle.Stage;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 방울숲 월드의 1~15 스테이지 StageData ScriptableObject 에셋을 자동으로 생성하거나 업데이트한다.
    /// 모든 필드는 private SerializeField이므로 SerializedObject로 채운다.
    /// 씬에 StageManager가 있으면 stageList / defaultStageData까지 자동 연결한다.
    /// </summary>
    public static class BubbleForestStageGenerator
    {
        private const string OutputFolder = "Assets/_Project/Data/Stages/BubbleForest";
        private const string FileNameFormat = "BubbleForest_Stage_{0:D3}.asset";

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

        private static readonly BlockType[] FourTypes =
        {
            BlockType.DreamBubble,
            BlockType.MoonRiceCake,
            BlockType.InkStar,
            BlockType.WaveCloud
        };

        private static readonly BlockType[] FiveTypes =
        {
            BlockType.DreamBubble,
            BlockType.MoonRiceCake,
            BlockType.InkStar,
            BlockType.WaveCloud,
            BlockType.HeartLight
        };

        [MenuItem("Tools/NabyeolDabyeol/Generate BubbleForest Stages")]
        public static void GenerateAll()
        {
            EnsureFolder(OutputFolder);

            StageRow[] rows = BuildBubbleForestRows();
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

            Debug.Log($"BubbleForestStageGenerator: Generation done. Created {created}, Updated {updated} (total {created + updated}).");

            int validCount = 0;
            for (int i = 1; i <= 15; i++)
            {
                string assetPath = $"{OutputFolder}/{string.Format(FileNameFormat, i)}";
                StageData sd = AssetDatabase.LoadAssetAtPath<StageData>(assetPath);
                if (sd != null && sd.IsValid())
                {
                    validCount++;
                }
                else
                {
                    Debug.LogWarning($"BubbleForestStageGenerator: Stage {i} failed validation at {assetPath}.");
                }
            }
            Debug.Log($"BubbleForestStageGenerator: Validation done. {validCount}/15 stages passed IsValid().");

            TryRegisterToStageManager();
        }

        [MenuItem("Tools/NabyeolDabyeol/Attach BubbleForest Stages to StageManager")]
        public static void AttachOnly()
        {
            TryRegisterToStageManager();
        }

        private static void TryRegisterToStageManager()
        {
            StageManager sm = Object.FindAnyObjectByType<StageManager>();
            if (sm == null)
            {
                Debug.Log("BubbleForestStageGenerator: No StageManager found in active scene. Skipping auto-registration. (Drag the assets manually into stageList / defaultStageData.)");
                return;
            }

            SerializedObject so = new SerializedObject(sm);
            SerializedProperty stageList = so.FindProperty("stageList");
            if (stageList == null)
            {
                Debug.LogWarning("BubbleForestStageGenerator: StageManager.stageList field not found. Field name may have changed.");
                return;
            }

            stageList.arraySize = 15;
            for (int i = 0; i < 15; i++)
            {
                string assetPath = $"{OutputFolder}/{string.Format(FileNameFormat, i + 1)}";
                StageData sd = AssetDatabase.LoadAssetAtPath<StageData>(assetPath);
                stageList.GetArrayElementAtIndex(i).objectReferenceValue = sd;
            }

            SerializedProperty defaultStage = so.FindProperty("defaultStageData");
            if (defaultStage != null)
            {
                string firstPath = $"{OutputFolder}/{string.Format(FileNameFormat, 1)}";
                defaultStage.objectReferenceValue = AssetDatabase.LoadAssetAtPath<StageData>(firstPath);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sm);
            AssetDatabase.SaveAssets();

            Debug.Log("BubbleForestStageGenerator: Attached 15 BubbleForest stages to scene StageManager (defaultStageData = Stage 001).");
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

        private static StageRow[] BuildBubbleForestRows()
        {
            return new StageRow[]
            {
                new StageRow
                {
                    stageId = 1,
                    stageName = "토끼의 첫 점프",
                    description = "깡충깡충 뛰는 토끼처럼 가볍게 점수를 모으는 첫 번째 스테이지입니다.",
                    goalType = StageGoalType.Score,
                    moveLimit = 22,
                    targetScore = 300,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 0,
                    availableBlockTypes = FourTypes,
                    rewardCardId = "card_rabbit_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 2,
                    stageName = "다람쥐 도토리길",
                    description = "도토리를 모으는 다람쥐처럼 노란 달떡 블록을 차근차근 모으는 스테이지입니다.",
                    goalType = StageGoalType.CollectBlock,
                    moveLimit = 22,
                    targetScore = 0,
                    targetBlockType = BlockType.MoonRiceCake,
                    targetBlockCount = 10,
                    availableBlockTypes = FourTypes,
                    rewardCardId = "card_squirrel_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 3,
                    stageName = "고슴도치 반짝가시",
                    description = "반짝이는 가시를 가진 고슴도치처럼 꿈방울을 빛나게 모아 봐요.",
                    goalType = StageGoalType.Score,
                    moveLimit = 20,
                    targetScore = 500,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 0,
                    availableBlockTypes = FourTypes,
                    rewardCardId = "card_hedgehog_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 4,
                    stageName = "여우의 작은 산책",
                    description = "살금살금 걷는 여우와 함께 마음빛 블록을 모으는 스테이지입니다.",
                    goalType = StageGoalType.CollectBlock,
                    moveLimit = 20,
                    targetScore = 0,
                    targetBlockType = BlockType.HeartLight,
                    targetBlockCount = 12,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_fox_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 5,
                    stageName = "사슴의 별빛길",
                    description = "사슴의 별빛길을 따라 점수를 모으는 첫 번째 보스 스테이지입니다.",
                    goalType = StageGoalType.Score,
                    moveLimit = 18,
                    targetScore = 800,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 0,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_deer_001",
                    rewardCardAmount = 2,
                    rewardSparklePieces = 10
                },
                new StageRow
                {
                    stageId = 6,
                    stageName = "너구리 비밀창고",
                    description = "비밀창고를 채우는 너구리처럼 잉크별을 모으는 수집 스테이지입니다.",
                    goalType = StageGoalType.CollectBlock,
                    moveLimit = 20,
                    targetScore = 0,
                    targetBlockType = BlockType.InkStar,
                    targetBlockCount = 14,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_raccoon_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 7,
                    stageName = "부엉이의 달밤",
                    description = "달밤을 지키는 부엉이와 함께 빛나는 점수를 모아 봐요.",
                    goalType = StageGoalType.Score,
                    moveLimit = 18,
                    targetScore = 1100,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 0,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_owl_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 8,
                    stageName = "개구리 연못놀이",
                    description = "연못에서 첨벙대는 개구리처럼 물결구름 블록을 모으는 스테이지입니다.",
                    goalType = StageGoalType.CollectBlock,
                    moveLimit = 18,
                    targetScore = 0,
                    targetBlockType = BlockType.WaveCloud,
                    targetBlockCount = 15,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_frog_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 9,
                    stageName = "오리 물결산책",
                    description = "잔잔한 물결을 따라가는 오리와 함께 점수를 쌓는 스테이지입니다.",
                    goalType = StageGoalType.Score,
                    moveLimit = 16,
                    targetScore = 1300,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 0,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_duck_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 10,
                    stageName = "고양이 별빛쇼",
                    description = "별빛 무대를 펼치는 고양이와 함께 잉크별 블록을 잔뜩 모아 봐요.",
                    goalType = StageGoalType.CollectBlock,
                    moveLimit = 18,
                    targetScore = 0,
                    targetBlockType = BlockType.InkStar,
                    targetBlockCount = 18,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_cat_001",
                    rewardCardAmount = 2,
                    rewardSparklePieces = 10
                },
                new StageRow
                {
                    stageId = 11,
                    stageName = "강아지 꼬리춤",
                    description = "신나게 흔드는 강아지 꼬리처럼 점수를 끌어올리는 스테이지입니다.",
                    goalType = StageGoalType.Score,
                    moveLimit = 18,
                    targetScore = 1500,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 0,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_dog_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 12,
                    stageName = "햄스터 도토리수집",
                    description = "볼주머니에 도토리를 가득 담는 햄스터처럼 달떡을 많이 모아 봐요.",
                    goalType = StageGoalType.CollectBlock,
                    moveLimit = 16,
                    targetScore = 0,
                    targetBlockType = BlockType.MoonRiceCake,
                    targetBlockCount = 20,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_hamster_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 13,
                    stageName = "팬더 대나무숲",
                    description = "대나무 숲을 거니는 팬더처럼 차분하게 점수를 모으는 스테이지입니다.",
                    goalType = StageGoalType.Score,
                    moveLimit = 18,
                    targetScore = 1800,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 0,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_panda_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 14,
                    stageName = "코알라 꿈나무",
                    description = "꿈나무에 매달린 코알라와 함께 마음빛 블록을 가득 모아 봐요.",
                    goalType = StageGoalType.CollectBlock,
                    moveLimit = 16,
                    targetScore = 0,
                    targetBlockType = BlockType.HeartLight,
                    targetBlockCount = 22,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_koala_001",
                    rewardCardAmount = 1,
                    rewardSparklePieces = 5
                },
                new StageRow
                {
                    stageId = 15,
                    stageName = "무지개숲 친구들",
                    description = "모든 친구가 모여 무지개를 그리는 방울숲 마무리 스테이지입니다.",
                    goalType = StageGoalType.Score,
                    moveLimit = 15,
                    targetScore = 2200,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 0,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_rainbow_friend_001",
                    rewardCardAmount = 2,
                    rewardSparklePieces = 10
                }
            };
        }
    }
}
