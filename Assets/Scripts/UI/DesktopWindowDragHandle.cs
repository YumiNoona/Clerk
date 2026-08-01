using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class DesktopWindowDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public RectTransform Window;
    private RectTransform workspace;
    private Vector2 pointerOffset;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Window == null)
        {
            return;
        }

        Window.SetAsLastSibling();
        workspace = Window.parent as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            workspace,eventData.position,eventData.pressEventCamera,out Vector2 pointer);
        pointerOffset = Window.anchoredPosition - pointer;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Window == null || workspace == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            workspace,eventData.position,eventData.pressEventCamera,out Vector2 pointer);
        Vector2 next = pointer + pointerOffset;
        Vector2 half = Window.rect.size * 0.5f;
        Rect bounds = workspace.rect;
        next.x = Mathf.Clamp(next.x,bounds.xMin + half.x * 0.25f,bounds.xMax - half.x * 0.25f);
        next.y = Mathf.Clamp(next.y,bounds.yMin + 28f,bounds.yMax - 28f);
        Window.anchoredPosition = next;
    }
}
