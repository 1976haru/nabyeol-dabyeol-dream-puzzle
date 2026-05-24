using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Region;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 방울숲(1~15)과 달떡계단(16~30) RestoreRegionData 자산을 자동 생성/업데이트.
    /// 씬에 RegionRestoreManager가 있으면 regions 리스트에 자동 attach.
    /// </summary>
    public static class RegionRestoreGenerator
    {
        private const string RegionFolder = "Assets/_Project/Data/Region";
        private const string BubbleForestPath = "Assets/_Project/Data/Region/Region_BubbleForest.asset";
        private const string MoonRiceCakeStairsPath = "Assets/_Project/Data/Region/Region_MoonRiceCakeStairs.asset";

        private struct RegionRow
        {
            public string assetPath;
            public string regionId;
            public string regionName;
            public int startStageId;
            public int endStageId;
        }

        [MenuItem("Tools/NabyeolDabyeol/Generate Region Restore Data")]
        public static void GenerateAll()
        {
            EnsureFolder(RegionFolder);

            RegionRow[] rows = new RegionRow[]
            {
                new RegionRow {
                    assetPath = BubbleForestPath,
                    regionId = "bubble_forest",
                    regionName = "방울숲",
                    startStageId = 1, endStageId = 15
                },
                new RegionRow {
                    assetPath = MoonRiceCakeStairsPath,
                    regionId = "moon_ricecake_stairs",
                    regionName = "달떡계단",
                    startStageId = 16, endStageId = 30
                }
            };

            int created = 0, updated = 0;
            RestoreRegionData[] generated = new RestoreRegionData[rows.Length];

            for (int i = 0; i < rows.Length; i++)
            {
                RegionRow row = rows[i];
                RestoreRegionData existing = AssetDatabase.LoadAssetAtPath<RestoreRegionData>(row.assetPath);
                bool isNew = existing == null;
                RestoreRegionData asset = existing != null ? existing : ScriptableObject.CreateInstance<RestoreRegionData>();

                ApplyRow(asset, row);

                if (isNew)
                {
                    AssetDatabase.CreateAsset(asset, row.assetPath);
                    created++;
                }
                else
                {
                    EditorUtility.SetDirty(asset);
                    updated++;
                }
                generated[i] = asset;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"RegionRestoreGenerator: Created {created}, Updated {updated} (total {created + updated}).");

            int validCount = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                if (generated[i] != null && generated[i].IsValid()) validCount++;
            }
            Debug.Log($"RegionRestoreGenerator: Validation done. {validCount}/{rows.Length} regions passed IsValid().");

            // 씬에 RegionRestoreManager가 있으면 자동 attach
            RegionRestoreManager manager = Object.FindAnyObjectByType<RegionRestoreManager>();
            if (manager != null)
            {
                SerializedObject so = new SerializedObject(manager);
                SerializedProperty regionsList = so.FindProperty("regions");
                regionsList.arraySize = generated.Length;
                for (int i = 0; i < generated.Length; i++)
                {
                    regionsList.GetArrayElementAtIndex(i).objectReferenceValue = generated[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(manager);
                AssetDatabase.SaveAssets();
                Debug.Log("RegionRestoreGenerator: Attached 2 regions to scene RegionRestoreManager.");
            }
            else
            {
                Debug.Log("RegionRestoreGenerator: No RegionRestoreManager found in active scene. Drag the assets manually into Regions list.");
            }
        }

        private static void ApplyRow(RestoreRegionData asset, RegionRow row)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("regionId").stringValue = row.regionId;
            so.FindProperty("regionName").stringValue = row.regionName;
            so.FindProperty("startStageId").intValue = row.startStageId;
            so.FindProperty("endStageId").intValue = row.endStageId;
            // 기본 desc는 SO 기본값 유지 ("아직 잠들어 있어요." 등)
            // restore*Sprite는 비워둔다. TODO: 디자이너가 일러스트 연결.
            so.ApplyModifiedPropertiesWithoutUndo();
            asset.name = System.IO.Path.GetFileNameWithoutExtension(row.assetPath);
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
