using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen transition overlay. You don't need to place this in the scene: the first time
/// something asks for it (ScreenFader.Get()), it builds its own overlay canvas.
/// Two styles: FadeTo (smooth fade) and IrisClose/IrisOpen (circle shrinks to / grows from a point).
/// </summary>
public class ScreenFader : MonoBehaviour
{
    private static ScreenFader instance;

    private CanvasGroup group;
    private Image fadeImage;
    private Color color = Color.black;

    // Iris pieces: one sprite with a circular hole, plus 4 big panels attached to its edges
    // so the whole screen is covered no matter how small the hole gets.
    private RectTransform irisRect;
    private Image irisImage;
    private readonly List<Image> irisPads = new List<Image>();

    public static ScreenFader Get()
    {
        if (instance != null) return instance;

        instance = FindFirstObjectByType<ScreenFader>();
        if (instance != null) { instance.EnsureBuilt(); return instance; }

        GameObject go = new GameObject("ScreenFader");
        instance = go.AddComponent<ScreenFader>();
        instance.EnsureBuilt();
        return instance;
    }

    private void EnsureBuilt()
    {
        if (group != null) return;

        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // above hearts and everything else

        group = gameObject.GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        GameObject imageGo = new GameObject("Fade", typeof(RectTransform));
        imageGo.transform.SetParent(transform, false);
        fadeImage = imageGo.AddComponent<Image>();
        fadeImage.color = color;
        fadeImage.raycastTarget = false;

        RectTransform rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        BuildIris();
    }

    private void BuildIris()
    {
        GameObject go = new GameObject("Iris", typeof(RectTransform));
        go.transform.SetParent(transform, false);

        irisRect = go.GetComponent<RectTransform>();
        irisRect.anchorMin = Vector2.zero;
        irisRect.anchorMax = Vector2.zero;
        irisRect.pivot = new Vector2(0.5f, 0.5f);

        irisImage = go.AddComponent<Image>();
        irisImage.sprite = MakeIrisSprite();
        irisImage.color = color;
        irisImage.raycastTarget = false;

        CreatePad("Top", new Vector2(0.5f, 1f), new Vector2(0.5f, 0f));
        CreatePad("Bottom", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f));
        CreatePad("Left", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f));
        CreatePad("Right", new Vector2(1f, 0.5f), new Vector2(0f, 0.5f));

        go.SetActive(false);
    }

    private void CreatePad(string padName, Vector2 anchor, Vector2 pivot)
    {
        GameObject p = new GameObject(padName, typeof(RectTransform));
        p.transform.SetParent(irisRect, false);

        RectTransform rt = p.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = new Vector2(20000f, 20000f);
        rt.anchoredPosition = Vector2.zero;

        Image img = p.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        irisPads.Add(img);
    }

    // 256x256 white square with a soft-edged transparent circle in the middle.
    // The hole radius is 64px, i.e. exactly 1/4 of the sprite's width.
    private static Sprite MakeIrisSprite()
    {
        const int size = 256;
        const float holeRadius = 64f;
        const float feather = 6f;
        float c = (size - 1) / 2f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c;
                float dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((d - (holeRadius - feather * 0.5f)) / feather);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>Sets the colour of the overlay (alpha is ignored).</summary>
    public void SetColor(Color newColor)
    {
        EnsureBuilt();
        newColor.a = 1f;
        color = newColor;
        fadeImage.color = color;
        irisImage.color = color;
        foreach (Image pad in irisPads) pad.color = color;
    }

    /// <summary>Smooth fade. 1 = fully covered, 0 = clear. Use: yield return fader.FadeTo(1f, 0.5f);</summary>
    public IEnumerator FadeTo(float targetAlpha, float duration)
    {
        EnsureBuilt();
        irisRect.gameObject.SetActive(false);
        fadeImage.gameObject.SetActive(true);

        if (duration <= 0f)
        {
            group.alpha = targetAlpha;
            yield break;
        }

        float start = group.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            group.alpha = Mathf.Lerp(start, targetAlpha, k);
            yield return null;
        }
        group.alpha = targetAlpha;
    }

    /// <summary>Circle shrinks down to a point at screenCenter (in screen pixels) until the screen is covered.</summary>
    public IEnumerator IrisClose(float duration, Vector2 screenCenter)
    {
        return Iris(true, duration, screenCenter);
    }

    /// <summary>Circle grows out from screenCenter (in screen pixels) until the screen is fully clear.</summary>
    public IEnumerator IrisOpen(float duration, Vector2 screenCenter)
    {
        return Iris(false, duration, screenCenter);
    }

    private IEnumerator Iris(bool closing, float duration, Vector2 center)
    {
        EnsureBuilt();

        // Distance from the centre to the farthest screen corner = radius that shows everything.
        float w = Screen.width;
        float h = Screen.height;
        float maxRadius = Mathf.Max(
            Mathf.Max(Vector2.Distance(center, new Vector2(0, 0)), Vector2.Distance(center, new Vector2(w, 0))),
            Mathf.Max(Vector2.Distance(center, new Vector2(0, h)), Vector2.Distance(center, new Vector2(w, h)))) + 10f;

        float from = closing ? maxRadius : 0f;
        float to = closing ? 0f : maxRadius;

        fadeImage.gameObject.SetActive(false);
        group.alpha = 1f;
        irisRect.gameObject.SetActive(true);
        irisRect.anchoredPosition = center;
        SetIrisRadius(from);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            SetIrisRadius(Mathf.Lerp(from, to, k));
            yield return null;
        }
        SetIrisRadius(to);

        if (!closing)
        {
            irisRect.gameObject.SetActive(false);
            group.alpha = 0f;
        }
    }

    private void SetIrisRadius(float radius)
    {
        // Hole radius is 1/4 of the sprite width, so width = 4 x radius.
        float size = Mathf.Max(0f, radius) * 4f;
        irisRect.sizeDelta = new Vector2(size, size);
    }
}