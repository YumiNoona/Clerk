using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Desktop Device Layout")]
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

    [Header("Mobile Device Layout")]
    public RectTransform MobileLayout;
    public RectTransform MobileFrame;
    public RectTransform MobileNavigation;
    public RectTransform MobileBody;
    public RectTransform MobileContent;
    public TextMeshProUGUI MobileClock;
    public TextMeshProUGUI MobileTitle;
    public Button MobileCloseButton;
    public Button[] MobileApplicationButtons;
    public RectTransform[] MobileApplicationPages;
    public RectTransform[] MobileApplicationContents;

    public bool IsComplete =>
        Canvas != null && HudRoot != null && PauseRoot != null &&
        MainMenuRoot != null && NotificationRoot != null &&
        PriceEditorRoot != null && DeviceRoot != null &&
        MoneyText != null && ClockText != null && StatusText != null &&
        StartButton != null && PriceInput != null &&
        DeviceContent != null && ApplicationPages != null &&
        ApplicationPages.Length == 7 && ApplicationContents != null &&
        ApplicationContents.Length == 7;

}
