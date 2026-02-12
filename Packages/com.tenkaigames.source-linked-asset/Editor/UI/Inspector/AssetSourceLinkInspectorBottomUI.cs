using System;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using Tenkai.SourceLinkedAsset.Domain;
using Tenkai.SourceLinkedAsset.EditorService;
using Tenkai.SourceLinkedAsset.EditorActions;

namespace Tenkai.SourceLinkedAsset.UI {

    /// <summary>
    /// InspectorWindow の rootVisualElement 末尾に SourceLinkedAsset の UI を差し込む。
    /// finishedDefaultInspectorGUI に依存しないため、Unity バージョン差に強い。
    /// </summary>
    [InitializeOnLoad]
    internal static class AssetSourceLinkInspectorBottomUI {

        private const string RootName = "tenkai-source-linked-asset-bottom-root";
        private const string PrefFoldout = "Tenkai.SourceLinkedAsset.InspectorFoldout";

        private const float BtnH = 18f;
        private const float BtnW = 72f;

        static AssetSourceLinkInspectorBottomUI() {
            AttachAllInspectors();

            Selection.selectionChanged += () => {
                AttachAllInspectors();
                RepaintAllInspectors();
            };

            EditorApplication.projectChanged += () => {
                AttachAllInspectors();
                RepaintAllInspectors();
            };
        }

        /// <summary>
        /// すべての InspectorWindow に対して末尾 UI の差し込みを保証する。
        /// </summary>
        private static void AttachAllInspectors() {
            var inspectorType = Type.GetType("UnityEditor.InspectorWindow,UnityEditor");
            if (inspectorType == null) return;

            var windows = Resources.FindObjectsOfTypeAll(inspectorType).Cast<EditorWindow>().ToArray();
            for (int i = 0; i < windows.Length; i++) {
                AttachOne(windows[i]);
            }
        }

        /// <summary>
        /// 1つの InspectorWindow に末尾 UI を差し込む。
        /// ScrollView が存在する場合のみ追加する。
        /// </summary>
        private static void AttachOne(EditorWindow inspectorWindow) {
            if (inspectorWindow == null) return;

            var root = inspectorWindow.rootVisualElement;
            if (root == null) return;

            // すでにどこかに入っていれば何もしない
            if (root.Q<VisualElement>(RootName) != null) return;

            // Inspector 内の ScrollView を探す（最下部に近いものを使う）
            var scrollViews = root.Query<ScrollView>().ToList();
            if (scrollViews == null || scrollViews.Count == 0) {
                // ScrollView がまだ構築されていないフレームでは何もしない
                return;
            }

            var scrollView = scrollViews[scrollViews.Count - 1];
            var host = scrollView.contentContainer;
            if (host == null) return;

            var container = new VisualElement();
            container.name = RootName;
            container.style.marginTop = 6;
            container.style.marginLeft = 6;
            container.style.marginRight = 6;
            container.style.marginBottom = 6;

            var imgui = new IMGUIContainer(DrawForCurrentSelection);
            container.Add(imgui);

            // 必ず ScrollView の末尾にだけ追加される
            host.Add(container);
        }



        /// <summary>
        /// 選択変更時に Inspector を再描画する。
        /// </summary>
        private static void RepaintAllInspectors() {
            var inspectorType = Type.GetType("UnityEditor.InspectorWindow,UnityEditor");
            if (inspectorType == null) return;

            var windows = Resources.FindObjectsOfTypeAll(inspectorType).Cast<EditorWindow>().ToArray();
            for (int i = 0; i < windows.Length; i++) {
                windows[i].Repaint();
            }
        }

        /// <summary>
        /// 現在の選択アセットに紐づく SourceLinkedAsset 情報を描画する。
        /// </summary>
        private static void DrawForCurrentSelection() {
            var obj = Selection.activeObject;
            if (obj == null) return;

            var assetPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath)) return;
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal)) return;
            if (AssetDatabase.IsValidFolder(assetPath)) return;

            var importer = AssetImporter.GetAtPath(assetPath);

            ImporterUserDataUtil.GetLinkFromImporter(importer)
                .Tap(link =>
                    DrawUI(assetPath, link)
                );
        }

        /// <summary>
        /// SourceLinkedAsset 用の UI セクションを描画する。
        /// </summary>
        private static void DrawUI(string assetPath, AssetSourceLink link) {
            var lang = SLASettingsCache
                        .GetUser()
                        .GetResolvedLanguage();

            bool foldout = EditorPrefs.GetBool(PrefFoldout, true);

            var project = SLASettingsCache.GetProject();
            var user = SLASettingsCache.GetUser();

            string resolvedAsOsPath = "";
            string resolvedAsDisplayPath = "";
            string resolveError = "";
            SourceLinkResolver.ResolveSourceAbsPath(project, user, link)
                .Inspect(
                    ok: absPath => {
                        resolvedAsOsPath = absPath.AsOsPath;
                        resolvedAsDisplayPath = absPath.AsDisplayPath;
                    },
                    fail: failReason => resolveError = failReason
                );

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                // タイトル + 折りたたみ
                bool next = EditorGUILayout.Foldout(foldout, "SourceLinkedAsset", true);
                if (next != foldout) {
                    foldout = next;
                    EditorPrefs.SetBool(PrefFoldout, foldout);
                }

                // 常時表示（最小）
                using (new EditorGUI.DisabledScope(true)) {
                    EditorGUILayout.TextField("Resolved", resolvedAsDisplayPath);
                }

                if (!string.IsNullOrEmpty(resolveError)) {
                    EditorGUILayout.HelpBox(resolveError, MessageType.Warning);
                }
                else if (!string.IsNullOrEmpty(resolvedAsOsPath) && !File.Exists(resolvedAsOsPath)) {
                    EditorGUILayout.HelpBox("Resolved source file not found.", MessageType.Warning);
                }

                // 展開時のみ詳細
                if (foldout) {
                    EditorGUILayout.Space(2);

                    using (new EditorGUI.DisabledScope(true)) {
                        EditorGUILayout.TextField("Asset", assetPath);
                        EditorGUILayout.TextField("Root ID", link.sourceRootId ?? "");
                        EditorGUILayout.TextField("Rel", link.sourceRel ?? "");
                        EditorGUILayout.TextField("Abs", link.sourceAbs ?? "");
                    }

                    DrawSyncInfo(link);
                }

                // ボタンは常に最下段に固定
                bool doReimport = false;
                bool doRelinkPick = false;
                bool doReveal = false;
                bool doCopy = false;

                EditorGUILayout.Space(2);
                using (new EditorGUILayout.HorizontalScope()) {
                    if (GUILayout.Button(
                            new GUIContent("Reimport", TextReimportTooltip.GetByLang(lang)),
                            GUILayout.Height(BtnH),
                            GUILayout.Width(BtnW))) {
                        doReimport = true;
                    }

                    if (GUILayout.Button(
                        new GUIContent("Relink", TextRelinkTooltip.GetByLang(lang)),
                        GUILayout.Height(BtnH),
                        GUILayout.Width(BtnW))) {
                        doRelinkPick = true;
                    }

                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(resolvedAsOsPath))) {

                        if (GUILayout.Button(
                            new GUIContent("Reveal", TextRevealTooltip.GetByLang(lang)),
                            GUILayout.Height(BtnH),
                            GUILayout.Width(BtnW))) {
                            doReveal = true;
                        }

                        if (GUILayout.Button(
                            new GUIContent("Copy Path", TextCopyTooltip.GetByLang(lang)),
                            GUILayout.Height(BtnH),
                            GUILayout.Width(BtnW))) {
                            doCopy = true;
                        }
                    }
                }

                if (doReimport) {
                    EditorApplication.delayCall += () => {
                        SourceReimporter.ReimportSelectedOrLogError();
                    };
                }

                if (doRelinkPick) {
                    EditorApplication.delayCall += () => {
                        SourceReimporter.ReimportSelectedWithPickFileOrLogError();
                    };
                }

                if (doReveal) {
                    var path = resolvedAsOsPath;
                    EditorApplication.delayCall += () => {
                        RevealInFileBrowser(path);
                    };
                }

                if (doCopy) {
                    var path = resolvedAsOsPath;
                    EditorApplication.delayCall += () => {
                        EditorGUIUtility.systemCopyBuffer = path ?? "";
                    };
                }
            }
        }

        /// <summary>
        /// .meta 内に保存されている同期情報を表示する。
        /// </summary>
        private static void DrawSyncInfo(AssetSourceLink link) {
            long ticks = link.sourceLastWriteUtc;
            string lastWrite = "";
            if (ticks > 0) {
                try {
                    var dt = new DateTime(ticks, DateTimeKind.Utc);
                    lastWrite = dt.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
                }
                catch {
                    lastWrite = "";
                }
            }

            using (new EditorGUI.DisabledScope(true)) {
                EditorGUILayout.TextField("Last Write", lastWrite);
                EditorGUILayout.LongField("Size (bytes)", link.sourceSizeBytes);
                EditorGUILayout.TextField("SHA-256", link.sourceSha256 ?? "");
                EditorGUILayout.TextField("AssetPath At Import", link.assetPathAtImport ?? "");
                EditorGUILayout.IntField("Link Version", link.version);
            }
        }

        /// <summary>
        /// OS のファイルブラウザでファイルを表示する。
        /// </summary>
        private static void RevealInFileBrowser(string absPath) {
            if (string.IsNullOrEmpty(absPath)) return;

            try {
                var full = Path.GetFullPath(absPath);
                if (File.Exists(full)) {
                    EditorUtility.RevealInFinder(full);
                }
            }
            catch {
                // 何もしない
            }
        }

        // === LocalizedText ===

        public static readonly LocalizedText TextReimportTooltip = new(
            en: "Reimport the selected asset(s) from the linked source file.",
            ja: "リンクされているソースファイルから選択アセットを再インポートする。"
        );

        public static readonly LocalizedText TextRelinkTooltip = new(
            en: "Pick a source file and relink, then reimport.",
            ja: "ソースファイルを選び直してリンク更新し、再インポートする。"
        );

        public static readonly LocalizedText TextRevealTooltip = new(
            en: "Show the resolved source file in the file browser.",
            ja: "解決されたソースファイルをOSのファイルブラウザで表示する。"
        );

        public static readonly LocalizedText TextCopyTooltip = new(
            en: "Copy the resolved source file path to the clipboard.",
            ja: "解決されたソースファイルのパスをクリップボードにコピーする。"
        );
    }
}
