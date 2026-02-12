using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using Tenkai.SourceLinkedAsset.Domain;

namespace Tenkai.SourceLinkedAsset.EditorService {

    internal static class HashUtil {

        /// <summary>
        /// 指定されたファイルの内容から SHA-256 ハッシュを計算し、
        /// 小文字16進数文字列として返す。
        ///
        /// 同期判定や変更検出用であり、暗号用途は想定しない。
        /// </summary>
        internal static Result<string> ComputeSha256Hex(AbsPath absPath) {
            if (absPath == null)
                return Result<string>.Fail("AbsPath is null.");

            try {
                using var sha = SHA256.Create();
                using var stream = File.OpenRead(absPath.AsOsPath);
                var hash = sha.ComputeHash(stream);

                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    sb.Append(hash[i].ToString("x2"));

                return sb.ToString().ToOk();
            }
            catch (Exception e) {
                return Result<string>.Fail(
                    $"Failed to compute SHA-256.\nPath: {absPath}\n{e.Message}"
                );
            }
        }

    }
}
