using Tenkai.SourceLinkedAsset.Domain;

namespace Tenkai.SourceLinkedAsset.EditorService {

    internal static class SourceLinkResolver {

        /// <summary>
        /// AssetSourceLink に基づいてソースファイルの絶対パスを解決する。
        ///
        /// rootId と relative path が両方揃っている場合はそれを優先し、
        /// 解決に失敗した場合のみ absolute path を使用する。
        ///
        /// いずれの情報も存在しない場合は Fail を返す。
        /// </summary>
        public static Result<AbsPath> ResolveSourceAbsPath(
            SLAProjectSettings project,
            SLAUserSettings user,
            AssetSourceLink link
        ) {
            // link 自体が存在しない場合は解決不能
            if (link == null)
                return Result<AbsPath>.Fail("AssetSourceLink is null.");

            // rootId + relative path による解決を最優先する
            if (!string.IsNullOrEmpty(link.sourceRootId) &&
                !string.IsNullOrEmpty(link.sourceRel) &&
                SLAUserSettingsIO.TryResolveByRootId(
                    project,
                    user,
                    link.sourceRootId,
                    link.sourceRel,
                    out var resolved
                )) {

                return AbsPath.Create(resolved);
            }

            // 上記が失敗した場合は absolute path をフォールバックとして使用する
            if (!string.IsNullOrEmpty(link.sourceAbs)) {
                return AbsPath.Create(link.sourceAbs);
            }

            // 解決に必要な情報がどちらも存在しない
            return Result<AbsPath>.Fail(
                "Missing source info (rootId/rel and sourceAbs are empty)."
            );
        }
    }
}
