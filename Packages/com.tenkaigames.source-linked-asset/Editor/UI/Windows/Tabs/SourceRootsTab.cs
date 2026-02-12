using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEngine;

using Tenkai.SourceLinkedAsset.Domain;
using Tenkai.SourceLinkedAsset.EditorService;

namespace Tenkai.SourceLinkedAsset.UI {

    /// <summary>
    /// Source Root設定タブ。
    /// ProjectSettings(rootId/name) と UserSettings(rootId->path) を編集する。
    /// </summary>
    public sealed class SourceRootsTab : ISettingsTab {
        public string TabName => TextTabName.GetByLang(SLASettingsCache.GetUser().GetResolvedLanguage());

        private Vector2 _scroll;
        private string _newId = "";
        private string _newName = "";

        public void OnGUI() {
            var project = SLASettingsCache.GetProject();
            var user = SLASettingsCache.GetUser();
            var lang = user.GetResolvedLanguage();

            if (project.sourceRootDict == null) project.sourceRootDict = new Dictionary<string, SourceRootEntry>();
            if (user.sourceRootDict == null) user.sourceRootDict = new Dictionary<string, string>();

            if (user.showHelp) {
                EditorGUILayout.HelpBox(TextHelp.GetByLang(lang), MessageType.Info);
                EditorGUILayout.Space(8);
            }

            DrawAddRoot(lang);

            EditorGUILayout.Space(8);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawRootsTable(lang);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(TextProjectPath.GetByLang(lang), SLAProjectSettingsIO.GetSettingsFilePath());
            EditorGUILayout.LabelField(TextUserPath.GetByLang(lang), SLAUserSettingsIO.GetSettingsFilePath());
        }

        /// <summary>
        /// Project/User設定を両方保存する。
        /// </summary>
        private static void SaveAll() {
            SLASettingsCache.SaveProject();
            SLASettingsCache.SaveUser();
        }

        /// <summary>
        /// rootId/name を追加し、任意で rootId->path も追加する。
        /// </summary>
        private void DrawAddRoot(SLALanguage lang) {
            var project = SLASettingsCache.GetProject();
            var user = SLASettingsCache.GetUser();

            EditorGUILayout.LabelField(TextAddRootHeader.GetByLang(lang), EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                _newId = EditorGUILayout.TextField(TextRootId.GetByLang(lang), _newId);
                _newName = EditorGUILayout.TextField(TextName.GetByLang(lang), _newName);

                using (new EditorGUILayout.HorizontalScope()) {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(TextAdd.GetByLang(lang), GUILayout.Width(120))) {
                        var id = (_newId ?? "").Trim();
                        var name = (_newName ?? "").Trim();

                        if (string.IsNullOrEmpty(id)) {
                            EditorUtility.DisplayDialog(
                                TextDialogAddRootTitle.GetByLang(lang),
                                TextRootIdEmpty.GetByLang(lang),
                                TextOk.GetByLang(lang));
                            return;
                        }

                        // id はファイル名/キーとして安全寄りに（最低限）
                        if (id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) {
                            EditorUtility.DisplayDialog(
                                TextDialogAddRootTitle.GetByLang(lang),
                                TextRootIdInvalidChars.GetByLang(lang),
                                TextOk.GetByLang(lang));
                            return;
                        }

                        if (project.sourceRootDict.ContainsKey(id)) {
                            EditorUtility.DisplayDialog(
                                TextDialogAddRootTitle.GetByLang(lang),
                                string.Format(TextRootIdAlreadyExists.GetByLang(lang), id),
                                TextOk.GetByLang(lang));
                            return;
                        }

                        project.sourceRootDict[id] = new SourceRootEntry { name = string.IsNullOrEmpty(name) ? id : name };

                        // 追加時にパスも設定したければフォルダを選ばせる（任意）
                        var picked = EditorUtility.OpenFolderPanel(TextSelectRootFolderOptional.GetByLang(lang), "", "");
                        if (!string.IsNullOrEmpty(picked) && Directory.Exists(picked)) {
                            user.sourceRootDict[id] = Path.GetFullPath(picked);
                        }

                        _newId = "";
                        _newName = "";

                        SaveAll();
                    }
                }
            }
        }

        /// <summary>
        /// 既存rootの一覧を描画し、変更を保存する。
        /// </summary>
        private static void DrawRootsTable(SLALanguage lang) {
            var project = SLASettingsCache.GetProject();
            var user = SLASettingsCache.GetUser();

            // project側に存在する rootId を表示対象として確定（安定した順序で描画する）
            var keys = project.sourceRootDict.Keys.OrderBy(k => k).ToList();

            // ループ中に Dictionary を直接いじると例外になるので、削除対象は一旦ここに貯める
            var removeIds = new List<string>();

            // 変更検出（最後にまとめて保存するためのフラグ）
            bool changedProject = false;
            bool changedUser = false;

            // ===== セクション: ヘッダ =====
            EditorGUILayout.LabelField(TextRootsHeader.GetByLang(lang), EditorStyles.boldLabel);
            if (keys.Count() == 0) {
                EditorGUILayout.LabelField(TextNothing.GetByLang(lang));
            }

            // ===== セクション: 各root行（カード） =====
            foreach (var id in keys) {

                // ===== セクション: Project側データの取得・補正 =====
                // project.sourceRootDict の value が null になっているケースに備えて補正する
                // （null のままだと name 編集で落ちるので、ここで必ず実体を作る）
                SourceRootEntry def = project.sourceRootDict[id];
                if (def == null) {
                    def = new SourceRootEntry();
                    project.sourceRootDict[id] = def;
                    changedProject = true;
                }

                // ===== セクション: User側データの取得 =====
                // UserSettings は rootId -> path の辞書なので、存在しない場合もある
                user.sourceRootDict.TryGetValue(id, out var path);

                // ===== セクション: 1root分のUI（helpBox内） =====
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    // ---- ブロック: RootID 表示（ラベル + 値を横並び） ----
                    {
                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.LabelField(
                            TextRootId.GetByLang(lang),
                            GUILayout.Width(150)
                        );

                        // コピーしやすいよう SelectableLabel を使う
                        EditorGUILayout.SelectableLabel(
                            id,
                            GUILayout.Height(EditorGUIUtility.singleLineHeight)
                        );

                        EditorGUILayout.EndHorizontal();
                    }

                    // ---- ブロック: Name [Project] 編集 ----
                    // ここで編集した名前は ProjectSettings 側に保存される（全員共通）
                    {
                        var newName = EditorGUILayout.TextField(TextNameProject.GetByLang(lang), def.name ?? "");
                        if (newName != (def.name ?? "")) {
                            def.name = newName;
                            changedProject = true;
                        }
                    }

                    // ---- ブロック: Path [User] 編集（TextField + Browseボタン） ----
                    // ここで編集したパスは UserSettings 側に保存される（ユーザーごと）
                    {
                        EditorGUILayout.LabelField(TextPathUser.GetByLang(lang)); // パス [User]
                        EditorGUILayout.BeginHorizontal();

                        // テキストで直接編集（空文字ならキーを削除して未設定扱いにする）
                        var newPath = EditorGUILayout.TextField(path ?? "", GUILayout.ExpandWidth(true));
                        if (newPath != (path ?? "")) {
                            if (string.IsNullOrEmpty(newPath)) user.sourceRootDict.Remove(id);
                            else user.sourceRootDict[id] = newPath;
                            changedUser = true;
                        }

                        // フォルダ選択で設定
                        if (GUILayout.Button(TextBrowse.GetByLang(lang), GUILayout.Width(90))) {
                            var picked = EditorUtility.OpenFolderPanel(TextSelectRootFolder.GetByLang(lang), path ?? "", "");
                            if (!string.IsNullOrEmpty(picked) && Directory.Exists(picked)) {
                                user.sourceRootDict[id] = Path.GetFullPath(picked);
                                changedUser = true;
                            }
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.Space(6);

                    // ---- ブロック: Remove Root [Project]（削除予約） ----
                    // ここでは即削除しない。foreach中に辞書を変更すると例外になるため、idだけ貯める
                    {
                        if (GUILayout.Button(TextRemoveRootProject.GetByLang(lang), GUILayout.Width(120))) {
                            var ok = EditorUtility.DisplayDialog(
                                TextDialogRemoveRootTitle.GetByLang(lang),
                                string.Format(TextRemoveRootConfirm.GetByLang(lang), id, def.name ?? ""),
                                TextRemove.GetByLang(lang),
                                TextCancel.GetByLang(lang)
                            );

                            if (ok) removeIds.Add(id);
                        }
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space(6);
            }

            // ===== セクション: 削除の確定 =====
            // ループ外でまとめて削除する（project側・user側の両方から消す）
            if (removeIds.Count > 0) {
                foreach (var id in removeIds) {
                    project.sourceRootDict.Remove(id);
                    user.sourceRootDict.Remove(id);
                }
                changedProject = true;
                changedUser = true;
            }

            // ===== セクション: 変更があれば保存 =====
            // 毎フレーム保存を避けるため、変更があった側だけ保存する
            if (changedProject) SLASettingsCache.SaveProject();
            if (changedUser) SLASettingsCache.SaveUser();
        }

        // ===== Localized Texts =====

        public static readonly LocalizedText TextTabName = new(
            en: "Source Roots",
            ja: "ソースルート"
        );

        public static readonly LocalizedText TextHelp = new(
            en:
                "Stored in ProjectSettings: Root Id, Name (shared by everyone)\n" +
                "Stored in UserSettings: Root Id -> Relative path (per user)\n" +
                "Reimport priority: 1) `Root + Relative path` -> 2) absolute path",
            ja:
                "ProjectSettings に保存: ルートID, 名前（全員共通）\n" +
                "UserSettings に保存: ルートID -> 相対パス（ユーザーごと）\n" +
                "再インポート時の優先順位: 1) `ルート + 相対パス` -> 2) 絶対パス"
        );

        public static readonly LocalizedText TextAddRootHeader = new(
            en: "Add Root [Project]",
            ja: "ルート追加 [Project]"
        );

        public static readonly LocalizedText TextRootsHeader = new(
            en: "Root List",
            ja: "ルート一覧"
        );
        public static readonly LocalizedText TextNothing = new(
            en: "Nothing",
            ja: "なし"
        );

        public static readonly LocalizedText TextRootId = new(
            en: "Root ID",
            ja: "ルートID"
        );

        public static readonly LocalizedText TextName = new(
            en: "Name",
            ja: "名前"
        );

        public static readonly LocalizedText TextAdd = new(
            en: "Add",
            ja: "追加"
        );

        public static readonly LocalizedText TextDialogAddRootTitle = new(
            en: "Add Root",
            ja: "ルート追加"
        );

        public static readonly LocalizedText TextRootIdEmpty = new(
            en: "Root ID is empty.",
            ja: "ルートIDが空。"
        );

        public static readonly LocalizedText TextRootIdInvalidChars = new(
            en: "Root ID contains invalid characters.",
            ja: "ルートIDに無効な文字が含まれている。"
        );

        public static readonly LocalizedText TextRootIdAlreadyExists = new(
            en: "Root ID already exists: {0}",
            ja: "ルートIDが既に存在する: {0}"
        );

        public static readonly LocalizedText TextOk = new(
            en: "OK",
            ja: "OK"
        );

        public static readonly LocalizedText TextSelectRootFolderOptional = new(
            en: "Select source root folder (optional)",
            ja: "ソースルートフォルダを選択（任意）"
        );

        public static readonly LocalizedText TextSelectRootFolder = new(
            en: "Select source root folder",
            ja: "ソースルートフォルダを選択"
        );

        public static readonly LocalizedText TextNameProject = new(
            en: "Name [Project]",
            ja: "名前 [Project]"
        );

        public static readonly LocalizedText TextPathUser = new(
            en: "Path [User]",
            ja: "パス [User]"
        );

        public static readonly LocalizedText TextBrowse = new(
            en: "Browse...",
            ja: "参照..."
        );

        public static readonly LocalizedText TextRemoveRootProject = new(
            en: "Remove Root",
            ja: "ルート削除"
        );

        public static readonly LocalizedText TextDialogRemoveRootTitle = new(
            en: "Remove Root",
            ja: "ルート削除"
        );

        public static readonly LocalizedText TextProjectPath = new(
            en: "Project:",
            ja: "Project:"
        );

        public static readonly LocalizedText TextUserPath = new(
            en: "User:",
            ja: "User:"
        );

        public static readonly LocalizedText TextRemoveRootConfirm = new(
            en:
                "Are you sure you want to remove this root?\n\n" +
                "Root ID: {0}\n" +
                "Name: {1}\n\n" +
                "This will remove the root from ProjectSettings and UserSettings.",
            ja:
                "このルートを削除してもいいですか？\n\n" +
                "ルートID: {0}\n" +
                "名前: {1}\n\n" +
                "ProjectSettings と UserSettings の両方から削除されます。"
        );

        public static readonly LocalizedText TextRemove = new(
            en: "Remove",
            ja: "削除"
        );

        public static readonly LocalizedText TextCancel = new(
            en: "Cancel",
            ja: "キャンセル"
        );

    }
}
