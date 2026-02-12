using System;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Tenkai.SourceLinkedAsset.Domain;

namespace Tenkai.SourceLinkedAsset.EditorService {

    /// <summary>
    /// AssetImporter.userData に保存する SourceLinkedAsset 用データの読み書きを担当する。
    /// </summary>
    internal static class ImporterUserDataUtil {

        /// <summary>
        /// userData に AssetSourceLink を追加または更新する。
        /// </summary>
        public static void UpsertAssetSourceLink(AssetImporter importer, AssetSourceLink link) {
            if (importer == null) return;
            if (link == null) return;

            var root = ParseOrCreate(importer.userData);

            var nsObj = root[UserDataSchema.RootKey] as JObject;
            if (nsObj == null) nsObj = new JObject();

            nsObj[UserDataSchema.LinkKey] = JObject.FromObject(link);
            root[UserDataSchema.RootKey] = nsObj;

            importer.userData = root.ToString(Formatting.None);
        }

        /// <summary>
        /// userData から AssetSourceLink を取得する
        /// </summary>
        [Obsolete("Use GetLinkFromImporter instead.")]
        public static bool TryGetAssetSourceLink(AssetImporter importer, out AssetSourceLink link) {
            link = null;

            if (importer == null) return false;
            if (string.IsNullOrEmpty(importer.userData)) return false;

            try {
                var root = JObject.Parse(importer.userData);

                if (!root.TryGetValue(UserDataSchema.RootKey, out var nsToken)) return false;
                if (nsToken is not JObject nsObj) return false;

                if (!nsObj.TryGetValue(UserDataSchema.LinkKey, out var token)) return false;

                link = token.ToObject<AssetSourceLink>();
                return link != null;
            }
            catch {
                // 何もしない
                return false;
            }
        }

        /// <summary>
        /// importer.userData から AssetSourceLink を取得する。
        /// </summary>
        public static Result<AssetSourceLink> GetLinkFromImporter(AssetImporter importer) {
            if (importer == null)
                return Result<AssetSourceLink>.Fail("Importer is null.");

            if (string.IsNullOrEmpty(importer.userData))
                return Result<AssetSourceLink>.Fail("importer.userData is null or empty.");

            try {
                var root = JObject.Parse(importer.userData);

                if (!root.TryGetValue(UserDataSchema.RootKey, out var nsToken))
                    return Result<AssetSourceLink>.Fail($"Missing root key: {UserDataSchema.RootKey}");

                if (nsToken is not JObject nsObj)
                    return Result<AssetSourceLink>.Fail($"Invalid root object type: {UserDataSchema.RootKey}");

                if (!nsObj.TryGetValue(UserDataSchema.LinkKey, out var linkToken))
                    return Result<AssetSourceLink>.Fail($"Missing link key: {UserDataSchema.RootKey}.{UserDataSchema.LinkKey}");

                var link = linkToken.ToObject<AssetSourceLink>();
                if (link == null)
                    return Result<AssetSourceLink>.Fail($"Failed to deserialize link: {UserDataSchema.RootKey}.{UserDataSchema.LinkKey}");

                if (link.version <= 0) link.version = AssetSourceLink.CurrentVersion;
                return Result<AssetSourceLink>.Ok(link);
            }
            catch (System.Exception e) {
                return Result<AssetSourceLink>.Fail($"Failed to parse importer.userData: {e.Message}");
            }
        }

        /// <summary>
        /// 文字列から JObject を生成し、失敗したら空の JObject を返す。
        /// </summary>
        private static JObject ParseOrCreate(string json) {
            if (string.IsNullOrEmpty(json)) return new JObject();
            try { return JObject.Parse(json); }
            catch { return new JObject(); }
        }
    }
}
