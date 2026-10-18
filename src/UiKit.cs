using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace PhigrosLoading
{
    internal static class UiKit
    {
        private static readonly Regex RichTag = new Regex("<[^>]+>", RegexOptions.Compiled);

        /// <summary>去掉谱面里带的富文本标签（&lt;size=...&gt; / &lt;color=...&gt; 之类）。</summary>
        public static string StripRichText(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return RichTag.Replace(value, "").Trim();
        }

        private static Font _font;

        public static Font GetFont()
        {
            if (_font != null) return _font;

            try
            {
                string[] candidates = new string[]
                {
                    "Microsoft YaHei UI", "Microsoft YaHei", "Segoe UI",
                    "PingFang SC", "Noto Sans CJK SC", "SimHei",
                    "Arial Unicode MS", "Arial"
                };
                _font = Font.CreateDynamicFontFromOSFont(candidates, 48);
            }
            catch (Exception e)
            {
                Main.Error(e);
            }

            if (_font == null)
            {
                try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            }
            if (_font == null)
            {
                try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            }
            return _font;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return rt;
        }

        public static Image Image(RectTransform rt, Color color, Sprite sprite = null)
        {
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;
            image.type = UnityEngine.UI.Image.Type.Simple;
            if (sprite == null) image.material = null;
            return image;
        }

        public static Text Text(RectTransform rt, string content, int size, Color color, TextAnchor anchor)
        {
            Text text = rt.gameObject.AddComponent<Text>();
            text.font = GetFont();
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 0.92f;
            return text;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        public static void AddOutline(Graphic graphic, float distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
        }

        public static void AddShadow(Graphic graphic, float distance)
        {
            Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(0f, -distance);
            shadow.useGraphicAlpha = true;
        }

        // ------------------------------------------------------------------
        // 生成的贴图
        // ------------------------------------------------------------------
        public static Sprite MakeVerticalGradient(Color bottom, Color top)
        {
            const int h = 128;
            Texture2D tex = new Texture2D(2, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[2 * h];
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                Color c = Color.Lerp(bottom, top, t);
                pixels[y * 2] = c;
                pixels[y * 2 + 1] = c;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, 2f, h), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite MakeGlow(int size, float power)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    float a = Mathf.Clamp01(1f - d);
                    a = Mathf.Pow(a, power);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite MakeRounded(int size, int radius, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = RoundedDistance(x + 0.5f, y + 0.5f, size, radius);
                    float a = Mathf.Clamp01(1f - d);
                    Color c = color;
                    c.a *= a;
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        private static float RoundedDistance(float x, float y, float size, float radius)
        {
            float cx = Mathf.Clamp(x, radius, size - radius);
            float cy = Mathf.Clamp(y, radius, size - radius);
            float dx = x - cx;
            float dy = y - cy;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            return d - radius + 1f;
        }

        public static Sprite LoadSpriteFromFile(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
                byte[] bytes = File.ReadAllBytes(path);
                if (bytes == null || bytes.Length == 0) return null;

                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(bytes)) return null;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            catch (Exception e)
            {
                Main.Error(e);
                return null;
            }
        }
    }
}
