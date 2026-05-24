using System.Text;
using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Puzzle;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 체크리스트 #23: BlockPrefab을 코드로 자동 생성하는 Editor 전용 툴.
    /// Unity 에디터 메뉴에서 실행하면 필요한 폴더, 임시 텍스처, 그리고 BlockPrefab.prefab을 만든다.
    /// </summary>
    public static class PrefabSetupTool
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string PrefabsRoot = "Assets/_Project/Prefabs";
        private const string BlocksPrefabFolder = "Assets/_Project/Prefabs/Blocks";
        private const string ArtRoot = "Assets/_Project/Art";
        private const string BlocksArtFolder = "Assets/_Project/Art/Blocks";

        private const string TempTexturePath = "Assets/_Project/Art/Blocks/TempBlockSquare.asset";
        private const string BlockPrefabPath = "Assets/_Project/Prefabs/Blocks/BlockPrefab.prefab";

        [MenuItem("NabyeolDabyeol/Setup/Create Block Prefab")]
        public static void CreateBlockPrefab()
        {
            EnsureFolders();
            EnsureTempTexture();

            GameObject root = new GameObject("BlockPrefab");
            try
            {
                root.transform.position = Vector3.zero;
                root.transform.rotation = Quaternion.identity;
                root.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

                SpriteRenderer spriteRenderer = root.AddComponent<SpriteRenderer>();
                spriteRenderer.color = Color.white;

                root.AddComponent<Block>();
                root.AddComponent<BlockVisual>();

                BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
                collider.isTrigger = false;
                collider.offset = Vector2.zero;
                collider.size = Vector2.one;

                root.AddComponent<BlockInput>();

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, BlockPrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (savedPrefab == null)
                {
                    Debug.LogError($"[PrefabSetupTool] BlockPrefab 저장 실패: {BlockPrefabPath}");
                    return;
                }

                Debug.Log($"[PrefabSetupTool] BlockPrefab 생성 완료: {BlockPrefabPath}");
            }
            finally
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }

            VerifyPrefab();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "_Project");
            EnsureFolder(ProjectRoot, "Prefabs");
            EnsureFolder(PrefabsRoot, "Blocks");
            EnsureFolder(ProjectRoot, "Art");
            EnsureFolder(ArtRoot, "Blocks");
            EnsureFolder("Assets/_Project/Scripts", "Editor");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string full = $"{parent}/{child}";
            if (AssetDatabase.IsValidFolder(full))
            {
                return;
            }
            AssetDatabase.CreateFolder(parent, child);
        }

        private static void EnsureTempTexture()
        {
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(TempTexturePath);
            if (existing != null)
            {
                return;
            }

            Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            texture.name = "TempBlockSquare";
            Color[] pixels = new Color[128 * 128];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);

            AssetDatabase.CreateAsset(texture, TempTexturePath);
            AssetDatabase.SaveAssets();
        }

        private static void VerifyPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BlockPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[PrefabSetupTool] 검증 실패: {BlockPrefabPath} 를 찾을 수 없습니다.");
                return;
            }

            Component[] components = prefab.GetComponents<Component>();
            StringBuilder sb = new StringBuilder();
            sb.Append("[PrefabSetupTool] BlockPrefab 검증 성공 → ");
            sb.Append(BlockPrefabPath);
            sb.Append(" / Components: ");
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    sb.Append("<missing>");
                }
                else
                {
                    sb.Append(components[i].GetType().Name);
                }
                if (i < components.Length - 1)
                {
                    sb.Append(", ");
                }
            }
            Debug.Log(sb.ToString());
        }
    }
}
