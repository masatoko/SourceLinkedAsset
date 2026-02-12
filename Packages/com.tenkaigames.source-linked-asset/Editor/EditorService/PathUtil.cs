using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor;

using Tenkai.SourceLinkedAsset.Domain;

namespace Tenkai.SourceLinkedAsset.EditorService {

    /// <summary>
    /// Unity の AssetPath（"Assets/..." 形式）を扱うための補助ユーティリティ。
    /// Selection 取得、結合、絶対パス変換など、AssetPath に関する共通処理を提供する。
    /// </summary>
    internal static class AssetPathUtil {
        /// <summary>
        /// 指定された AssetPath を、既存アセットと衝突しないユニークなパスに変換する。
        /// </summary>
        /// <remarks>
        /// AssetDatabase.GenerateUniqueAssetPath を使用し、
        /// 同名アセットが存在する場合は連番付きのパスを生成する。
        /// </remarks>
        public static Result<AssetPath> ToUnique(AssetPath assetPath) {
            var unique = AssetDatabase.GenerateUniqueAssetPath(assetPath.AsAssetPath);
            return AssetPath.Create(unique);
        }

        /// <summary>
        /// AssetPath から対応する AssetImporter を取得する。
        /// </summary>
        /// <remarks>
        /// 指定されたパスにアセットが存在しない場合や、
        /// Importer を持たないアセットの場合は Fail を返す。
        /// </remarks>
        public static Result<AssetImporter> ToAssetImporter(AssetPath assetPath) {
            var importer = AssetImporter.GetAtPath(assetPath.AsAssetPath);
            return
                importer == null
                    ? Result<AssetImporter>.Fail($"Importer not found: {assetPath}")
                    : importer.ToOk();
        }

        /// <summary>
        /// 現在の Unity Editor の選択対象から、
        /// Assets 配下に存在する「ファイルの AssetPath」のみを列挙する。
        /// フォルダは除外される。
        /// </summary>
        public static IEnumerable<AssetPath> GetSelectionAssetPaths() =>
            Selection.assetGUIDs
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .Select(AssetPath.Create)
                .Where(r => r.IsOk)
                .Select(r => r.GetOrThrow()) // Result<AssetPath> をアンラップ
                .Where(p => !AssetDatabase.IsValidFolder(p.AsAssetPath)); // フォルダは除外

        // Project ウィンドウで現在アクティブなフォルダを取得する。
        // 
        // ・ProjectBrowser は内部状態に依存するため、
        //   Project ウィンドウが一度も開かれていない場合は例外が発生する。
        // ・その場合は Result.Fail を返し、
        //   ユーザーに Project ウィンドウを開くよう促すメッセージを含める。
        // 
        // 戻り値は "Assets" から始まるアセットフォルダパス
        // 例: "Assets/SomeFolder"
        internal static Result<AssetPath> GetCurrentDirectory() {

            Result<AssetPath> MkFail(string msg) =>
                Result<AssetPath>.Fail($"PathUtil.GetCurrentDirectory() failed: {msg}");

            try {
                var flags = BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Static |
                            BindingFlags.Instance;

                var asm = typeof(Editor).Assembly; // UnityEditor.dll
                var type = asm.GetType("UnityEditor.ProjectBrowser");
                if (type == null)
                    return MkFail("ProjectBrowser type not found.");

                var win = EditorWindow.GetWindow(type);
                if (win == null)
                    return MkFail("ProjectBrowser window not available.");

                var method = type.GetMethod("GetActiveFolderPath", flags);
                if (method == null)
                    return MkFail("GetActiveFolderPath method not found.");

                var folderPathStr = method.Invoke(win, null) as string;
                if (!AssetDatabase.IsValidFolder(folderPathStr))
                    return MkFail($"Invalid asset folder path: {folderPathStr}");

                return AssetPath.Create(folderPathStr);
            }
            catch (Exception) {
                var lang = SLASettingsCache.GetUser().GetResolvedLanguage();
                var TextOpenProjectWindow = new LocalizedText(
                    en: "Failed to get the active folder from the Project window. Please open the Project window once and try again.",
                    ja: "ProjectBrowser からアクティブなフォルダを取得できませんでした。Projectウィンドウを一度開いてから再実行してください。"
                );
                return MkFail(TextOpenProjectWindow.GetByLang(lang));
            }
        }

        /// <summary>
        /// AssetPath（"Assets/..."）を OS の絶対パスに変換する。
        /// </summary>
        /// <remarks>
        /// 例：
        /// "Assets/Foo/bar.png"
        /// -> "C:/Project/Assets/Foo/bar.png"
        /// </remarks>
        internal static Result<AbsPath> ToAbsPath(AssetPath assetPath) {
            if (!assetPath.IsValid())
                return Result<AbsPath>.Fail($"AssetPath is not valid: {assetPath.AsAssetPath}");

            // Application.dataPath は ".../Project/Assets" を指すため、
            // その親ディレクトリをプロジェクトルートとして取得する
            var strProjectRoot = Directory.GetParent(UnityEngine.Application.dataPath).FullName;

            return
                AbsPath.Create(strProjectRoot)
                    .Match(
                        ok: projectRoot =>
                            // AssetPath は "Assets/..." を含む相対パスとして扱う
                            // var abs = Path.Combine(projectRoot, rawAssetPath);
                            projectRoot.Combine(assetPath.AsAssetPath)
                        ,
                        fail: failReason =>
                            Result<AbsPath>.Fail($"Project root could not be resolved: {failReason}")
                    );
        }
    }

}
