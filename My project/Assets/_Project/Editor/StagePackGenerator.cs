using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Stage;
using NabyeolDabyeolDreamPuzzle.Puzzle;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 3개 StagePack 자산을 자동 생성/업데이트:
    ///   1. BubbleForest (1~15, 15 stages)
    ///   2. MoonRiceCakeStairs (16~30, 15 stages)
    ///   3. Boss (31~32, 2 stages)
    /// AssetDatabase.FindAssets로 StageData를 스캔해 각 팩 범위에 맞게 자동 등록.
    /// 공통 StageBoardRule(width/height/blocks/moveLimit)도 팩별 기본값으로 함께 설정.
    /// 씬에 StagePackManager가 있으면 database 자동 attach.
    /// </summary>
    public static class StagePackGenerator
    {
        private const string OutputFolder = "Assets/_Project/Data/StagePacks";
        private const string DatabasePath = "Assets/_Project/Data/StagePacks/StagePackDatabase.asset";
        private const string StageRoot = "Assets/_Project/Data/Stages";

        private struct PackRow
        {
            public string fileTag;
            public string packId;
            public string packName;
            public StagePackType packType;
            public string description;
            public int startStageId;
            public int endStageId;
            // 공통 보드 규칙 설정
            public int ruleWidth;
            public int ruleHeight;
            public int ruleMoveLimit;
            public BlockType[] ruleBlocks;
        }

        [MenuItem("Tools/NabyeolDabyeol/Generate Stage Packs")]
        public static void GenerateAll()
        {
            EnsureFolder(OutputFolder);

            List<StageData> allStages = LoadAllStages();
            Debug.Log($"StagePackGenerator: Discovered {allStages.Count} StageData assets under {StageRoot}.");

            PackRow[] rows = BuildRows();
            int created = 0, updated = 0;
            StagePackData[] generated = new StagePackData[rows.Length];

            for (int i = 0; i < rows.Length; i++)
            {
                PackRow row = rows[i];
                string assetPath = $"{OutputFolder}/StagePack_{row.fileTag}.asset";
                StagePackData existing = AssetDatabase.LoadAssetAtPath<StagePackData>(assetPath);
                bool isNew = existing == null;
                StagePackData asset = existing != null ? existing : ScriptableObject.CreateInstance<StagePackData>();

                ApplyRow(asset, row, allStages);

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

            StagePackDatabase db = AssetDatabase.LoadAssetAtPath<StagePackDatabase>(DatabasePath);
            bool dbIsNew = db == null;
            if (db == null) db = ScriptableObject.CreateInstance<StagePackDatabase>();

            SerializedObject dbSo = new SerializedObject(db);
            SerializedProperty packsList = dbSo.FindProperty("stagePacks");
            packsList.arraySize = generated.Length;
            for (int i = 0; i < generated.Length; i++)
            {
                packsList.GetArrayElementAtIndex(i).objectReferenceValue = generated[i];
            }
            dbSo.ApplyModifiedPropertiesWithoutUndo();

            if (dbIsNew) AssetDatabase.CreateAsset(db, DatabasePath);
            else EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"StagePackGenerator: Packs Created {created}, Updated {updated} (total {created + updated}).");
            Debug.Log($"StagePackGenerator: Database {(dbIsNew ? "created" : "updated")} at {DatabasePath}.");

            StagePackDatabase loaded = AssetDatabase.LoadAssetAtPath<StagePackDatabase>(DatabasePath);
            if (loaded != null)
            {
                bool ok = loaded.ValidatePacks();
                Debug.Log($"StagePackGenerator: ValidatePacks = {ok} (count={loaded.Count}).");
            }

            // 씬의 StagePackManager 자동 attach
            StagePackManager mgr = Object.FindAnyObjectByType<StagePackManager>();
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
                    Debug.Log("StagePackGenerator: Attached database to scene StagePackManager.");
                }
            }
            else
            {
                Debug.Log("StagePackGenerator: No StagePackManager found in active scene. Drag the asset manually into Database slot.");
            }
        }

        private static List<StageData> LoadAllStages()
        {
            List<StageData> result = new List<StageData>();
            string[] roots = AssetDatabase.IsValidFolder(StageRoot)
                ? new[] { StageRoot }
                : new[] { "Assets" };
            string[] guids = AssetDatabase.FindAssets("t:StageData", roots);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                StageData s = AssetDatabase.LoadAssetAtPath<StageData>(path);
                if (s != null) result.Add(s);
            }
            return result;
        }

        private static void ApplyRow(StagePackData asset, PackRow row, List<StageData> allStages)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("packId").stringValue = row.packId;
            so.FindProperty("packName").stringValue = row.packName;
            so.FindProperty("packType").enumValueIndex = (int)row.packType;
            so.FindProperty("description").stringValue = row.description;
            so.FindProperty("startStageId").intValue = row.startStageId;
            so.FindProperty("endStageId").intValue = row.endStageId;

            // 범위에 맞는 StageData 자동 등록
            SerializedProperty stagesProp = so.FindProperty("stages");
            List<StageData> matched = new List<StageData>();
            for (int i = 0; i < allStages.Count; i++)
            {
                StageData s = allStages[i];
                if (s == null) continue;
                if (s.StageId >= row.startStageId && s.StageId <= row.endStageId)
                {
                    matched.Add(s);
                }
            }
            matched.Sort((a, b) => a.StageId.CompareTo(b.StageId));
            stagesProp.arraySize = matched.Count;
            for (int i = 0; i < matched.Count; i++)
            {
                stagesProp.GetArrayElementAtIndex(i).objectReferenceValue = matched[i];
            }

            // 공통 보드 규칙 설정
            SerializedProperty rule = so.FindProperty("defaultBoardRule");
            rule.FindPropertyRelative("defaultBoardWidth").intValue = row.ruleWidth;
            rule.FindPropertyRelative("defaultBoardHeight").intValue = row.ruleHeight;
            rule.FindPropertyRelative("defaultMoveLimit").intValue = row.ruleMoveLimit;
            rule.FindPropertyRelative("allowCascade").boolValue = true;
            rule.FindPropertyRelative("allowSkills").boolValue = true;
            SerializedProperty blocksProp = rule.FindPropertyRelative("defaultAvailableBlockTypes");
            blocksProp.arraySize = row.ruleBlocks.Length;
            for (int i = 0; i < row.ruleBlocks.Length; i++)
            {
                blocksProp.GetArrayElementAtIndex(i).enumValueIndex = (int)row.ruleBlocks[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            asset.name = "StagePack_" + row.fileTag;
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
            BlockType[] fourTypes = {
                BlockType.DreamBubble, BlockType.MoonRiceCake, BlockType.InkStar, BlockType.WaveCloud
            };
            BlockType[] fiveTypes = {
                BlockType.DreamBubble, BlockType.MoonRiceCake, BlockType.InkStar, BlockType.WaveCloud, BlockType.HeartLight
            };

            return new PackRow[]
            {
                new PackRow {
                    fileTag = "BubbleForest", packId = "pack_bubble_forest", packName = "방울숲",
                    packType = StagePackType.Region,
                    description = "방울숲 1~15 스테이지를 묶은 월드 팩입니다.",
                    startStageId = 1, endStageId = 15,
                    ruleWidth = 8, ruleHeight = 8, ruleMoveLimit = 22,
                    ruleBlocks = fourTypes
                },
                new PackRow {
                    fileTag = "MoonRiceCakeStairs", packId = "pack_moon_ricecake_stairs", packName = "달떡계단",
                    packType = StagePackType.Region,
                    description = "달떡계단 16~30 스테이지를 묶은 월드 팩입니다.",
                    startStageId = 16, endStageId = 30,
                    ruleWidth = 8, ruleHeight = 8, ruleMoveLimit = 20,
                    ruleBlocks = fiveTypes
                },
                new PackRow {
                    fileTag = "Boss", packId = "pack_boss_stages", packName = "보스 스테이지",
                    packType = StagePackType.Boss,
                    description = "기억나무·거꾸로 시계탑 보스 스테이지를 묶은 팩입니다.",
                    startStageId = 31, endStageId = 32,
                    ruleWidth = 8, ruleHeight = 8, ruleMoveLimit = 17,
                    ruleBlocks = fiveTypes
                }
            };
        }
    }
}
