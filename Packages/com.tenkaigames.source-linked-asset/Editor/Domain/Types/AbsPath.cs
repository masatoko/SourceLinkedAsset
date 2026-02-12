using System;
using System.IO;

namespace Tenkai.SourceLinkedAsset.Domain {

    /// <summary>
    /// ファイルシステム上の「絶対パス」を表す Value Object。
    /// 常に Path.GetFullPath によって正規化されたパスのみを保持する。
    /// </summary>
    public sealed class AbsPath : IEquatable<AbsPath> {

        /// <summary>
        /// OS ネイティブ形式で保持する、
        /// Path.GetFullPath により正規化済みの絶対パス。
        /// このクラス内部での基準となる表現。
        /// </summary>
        private string _osPath { get; }

        /// <summary>
        /// 外部からの直接生成を禁止し、Create 経由でのみ生成させる
        /// </summary>
        private AbsPath(string osPath) {
            _osPath = osPath;
        }

        /// <summary>
        /// OS ネイティブ形式の絶対パスを返す。
        /// File / Directory / Path など、
        /// System.IO の API に渡す用途専用。
        /// </summary>
        public string AsOsPath => _osPath;

        /// <summary>
        /// 区切り文字を '/' に統一したパス表現を返す。
        /// 
        /// OS 非依存での比較・判定を目的とした形式であり、
        /// 文字列比較（StartsWith など）に安全に使用できる。
        /// 
        /// System.IO API へ渡す用途は想定しない。
        /// </summary>
        public string AsSlashPath => _osPath.Replace('\\', '/');

        /// <summary>
        /// 永続化・比較向けのパス表現。
        /// 現状は AsSlashPath と同一だが、
        /// 将来フォーマット変更の余地を残す。
        /// </summary>
        public string AsStoredPath => AsSlashPath;

        /// <summary>
        /// 表示向けのパス表現。
        /// 現状は AsStoredPath と同一。
        /// </summary>
        public string AsDisplayPath => AsSlashPath;

        /// <summary>
        /// 文字列から AbsPath を生成するファクトリメソッド。
        /// Path.GetFullPath に失敗した場合は Result.Fail を返す。
        /// </summary>
        public static Result<AbsPath> Create(string path) {
            // null または空文字は不正
            if (string.IsNullOrEmpty(path))
                return Result<AbsPath>.Fail("Path is empty.");

            try {
                // 相対パスを含めて絶対パスに正規化
                var osNativePath = Path.GetFullPath(path);
                return new AbsPath(osNativePath).ToOk();
            }
            catch (Exception e) {
                // 不正なパス文字などで例外が出た場合
                return Result<AbsPath>.Fail($"Invalid path: {e.Message}");
            }
        }

        // ---- Convenience ----

        /// <summary>
        /// このパスが既存のファイルを指しているか
        /// </summary>
        public bool ExistsFile() =>
            File.Exists(_osPath);

        public static bool ExistsFile(AbsPath absPath) => absPath.ExistsFile();

        /// <summary>
        /// このパスが既存のディレクトリを指しているか
        /// </summary>
        public bool ExistsDirectory() =>
            Directory.Exists(_osPath);

        /// <summary>
        /// 現在のパスを基準に相対パスを結合し、
        /// 新しい絶対パスとして返す。
        /// </summary>
        public Result<AbsPath> Combine(string relative) =>
            Create(
                Path.Combine(_osPath, relative));

        public string GetFileName() =>
            Path.GetFileName(_osPath);

        public string GetDirectoryName() =>
            Path.GetDirectoryName(_osPath);

        // ---- Equality ----

        /// <summary>
        /// OS に応じて大文字小文字の扱いを切り替えて比較する。
        /// Windows: case-insensitive
        /// それ以外: case-sensitive
        /// </summary>
        public bool Equals(AbsPath other) {
            if (other == null) return false;

            var comp = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            return string.Equals(_osPath, other._osPath, comp);
        }

        public override bool Equals(object obj) =>
            obj is AbsPath other && Equals(other);

        public override int GetHashCode() => _osPath.GetHashCode();

        public override string ToString() => AsStoredPath;
    }

}
