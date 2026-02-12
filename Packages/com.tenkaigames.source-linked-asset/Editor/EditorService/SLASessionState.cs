namespace Tenkai.SourceLinkedAsset.EditorService {

    /// <summary>
    /// SourceLinkedAsset の Editor セッション中のみ保持する状態を管理する。
    /// 永続化しない（ProjectSettings / UserSettings には保存しない）
    /// </summary>
    public static class SLASessionState {

        /// <summary>
        /// ドラッグ＆ドロップインポート時に既存アセットを上書きするかどうか。
        /// </summary>
        public static bool ImportOverwriteEnabled {
            get => _importOverwriteEnabled;
            set => _importOverwriteEnabled = value;
        }

        private static bool _importOverwriteEnabled = false;

        /// <summary>
        /// セッション状態を既定値へ戻す。
        /// </summary>
        public static void Reset() {
            _importOverwriteEnabled = false;
        }
    }
}
