using System;
using System.IO;

using Tenkai.SourceLinkedAsset.Domain;

namespace Tenkai.SourceLinkedAsset.EditorService {

    /// <summary>
    /// ファイル操作に関する低レベルユーティリティ。
    /// </summary>
    public static class AbsFileUtil {

        /// <summary>
        /// source から dest へファイルをコピーする。
        /// 失敗した場合は例外を投げず、Result.Fail を返す。
        /// </summary>
        public static Result Copy(AbsPath source, AbsPath dest, bool overwrite) {
            if (source == null) return Result.Fail("Source path is null.");
            if (dest == null) return Result.Fail("Destination path is null.");

            var src = source.AsOsPath;
            var dst = dest.AsOsPath;

            try {
                if (!File.Exists(src))
                    return Result.Fail($"Source file not found: {src}");

                var dir = dest.GetDirectoryName();
                if (!string.IsNullOrEmpty(dir)) {
                    Directory.CreateDirectory(dir);
                }

                File.Copy(src, dst, overwrite);
                return Result.Ok;
            }
            catch (Exception e) {
                return Result.Fail(
                    $"File copy failed.\n" +
                    $"Source: {src}\n" +
                    $"Dest: {dst}\n" +
                    $"{e.Message}"
                );
            }
        }
    }
}
