#if UNITY_EDITOR

using ApexionGame.Entities.Stats.Editor.Views;
using EncosyTower.Editor.UIElements;
using UnityEditor;
using UnityEngine;

namespace ApexionGame.Entities.Stats.Editor
{
    /// <summary>
    /// Tasks 5.1 + 5.2 — reads any store that registered itself, and draws its observer graph.
    /// </summary>
    /// <remarks>
    /// Views live in this editor assembly rather than in the runtime one. EncosyTower keeps its
    /// Visual Command views in a runtime assembly because the same elements are reskinned for an
    /// in-game console; this debugger has no in-game use, so shipping it to players would be dead
    /// weight.
    /// </remarks>
    internal sealed class StatDebuggerWindow : EditorWindow
    {
        private const string MODULE_ROOT = "Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats.Editor";
        private const string STYLE_SHEETS_PATH = $"{MODULE_ROOT}/StyleSheets";
        private const string FILE_NAME = nameof(StatDebuggerWindow);

        public const string THEME_STYLE_SHEET = $"{STYLE_SHEETS_PATH}/{FILE_NAME}.tss";
        public const string STYLE_SHEET_DARK = $"{STYLE_SHEETS_PATH}/{FILE_NAME}_Dark.uss";
        public const string STYLE_SHEET_LIGHT = $"{STYLE_SHEETS_PATH}/{FILE_NAME}_Light.uss";

        private StatDebuggerViewController _controller;
        private double _nextPoll;

        [MenuItem("ApexionGame/Stats/Stat Debugger", priority = 42_00_00_10)]
        public static void OpenStatDebuggerWindow()
        {
            var window = GetWindow<StatDebuggerWindow>();
            window.titleContent = new GUIContent("Stat Debugger");
            window.minSize = new Vector2(760f, 420f);
            window.Show();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.WithEditorStyleSheet(THEME_STYLE_SHEET);
            root.WithEditorStyleSheet(STYLE_SHEET_DARK, STYLE_SHEET_LIGHT);

            _controller = StatDebuggerAPI.CreateView(root);
        }

        /// <remarks>
        /// Polls rather than subscribing: stat values change in native memory with nothing to raise
        /// an event. The controller compares the shape of the data before touching the tree, so a
        /// poll that finds nothing new costs a few list comparisons and some label assignments.
        /// <c>StatDebugRegistry.Changed</c> still covers stores coming and going.
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
