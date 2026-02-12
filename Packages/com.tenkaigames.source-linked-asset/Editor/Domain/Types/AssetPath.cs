using System;
using System.IO;

namespace Tenkai.SourceLinkedAsset.Domain {

    /// <summary>
    /// Unity の Assets 以下のパスのみを表現する Value Object。
    /// 正規化・検証されたパスのみを保持する。
    /// </summary>
    public sealed class AssetPath : IEquatable<AssetPath> {

        /// <summary>
        /// 正規化済みのアセットパス文字列（例: "Assets/Foo/Bar"）
        /// </summary>
        private string _value { get; }

        /// <summary>
        /// 外部からの生成を禁止し、Create 経由でのみ生成させる
        /// </summary>
        private AssetPath(string value) {
            _value = value;
        }

        /// <summary>
        /// 正規化済みのアセットパス文字列（例: "Assets/Foo/Bar"）
        /// </summary>
        public string AsAssetPath => _value;

        /// <summary>
        /// 文字列から AssetPath を生成するファクトリメソッド。
        /// </summary>
        public static Result<AssetPath> Create(string path) {
            // null または空文字は不正
            if (string.IsNullOrEmpty(path))
                return Result<AssetPath>.Fail("Path is empty.");

            // パスの区切り文字や末尾スラッシュを正規化
            path = Normalize(path);

            // Assets 配下でなければ無効
            if (!IsValidAssetPath(path))
                return Result<AssetPath>.Fail($"Not an asset path: {path}");

            return Result<AssetPath>.Ok(new AssetPath(path));
        }

        // ---- Validation ----

        /// <summary>
        /// Unity のアセットパスとして有効かを判定する。
        /// "Assets" または "Assets/..." のみ許可。
        /// </summary>
        private static bool IsValidAssetPath(string path) {
            if (path == "Assets") return true;
            return path.StartsWith("Assets/", StringComparison.Ordinal);
        }

        /// <summary>
        /// パスを正規化する。
        /// - バックスラッシュをスラッシュに変換
        /// - 末尾のスラッシュを除去
        /// </summary>
        private static string Normalize(string path) =>
            path
                .Replace('\\', '/')
                .TrimEnd('/');

        // ---- Convenience ----

        public bool IsValid() =>
            IsValidAssetPath(_value);

        /// <summary>
        /// 現在のパスにファイル名を結合した新しい AssetPath を返す。
        /// </summary>
        public AssetPath Combine(string fileName) {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException(nameof(fileName));

            var combined = $"{_value}/{fileName}";
            return new AssetPath(combined);
        }

        /// <summary>
        /// パスの末尾要素（ファイル名またはフォルダ名）を取得する。
        /// </summary>
        public string GetFileName() =>
            Path.GetFileName(_value);

        public string ToStoredString() => _value;

        // ---- Equality ----

        /// <summary>
        /// AssetPath 同士の値比較（Ordinal）
        /// </summary>
        public bool Equals(AssetPath other) {
            if (other == null) return false;
            return string.Equals(_value, other.AsAssetPath, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) =>
            obj is AssetPath other && Equals(other);

        public override int GetHashCode() =>
            _value.GetHashCode();

        public override string ToString() =>
            _value;
    }

}
