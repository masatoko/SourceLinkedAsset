namespace Tenkai.SourceLinkedAsset.UI {

    /// <summary>
    /// 設定ウィンドウ上のタブを表すインターフェイス。
    /// </summary>
    public interface ISettingsTab {
        string TabName { get; }
        void OnGUI();
    }
}
