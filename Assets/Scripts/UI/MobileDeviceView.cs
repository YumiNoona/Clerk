using UnityEngine;

[DisallowMultipleComponent]
public sealed class MobileDeviceView : MonoBehaviour
{
    [Header("Editable Phone Screen")]
    public Renderer ScreenRenderer;
    public Canvas ScreenCanvas;
    public RectTransform MobileLayout;
}
