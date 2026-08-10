#if UNITY_EDITOR

using ApexionGame.HFSM.Editor.Views;
using EncosyTower.Editor.UIElements;
using UnityEditor;
using UnityEngine;

namespace ApexionGame.HFSM.Editor
{
    /// <summary>
    /// HFSM - Debugging.md §4 — active path, state tree, guard inspector and transition log for
    /// every live machine, found through <see cref="Debugging.MachineDebugRegistry"/>.
    /// </summary>
    internal sealed class MachineDebuggerWindow : EditorWindow
    {
        private const string MODULE_ROOT = "Assets/ApexionGame/ApexionGame.Editor";
        private const string STYLE_SHEETS_PATH = $"{MODULE_ROOT}/StyleSheets";
        private const string FILE_NAME = nameof(MachineDebuggerWindow);

        public const string THEME_STYLE_SHEET = $"{STYLE_SHEETS_PATH}/{FILE_NAME}.tss";
        public const string STYLE_SHEET_DARK = $"{STYLE_SHEETS_PATH}/{FILE_NAME}_Dark.uss";
        public const string STYLE_SHEET_LIGHT = $"{STYLE_SHEETS_PATH}/{FILE_NAME}_Light.uss";

        private MachineDebuggerViewController _controller;
        private double _nextPoll;

        [MenuItem("ApexionGame/HFSM/Debugger", priority = 43_00_00_10)]
        public static void OpenMachineDebuggerWindow()
        {
            var window = GetWindow<MachineDebuggerWindow>();
            window.titleContent = new GUIContent("HFSM Debugger");
            window.minSize = new Vector2(820f, 460f);
            window.Show();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.WithEditorStyleSheet(THEME_STYLE_SHEET);
            root.WithEditorStyleSheet(STYLE_SHEET_DARK, STYLE_SHEET_LIGHT);

            _controller = MachineDebuggerAPI.CreateView(root);
        }

        /// <remarks>
        /// Polls rather than subscribing: guard results and timers change every tick with nothing
        /// to raise an event for. The controller diffs shape before touching the tree, so a poll
        /// that finds nothing new costs a few list comparisons and label writes.
        /// <see cref="Debugging.MachineDebugRegistry.Changed"/> still covers machines coming and going.
        /// </remarks>
        private void Update()
        {
            if (_controller == null || EditorApplication.timeSinceStartup < _nextPoll)
            {
                return;
            }

            _nextPoll = EditorApplication.timeSinceStartup + 0.2d;
            _controller.Poll();
        }

        private void OnDestroy()
        {
            _controller?.Dispose();
            _controller = null;
        }
    }
}

#endif
