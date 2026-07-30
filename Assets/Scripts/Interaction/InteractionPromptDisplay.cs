using UnityEngine;

public class InteractionPromptDisplay : MonoBehaviour
{
    public enum ScreenAnchor
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    [Header("References")]
    public PlayerInteractionController InteractionController;

    [Header("Placement")]
    public ScreenAnchor Anchor = ScreenAnchor.MiddleLeft;

    public Vector2 Offset = new Vector2(30f,0f);

    public float Width = 520f;
    public float Height = 220f;

    [Header("Text Style")]
    public int FontSize = 24;

    public Color TextColor =
        new Color(1f,0.78f,0.15f,1f);

    public FontStyle FontStyle =
        FontStyle.Bold;

    public TextAnchor TextAlignment =
        TextAnchor.MiddleLeft;

    public bool RichText = true;
    public bool WordWrap = true;

    [Header("Optional Background")]
    public bool ShowBackground;

    public Color BackgroundColor =
        new Color(0f,0f,0f,0.45f);

    public Vector2 BackgroundPadding =
        new Vector2(14f,10f);

    [Header("Behaviour")]
    public bool HideWhenEmpty = true;

    private GUIStyle textStyle;
    private Texture2D backgroundTexture;

    private int cachedFontSize;
    private Color cachedTextColor;
    private FontStyle cachedFontStyle;
    private TextAnchor cachedTextAlignment;
    private bool cachedRichText;
    private bool cachedWordWrap;
    private Color cachedBackgroundColor;

    private void Awake()
    {
        ResolveController();
    }

    private void OnEnable()
    {
        ResolveController();
    }

    private void OnValidate()
    {
        FontSize = Mathf.Max(1,FontSize);
        Width = Mathf.Max(1f,Width);
        Height = Mathf.Max(1f,Height);

        BackgroundPadding.x =
            Mathf.Max(0f,BackgroundPadding.x);

        BackgroundPadding.y =
            Mathf.Max(0f,BackgroundPadding.y);

        // Forces the style to rebuild during the next OnGUI call.
        textStyle = null;
    }

    private void ResolveController()
    {
        if (InteractionController == null)
        {
            InteractionController =
                GetComponent<PlayerInteractionController>();
        }

        if (InteractionController == null)
        {
            InteractionController =
                FindAnyObjectByType<
                    PlayerInteractionController>();
        }
    }

    private void OnGUI()
    {
        if (InteractionController == null)
        {
            ResolveController();

            if (InteractionController == null)
            {
                return;
            }
        }

        string prompt =
            InteractionController.GetCurrentPrompt();

        if (HideWhenEmpty &&
            string.IsNullOrWhiteSpace(prompt))
        {
            return;
        }

        EnsureGuiResources();

        Rect textRect = GetAnchoredRect();

        if (ShowBackground &&
            backgroundTexture != null)
        {
            Rect backgroundRect = new Rect(
                textRect.x - BackgroundPadding.x,
                textRect.y - BackgroundPadding.y,
                textRect.width +
                BackgroundPadding.x * 2f,
                textRect.height +
                BackgroundPadding.y * 2f);

            GUI.DrawTexture(
                backgroundRect,
                backgroundTexture,
                ScaleMode.StretchToFill,
                true);
        }

        GUI.Label(
            textRect,
            prompt,
            textStyle);
    }

    private void EnsureGuiResources()
    {
        bool styleChanged =
            textStyle == null ||
            cachedFontSize != FontSize ||
            cachedTextColor != TextColor ||
            cachedFontStyle != FontStyle ||
            cachedTextAlignment != TextAlignment ||
            cachedRichText != RichText ||
            cachedWordWrap != WordWrap;

        if (styleChanged)
        {
            textStyle =
                new GUIStyle(GUI.skin.label);

            textStyle.fontSize = FontSize;
            textStyle.fontStyle = FontStyle;
            textStyle.alignment = TextAlignment;
            textStyle.richText = RichText;
            textStyle.wordWrap = WordWrap;
            textStyle.normal.textColor = TextColor;

            cachedFontSize = FontSize;
            cachedTextColor = TextColor;
            cachedFontStyle = FontStyle;
            cachedTextAlignment = TextAlignment;
            cachedRichText = RichText;
            cachedWordWrap = WordWrap;
        }

        if (backgroundTexture == null)
        {
            backgroundTexture =
                new Texture2D(
                    1,
                    1,
                    TextureFormat.RGBA32,
                    false);

            backgroundTexture.hideFlags =
                HideFlags.HideAndDontSave;

            cachedBackgroundColor =
                new Color(
                    float.NaN,
                    float.NaN,
                    float.NaN,
                    float.NaN);
        }

        if (cachedBackgroundColor !=
            BackgroundColor)
        {
            backgroundTexture.SetPixel(
                0,
                0,
                BackgroundColor);

            backgroundTexture.Apply();

            cachedBackgroundColor =
                BackgroundColor;
        }
    }

    private Rect GetAnchoredRect()
    {
        float x;
        float y;

        switch (Anchor)
        {
            case ScreenAnchor.TopLeft:
                x = 0f;
                y = 0f;
                break;

            case ScreenAnchor.TopCenter:
                x =
                    (Screen.width - Width) *
                    0.5f;

                y = 0f;
                break;

            case ScreenAnchor.TopRight:
                x = Screen.width - Width;
                y = 0f;
                break;

            case ScreenAnchor.MiddleLeft:
                x = 0f;

                y =
                    (Screen.height - Height) *
                    0.5f;

                break;

            case ScreenAnchor.MiddleCenter:
                x =
                    (Screen.width - Width) *
                    0.5f;

                y =
                    (Screen.height - Height) *
                    0.5f;

                break;

            case ScreenAnchor.MiddleRight:
                x = Screen.width - Width;

                y =
                    (Screen.height - Height) *
                    0.5f;

                break;

            case ScreenAnchor.BottomLeft:
                x = 0f;
                y = Screen.height - Height;
                break;

            case ScreenAnchor.BottomCenter:
                x =
                    (Screen.width - Width) *
                    0.5f;

                y = Screen.height - Height;
                break;

            case ScreenAnchor.BottomRight:
                x = Screen.width - Width;
                y = Screen.height - Height;
                break;

            default:
                x = 0f;

                y =
                    (Screen.height - Height) *
                    0.5f;

                break;
        }

        return new Rect(
            x + Offset.x,
            y + Offset.y,
            Width,
            Height);
    }

    private void OnDestroy()
    {
        if (backgroundTexture != null)
        {
            Destroy(backgroundTexture);
            backgroundTexture = null;
        }
    }
}