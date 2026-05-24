using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Cards;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 지식카드 30장(방울숲 15 + 달떡계단 13 + 보스 2) + Database 자산을 자동 생성/업데이트.
    /// 한 카드 = 한 문장 중심 (20~45자), 어린이 친화 표현만 사용.
    /// TODO: Add card illustration sprites via Inspector.
    /// TODO: Connect to ResultPopup reward flow.
    /// TODO: Add Collection scene card grid UI.
    /// </summary>
    public static class KnowledgeCardGenerator
    {
        private const string CardFolder = "Assets/_Project/Data/Cards/Knowledge";
        private const string DatabasePath = "Assets/_Project/Data/Cards/KnowledgeCardDatabase.asset";
        private const string CardFileNameFormat = "Card_{0:D3}_{1}";

        private struct CardRow
        {
            public int index;
            public string fileTag;
            public string cardId;
            public string cardName;
            public KnowledgeCardCategory category;
            public KnowledgeCardRarity rarity;
            public int linkedStageId;
            public string shortText;
        }

        [MenuItem("Tools/NabyeolDabyeol/Generate Knowledge Cards")]
        public static void GenerateAll()
        {
            EnsureFolder(CardFolder);

            CardRow[] rows = BuildRows();
            int created = 0, updated = 0;
            KnowledgeCardData[] generatedCards = new KnowledgeCardData[rows.Length];

            for (int i = 0; i < rows.Length; i++)
            {
                CardRow row = rows[i];
                string assetPath = $"{CardFolder}/{string.Format(CardFileNameFormat, row.index, row.fileTag)}.asset";
                KnowledgeCardData existing = AssetDatabase.LoadAssetAtPath<KnowledgeCardData>(assetPath);
                bool isNew = existing == null;

                KnowledgeCardData asset = existing != null ? existing : ScriptableObject.CreateInstance<KnowledgeCardData>();
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
                generatedCards[i] = asset;
            }

            // Database 생성/업데이트
            EnsureFolder("Assets/_Project/Data/Cards");
            KnowledgeCardDatabase db = AssetDatabase.LoadAssetAtPath<KnowledgeCardDatabase>(DatabasePath);
            bool dbIsNew = db == null;
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<KnowledgeCardDatabase>();
            }

            SerializedObject dbSo = new SerializedObject(db);
            SerializedProperty cardsList = dbSo.FindProperty("cards");
            cardsList.arraySize = generatedCards.Length;
            for (int i = 0; i < generatedCards.Length; i++)
            {
                cardsList.GetArrayElementAtIndex(i).objectReferenceValue = generatedCards[i];
            }
            dbSo.ApplyModifiedPropertiesWithoutUndo();

            if (dbIsNew)
            {
                AssetDatabase.CreateAsset(db, DatabasePath);
            }
            else
            {
                EditorUtility.SetDirty(db);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"KnowledgeCardGenerator: Cards generated. Created {created}, Updated {updated} (total {created + updated}).");
            Debug.Log($"KnowledgeCardGenerator: Database {(dbIsNew ? "created" : "updated")} at {DatabasePath}.");

            // 검증
            int validCount = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                if (generatedCards[i] != null && generatedCards[i].IsValid()) validCount++;
                else Debug.LogWarning($"KnowledgeCardGenerator: Card {rows[i].index} ({rows[i].cardId}) failed validation.");
            }
            Debug.Log($"KnowledgeCardGenerator: Validation done. {validCount}/{rows.Length} cards passed IsValid().");

            KnowledgeCardDatabase loadedDb = AssetDatabase.LoadAssetAtPath<KnowledgeCardDatabase>(DatabasePath);
            if (loadedDb != null)
            {
                bool dbOk = loadedDb.ValidateCards();
                Debug.Log($"KnowledgeCardGenerator: Database ValidateCards = {dbOk} (count={loadedDb.Count}).");
            }
        }

        private static void ApplyRow(KnowledgeCardData asset, CardRow row)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("cardId").stringValue = row.cardId;
            so.FindProperty("cardName").stringValue = row.cardName;
            so.FindProperty("category").enumValueIndex = (int)row.category;
            so.FindProperty("rarity").enumValueIndex = (int)row.rarity;
            so.FindProperty("linkedStageId").intValue = row.linkedStageId;
            so.FindProperty("shortText").stringValue = row.shortText;
            // image은 비워둔다. TODO: Connect illustration sprite later.
            so.ApplyModifiedPropertiesWithoutUndo();

            asset.name = string.Format(CardFileNameFormat, row.index, row.fileTag);
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

        private static CardRow C(int index, string fileTag, string cardId, string cardName,
            KnowledgeCardCategory category, KnowledgeCardRarity rarity, int linkedStageId, string shortText)
        {
            return new CardRow
            {
                index = index, fileTag = fileTag, cardId = cardId, cardName = cardName,
                category = category, rarity = rarity, linkedStageId = linkedStageId, shortText = shortText
            };
        }

        private static CardRow[] BuildRows()
        {
            return new CardRow[]
            {
                // ─── 방울숲 15장 (#67 검수본 적용) ───
                C( 1, "Rabbit",     "card_rabbit_001",         "토끼의 첫 점프",   KnowledgeCardCategory.Animal, KnowledgeCardRarity.Common,  1, "토끼는 긴 뒷다리로 폴짝 잘 뛰어요."),
                C( 2, "Squirrel",   "card_squirrel_001",       "다람쥐의 도토리", KnowledgeCardCategory.Animal, KnowledgeCardRarity.Common,  2, "다람쥐는 먹이를 모아 두는 버릇이 있어요."),
                C( 3, "Hedgehog",   "card_hedgehog_001",       "고슴도치의 가시", KnowledgeCardCategory.Animal, KnowledgeCardRarity.Common,  3, "고슴도치의 가시는 몸을 지키는 데 도움이 돼요."),
                C( 4, "Fox",        "card_fox_001",            "여우의 살금걸음", KnowledgeCardCategory.Animal, KnowledgeCardRarity.Common,  4, "여우는 조용히 걸으며 주변을 잘 살펴요."),
                C( 5, "Deer",       "card_deer_001",           "사슴의 숲길",     KnowledgeCardCategory.Animal, KnowledgeCardRarity.Rare,    5, "사슴은 귀가 좋아 작은 소리도 잘 들어요."),
                C( 6, "Raccoon",    "card_raccoon_001",        "너구리의 장난",   KnowledgeCardCategory.Animal, KnowledgeCardRarity.Common,  6, "너구리는 밤에 많이 움직이는 동물이에요."),
                C( 7, "Owl",        "card_owl_001",            "부엉이의 밤눈",   KnowledgeCardCategory.Animal, KnowledgeCardRarity.Common,  7, "부엉이는 밤에도 주변을 잘 살펴요."),
                C( 8, "Frog",       "card_frog_001",           "개구리의 연못",   KnowledgeCardCategory.Animal, KnowledgeCardRarity.Common,  8, "개구리는 물가와 풀숲에서 자주 볼 수 있어요."),
                C( 9, "Duck",       "card_duck_001",           "오리의 물결",     KnowledgeCardCategory.Animal, KnowledgeCardRarity.Common,  9, "오리는 물 위에 둥둥 떠서 헤엄쳐요."),
                C(10, "Cat",        "card_cat_001",            "고양이의 낮잠",   KnowledgeCardCategory.Animal, KnowledgeCardRarity.Rare,   10, "고양이는 쉬거나 잠자는 시간이 많아요."),
                C(11, "Puppy",      "card_puppy_001",          "강아지의 꼬리",   KnowledgeCardCategory.Animal, KnowledgeCardRarity.Common, 11, "강아지는 꼬리로 기분을 알려주기도 해요."),
                C(12, "Hamster",    "card_hamster_001",        "햄스터의 볼주머니", KnowledgeCardCategory.Animal, KnowledgeCardRarity.Common, 12, "햄스터는 볼주머니에 먹이를 넣을 수 있어요."),
                C(13, "Panda",      "card_panda_001",          "팬더의 대나무",   KnowledgeCardCategory.Animal, KnowledgeCardRarity.Rare,   13, "팬더는 대나무를 아주 좋아해요."),
                C(14, "Koala",      "card_koala_001",          "코알라의 나무",   KnowledgeCardCategory.Animal, KnowledgeCardRarity.Rare,   14, "코알라는 나무 위에서 쉬는 시간이 많아요."),
                C(15, "Rainbow",    "card_rainbow_friend_001", "무지개 숲친구",   KnowledgeCardCategory.Nature, KnowledgeCardRarity.Epic,   15, "여러 색이 모이면 무지개처럼 예뻐요."),

                // ─── 달떡계단 13장 (#67 검수본 적용) ───
                C(16, "MoonRicecake",     "card_moon_ricecake_001",     "하나둘 달떡",     KnowledgeCardCategory.Number, KnowledgeCardRarity.Common, 16, "숫자는 하나씩 순서대로 세면 쉬워요."),
                C(17, "NumberThree",      "card_number_three_001",      "셋째 숫자별",     KnowledgeCardCategory.Number, KnowledgeCardRarity.Common, 17, "셋은 하나, 둘 다음에 오는 숫자예요."),
                C(18, "NumberStar",       "card_number_star_001",       "반짝 숫자별",     KnowledgeCardCategory.Number, KnowledgeCardRarity.Common, 18, "작은 수를 모으면 더 큰 수가 돼요."),
                C(19, "NumberFour",       "card_number_four_001",       "넷씩 모아보기",   KnowledgeCardCategory.Number, KnowledgeCardRarity.Common, 19, "같은 것끼리 묶으면 세기 쉬워요."),
                C(20, "NumberFive",       "card_number_five_001",       "다섯달떡 완성",   KnowledgeCardCategory.Number, KnowledgeCardRarity.Rare,   20, "다섯은 한 손 손가락 수와 같아요."),
                C(21, "AdditionStep",     "card_addition_step_001",     "더하기 첫걸음",   KnowledgeCardCategory.Number, KnowledgeCardRarity.Common, 21, "더하기는 수를 함께 모으는 방법이에요."),
                C(22, "EvenMoonlight",    "card_even_moonlight_001",    "짝수 달빛길",     KnowledgeCardCategory.Number, KnowledgeCardRarity.Common, 22, "짝수는 둘씩 나누면 딱 맞는 수예요."),
                C(23, "OddStarcandy",     "card_odd_starcandy_001",     "홀수 별사탕",     KnowledgeCardCategory.Number, KnowledgeCardRarity.Common, 23, "홀수는 둘씩 나누면 하나가 남아요."),
                C(24, "SevenJump",        "card_seven_jump_001",        "일곱칸 점프",     KnowledgeCardCategory.Number, KnowledgeCardRarity.Common, 24, "순서대로 세면 일곱 번째를 찾을 수 있어요."),
                C(25, "EightBox",         "card_eight_ricecake_box_001","여덟떡 상자",     KnowledgeCardCategory.Number, KnowledgeCardRarity.Rare,   25, "물건을 상자에 넣으면 정리하기 쉬워요."),
                C(26, "PatternStairs",    "card_pattern_stairs_001",    "규칙찾기 계단",   KnowledgeCardCategory.Puzzle, KnowledgeCardRarity.Common, 26, "반복되는 모양을 찾으면 규칙이 보여요."),
                C(27, "CountToTen",       "card_count_to_ten_001",      "열까지 세기",     KnowledgeCardCategory.Number, KnowledgeCardRarity.Common, 27, "열까지 세면 많은 것도 차근차근 셀 수 있어요."),
                C(28, "AdditionMoon",     "card_addition_moon_001",     "덧셈달",           KnowledgeCardCategory.Number, KnowledgeCardRarity.Rare,   28, "수를 더하면 처음보다 더 큰 수가 돼요."),

                // ─── 보스 2장 (#67 검수본 적용) ───
                C(29, "MemoryTree",       "card_boss_memory_tree_001",         "기억나무의 조각", KnowledgeCardCategory.Boss, KnowledgeCardRarity.Epic, 31, "기억은 하나씩 모이면 더 잘 떠올라요."),
                C(30, "ReverseClockTower","card_boss_reverse_clocktower_001",  "거꾸로 시계탑",   KnowledgeCardCategory.Boss, KnowledgeCardRarity.Epic, 32, "시간은 순서대로 흐를 때 편안해요.")
            };
        }
    }
}
