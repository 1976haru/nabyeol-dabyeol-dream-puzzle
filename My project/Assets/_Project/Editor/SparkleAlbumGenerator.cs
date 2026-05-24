using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Album;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 반짝 앨범 샘플 데이터 자동 생성:
    /// - 방울숲 1~5 AlbumPageData 5개
    /// - AlbumDatabase 자산 1개 (5개 페이지 등록)
    /// TODO: Extend to all 30+ stages once artwork is ready.
    /// </summary>
    public static class SparkleAlbumGenerator
    {
        private const string AlbumFolder = "Assets/_Project/Data/Album";
        private const string DatabasePath = "Assets/_Project/Data/Album/AlbumDatabase.asset";
        private const string PageFileNameFormat = "AlbumPage_{0:D3}_{1}";

        private struct PageRow
        {
            public int pageId;
            public int linkedStageId;
            public string worldName;
            public string pageTitle;
            public string pageDescription;
            public string linkedCardId;
            public string fileTag;
        }

        [MenuItem("Tools/NabyeolDabyeol/Generate Sparkle Album Data")]
        public static void GenerateAll()
        {
            EnsureFolder(AlbumFolder);

            PageRow[] rows = BuildRows();
            int created = 0, updated = 0;
            AlbumPageData[] generatedPages = new AlbumPageData[rows.Length];

            for (int i = 0; i < rows.Length; i++)
            {
                PageRow row = rows[i];
                string assetPath = $"{AlbumFolder}/{string.Format(PageFileNameFormat, row.pageId, row.fileTag)}.asset";
                AlbumPageData existing = AssetDatabase.LoadAssetAtPath<AlbumPageData>(assetPath);
                bool isNew = existing == null;
                AlbumPageData asset = existing != null ? existing : ScriptableObject.CreateInstance<AlbumPageData>();

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
                generatedPages[i] = asset;
            }

            // Database
            AlbumDatabase db = AssetDatabase.LoadAssetAtPath<AlbumDatabase>(DatabasePath);
            bool dbIsNew = db == null;
            if (db == null) db = ScriptableObject.CreateInstance<AlbumDatabase>();

            SerializedObject dbSo = new SerializedObject(db);
            SerializedProperty pagesList = dbSo.FindProperty("pages");
            pagesList.arraySize = generatedPages.Length;
            for (int i = 0; i < generatedPages.Length; i++)
            {
                pagesList.GetArrayElementAtIndex(i).objectReferenceValue = generatedPages[i];
            }
            dbSo.ApplyModifiedPropertiesWithoutUndo();

            if (dbIsNew) AssetDatabase.CreateAsset(db, DatabasePath);
            else EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"SparkleAlbumGenerator: Pages Created {created}, Updated {updated} (total {created + updated}).");
            Debug.Log($"SparkleAlbumGenerator: Database {(dbIsNew ? "created" : "updated")} at {DatabasePath}.");

            int validCount = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                if (generatedPages[i] != null && generatedPages[i].IsValid()) validCount++;
            }
            Debug.Log($"SparkleAlbumGenerator: Validation done. {validCount}/{rows.Length} pages passed IsValid().");

            AlbumDatabase loaded = AssetDatabase.LoadAssetAtPath<AlbumDatabase>(DatabasePath);
            if (loaded != null)
            {
                bool ok = loaded.ValidatePages();
                Debug.Log($"SparkleAlbumGenerator: Database ValidatePages = {ok} (count={loaded.Count}).");
            }
        }

        private static void ApplyRow(AlbumPageData asset, PageRow row)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("pageId").intValue = row.pageId;
            so.FindProperty("linkedStageId").intValue = row.linkedStageId;
            so.FindProperty("worldName").stringValue = row.worldName;
            so.FindProperty("pageTitle").stringValue = row.pageTitle;
            so.FindProperty("pageDescription").stringValue = row.pageDescription;
            so.FindProperty("linkedCardId").stringValue = row.linkedCardId;
            // pageImage는 비워둔다. TODO: Connect artwork sprite later.
            so.ApplyModifiedPropertiesWithoutUndo();

            asset.name = string.Format(PageFileNameFormat, row.pageId, row.fileTag);
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

        private static PageRow[] BuildRows()
        {
            return new PageRow[]
            {
                new PageRow {
                    pageId = 1, linkedStageId = 1, worldName = "방울숲", fileTag = "Rabbit",
                    pageTitle = "토끼의 첫 점프",
                    pageDescription = "토끼처럼 첫 숲길을 폴짝 넘어간 장면이에요.",
                    linkedCardId = "card_rabbit_001"
                },
                new PageRow {
                    pageId = 2, linkedStageId = 2, worldName = "방울숲", fileTag = "Squirrel",
                    pageTitle = "다람쥐 도토리길",
                    pageDescription = "다람쥐와 함께 도토리길을 지나간 장면이에요.",
                    linkedCardId = "card_squirrel_001"
                },
                new PageRow {
                    pageId = 3, linkedStageId = 3, worldName = "방울숲", fileTag = "Hedgehog",
                    pageTitle = "고슴도치 반짝길",
                    pageDescription = "반짝이는 고슴도치 길을 찾은 장면이에요.",
                    linkedCardId = "card_hedgehog_001"
                },
                new PageRow {
                    pageId = 4, linkedStageId = 4, worldName = "방울숲", fileTag = "Fox",
                    pageTitle = "여우의 살금걸음",
                    pageDescription = "여우처럼 조심조심 숲길을 지나간 장면이에요.",
                    linkedCardId = "card_fox_001"
                },
                new PageRow {
                    pageId = 5, linkedStageId = 5, worldName = "방울숲", fileTag = "Deer",
                    pageTitle = "사슴의 숲길",
                    pageDescription = "사슴이 안내한 숲길을 환하게 밝힌 장면이에요.",
                    linkedCardId = "card_deer_001"
                }
            };
        }
    }
}
