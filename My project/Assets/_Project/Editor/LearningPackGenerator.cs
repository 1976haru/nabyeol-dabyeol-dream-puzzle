using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Cards;
using NabyeolDabyeolDreamPuzzle.Learning;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 3개 LearningPack 자산을 자동 생성/업데이트:
    ///   1. BubbleForest Animals (1~15, 15장)
    ///   2. MoonRiceCakeStairs Numbers (16~28, 13장)
    ///   3. Boss Lessons (31~32, 2장)
    /// AssetDatabase.FindAssets로 KnowledgeCardData를 스캔해 각 팩 범위에 맞게 자동 등록.
    /// 학습 목표 샘플도 함께 포함. 씬에 LearningPackManager가 있으면 database 자동 attach.
    /// TODO: Add LearningPack JSON import/export.
    /// TODO: Add downloadable learning packs.
    /// TODO: Add language-specific LearningPack variants.
    /// </summary>
    public static class LearningPackGenerator
    {
        private const string OutputFolder = "Assets/_Project/Data/LearningPacks";
        private const string DatabasePath = "Assets/_Project/Data/LearningPacks/LearningPackDatabase.asset";
        private const string KnowledgeRoot = "Assets/_Project/Data/Cards/Knowledge";

        private struct GoalRow
        {
            public string goalId;
            public string goalTitle;
            public string goalDescription;
            public LearningGoalType goalType;
            public int linkedStageId;
            public string linkedCardId;
        }

        private struct PackRow
        {
            public string fileTag;
            public string packId;
            public string packName;
            public LearningPackType packType;
            public string description;
            public int startStageId;
            public int endStageId;
            public GoalRow[] goals;
        }

        [MenuItem("Tools/NabyeolDabyeol/Generate Learning Packs")]
        public static void GenerateAll()
        {
            EnsureFolder(OutputFolder);

            List<KnowledgeCardData> allCards = LoadAllCards();
            Debug.Log($"LearningPackGenerator: Discovered {allCards.Count} KnowledgeCardData assets under {KnowledgeRoot}.");

            PackRow[] rows = BuildRows();
            int created = 0, updated = 0;
            LearningPackData[] generated = new LearningPackData[rows.Length];

            for (int i = 0; i < rows.Length; i++)
            {
                PackRow row = rows[i];
                string assetPath = $"{OutputFolder}/LearningPack_{row.fileTag}.asset";
                LearningPackData existing = AssetDatabase.LoadAssetAtPath<LearningPackData>(assetPath);
                bool isNew = existing == null;
                LearningPackData asset = existing != null ? existing : ScriptableObject.CreateInstance<LearningPackData>();

                ApplyRow(asset, row, allCards);

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

            LearningPackDatabase db = AssetDatabase.LoadAssetAtPath<LearningPackDatabase>(DatabasePath);
            bool dbIsNew = db == null;
            if (db == null) db = ScriptableObject.CreateInstance<LearningPackDatabase>();

            SerializedObject dbSo = new SerializedObject(db);
            SerializedProperty packsList = dbSo.FindProperty("learningPacks");
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

            Debug.Log($"LearningPackGenerator: Packs Created {created}, Updated {updated} (total {created + updated}).");
            Debug.Log($"LearningPackGenerator: Database {(dbIsNew ? "created" : "updated")} at {DatabasePath}.");

            LearningPackDatabase loaded = AssetDatabase.LoadAssetAtPath<LearningPackDatabase>(DatabasePath);
            if (loaded != null)
            {
                bool ok = loaded.ValidatePacks();
                Debug.Log($"LearningPackGenerator: ValidatePacks = {ok} (count={loaded.Count}).");
            }

            // 씬의 LearningPackManager 자동 attach
            LearningPackManager mgr = Object.FindAnyObjectByType<LearningPackManager>();
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
                    Debug.Log("LearningPackGenerator: Attached database to scene LearningPackManager.");
                }
            }
            else
            {
                Debug.Log("LearningPackGenerator: No LearningPackManager found in active scene. Drag the asset manually into Database slot.");
            }
        }

        private static List<KnowledgeCardData> LoadAllCards()
        {
            List<KnowledgeCardData> result = new List<KnowledgeCardData>();
            string[] roots = AssetDatabase.IsValidFolder(KnowledgeRoot)
                ? new[] { KnowledgeRoot }
                : new[] { "Assets" };
            string[] guids = AssetDatabase.FindAssets("t:KnowledgeCardData", roots);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                KnowledgeCardData c = AssetDatabase.LoadAssetAtPath<KnowledgeCardData>(path);
                if (c != null) result.Add(c);
            }
            return result;
        }

        private static void ApplyRow(LearningPackData asset, PackRow row, List<KnowledgeCardData> allCards)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("packId").stringValue = row.packId;
            so.FindProperty("packName").stringValue = row.packName;
            so.FindProperty("packType").enumValueIndex = (int)row.packType;
            so.FindProperty("description").stringValue = row.description;
            so.FindProperty("startStageId").intValue = row.startStageId;
            so.FindProperty("endStageId").intValue = row.endStageId;

            // 범위에 맞는 KnowledgeCardData 자동 등록
            SerializedProperty cardsProp = so.FindProperty("cards");
            List<KnowledgeCardData> matched = new List<KnowledgeCardData>();
            for (int i = 0; i < allCards.Count; i++)
            {
                KnowledgeCardData c = allCards[i];
                if (c == null) continue;
                if (c.LinkedStageId >= row.startStageId && c.LinkedStageId <= row.endStageId)
                {
                    matched.Add(c);
                }
            }
            matched.Sort((a, b) => a.LinkedStageId.CompareTo(b.LinkedStageId));
            cardsProp.arraySize = matched.Count;
            for (int i = 0; i < matched.Count; i++)
            {
                cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = matched[i];
            }

            // 학습 목표 적용
            SerializedProperty goalsProp = so.FindProperty("learningGoals");
            int goalCount = row.goals == null ? 0 : row.goals.Length;
            goalsProp.arraySize = goalCount;
            for (int i = 0; i < goalCount; i++)
            {
                SerializedProperty g = goalsProp.GetArrayElementAtIndex(i);
                g.FindPropertyRelative("goalId").stringValue = row.goals[i].goalId;
                g.FindPropertyRelative("goalTitle").stringValue = row.goals[i].goalTitle;
                g.FindPropertyRelative("goalDescription").stringValue = row.goals[i].goalDescription;
                g.FindPropertyRelative("goalType").enumValueIndex = (int)row.goals[i].goalType;
                g.FindPropertyRelative("linkedStageId").intValue = row.goals[i].linkedStageId;
                g.FindPropertyRelative("linkedCardId").stringValue = row.goals[i].linkedCardId ?? string.Empty;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            asset.name = "LearningPack_" + row.fileTag;
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

        private static GoalRow G(string id, string title, string desc, LearningGoalType type, int stageId, string cardId)
        {
            return new GoalRow
            {
                goalId = id, goalTitle = title, goalDescription = desc,
                goalType = type, linkedStageId = stageId, linkedCardId = cardId
            };
        }

        private static PackRow[] BuildRows()
        {
            return new PackRow[]
            {
                new PackRow {
                    fileTag = "BubbleForest_Animals",
                    packId = "pack_bubble_forest_animals",
                    packName = "방울숲 동물 친구들",
                    packType = LearningPackType.AnimalLessons,
                    description = "방울숲 1~15 스테이지의 동물 친구들을 통해 배우는 자연 상식 학습팩입니다.",
                    startStageId = 1, endStageId = 15,
                    goals = new[] {
                        G("forest_first_meet", "숲 친구 만나기",
                          "토끼, 다람쥐 같은 동물 친구의 특징을 알아요.",
                          LearningGoalType.AnimalFact, 1, "card_rabbit_001"),
                        G("forest_night_animals", "밤의 친구들",
                          "부엉이처럼 밤에 잘 보는 동물이 있어요.",
                          LearningGoalType.AnimalFact, 7, "card_owl_001"),
                        G("forest_color_friends", "무지개 친구들",
                          "여러 색이 모이면 더 예쁘다는 것을 알아요.",
                          LearningGoalType.SpecialFact, 15, "card_rainbow_friend_001")
                    }
                },
                new PackRow {
                    fileTag = "MoonRiceCakeStairs_Numbers",
                    packId = "pack_moon_ricecake_stairs_numbers",
                    packName = "달떡계단 숫자 이야기",
                    packType = LearningPackType.NumberLessons,
                    description = "달떡계단 16~28 스테이지의 숫자·순서·덧셈 학습팩입니다.",
                    startStageId = 16, endStageId = 28,
                    goals = new[] {
                        G("number_order_lesson", "숫자의 순서",
                          "하나, 둘, 셋처럼 숫자에 순서가 있다는 것을 알아요.",
                          LearningGoalType.NumberRule, 16, "card_moon_ricecake_001"),
                        G("even_odd_lesson", "짝수와 홀수",
                          "둘씩 나누면 딱 맞는 수와 하나 남는 수가 있어요.",
                          LearningGoalType.NumberRule, 22, "card_even_moonlight_001"),
                        G("addition_lesson", "더하기 기본",
                          "작은 수를 더하면 더 큰 수가 돼요.",
                          LearningGoalType.NumberRule, 28, "card_addition_moon_001")
                    }
                },
                new PackRow {
                    fileTag = "BossLessons",
                    packId = "pack_boss_lessons",
                    packName = "보스 학습 이야기",
                    packType = LearningPackType.BossLessons,
                    description = "보스 스테이지에서 얻을 수 있는 특별한 학습팩입니다.",
                    startStageId = 31, endStageId = 32,
                    goals = new[] {
                        G("memory_collection_lesson", "기억을 모으는 법",
                          "작은 기억도 하나씩 모으면 더 잘 떠올릴 수 있어요.",
                          LearningGoalType.BossLesson, 31, "card_boss_memory_tree_001"),
                        G("time_order_lesson", "시간의 순서",
                          "시간은 순서대로 흐를 때 편안하다는 것을 알아요.",
                          LearningGoalType.BossLesson, 32, "card_boss_reverse_clocktower_001")
                    }
                }
            };
        }
    }
}
