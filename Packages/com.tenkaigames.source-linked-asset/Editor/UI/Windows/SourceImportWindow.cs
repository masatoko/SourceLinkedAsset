using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

using Tenkai.SourceLinkedAsset.Domain;
using Tenkai.SourceLinkedAsset.EditorService;
using Tenkai.SourceLinkedAsset.EditorActions;

namespace Tenkai.SourceLinkedAsset.UI {

    // ドラッグ&ドロップ取り込みウィンドウ
    public class SourceImportWindow : EditorWindow {
        public static void ShowWindow() {
            var w = GetWindow<SourceImportWindow>("Import Window");
            w.minSize = new Vector2(420, 180);
            w.Show();
        }

        private const float Pad = 10f;
        private const float DropMinH = 110f;

        private void OnGUI() {
            var userStg = SLASettingsCache.GetUser();
            var lang = userStg.GetResolvedLanguage();

            if (userStg.showHelp) {
                GUILayout.Space(Pad);
                EditorGUILayout.HelpBox(TextDetail.GetByLang(lang), MessageType.Info);
            }

            GUILayout.Space(Pad);

            { // アセット上書き有効化のトグル
                var newValue = EditorGUILayout.ToggleLeft(
                    TextOverwriteToggle.GetByLang(lang),
                    SLASessionState.ImportOverwriteEnabled
                );
                SLASessionState.ImportOverwriteEnabled = newValue;
            }

            GUILayout.Space(Pad);

            // 残り領域をドロップエリアとして確保する
            var dropRect = GUILayoutUtility.GetRect(
                0f,
                DropMinH,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );

            // 外側に余白を作る
            dropRect = Inset(dropRect, Pad);

            var e = Event.current;

            bool isDragEvent = e.type == EventType.DragUpdated || e.type == EventType.DragPerform;
            bool isHover = dropRect.Contains(e.mousePosition);

            // ドラッグ中はマウスが領域外でも見やすいように多少強調してもいいが、
            // 誤反応を避けるため受け付け自体は hover 時のみ行う
            DrawDropArea(dropRect, TextDropHere.GetByLang(lang), isHover, isDragEvent);

            if (!isHover) return;

            if (isDragEvent) {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (e.type == EventType.DragPerform) {
                    DragAndDrop.AcceptDrag();
                    var paths = DragAndDrop.paths;

                    foreach (var p in paths.Where(File.Exists)) {
                        SourceImporter.ImportOneOrLogError(p, overwrite: SLASessionState.ImportOverwriteEnabled);
                    }
                }

                e.Use();
            }
        }

        // =====

        private static Rect Inset(Rect r, float pad) {
            r.xMin += pad;
            r.yMin += pad;
            r.xMax -= pad;
            r.yMax -= pad;
            return r;
        }

        /// <summary>
        /// ドロップエリアを描画する
        /// </summary>
        private static void DrawDropArea(Rect rect, string label, bool isHover, bool isDragEvent) {
            // 背景
            var bg = isDragEvent && isHover
                ? new Color(0.25f, 0.55f, 0.95f, 0.14f)
                : (isHover ? new Color(1f, 1f, 1f, 0.06f) : new Color(0f, 0f, 0f, 0.08f));
            EditorGUI.DrawRect(rect, bg);

            // 枠線
            var border = isDragEvent && isHover
                ? new Color(0.25f, 0.55f, 0.95f, 0.90f)
                : new Color(1f, 1f, 1f, isHover ? 0.35f : 0.18f);
            DrawBorder(rect, 2f, border);

            // ラベル
            var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel) {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = 12,
            };
            GUI.Label(rect, label, style);
        }

        /// <summary>
        /// Rect に枠線を描画する
        /// </summary>
        private static void DrawBorder(Rect r, float t, Color c) {
            EditorGUI.DrawRect(new Rect(r.xMin, r.yMin, r.width, t), c);               // top
            EditorGUI.DrawRect(new Rect(r.xMin, r.yMax - t, r.width, t), c);           // bottom
            EditorGUI.DrawRect(new Rect(r.xMin, r.yMin, t, r.height), c);              // left
            EditorGUI.DrawRect(new Rect(r.xMax - t, r.yMin, t, r.height), c);          // right
        }

        public static readonly LocalizedText TextDetail = new(
            en: "Drag and drop external files into this window to copy and import them\n" +
                "into the currently selected folder in the Project (or Assets if none is selected).\n" +
                "Import information is stored in the .meta file (AssetImporter.userData).",
            ja: "外部ファイルをこのウィンドウにドラッグ&ドロップすると、\n" +
                "現在Projectで選択中のフォルダ (なければAssets) へコピーして取り込みます。\n" +
                "取り込み情報は .meta (AssetImporter.userData) に保存されます。"
        );

        public static readonly LocalizedText TextDropHere = new(
            en: "Drop external files here",
            ja: "ここに外部ファイルをドロップ"
        );

        public static readonly LocalizedText TextOverwriteToggle = new(
            en: "Overwrite if asset exists (same name)",
            ja: "同名アセットがある場合は上書き"
        );
    }
}
