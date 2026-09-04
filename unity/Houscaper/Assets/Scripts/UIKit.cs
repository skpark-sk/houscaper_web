using UnityEngine;
using UnityEngine.UI;

namespace Houscaper
{
    /// <summary>Small helpers for assembling the runtime UI without prefabs.</summary>
    public static class UIKit
    {
        static Sprite _rounded;
        static Sprite _circle;
        static Font _font;

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return _font;
            }
        }

        /// <summary>A 9-sliced rounded rectangle, generated once and shared.</summary>
        public static Sprite Rounded
        {
            get
            {
                if (_rounded == null) _rounded = BuildRounded(48, 14f);
                return _rounded;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (_circle == null) _circle = BuildRounded(48, 24f);
                return _circle;
            }
        }

        static Sprite BuildRounded(int size, float radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distance to the rounded-rect boundary, anti-aliased over one pixel.
                    float dx = Mathf.Max(radius - (x + 0.5f), (x + 0.5f) - (size - radius));
                    float dy = Mathf.Max(radius - (y + 0.5f), (y + 0.5f) - (size - radius));
                    float d = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
                    float alpha = Mathf.Clamp01(radius - d + 0.5f);

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            float border = Mathf.Min(radius, size * 0.5f - 1f);
            return Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }

        public static RectTransform Panel(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = sprite ?? Rounded;
            image.type = Image.Type.Sliced;
            image.color = color;

            return (RectTransform)go.transform;
        }

        public static Text Label(Transform parent, string name, string content, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            return text;
        }

        public static Button Chip(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var rect = Panel(parent, name, color, sprite);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            return button;
        }

        public static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        /// <summary>Places a fixed-size element relative to one anchor point.</summary>
        public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
