using System;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEngine;

using Tenkai.SourceLinkedAsset.Domain;
using Tenkai.SourceLinkedAsset.EditorService;

namespace Tenkai.SourceLinkedAsset.EditorActions {

    /// <summary>
    /// SourceLinkedAsset の再インポート処理を提供するユーティリティ。
    /// 選択アセットの再インポート、ソースの再指定（リリンク）を扱う。
    /// </summary>
    public static class SourceReimporter {

        /// <summary>
        /// Reimport の処理結果
        /// </summary>
        internal enum ReimportResultKind {
            Reimported,     // コピー + Import + SaveAndReimport
            Skipped,        // 内容一致で何もせず
            RelinkedOnly,   // リンク更新のみ（PickFile + 同一内容）
        }

        /// <summary>
        /// 選択に "Assets/" 配下のファイルが1つでもあるか（メニューから呼ぶ想定）。
        /// </summary>
        public static bool HasAnyAssetsInSelection() =>
            AssetPathUtil.GetSelectionAssetPaths().Any();

        /// <summary>
        /// 選択アセットをまとめて再インポートし、失敗はログに出す。
        /// </summary>
        public static void ReimportSelectedOrLogError() {
            var assetPaths = AssetPathUtil.GetSelectionAssetPaths();

            if (assetPaths.Count() == 0) {
                Debug.LogWarning("No asset selected.");
                return;
            }

            int reimported = 0, skipped = 0, relinked = 0, failed = 0;
            foreach (var assetPath in assetPaths) {
                var res = ReimportOne(assetPath);

                if (res.TryGetOk(out var kind)) {
                    switch (kind) {
                        case ReimportResultKind.Reimported: reimported++; break;
                        case ReimportResultKind.Skipped: skipped++; break;
                        case ReimportResultKind.RelinkedOnly: relinked++; break;
                    }
                }
                else {
                    Debug.LogError(res.FailReason);
                    failed++;
                }
            }

            Debug.Log($"Source Linked Reimport done. reimported={reimported}, skipped={skipped}, relinked={relinked}, fail={failed}");
        }

        /// <summary>
        /// ファイルピッカーでソースを選び直して、最初の1件だけ再インポートする。
        /// </summary>
        public static void ReimportSelectedWithPickFileOrLogError() {
            var assetPaths = AssetPathUtil.GetSelectionAssetPaths();

            if (assetPaths.Count() == 0) {
                Debug.LogWarning("No asset selected.");
                return;
            }

            // まとめて同じソースに張り替えるのは危険なので、
            // ここでは「最初の1件だけ」ピックして、それに対して行う
            if (assetPaths.Count() > 1) {
                Debug.LogWarning("Multiple assets selected. Pick File... will apply only to the first asset.");
            }

            var assetPath = assetPaths.First();
            var res = ReimportOneWithPickFile(assetPath);
            if (res.TryGetFailReason(out var reason)) {
                Debug.LogError(reason);
            }
        }

        // ----------------------------
        // Reimport core
        // ----------------------------

        /// <summary>
        /// userData のリンク情報からソースを解決し、物理コピーしてから再インポートする。
        /// </summary>
        private static Result<ReimportResultKind> ReimportOne(AssetPath assetPath) {

            var projectStg = SLASettingsCache.GetProject();
            var userStg = SLASettingsCache.GetUser();

            Result<ReimportResultKind> res =
                from importer in AssetPathUtil.ToAssetImporter(assetPath)
                from link in GetOrCreateLinkFromImporter(importer, requireLinked: true)
                from sourceAbsPath in SourceLinkResolver
                                .ResolveSourceAbsPath(projectStg, userStg, link)
                                .Where(
                                    p => p.ExistsFile(),
                                    p => $"Source file not found: {p}\nAsset: {assetPath}\n" +
                                        $"Hint: Check Source Roots (Tools/Source Linked/Source Roots.)"
                                )
                from kind in ReimportFromSourceAbsPath(
                    assetPath: assetPath,
                    importer: importer,
                    link: link,
                    projectStg: projectStg,
                    userStg: userStg,
                    sourceAbs: sourceAbsPath,
                    updateSourceAbs: false
                )
                select kind;

            // 失敗理由を加工
            return res.Match(
                ok: k => k.ToOk(),
                fail: err => Result<ReimportResultKind>.Fail($"ReimportOne('{assetPath}') failed : {err}")
            );
        }

        /// <summary>
        /// ファイルピッカーでソースを指定し直し、リンク更新してから再インポートする。
        /// </summary>
        private static Result<ReimportResultKind> ReimportOneWithPickFile(AssetPath assetPath) {

            // --- ファイルピッカーによるパス取得関数 ---
            Result<AbsPath> PickSourceAbsPath(string strInitialDir) {
                var strPickedSourceAbsPath = EditorUtility.OpenFilePanel("Select source file", strInitialDir, "");
                return AbsPath.Create(strPickedSourceAbsPath)
                    .Where(p => p.ExistsFile(), p => $"Source file not found: {p}");
            }
            // ----- ----- ----- ----- ----- ----- ---

            var projectStg = SLASettingsCache.GetProject();
            var userStg = SLASettingsCache.GetUser();

            Result<ReimportResultKind> res =
                from importer in AssetPathUtil.ToAssetImporter(assetPath)
                from link in GetOrCreateLinkFromImporter(importer, requireLinked: false)
                let strInitialDir = GetInitialDirForPick(link)
                from pickedSourceAbsPath in PickSourceAbsPath(strInitialDir)
                from kind in ReimportFromSourceAbsPath(
                    assetPath: assetPath,
                    importer: importer,
                    link: link,
                    projectStg: projectStg,
                    userStg: userStg,
                    sourceAbs: pickedSourceAbsPath,
                    updateSourceAbs: true
                )
                select kind;

            return res;
        }

        // ----------------------------
        // Shared
        // ----------------------------

        /// <summary>
        /// AssetImporter を取得し、存在しなければ失敗を返す。
        /// </summary>
        private static Result GetImporter(string assetPath, out AssetImporter importer) {
            importer = null;

            if (string.IsNullOrEmpty(assetPath))
                return Result.Fail("Asset path is empty.");

            importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null)
                return Result.Fail($"Importer not found: {assetPath}");

            return Result.Ok;
        }

        /// <summary>
        /// userData から link を取得する。
        /// requireLinked が false の場合、存在しなければ新規作成する。
        /// </summary>
        private static Result<AssetSourceLink> GetOrCreateLinkFromImporter(AssetImporter importer, bool requireLinked) {
            if (importer == null)
                return Result<AssetSourceLink>.Fail("Importer is null.");

            // Importer から取得
            var resLink = ImporterUserDataUtil.GetLinkFromImporter(importer);

            if (resLink.TryGetOk(out var link))
                return link.ToOk(); // 存在したら即座に返す

            // 既存なし かつ 必要 ならば失敗
            if (requireLinked)
                return Result<AssetSourceLink>.Fail($"No AssetSourceLink: {importer.assetPath} - {resLink.FailReason}");

            // 作成
            return Result<AssetSourceLink>.Ok(
                    new AssetSourceLink {
                        version = AssetSourceLink.CurrentVersion
                    });
        }

        /// <summary>
        /// ファイルピッカーの初期ディレクトリを決める。
        /// </summary>
        private static string GetInitialDirForPick(AssetSourceLink link) {
            // OpenFilePanel は不正な初期ディレクトリを渡すと環境依存の挙動になる可能性がある。
            // ここでは「存在するディレクトリだけ」を返し、ダメなら空文字（Unity側のデフォルト挙動）に任せる。

            if (link == null || string.IsNullOrEmpty(link.sourceAbs)) return "";

            var res = AbsPath.Create(link.sourceAbs);
            if (!res.TryGetOk(out var abs)) return "";

            try {
                var dir = Path.GetDirectoryName(abs.AsOsPath);
                return (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) ? dir : "";
            }
            catch {
                return "";
            }
        }


        /// <summary>
        /// 指定された srcAbs から assetPath を更新し、リンク情報も同期状態へ更新する。
        /// updateSourceAbs が true の場合、link.sourceAbs と rootId/rel も更新する。
        /// </summary>
        private static Result<ReimportResultKind> ReimportFromSourceAbsPath(
            AssetPath assetPath,
            AssetImporter importer,
            AssetSourceLink link,
            SLAProjectSettings projectStg,
            SLAUserSettings userStg,
            AbsPath sourceAbs,
            bool updateSourceAbs
        ) {
            if (importer == null) return Result<ReimportResultKind>.Fail("Importer is null.");
            if (link == null) return Result<ReimportResultKind>.Fail("AssetSourceLink is null.");

            if (!sourceAbs.ExistsFile())
                return Result<ReimportResultKind>.Fail($"Source file not found: {sourceAbs}");

            // 比較用の SHA-256 を先に計算
            var resCurrentSha = HashUtil.ComputeSha256Hex(sourceAbs);
            if (!resCurrentSha.TryGetOk(out string currentSha))
                return Result<ReimportResultKind>.Fail($"HashUtil.ComputeSha256Hex failed: {resCurrentSha.FailReason}");

            bool hasPrevSha = !string.IsNullOrEmpty(link.sourceSha256);
            bool isSameContent =
                hasPrevSha &&
                string.Equals(link.sourceSha256, currentSha, StringComparison.OrdinalIgnoreCase);

            // relink は内容が同じでもリンク更新だけはしたい
            if (updateSourceAbs) {
                UpdateLinkSourceLocation(projectStg, userStg, link, sourceAbs);

                if (isSameContent) {
                    // 内容一致の場合はコピーと再インポートをスキップして、リンク情報だけ保存する
                    UpdateLinkSyncInfo(link, sourceAbs, assetPath, currentSha);
                    ImporterUserDataUtil.UpsertAssetSourceLink(importer, link);

                    // userData をディスクへ書き出す（再インポートはしない）
                    AssetDatabase.WriteImportSettingsIfDirty(assetPath.AsAssetPath);

                    Debug.Log($"Relinked (no changes): {assetPath}\n<- {sourceAbs}");
                    return Result<ReimportResultKind>.Ok(ReimportResultKind.RelinkedOnly);
                }
            }
            else {
                // 通常 reimport は内容一致なら何もしない
                if (isSameContent) {
                    Debug.Log($"Source is up to date. Skip reimport: {assetPath}\n<- {sourceAbs}");
                    return Result<ReimportResultKind>.Ok(ReimportResultKind.Skipped);
                }
            }

            // 実ファイル上書き
            var resDestPath = AssetPathUtil.ToAbsPath(assetPath); // AssetPath -> AbsPath
            if (!resDestPath.TryGetOk(out AbsPath destAbsPath))
                return Result<ReimportResultKind>.Fail("");

            var resCopy = AbsFileUtil.Copy(sourceAbs, destAbsPath, overwrite: true);
            if (resCopy.TryGetFailReason(out var copyFail))
                return Result<ReimportResultKind>.Fail(copyFail);

            // 同期情報更新（hash は再計算しない）
            UpdateLinkSyncInfo(link, sourceAbs, assetPath, currentSha);

            // userData 差分更新
            ImporterUserDataUtil.UpsertAssetSourceLink(importer, link);

            // コピー内容を確実に拾わせる
            AssetDatabase.ImportAsset(assetPath.AsAssetPath, ImportAssetOptions.ForceUpdate);

            // meta 保存 + 再インポート
            importer.SaveAndReimport();

            Debug.Log($"Reimported from source: {assetPath}\n<- {sourceAbs}");
            return Result<ReimportResultKind>.Ok(ReimportResultKind.Reimported);
        }

        /// <summary>
        /// link のソース位置情報（sourceAbs, rootId, rel）を更新する。
        /// </summary>
        private static void UpdateLinkSourceLocation(
            SLAProjectSettings project,
            SLAUserSettings user,
            AssetSourceLink link,
            AbsPath srcAbs
        ) {
            link.sourceAbs = user.saveAbsolutePath ? srcAbs.AsStoredPath : "";

            if (SLAUserSettingsIO.TryFindBestRootForFile(project, user, srcAbs, out var foundId, out var rootDir)) {
                link.sourceRootId = foundId;
                link.sourceRel = SLAUserSettingsIO.MakeRelativePath(rootDir, srcAbs);
            }
            else {
                link.sourceRootId = "";
                link.sourceRel = "";
            }
        }

        /// <summary>
        /// link の同期情報を更新する。
        /// </summary>
        private static void UpdateLinkSyncInfo(AssetSourceLink link, AbsPath srcAbs, AssetPath assetPath, string sha256Hex) {
            link.sourceLastWriteUtc = File.GetLastWriteTimeUtc(srcAbs.AsOsPath).Ticks;
            link.sourceSizeBytes = new FileInfo(srcAbs.AsOsPath).Length;
            link.sourceSha256 = sha256Hex;
            link.assetPathAtImport = assetPath.ToStoredString();
        }
    }
}
