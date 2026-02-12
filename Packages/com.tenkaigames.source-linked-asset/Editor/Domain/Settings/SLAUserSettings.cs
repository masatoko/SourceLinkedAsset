using System;
using System.Collections.Generic;

namespace Tenkai.SourceLinkedAsset.Domain {

    [Serializable]
    public class SLAUserSettings {
        public int version = 1;

        /// <summary>絶対パスを保存する</summary>
        public bool saveAbsolutePath = true;

        /// <summary>UI用言語を自動で設定</summary>
        public bool useSystemLanguage = true;

        /// <summary>autoLanguage が `false` のときのUI用言語</summary>
        public SLALanguage language = SLALanguage.English;

        /// <summary>ウィンドウのヘルプを表示する</summary>
        public bool showHelp = true;

        // key: rootId, value: absolute path to directory
        public Dictionary<string, string> sourceRootDict = new Dictionary<string, string>();

        // ===

        public SLALanguage GetResolvedLanguage() =>
            SLALanguageUtil.Resolve(useSystemLanguage, language);
    }

}
