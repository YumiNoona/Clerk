using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DesktopScrollRegion : MonoBehaviour, IScrollHandler
{
    public ScrollRect ScrollRect;
    [Min(0.01f)] public float Step = 0.14f;

    public void OnScroll(PointerEventData eventData)
    {
        if (ScrollRect == null || !ScrollRect.vertical)
        {
            return;
        }
        ScrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            ScrollRect.verticalNormalizedPosition + eventData.scrollDelta.y * Step);
        eventData.Use();
    }
}

public static class UIFactory
{
    public static readonly Color Background =
        new Color32(15,23,42,255);

    public static readonly Color Surface =
        new Color32(30,41,59,255);

    public static readonly Color SurfaceRaised =
        new Color32(51,65,85,255);

    public static readonly Color Accent =
        new Color32(96,165,250,255);

    public static readonly Color Muted =
        new Color32(148,163,184,255);

    public static readonly Color Danger =
        new Color32(248,113,113,255);

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
        Image square = rect.GetComponent<Image>();
        UnityEngine.Object.DestroyImmediate(square);
        RoundedRectGraphic rounded = rect.gameObject.AddComponent<RoundedRectGraphic>();
        rounded.color = color ?? SurfaceRaised;
        rounded.Radius = 10f;
        rounded.CornerSegments = 6;
        rounded.raycastTarget = true;
        button.targetGraphic = rounded;

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
        root.GetComponent<Image>().raycastTarget = true;

        RectTransform viewport =
            Panel(root,"Viewport",Color.clear);

        viewport.gameObject.AddComponent<
            RectMask2D>();
        viewport.GetComponent<Image>().raycastTarget = true;

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
        scroll.scrollSensitivity = 42f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.12f;

        DesktopScrollRegion wheel =
            viewport.gameObject.AddComponent<DesktopScrollRegion>();
        wheel.ScrollRect = scroll;

        RectTransform scrollbarRoot =
            Panel(root,"Vertical Scrollbar",new Color32(31,41,55,220));
        scrollbarRoot.anchorMin = new Vector2(0.988f,0.02f);
        scrollbarRoot.anchorMax = new Vector2(0.998f,0.98f);
        scrollbarRoot.offsetMin = scrollbarRoot.offsetMax = Vector2.zero;
        Scrollbar scrollbar = scrollbarRoot.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        RectTransform handle = Panel(scrollbarRoot,"Handle",new Color32(96,165,250,255));
        handle.anchorMin = Vector2.zero;
        handle.anchorMax = Vector2.one;
        handle.offsetMin = handle.offsetMax = Vector2.zero;
        handle.GetComponent<Image>().raycastTarget = true;
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handle.GetComponent<Image>();
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scroll.verticalScrollbarSpacing = -8f;

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
