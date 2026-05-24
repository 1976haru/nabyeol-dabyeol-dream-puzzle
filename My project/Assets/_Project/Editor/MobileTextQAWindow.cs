// MobileTextQAWindow.cs
// Task 99 — Editor sweep for mobile text readability.
//
// Tools > MallangTwins > Mobile Text QA. Scans every TextMeshProUGUI and
// Button in the active scene and surfaces problems: too-small fonts,
// short buttons, empty texts. Read-only — does not mutate anything.

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NabyeolDabyeolDreamPuzzle.UI.EditorTools
{
    public class MobileTextQAWindow : EditorWindow
    {
        private int   minFontSize     = 20;
        private float minButtonHeight = 72f;
        private bool  warnEmptyTexts  = true;
        private bool  includeInactive = true;

        private Vector2 scroll;
        private string  summary;

        [MenuItem("Tools/MallangTwins/Mobile Text QA")]
        public static void Open()
        {
            var window = GetWindow<MobileTextQAWindow>("Mobile Text QA");
            window.minSize = new Vector2(360f, 240f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Scan thresholds", EditorStyles.boldLabel);
            minFontSize     = EditorGUILayout.IntField("Min font size",     minFontSize);
            minButtonHeight = EditorGUILayout.FloatField("Min button height", minButtonHeight);
            warnEmptyTexts  = EditorGUILayout.Toggle("Warn empty texts",   warnEmptyTexts);
            includeInactive = EditorGUILayout.Toggle("Include inactive",   includeInactive);

            EditorGUILayout.Space();
            if (GUILayout.Button("Scan active scene"))
            {
                summary = ScanActiveScene();
            }

            if (!string.IsNullOrEmpty(summary))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Last scan summary", EditorStyles.boldLabel);
                scroll = EditorGUILayout.BeginScrollView(scroll);
                EditorGUILayout.HelpBox(summary, MessageType.Info);
                EditorGUILayout.EndScrollView();
            }
        }

        private string ScanActiveScene()
        {
            TextMeshProUGUI[] texts   = FindObjectsByType<TextMeshProUGUI>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            Button[]          buttons = FindObjectsByType<Button>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            int tooSmall = 0;
            int empty    = 0;
            int shortBtn = 0;

            foreach (var t in texts)
            {
                if (t == null) continue;
                if (t.fontSize > 0 && t.fontSize < minFontSize)
                {
                    Debug.LogWarning(
                        $"[Mobile Text QA] '{GetPath(t.transform)}' fontSize={t.fontSize} < {minFontSize}.", t);
                    tooSmall++;
                }
                if (warnEmptyTexts && string.IsNullOrWhiteSpace(t.text))
                {
                    Debug.LogWarning($"[Mobile Text QA] '{GetPath(t.transform)}' has empty text.", t);
                    empty++;
                }
            }

            foreach (var b in buttons)
            {
                if (b == null) continue;
                RectTransform rect = b.transform as RectTransform;
                if (rect == null) continue;
                float h = rect.rect.height;
                if (h < minButtonHeight)
                {
                    Debug.LogWarning(
                        $"[Mobile Text QA] button '{GetPath(b.transform)}' height={h:F1} < {minButtonHeight}.", b);
                    shortBtn++;
                }
            }

            string result =
                $"Checked {texts.Length} TMP texts and {buttons.Length} buttons.\n" +
                $"  too-small fonts (< {minFontSize}): {tooSmall}\n" +
                (warnEmptyTexts ? $"  empty texts: {empty}\n" : string.Empty) +
                $"  short buttons (< {minButtonHeight}): {shortBtn}\n" +
                $"See Console for individual warnings.";

            Debug.Log($"[Mobile Text QA] {result}");
            return result;
        }

        private static string GetPath(Transform t)
        {
            if (t == null) return string.Empty;
            string path = t.name;
            Transform p = t.parent;
            while (p != null)
            {
                path = p.name + "/" + path;
                p = p.parent;
            }
            return path;
        }
    }
}
#endif
