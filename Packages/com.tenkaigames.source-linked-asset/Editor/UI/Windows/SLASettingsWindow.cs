using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tenkai.SourceLinkedAsset.UI {

    /// <summary>
    /// SourceLinkedAssetの設定UIを提供するウィンドウ。
    /// タブの登録と共通レイアウトのみを担当する。
    /// </summary>
    public sealed class SLASettingsWindow : EditorWindow {
        private readonly List<ISettingsTab> tabs = new();
        private int selectedTab;

        [MenuItem("Tools/SourceLinkedAsset/Window/Settings Window")]
        private static void Open() {
            GetWindow<SLASettingsWindow>("SourceLinkedAsset");
        }

        private void OnEnable() {
            tabs.Clear();
            tabs.Add(new SourceRootsTab());
            tabs.Add(new UserSettingsTab());
        }

        private void OnGUI() {
            if (tabs.Count == 0) return;

            var names = new string[tabs.Count];
            for (int i = 0; i < tabs.Count; i++) names[i] = tabs[i].TabName;

            selectedTab = GUILayout.Toolbar(selectedTab, names);

            EditorGUILayout.Space(8);

            tabs[selectedTab].OnGUI();
        }
    }
}
