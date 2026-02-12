using System.IO;

using UnityEditor;
using UnityEngine;

using Tenkai.SourceLinkedAsset.Domain;
using Tenkai.SourceLinkedAsset.EditorService;

namespace Tenkai.SourceLinkedAsset.EditorActions {

    /// <summary>
    /// 外部ソースファイルを Unity アセットとして取り込み、
    /// ソース情報を保持した「Source Linked Asset」を作成するためのユーティリティ。
    /// </summary>
    public static class SourceImporter {

        /// <summary>
        /// 単一の外部ソースファイルをインポートする。
        /// 失敗した場合は Result を返さず、エラーログとして出力する。
        /// </summary>
        public static void ImportOneOrLogError(string srcAbsPath, bool overwrite) {
            if (ImportOne(srcAbsPath, overwrite).TryGetFailReason(out string failReason)) {
                Debug.LogError(failReason);
            }
        }

        /// <summary>
        /// 単一の外部ソースファイルをインポートし、
        /// インポート結果を Result として返す。
        /// </summary>
        public static Result ImportOne(string strSrcAbsPath, bool overwrite) {

            // 失敗しうる「値の決定」だけを LINQ に寄せる
            Result<ImportContext> resCtx =
                from srcAbsPath in AbsPath
                                        .Create(strSrcAbsPath)
                                        .Where(
                                            p => p.ExistsFile(),
                                            p => $"Source file not found: {p}")
                from assetDestFolder in AssetPathUtil.GetCurrentDirectory()
                let assetDestPathNotUnique = assetDestFolder.Combine(srcAbsPath.GetFileName())
                from assetDestPath in ResolveDestAssetPath(assetDestPathNotUnique, overwrite)
                from destAbsPath in AssetPathUtil.ToAbsPath(assetDestPath)
                from sha256 in HashUtil.ComputeSha256Hex(srcAbsPath)
                select new ImportContext(srcAbsPath, assetDestPath, destAbsPath, sha256);

            return resCtx.Match(
                ok: ctx => {
                    // ===== ここから副作用まとめ =====

                    var dir = ctx.DestAbsPath.GetDirectoryName();
                    if (!string.IsNullOrEmpty(dir)) {
                        Directory.CreateDirectory(dir);
                    }

                    var resCopy = AbsFileUtil.Copy(ctx.SrcAbsPath, ctx.DestAbsPath, overwrite: true);
                    if (resCopy.TryGetFailReason(out var copyFailReason))
                        return Result.Fail(copyFailReason);

                    AssetDatabase.ImportAsset(ctx.AssetDestPath.AsAssetPath, ImportAssetOptions.ForceUpdate);

                    var projectStg = SLASettingsCache.GetProject();
                    var userStg = SLASettingsCache.GetUser();

                    string sourceRootId = "";
                    string sourceRel = "";

                    if (SLAUserSettingsIO.TryFindBestRootForFile(projectStg, userStg, ctx.SrcAbsPath, out var foundId, out var rootDir)) {
                        sourceRootId = foundId;
                        sourceRel = SLAUserSettingsIO.MakeRelativePath(rootDir, ctx.SrcAbsPath);
                    }
                    else {
                        Debug.LogWarning(
                            $"[SourceLinkedAsset] No source root matched. rootId/sourceRel will be empty.\n" +
                            $"Source: {ctx.SrcAbsPath}\n" +
                            $"Open: Tools/SourceLinkedAsset/Window/Source Roots"
                        );
                    }

                    var importer = AssetImporter.GetAtPath(ctx.AssetDestPath.AsAssetPath);
                    if (importer != null) {
                        var link = new AssetSourceLink {
                            version = AssetSourceLink.CurrentVersion,
                            sourceRootId = sourceRootId,
                            sourceRel = sourceRel,
                            sourceAbs = userStg.saveAbsolutePath ? ctx.SrcAbsPath.AsStoredPath : "",
                            sourceLastWriteUtc = File.GetLastWriteTimeUtc(ctx.SrcAbsPath.AsOsPath).Ticks,
                            sourceSizeBytes = new FileInfo(ctx.SrcAbsPath.AsOsPath).Length,
                            sourceSha256 = ctx.SrcSha256Hex,
                            assetPathAtImport = ctx.AssetDestPath.AsAssetPath,
                        };

                        ImporterUserDataUtil.UpsertAssetSourceLink(importer, link);
                        importer.SaveAndReimport();
                    }

                    var obj = AssetDatabase.LoadMainAssetAtPath(ctx.AssetDestPath.AsAssetPath);
                    if (obj != null) {
                        Selection.activeObject = obj;
                    }

                    Debug.Log($"Imported source-linked asset: {ctx.AssetDestPath.AsAssetPath}");
                    return Result.Ok;
                },
                fail: err => Result.Fail(err)
            );
        }

        // ===

        /// <summary>
        /// 書き込み先 AssetPath を解決する。
        /// overwriteExistingAsset が true の場合はユニーク化せず、そのまま採用する。
        /// </summary>
        private static Result<AssetPath> ResolveDestAssetPath(AssetPath notUnique, bool overwriteExistingAsset) =>
            overwriteExistingAsset
                ? notUnique.ToOk()
                : AssetPathUtil.ToUnique(notUnique);

        /// <summary>
        /// ImportOne で必要な中間値をまとめるコンテキスト。
        /// </summary>
        internal readonly struct ImportContext {
            public readonly AbsPath SrcAbsPath;
            public readonly AssetPath AssetDestPath;
            public readonly AbsPath DestAbsPath;
            public readonly string SrcSha256Hex;

            public ImportContext(
                AbsPath srcAbsPath,
                AssetPath assetDestPath,
                AbsPath destAbsPath,
                string srcSha256Hex
            ) {
                SrcAbsPath = srcAbsPath;
                AssetDestPath = assetDestPath;
                DestAbsPath = destAbsPath;
                SrcSha256Hex = srcSha256Hex;
            }
        }
    }

}
