using System.Globalization;
using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    public static UIController Instance;

    [Header("Update Price Panel")]
    public GameObject UpdatePricePanel;

    [Header("Price Text")]
    public TMP_Text BasePriceText;
    public TMP_Text CurrentPriceText;

    [Header("Price Input")]
    public TMP_InputField PriceInputField;

    [Header("Settings")]
    public string CurrencySymbol = "$";

    public bool IsPricePanelOpen
    {
        get
        {
            return UpdatePricePanel != null && UpdatePricePanel.activeSelf;
        }
    }

    private ShelfSpaceController activeShelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (UpdatePricePanel != null)
        {
            UpdatePricePanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (IsPricePanelOpen &&
            GameBootstrap.Instance != null &&
            GameBootstrap.Instance.Input
                .WasPressedThisFrame(
                    GameplayAction.Cancel))
        {
            CloseUpdatePrice();
        }
    }

    public void OpenUpdatePrice(ShelfSpaceController shelf)
    {
        if (shelf == null || shelf.Info == null)
        {
            return;
        }

        activeShelf = shelf;

        if (UpdatePricePanel != null)
        {
            UpdatePricePanel.SetActive(true);
        }

        BasePriceText.text = CurrencySymbol + shelf.Info.BasePrice.ToString("0.00");
        CurrentPriceText.text =
            CurrencySymbol +
            shelf.CurrentPrice.ToString("0.00");

        if (PriceInputField != null)
        {
            PriceInputField.text = string.Empty;
            PriceInputField.ActivateInputField();
        }

        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.GameplayModes
                .TrySetMode(GameplayMode.PriceEditing);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ApplyPriceUpdate()
    {
        if (activeShelf == null || activeShelf.Info == null || PriceInputField == null)
        {
            return;
        }

        string enteredPrice = PriceInputField.text.Trim();

        if (!float.TryParse(enteredPrice,NumberStyles.Float,CultureInfo.InvariantCulture,out float newPrice))
        {
            Debug.LogWarning("Enter a valid price, for example 2.99.");
            return;
        }

        if (newPrice < 0f)
        {
            Debug.LogWarning("Price cannot be negative.");
            return;
        }

        activeShelf.SetCurrentPrice(newPrice);

        if (CurrentPriceText != null)
        {
            CurrentPriceText.text = CurrencySymbol + newPrice.ToString("0.00");
        }

        PriceInputField.text = string.Empty;
        PriceInputField.ActivateInputField();
    }

    public void CloseUpdatePrice()
    {
        if (UpdatePricePanel != null)
        {
            UpdatePricePanel.SetActive(false);
        }

        activeShelf = null;

        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.GameplayModes
                .TrySetMode(GameplayMode.Gameplay);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
