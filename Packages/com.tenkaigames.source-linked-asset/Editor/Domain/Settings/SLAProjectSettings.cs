using System;
using System.Collections.Generic;

namespace Tenkai.SourceLinkedAsset.Domain {

    [Serializable]
    public class SourceRootEntry {
        public string name; // 表示名（共有）
    }

    [Serializable]
    public class SLAProjectSettings {
        public int version = 1;

        // key: rootId (例: "main_art", "audio" など固定文字列推奨)
        public Dictionary<string, SourceRootEntry> sourceRootDict = new Dictionary<string, SourceRootEntry>();
    }
}
