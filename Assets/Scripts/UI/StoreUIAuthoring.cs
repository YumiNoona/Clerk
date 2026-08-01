using TMPro;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class StoreUIAuthoring : MonoBehaviour
{
    [Header("Roots")]
    public Canvas Canvas;
    public RectTransform HudRoot;
    public RectTransform PauseRoot;
    public RectTransform MainMenuRoot;
    public RectTransform NotificationRoot;
    public RectTransform PriceEditorRoot;
    public RectTransform DeviceRoot;

    [Header("HUD")]
    public TextMeshProUGUI MoneyText;
    public TextMeshProUGUI ClockText;
    public TextMeshProUGUI StatusText;

    [Header("Main Menu")]
    public Button StartButton;
    public Button ContinueButton;
    public Button MainSettingsButton;
    public Button MainQuitButton;

    [Header("Pause Menu")]
    public Button ResumeButton;
    public Button DesktopButton;
    public Button PhoneButton;
    public Button SaveButton;
    public Button LoadButton;
    public Button PauseQuitButton;

    [Header("Price Editor")]
    public TextMeshProUGUI PriceDetailsText;
    public TMP_InputField PriceInput;
    public Button ApplyPriceButton;
    public Button CancelPriceButton;

    [Header("Device")]
    public RectTransform DeviceFrame;
    public RectTransform DeviceNavigation;
    public RectTransform DeviceBody;
    public RectTransform DeviceContent;
    public TextMeshProUGUI DeviceBrand;
    public TextMeshProUGUI DeviceTitle;
    public Button DeviceCloseButton;
    public Button[] ApplicationButtons;
    public RectTransform[] ApplicationPages;
    public RectTransform[] ApplicationContents;

    public bool IsComplete =>
        Canvas != null && HudRoot != null && PauseRoot != null &&
        MainMenuRoot != null && NotificationRoot != null &&
        PriceEditorRoot != null && DeviceRoot != null &&
        MoneyText != null && ClockText != null && StatusText != null &&
        StartButton != null && PriceInput != null &&
        DeviceContent != null && ApplicationPages != null &&
        ApplicationPages.Length == 7 && ApplicationContents != null &&
        ApplicationContents.Length == 7;

#if UNITY_EDITOR
    private void OnEnable()
    {
        RequestEditorBuild();
    }

    private void OnValidate()
    {
        RequestEditorBuild();
    }

    private void RequestEditorBuild()
    {
        if (Application.isPlaying || IsComplete)
        {
            return;
        }

        Type builderType = Type.GetType(
            "StoreUIHierarchyBuilder, Clerk.Editor");
        MethodInfo buildMethod = builderType?.GetMethod(
            "Build",
            BindingFlags.Public | BindingFlags.Static);
        buildMethod?.Invoke(null,new object[] { this });
    }
#endif
}
