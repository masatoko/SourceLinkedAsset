using System;
using System.IO;

using UnityEditor;
using UnityEngine;

using Newtonsoft.Json;

using Tenkai.SourceLinkedAsset.Domain;

namespace Tenkai.SourceLinkedAsset.EditorService {

    public static class SLAProjectSettingsIO {

        private const string FileName = "SourceLinkedAsset.json";

        /// <summary>ProjectSettings/SourceLinkedAsset.json</summary>
        public static string GetSettingsFilePath() {
            // ProjectSettings は projectRoot 直下にある
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            return Path.Combine(projectRoot, "ProjectSettings", FileName);
        }

        /// <summary>
        /// SLASettingsCache からの実行のみを許可
        /// </summary>
        public static SLAProjectSettings Load() {
            var path = GetSettingsFilePath();
            if (!File.Exists(path)) return new SLAProjectSettings();

            try {
                var json = File.ReadAllText(path);
                var s = JsonConvert.DeserializeObject<SLAProjectSettings>(json);
                return s ?? new SLAProjectSettings();
            }
            catch (Exception e) {
                Debug.LogWarning($"[SourceLinkedAsset] Failed to load project settings: {path}\n{e}");
                return new SLAProjectSettings();
            }
        }

        /// <summary>
        /// SLASettingsCache からの実行のみを許可
        /// </summary>
        public static void Save(SLAProjectSettings settings) {
            var path = GetSettingsFilePath();
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(path, json);
                AssetDatabase.Refresh();

                Debug.Log($"[SourceLinkedAsset] Project settings saved: {path}");
            }
            catch (Exception e) {
                Debug.LogError($"[SourceLinkedAsset] Failed to save project settings: {path}\n{e}");
            }
        }
    }
}
