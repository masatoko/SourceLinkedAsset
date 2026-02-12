using Tenkai.SourceLinkedAsset.Domain;

namespace Tenkai.SourceLinkedAsset.EditorService {

    /// <summary>
    /// SourceLinkedAsset の設定を Editor セッション中キャッシュする。
    /// </summary>
    internal static class SLASettingsCache {

        // === SLAProjectSettings ===
        private static SLAProjectSettings _project;

        public static SLAProjectSettings GetProject() {
            if (_project == null) {
                _project = SLAProjectSettingsIO.Load();
            }
            return _project;
        }

        public static void SaveProject() {
            if (_project != null) {
                SLAProjectSettingsIO.Save(_project);
            }
        }
        // ===


        // === SLAUserSettings
        private static SLAUserSettings _user;

        public static SLAUserSettings GetUser() {
            var project = GetProject();
            if (_user == null) {
                _user = SLAUserSettingsIO.Load(project);
            }
            return _user;
        }

        public static void SaveUser() {
            if (_user != null) {
                SLAUserSettingsIO.Save(_user);
            }
        }
        // ===
    }
}
