using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class UIFactory
{
    public static readonly Color Background =
        new Color32(10,11,16,255);

    public static readonly Color Surface =
        new Color32(27,30,39,255);

    public static readonly Color SurfaceRaised =
        new Color32(36,40,51,255);

    public static readonly Color Accent =
        new Color32(198,255,61,255);

    public static readonly Color Muted =
        new Color32(142,151,173,255);

    public static readonly Color Danger =
        new Color32(255,91,105,255);

    public static RectTransform Panel(
        Transform parent,
        string name,
        Color color)
    {
        GameObject instance =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

        instance.transform.SetParent(parent,false);

        RectTransform rect =
            instance.GetComponent<RectTransform>();

        Stretch(rect);

        Image image =
            instance.GetComponent<Image>();

        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    public static TextMeshProUGUI Text(
        Transform parent,
        string name,
        string value,
        float size = 22f,
        TextAlignmentOptions alignment =
            TextAlignmentOptions.Left)
    {
        GameObject instance =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

        instance.transform.SetParent(parent,false);

        TextMeshProUGUI text =
            instance.GetComponent<TextMeshProUGUI>();

        text.text = value;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = alignment;
        text.textWrappingMode =
            TextWrappingModes.Normal;
        text.raycastTarget = false;

        Stretch(text.rectTransform);
        return text;
    }

    public static Button Button(
        Transform parent,
        string name,
        string label,
        UnityAction onClick,
        Color? color = null)
    {
        RectTransform rect =
            Panel(
                parent,
                name,
                color ?? SurfaceRaised);

        Button button =
            rect.gameObject.AddComponent<Button>();

        rect.GetComponent<Image>().raycastTarget = true;

        ColorBlock colors = button.colors;
        colors.highlightedColor =
            Color.Lerp(
                color ?? SurfaceRaised,
                Color.white,
                0.12f);
        colors.pressedColor =
            Color.Lerp(
                color ?? SurfaceRaised,
                Color.black,
                0.18f);
        button.colors = colors;

        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        TextMeshProUGUI text =
            Text(
                rect,
                "Label",
                label,
                18f,
                TextAlignmentOptions.Center);

        text.color =
            color.HasValue &&
            color.Value == Accent
                ? Background
                : Color.white;

        return button;
    }

    public static VerticalLayoutGroup Vertical(
        Transform parent,
        float spacing = 10f,
        float padding = 12f)
    {
        VerticalLayoutGroup layout =
            parent.gameObject
                .AddComponent<VerticalLayoutGroup>();

        int safePadding =
            Mathf.RoundToInt(padding);

        layout.padding =
            new RectOffset(
                safePadding,
                safePadding,
                safePadding,
                safePadding);

        layout.spacing = spacing;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        return layout;
    }

    public static HorizontalLayoutGroup Horizontal(
        Transform parent,
        float spacing = 10f,
        float padding = 12f)
    {
        HorizontalLayoutGroup layout =
            parent.gameObject
                .AddComponent<HorizontalLayoutGroup>();

        int safePadding =
            Mathf.RoundToInt(padding);

        layout.padding =
            new RectOffset(
                safePadding,
                safePadding,
                safePadding,
                safePadding);

        layout.spacing = spacing;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;
        return layout;
    }

    public static RectTransform ScrollContent(
        Transform parent,
        string name)
    {
        RectTransform root =
            Panel(parent,name,Color.clear);

        ScrollRect scroll =
            root.gameObject.AddComponent<ScrollRect>();

        RectTransform viewport =
            Panel(root,"Viewport",Color.clear);

        viewport.gameObject.AddComponent<
            RectMask2D>();

        RectTransform content =
            Panel(viewport,"Content",Color.clear);

        content.anchorMin = new Vector2(0f,1f);
        content.anchorMax = new Vector2(1f,1f);
        content.pivot = new Vector2(0.5f,1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        ContentSizeFitter fitter =
            content.gameObject.AddComponent<
                ContentSizeFitter>();

        fitter.verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType =
            ScrollRect.MovementType.Clamped;

        return content;
    }

    public static void Size(
        Component component,
        float preferredWidth,
        float preferredHeight)
    {
        LayoutElement layout =
            component.gameObject
                .GetComponent<LayoutElement>() ??
            component.gameObject
                .AddComponent<LayoutElement>();

        layout.preferredWidth = preferredWidth;
        layout.preferredHeight = preferredHeight;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void Clear(Transform parent)
    {
        for (int i = parent.childCount - 1;
             i >= 0;
             i--)
        {
            UnityEngine.Object.Destroy(
                parent.GetChild(i).gameObject);
        }
    }
}
