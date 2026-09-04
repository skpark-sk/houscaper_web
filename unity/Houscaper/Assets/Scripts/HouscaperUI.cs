using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Houscaper
{
    /// <summary>Builds and drives the pastel HUD. No prefabs, no scene wiring.</summary>
    public class HouscaperUI : MonoBehaviour
    {
        static readonly Color Ink = Swatch.Hex("#4a4640");
        static readonly Color InkSoft = Swatch.Hex("#8b8378");
        static readonly Color Card = new Color(1f, 0.996f, 0.98f, 0.92f);
        static readonly Color ChipIdle = new Color(1f, 0.99f, 0.97f, 0.86f);
        static readonly Color ChipActive = Swatch.Hex("#f2c8a0");

        BuildController _controller;

        readonly List<Button> _modeButtons = new List<Button>();
        readonly List<RectTransform> _swatchRings = new List<RectTransform>();
        Text _status;
        Text _gridLabel;

        public void Build(BuildController controller)
        {
            _controller = controller;

            EnsureEventSystem();
            var canvas = CreateCanvas();

            BuildTitleCard(canvas);
            BuildToolbar(canvas);
            BuildActions(canvas);

            _controller.StateChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (_controller != null) _controller.StateChanged -= Refresh;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go);
        }

        static Transform CreateCanvas()
        {
            var go = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            return go.transform;
        }

        // ── Panels ──────────────────────────────────────────────────────────────

        void BuildTitleCard(Transform canvas)
        {
            var card = UIKit.Panel(canvas, "TitleCard", Card);
            UIKit.Place(card, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(268f, 128f));

            var title = UIKit.Label(card, "Title", "Houscaper", 30, Ink);
            UIKit.Place((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -16f), new Vector2(230f, 38f));

            var subtitle = UIKit.Label(card, "Subtitle", "작은 섬 위에 집을 지어보세요", 14, InkSoft);
            UIKit.Place((RectTransform)subtitle.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(21f, -52f), new Vector2(230f, 20f));

            var hint = UIKit.Label(card, "Hint",
                "좌클릭 짓기 · 우클릭 지우기\n드래그 회전 · 휠 줌 · G 격자",
                13, InkSoft);
            hint.lineSpacing = 1.25f;
            UIKit.Place((RectTransform)hint.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(21f, -76f), new Vector2(230f, 40f));
        }

        void BuildToolbar(Transform canvas)
        {
            var bar = UIKit.Panel(canvas, "Toolbar", Card);
            UIKit.Place(bar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(660f, 96f));

            // Modes.
            string[] labels = { "짓기", "지우기", "칠하기" };
            var modes = new[] { BuildMode.Build, BuildMode.Erase, BuildMode.Paint };

            for (int i = 0; i < labels.Length; i++)
            {
                var mode = modes[i];
                var button = UIKit.Chip(bar, "Mode" + i, ChipIdle);
                UIKit.Place((RectTransform)button.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(18f + i * 74f, 0f), new Vector2(68f, 48f));

                var label = UIKit.Label(button.transform, "Label", labels[i], 15, Ink, TextAnchor.MiddleCenter);
                UIKit.Anchor((RectTransform)label.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

                button.onClick.AddListener(() => _controller.SetMode(mode));
                _modeButtons.Add(button);
            }

            // Divider.
            var divider = UIKit.Panel(bar, "Divider", new Color(0f, 0f, 0f, 0.08f));
            UIKit.Place(divider, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(252f, 0f), new Vector2(2f, 44f));

            // Colour swatches.
            for (int i = 0; i < Palette.Swatches.Length; i++)
            {
                int index = i;
                var swatch = Palette.Get(i);

                var ring = UIKit.Panel(bar, "Ring" + i, Color.clear, UIKit.Circle);
                UIKit.Place(ring, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(272f + i * 46f, 0f), new Vector2(44f, 44f));
                _swatchRings.Add(ring);

                var button = UIKit.Chip(ring, "Swatch" + i, swatch.Wall, UIKit.Circle);
                UIKit.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(34f, 34f));

                // A roof-coloured cap so each chip previews the whole scheme.
                var cap = UIKit.Panel(button.transform, "Cap", swatch.Roof, UIKit.Circle);
                UIKit.Place(cap, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -3f), new Vector2(20f, 10f));
                cap.GetComponent<Image>().raycastTarget = false;

                button.onClick.AddListener(() => _controller.SetSwatch(index));
            }
        }

        void BuildActions(Transform canvas)
        {
            var card = UIKit.Panel(canvas, "Actions", Card);
            UIKit.Place(card, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(228f, 60f));

            var undo = UIKit.Chip(card, "Undo", ChipIdle);
            UIKit.Place((RectTransform)undo.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(62f, 40f));
            var undoLabel = UIKit.Label(undo.transform, "Label", "되돌리기", 13, Ink, TextAnchor.MiddleCenter);
            UIKit.Anchor((RectTransform)undoLabel.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            undo.onClick.AddListener(() => _controller.Undo());

            var grid = UIKit.Chip(card, "Grid", ChipIdle);
            UIKit.Place((RectTransform)grid.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(82f, 0f), new Vector2(56f, 40f));
            _gridLabel = UIKit.Label(grid.transform, "Label", "격자", 13, Ink, TextAnchor.MiddleCenter);
            UIKit.Anchor((RectTransform)_gridLabel.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            grid.onClick.AddListener(() => _controller.ToggleGrid());

            var clear = UIKit.Chip(card, "Clear", ChipIdle);
            UIKit.Place((RectTransform)clear.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(144f, 0f), new Vector2(70f, 40f));
            var clearLabel = UIKit.Label(clear.transform, "Label", "전체 삭제", 13, Ink, TextAnchor.MiddleCenter);
            UIKit.Anchor((RectTransform)clearLabel.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            clear.onClick.AddListener(() => _controller.ClearAll());

            _status = UIKit.Label(canvas, "Status", string.Empty, 13, new Color(0.29f, 0.28f, 0.25f, 0.75f), TextAnchor.LowerRight);
            UIKit.Place((RectTransform)_status.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(240f, 20f));
        }

        // ── State ───────────────────────────────────────────────────────────────

        void Refresh()
        {
            for (int i = 0; i < _modeButtons.Count; i++)
            {
                bool active = (int)_controller.Mode == i;
                _modeButtons[i].GetComponent<Image>().color = active ? ChipActive : ChipIdle;
            }

            for (int i = 0; i < _swatchRings.Count; i++)
            {
                bool active = _controller.Swatch == i;
                _swatchRings[i].GetComponent<Image>().color = active ? Ink : Color.clear;
            }

            if (_gridLabel != null) _gridLabel.color = _controller.GridVisible ? Ink : InkSoft;
            if (_status != null) _status.text = _controller.BlockCount + " blocks";
        }
    }
}
