using System.IO;
using UnityEditor;

using Tenkai.SourceLinkedAsset.EditorActions;

namespace Tenkai.SourceLinkedAsset.UI {

    /// <summary>
    /// Tools メニュー（上部）
    /// </summary>
    public static class ToolsMenus {

        // === Import
        // ファイルピッカー
        [MenuItem("Tools/SourceLinkedAsset/Import From Source File")]
        public static void ImportFromSourceFile_NoOverwrite() =>
            ImportFromSourceFile(false);

        [MenuItem("Tools/SourceLinkedAsset/Import From Source File (Overwrite)")]
        public static void ImportFromSourceFile_Overwrite() =>
            ImportFromSourceFile(true);

        private static void ImportFromSourceFile(bool overwrite) {
            var sourcePath = EditorUtility.OpenFilePanel("Select source file", "", "");
            if (string.IsNullOrEmpty(sourcePath)) return;
            SourceImporter.ImportOneOrLogError(sourcePath, overwrite);
        }


        // === ドラッグ&ドロップ用ウィンドウ
        [MenuItem("Tools/SourceLinkedAsset/Window/Import Window")]
        public static void OpenWindow() =>
            SourceImportWindow.ShowWindow();

        // === Reimport
        [MenuItem("Tools/SourceLinkedAsset/Reimport Selected")]
        private static void ReimportSelected_FromTools() =>
            SourceReimporter.ReimportSelectedOrLogError();

        [MenuItem("Tools/SourceLinkedAsset/Reimport Selected (Pick File...)")]
        private static void ReimportSelectedPick_FromTools() =>
            SourceReimporter.ReimportSelectedWithPickFileOrLogError();
    }

}