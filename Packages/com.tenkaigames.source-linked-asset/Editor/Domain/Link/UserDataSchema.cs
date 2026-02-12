namespace Tenkai.SourceLinkedAsset.Domain {

    /// <summary>
    /// AssetImporter.userData 内の SourceLinkedAsset 名前空間とキーを定義する。
    /// JSON構造の「契約」をここに集約する。
    /// </summary>
    public static class UserDataSchema {
        public const string RootKey = "tenkai.sourceLinkedAsset";
        public const string LinkKey = "assetSourceLink";
    }
}
