using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

[DisallowMultipleComponent]
public sealed class DesktopWindowControls : MonoBehaviour
{
    public RectTransform Window;
    public GameObject Content;
    public Button MinimizeButton;
    public Button MaximizeButton;
    public Button CloseButton;

    private Vector2 normalAnchorMin;
    private Vector2 normalAnchorMax;
    private Vector2 normalOffsetMin;
    private Vector2 normalOffsetMax;
    private bool maximized;
    private bool collapsed;

    private void Awake()
    {
        if (Window == null) return;
        SaveNormalRect();
    }

    private void SaveNormalRect()
    {
        normalAnchorMin = Window.anchorMin;
        normalAnchorMax = Window.anchorMax;
        normalOffsetMin = Window.offsetMin;
        normalOffsetMax = Window.offsetMax;
    }

    public void ToggleCollapsed()
    {
        collapsed = !collapsed;
        if (Content != null) Content.SetActive(!collapsed);
        if (collapsed)
        {
            if (!maximized) SaveNormalRect();
            Window.anchorMin = new Vector2(normalAnchorMin.x,normalAnchorMax.y - 0.085f);
            Window.anchorMax = normalAnchorMax;
            Window.offsetMin = Window.offsetMax = Vector2.zero;
        }
        else if (!maximized)
        {
            RestoreNormalRect();
        }
    }

    public void RestoreForOpen()
    {
        if (!collapsed) return;
        collapsed = false;
        if (Content != null) Content.SetActive(true);
        if (!maximized) RestoreNormalRect();
    }

    public void ToggleMaximized()
    {
        if (collapsed)
        {
            collapsed = false;
            if (Content != null) Content.SetActive(true);
        }
        if (!maximized)
        {
            SaveNormalRect();
            Window.anchorMin = new Vector2(0.01f,0.01f);
            Window.anchorMax = new Vector2(0.99f,0.99f);
            Window.offsetMin = Window.offsetMax = Vector2.zero;
            maximized = true;
        }
        else
        {
            RestoreNormalRect();
            maximized = false;
        }
    }

    public void CloseWindow()
    {
        if (Window != null) Window.gameObject.SetActive(false);
    }

    private void RestoreNormalRect()
    {
        Window.anchorMin = normalAnchorMin;
        Window.anchorMax = normalAnchorMax;
        Window.offsetMin = normalOffsetMin;
        Window.offsetMax = normalOffsetMax;
    }
}

public enum DesktopWindowCommand
{
    Minimize,
    Maximize,
    Close
}

[DisallowMultipleComponent]
public sealed class DesktopWindowControlButton : MonoBehaviour, IPointerClickHandler
{
    public DesktopWindowControls Controls;
    public DesktopWindowCommand Command;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || Controls == null) return;
        switch (Command)
        {
            case DesktopWindowCommand.Minimize: Controls.ToggleCollapsed(); break;
            case DesktopWindowCommand.Maximize: Controls.ToggleMaximized(); break;
            case DesktopWindowCommand.Close: Controls.CloseWindow(); break;
        }
        eventData.Use();
    }
}
