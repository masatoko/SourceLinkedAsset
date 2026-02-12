using UnityEditor;

using Tenkai.SourceLinkedAsset.EditorActions;

namespace Tenkai.SourceLinkedAsset.UI {

    /// <summary>
    /// Assets (右クリック) メニュー
    /// </summary>
    public static class AssetsMenus {
        [MenuItem("Assets/SourceLinkedAsset/Reimport From Source", priority = 2200)]
        private static void ReimportSelectedAssets_FromContext() {
            SourceReimporter.ReimportSelectedOrLogError();
        }

        [MenuItem("Assets/SourceLinkedAsset/Reimport From Source", validate = true)]
        private static bool ReimportSelectedAssets_FromContext_Validate() {
            return SourceReimporter.HasAnyAssetsInSelection();
        }

        [MenuItem("Assets/SourceLinkedAsset/Reimport From Source (Pick File...)", priority = 2201)]
        private static void ReimportSelectedAssets_PickFile_FromContext() {
            SourceReimporter.ReimportSelectedWithPickFileOrLogError();
        }

        [MenuItem("Assets/SourceLinkedAsset/Reimport From Source (Pick File...)", validate = true)]
        private static bool ReimportSelectedAssets_PickFile_FromContext_Validate() {
            return SourceReimporter.HasAnyAssetsInSelection(); // 選択がアセットならOK（リンク有無は後で案内）
        }
    }

}