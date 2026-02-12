using System;

namespace Tenkai.SourceLinkedAsset.Domain {

    [Serializable]
    public class AssetSourceLink {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        public string sourceAbs;           // 外部ファイル絶対パス
        public string sourceRootId;        // ソースルートのID
        public string sourceRel;           // ソースルートからの相対パス
        public long sourceLastWriteUtc;    // 最終更新(UTC)
        public long sourceSizeBytes;       // サイズ
        public string sourceSha256;        // ハッシュ
        public string assetPathAtImport;   // Import時のAssets内パス
    }

}