using System;
using System.IO;

using UnityEngine;
using Newtonsoft.Json;

using Tenkai.SourceLinkedAsset.Domain;

namespace Tenkai.SourceLinkedAsset.EditorService {

    public static class SLAUserSettingsIO {

        private const string FileName = "SourceLinkedAsset.json";

        /// <summary>ProjectRoot/UserSettings/SourceLinkedAsset.json</summary>
        public static string GetSettingsFilePath() {
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            return Path.Combine(projectRoot, "UserSettings", FileName);
        }

        /// <summary>
        /// SLASettingsCache からの実行のみを許可
        /// </summary>
        public static SLAUserSettings Load(SLAProjectSettings project) {
            var path = GetSettingsFilePath();
            if (!File.Exists(path)) return new SLAUserSettings();

            try {
                var json = File.ReadAllText(path);
                var s = JsonConvert.DeserializeObject<SLAUserSettings>(json);

                if (s != null && s.sourceRootDict != null) {
                    PruneUnknownKeys(project, s);
                    return s;
                }

                return new SLAUserSettings();
            }
            catch (Exception e) {
                Debug.LogWarning($"[SourceLinkedAsset] Failed to load user settings: {path}\n{e}");
                return new SLAUserSettings();
            }
        }

        /// <summary>
        /// SLASettingsCache からの実行のみを許可
        /// </summary>
        public static void Save(SLAUserSettings settings) {
            var path = GetSettingsFilePath();
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(path, json);

                Debug.Log($"[SourceLinkedAsset] User settings saved: {path}");
            }
            catch (Exception e) {
                Debug.LogError($"[SourceLinkedAsset] Failed to save user settings: {path}\n{e}");
            }
        }

        private static void PruneUnknownKeys(SLAProjectSettings project, SLAUserSettings user) {
            if (project?.sourceRootDict == null || user?.sourceRootDict == null) return;

            // project に存在しない rootId はユーザー側から除去（任意）
            // ※消したくない場合はこの関数を呼ばないでOK
            var remove = new System.Collections.Generic.List<string>();
            foreach (var kv in user.sourceRootDict) {
                if (!project.sourceRootDict.ContainsKey(kv.Key)) remove.Add(kv.Key);
            }
            foreach (var id in remove) user.sourceRootDict.Remove(id);
        }

        /// <summary>
        /// 指定された srcAbs が複数のソースルート配下に一致する場合、
        /// ディレクトリ階層が最も深い（最も長い）ルートを選択する。
        ///
        /// 処理:
        /// 正規化したルートディレクトリ文字列に対して
        /// srcAbs のパスがプレフィックス一致するもののうち
        /// 一致長が最大となるルートを採用する
        /// </summary>
        public static bool TryFindBestRootForFile(
            SLAProjectSettings project, SLAUserSettings user, AbsPath srcAbs,
            out string rootId, out AbsPath rootDir) {

            rootId = null;
            rootDir = null;

            // ルート定義（Project）と実パス設定（User）が両方揃っていないと解決不能
            if (project?.sourceRootDict == null || project.sourceRootDict.Count == 0) return false;
            if (user?.sourceRootDict == null || user.sourceRootDict.Count == 0) return false;

            // 現時点で採用しているルートの「深さ」指標。
            // NormalizeDir の結果（末尾 '/' 付き）の文字列長を使い、
            // 文字列長が長いほど階層が深い（より具体的な）ルートとして扱う。
            int bestLen = -1;

            foreach (var def in project.sourceRootDict) {
                var id = def.Key;
                if (string.IsNullOrEmpty(id)) continue;

                // User 側にこの rootId の実ディレクトリが設定されていない場合は対象外
                if (!user.sourceRootDict.TryGetValue(id, out var p) || string.IsNullOrEmpty(p)) continue;

                // 比較用にディレクトリを正規化する（絶対化 + '/' 統一 + 末尾 '/' 付与）
                var dir = NormalizeDir(p);

                // srcAbs がこのルート配下に無いなら候補外
                if (!IsUnder(srcAbs, dir)) continue;

                // ネストして複数一致した場合は、階層が最も深いルートを採用する。
                // 技術的には、srcAbs に対して最長のプレフィックス一致（Longest Prefix Match）となる dir を選ぶ。
                if (dir.Length > bestLen) {
                    bestLen = dir.Length;
                    rootId = id;

                    // 採用ルートの正規化済み絶対パスを Value Object 化して返す
                    var res = AbsPath.Create(dir);
                    if (!res.TryGetOk(out rootDir)) {
                        // ルートが壊れている（不正なパス等）。ここで解決失敗扱いにする
                        return false;
                    }
                }
            }

            // 1件でも一致して採用できた場合のみ成功
            return bestLen >= 0;
        }

        public static bool TryResolveByRootId(SLAProjectSettings project, SLAUserSettings user,
            string sourceRootId, string sourceRel, out string absPath) {

            absPath = null;

            if (project?.sourceRootDict == null || user?.sourceRootDict == null) return false;
            if (string.IsNullOrEmpty(sourceRootId) || string.IsNullOrEmpty(sourceRel)) return false;

            // Projectで定義されているrootIdのみ許可
            if (!project.sourceRootDict.ContainsKey(sourceRootId)) return false;

            if (!user.sourceRootDict.TryGetValue(sourceRootId, out var rootPath) || string.IsNullOrEmpty(rootPath)) return false;

            var rootDir = Path.GetFullPath(rootPath);
            absPath = Path.GetFullPath(Path.Combine(rootDir, sourceRel));
            return true;
        }

        /// <summary>
        /// rootDir を基準に srcAbs の相対パスを生成する。
        /// 戻り値の区切り文字は常に '/' に正規化される。
        /// </summary>
        public static string MakeRelativePath(AbsPath rootDir, AbsPath srcAbs) {
            if (rootDir == null) throw new ArgumentNullException(nameof(rootDir));
            if (srcAbs == null) throw new ArgumentNullException(nameof(srcAbs));

            var root = rootDir.AsStoredPath.TrimEnd('/') + "/"; // 末尾スラッシュを保証
            var src = srcAbs.AsStoredPath;

            var rel = Path.GetRelativePath(root, src);
            return rel.Replace('\\', '/');
        }

        // ------------------------
        // Path helpers
        // ------------------------

        /// <summary>
        /// ディレクトリパスを比較用に正規化する。
        ///
        /// - Path.GetFullPath により絶対パスへ正規化
        /// - 区切り文字を '/' に統一
        /// - 末尾スラッシュを必ず 1 つ付与する
        ///
        /// 末尾に '/' を付けることで、
        /// "/foo/bar" と "/foo/bar_baz" のような誤一致を防ぎ、
        /// StartsWith による安全な配下判定を可能にする。
        /// </summary>
        private static string NormalizeDir(string p) {
            var full = Path.GetFullPath(p).Replace('\\', '/').TrimEnd('/');
            return full + "/";
        }

        /// <summary>
        /// absPath が rootDirFullWithSlash 配下に存在するかを判定する。
        ///
        /// rootDirFullWithSlash は NormalizeDir により
        /// - '/' 区切り
        /// - 末尾 '/' 付き
        /// に正規化されていることを前提とする。
        ///
        /// 実装上は StartsWith によるプレフィックス一致で判定し、
        /// OS に応じて大文字小文字の扱いを切り替える。
        /// </summary>
        private static bool IsUnder(AbsPath absPath, string rootDirFullWithSlash) {
            if (absPath == null) return false;
            if (string.IsNullOrEmpty(rootDirFullWithSlash)) return false;

            var comparison =
                Application.platform == RuntimePlatform.WindowsEditor
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;

            // 両方 '/' 区切りに揃える
            var a = absPath.AsSlashPath;
            var root = rootDirFullWithSlash;

            return a.StartsWith(root, comparison);
        }

    }
}
