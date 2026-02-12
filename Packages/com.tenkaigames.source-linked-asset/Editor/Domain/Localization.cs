using System;
using System.Globalization;

namespace Tenkai.SourceLinkedAsset.Domain {

    /// <summary>
    /// UI表示に使用する言語
    /// </summary>
    public enum SLALanguage {
        English = 0,
        Japanese = 1,
    }

    /// <summary>
    /// 言語指定に応じた文字列を取り出すためのローカライズ文字列
    /// </summary>
    [Serializable]
    public sealed class LocalizedText {
        public string en;
        public string ja;

        public LocalizedText(string en, string ja) {
            this.en = en;
            this.ja = ja;
        }

        /// <summary>
        /// 指定言語に対応する文字列を返す
        /// </summary>
        public string GetByLang(SLALanguage lang) {
            return lang switch {
                SLALanguage.English => en ?? string.Empty,
                SLALanguage.Japanese => ja ?? string.Empty,
                _ => en ?? ja ?? string.Empty,
            };
        }
    }

    /// <summary>
    /// 言語設定を実際に使用する言語へ解決するユーティリティ
    /// </summary>
    public static class SLALanguageUtil {

        /// <summary>
        /// useSystemLang: true の場合 CultureInfo.CurrentUICulture を見る
        /// </summary>
        public static SLALanguage Resolve(bool useSystemLang, SLALanguage lang) {
            if (!useSystemLang) return lang;

            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            switch (culture) {
                case ("en"): return SLALanguage.English;
                case ("ja"): return SLALanguage.Japanese;
                default: return SLALanguage.English;
            }
        }
    }

}