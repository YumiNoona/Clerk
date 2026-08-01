using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StoreUIAuthoring : MonoBehaviour
{
    [SerializeField,HideInInspector] private int authoredLayoutVersion;
    public int AuthoredLayoutVersion => authoredLayoutVersion;
#if UNITY_EDITOR
    public void SetAuthoredLayoutVersion(int version) => authoredLayoutVersion = version;
#endif
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
    [HideInInspector] public Button PhoneButton;
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

    [Header("Desktop Application Icons")]
    [Min(32f)] public float ApplicationIconSize = 64f;
    public Sprite OverviewIcon;
    [FormerlySerializedAs("SupplyIcon")]
    public Sprite StoreIcon;
    public Sprite RegisterIcon;
    public Sprite BankIcon;
    public Sprite HiringIcon;
    [FormerlySerializedAs("JobsIcon")]
    public Sprite LinkedInIcon;
    public Sprite HistoryIcon;
    public Sprite SettingsIcon;
    public Sprite LoginIcon;
    public Sprite MailIcon;
    public Sprite AppMarketIcon;
    public Sprite MessagesIcon;
    public Sprite SecurityIcon;
    public Sprite TodoIcon;
    public Sprite WeatherIcon;
    public Sprite NotepadIcon;
    public Sprite CalculatorIcon;

    [Header("Desktop Taskbar")]
    public Button TaskbarStartButton;
    public TextMeshProUGUI TaskbarClockText;
    public RectTransform StartMenuRoot;
    public Button StoreToggleButton;
    public Button RestartComputerButton;
    public Button ShutDownComputerButton;
    public RectTransform BootScreenRoot;
    public TextMeshProUGUI BootStatusText;
    public Image DesktopWallpaperImage;
    public Sprite[] DesktopWallpapers;

    [Header("Career")]
    [Min(0f)] public float PlayerDailyWage = 80f;

    // Legacy mobile references are hidden while old save scenes migrate to desktop-only UI.
    [HideInInspector] public RectTransform MobileLayout;
    [HideInInspector] public RectTransform MobileFrame;
    [HideInInspector] public RectTransform MobileNavigation;
    [HideInInspector] public RectTransform MobileBody;
    [HideInInspector] public RectTransform MobileContent;
    [HideInInspector] public TextMeshProUGUI MobileClock;
    [HideInInspector] public TextMeshProUGUI MobileTitle;
    [HideInInspector] public Button MobileCloseButton;
    [HideInInspector] public Button[] MobileApplicationButtons;
    [HideInInspector] public RectTransform[] MobileApplicationPages;
    [HideInInspector] public RectTransform[] MobileApplicationContents;

    public bool IsComplete =>
        authoredLayoutVersion == 4 &&
        Canvas != null && HudRoot != null && PauseRoot != null &&
        MainMenuRoot != null && NotificationRoot != null &&
        PriceEditorRoot != null && DeviceRoot != null &&
        MoneyText != null && ClockText != null && StatusText != null &&
        StartButton != null && PriceInput != null &&
        DeviceContent != null && ApplicationPages != null &&
        ApplicationPages.Length == 17 && ApplicationContents != null &&
        ApplicationContents.Length == 17;

}
