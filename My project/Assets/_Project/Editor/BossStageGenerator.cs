using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Puzzle;
using NabyeolDabyeolDreamPuzzle.Stage;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 보스형 퍼즐 StageData asset 2개(기억나무·거꾸로 시계탑)를 자동 생성/업데이트한다.
    /// 모든 필드는 private SerializeField이므로 SerializedObject로 채운다.
    /// 생성 후 MoonRiceCakeStairsStageGenerator.AttachAllStagesToManager를 호출해
    /// 1~30 일반 스테이지와 31~32 보스 스테이지를 stageList에 일괄 등록한다.
    /// </summary>
    public static class BossStageGenerator
    {
        private const string OutputFolder = "Assets/_Project/Data/Stages/Boss";

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
            public bool isBossStage;
            public BossStageType bossStageType;
            public string specialGoalDescription;
            public string fileName;
        }

        private static readonly BlockType[] FiveTypes =
        {
            BlockType.DreamBubble,
            BlockType.MoonRiceCake,
            BlockType.InkStar,
            BlockType.WaveCloud,
            BlockType.HeartLight
        };

        [MenuItem("Tools/NabyeolDabyeol/Generate Boss Stages")]
        public static void GenerateAll()
        {
            EnsureFolder(OutputFolder);

            StageRow[] rows = BuildBossRows();
            int created = 0;
            int updated = 0;

            foreach (StageRow row in rows)
            {
                string assetPath = $"{OutputFolder}/{row.fileName}.asset";
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

            Debug.Log($"BossStageGenerator: Generation done. Created {created}, Updated {updated} (total {created + updated}).");

            int validCount = 0;
            foreach (StageRow row in rows)
            {
                string assetPath = $"{OutputFolder}/{row.fileName}.asset";
                StageData sd = AssetDatabase.LoadAssetAtPath<StageData>(assetPath);
                if (sd != null && sd.IsValid())
                {
                    validCount++;
                }
                else
                {
                    Debug.LogWarning($"BossStageGenerator: Stage {row.stageId} failed validation at {assetPath}.");
                }
            }
            Debug.Log($"BossStageGenerator: Validation done. {validCount}/{rows.Length} boss stages passed IsValid().");

            MoonRiceCakeStairsStageGenerator.AttachAllStagesToManager();
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

            so.FindProperty("isBossStage").boolValue = row.isBossStage;
            so.FindProperty("bossStageType").enumValueIndex = (int)row.bossStageType;
            so.FindProperty("specialGoalDescription").stringValue = row.specialGoalDescription;

            so.FindProperty("rewardCardId").stringValue = row.rewardCardId;
            so.FindProperty("rewardCardAmount").intValue = row.rewardCardAmount;
            so.FindProperty("rewardSparklePieces").intValue = row.rewardSparklePieces;

            so.ApplyModifiedPropertiesWithoutUndo();

            asset.name = row.fileName;
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

        private static StageRow[] BuildBossRows()
        {
            return new StageRow[]
            {
                new StageRow
                {
                    stageId = 31,
                    stageName = "기억나무",
                    description = "오래된 기억나무가 잃어버린 기억 조각을 되찾을 수 있도록 초록 기억 블록을 모으는 보스 스테이지입니다.",
                    goalType = StageGoalType.CollectBlock,
                    moveLimit = 18,
                    targetScore = 2500,
                    targetBlockType = BlockType.MoonRiceCake,
                    targetBlockCount = 25,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_boss_memory_tree_001",
                    rewardCardAmount = 2,
                    rewardSparklePieces = 15,
                    isBossStage = true,
                    bossStageType = BossStageType.MemoryTree,
                    specialGoalDescription = "초록 기억 블록 25개를 모아 기억나무를 깨우세요.",
                    fileName = "Boss_Stage_031_MemoryTree"
                },
                new StageRow
                {
                    stageId = 32,
                    stageName = "거꾸로 시계탑",
                    description = "시간이 거꾸로 흐르는 시계탑에서 연쇄 보너스를 활용해 높은 점수를 달성하는 보스 스테이지입니다.",
                    goalType = StageGoalType.Score,
                    moveLimit = 16,
                    targetScore = 3200,
                    targetBlockType = BlockType.DreamBubble,
                    targetBlockCount = 0,
                    availableBlockTypes = FiveTypes,
                    rewardCardId = "card_boss_reverse_clocktower_001",
                    rewardCardAmount = 2,
                    rewardSparklePieces = 15,
                    isBossStage = true,
                    bossStageType = BossStageType.ReverseClockTower,
                    specialGoalDescription = "제한 이동 안에 3,200점을 달성해 시계탑의 시간을 되돌리세요.",
                    fileName = "Boss_Stage_032_ReverseClockTower"
                }
            };
        }
    }
}
