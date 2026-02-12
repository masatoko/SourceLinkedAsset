using UnityEditor;

using Tenkai.SourceLinkedAsset.Domain;
using Tenkai.SourceLinkedAsset.EditorService;

namespace Tenkai.SourceLinkedAsset.UI {

    /// <summary>
    /// UserSettingsを編集するタブ。
    /// ソースルート以外の個人設定をここに集約する。
    /// </summary>
    public sealed class UserSettingsTab : ISettingsTab {
        public string TabName => TextTabName.GetByLang(SLASettingsCache.GetUser().GetResolvedLanguage());

        public void OnGUI() {
            var user = SLASettingsCache.GetUser();
            var lang = user.GetResolvedLanguage();

            DrawImport(user, lang);
            EditorGUILayout.Space(12);

            DrawLanguage(user, lang);
            EditorGUILayout.Space(12);

            DrawHelp(user, lang);
            EditorGUILayout.Space(12);

            // 追加が可能
            // 例:
            // DrawImportBehavior(user, lang);
            // DrawLogging(user, lang);
        }

        /// <summary>
        /// UI言語設定を描画
        /// </summary>
        private static void DrawLanguage(SLAUserSettings user, SLALanguage lang) {
            EditorGUILayout.LabelField(TextLanguageHeader.GetByLang(lang), EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                var newUseSystem = EditorGUILayout.ToggleLeft(TextUseSystemLanguage.GetByLang(lang), user.useSystemLanguage);
                if (newUseSystem != user.useSystemLanguage) {
                    user.useSystemLanguage = newUseSystem;
                    SLASettingsCache.SaveUser();

                    // useSystemLanguage 切り替え直後は解決言語が変わる可能性があるので、即時反映したければ Repaint を呼ぶ側で対応する
                    // ここではデータ保存のみ行う
                }

                using (new EditorGUI.DisabledScope(user.useSystemLanguage)) {
                    var newLang = (SLALanguage)EditorGUILayout.EnumPopup(TextLanguageField.GetByLang(lang), user.language);
                    if (newLang != user.language) {
                        user.language = newLang;
                        SLASettingsCache.SaveUser();
                    }
                }

                EditorGUILayout.LabelField(TextLanguageInUse.GetByLang(lang), user.GetResolvedLanguage().ToString());
            }
        }

        /// <summary>
        /// ヘルプ表示設定を描画
        /// </summary>
        private static void DrawHelp(SLAUserSettings user, SLALanguage lang) {
            EditorGUILayout.LabelField(TextHelpHeader.GetByLang(lang), EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                var newShowHelp = EditorGUILayout.ToggleLeft(TextShowHelp.GetByLang(lang), user.showHelp);
                if (newShowHelp != user.showHelp) {
                    user.showHelp = newShowHelp;
                    SLASettingsCache.SaveUser();
                }
            }
        }

        /// <summary>
        /// インポート設定を描画
        /// </summary>
        private static void DrawImport(SLAUserSettings user, SLALanguage lang) {
            EditorGUILayout.LabelField(TextImportHeader.GetByLang(lang), EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                var newSaveAbsPath = EditorGUILayout.ToggleLeft(TextSaveAbsPath.GetByLang(lang), user.saveAbsolutePath);
                if (newSaveAbsPath != user.saveAbsolutePath) {
                    user.saveAbsolutePath = newSaveAbsPath;
                    SLASettingsCache.SaveUser();
                }
            }
        }

        // ===== Localized Texts =====

        public static readonly LocalizedText TextTabName = new(
            en: "User Settings",
            ja: "ユーザー設定"
        );

        public static readonly LocalizedText TextLanguageHeader = new(
            en: "Language",
            ja: "言語"
        );

        public static readonly LocalizedText TextUseSystemLanguage = new(
            en: "Use system language",
            ja: "システムの言語を使用"
        );

        public static readonly LocalizedText TextLanguageField = new(
            en: "Language",
            ja: "言語"
        );

        public static readonly LocalizedText TextLanguageInUse = new(
            en: "Language in use",
            ja: "現在の表示言語"
        );

        public static readonly LocalizedText TextHelpHeader = new(
            en: "Help",
            ja: "ヘルプ"
        );

        public static readonly LocalizedText TextShowHelp = new(
            en: "Show help messages",
            ja: "ヘルプメッセージを表示"
        );

        public static readonly LocalizedText TextImportHeader = new(
            en: "Import",
            ja: "インポート"
        );

        public static readonly LocalizedText TextSaveAbsPath = new(
            en: "Save absolute path",
            ja: "絶対パスを保存"
        );
    }
}
