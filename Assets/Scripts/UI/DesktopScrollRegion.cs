using UnityEngine;
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
