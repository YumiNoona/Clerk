using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class StockBoxController :
    InteractableBehaviour,
    IHeldItem
{
    public StockInfo Product;
    public BoxLayout Layout;
    public int Quantity = 12;

    public Rigidbody TheRB;
    public Collider BoxCollider;

    public Transform LeftFlapPivot;
    public Transform RightFlapPivot;

    public Vector3 LeftFlapClosedRotation = Vector3.zero;
    public Vector3 LeftFlapOpenRotation =
        new Vector3(0f,0f,110f);

    public Vector3 RightFlapClosedRotation = Vector3.zero;
    public Vector3 RightFlapOpenRotation =
        new Vector3(0f,0f,-110f);

    public float FlapAnimationSpeed = 5f;

    public bool ShowRuntimeContents = true;
    public Transform ContentOrigin;
    public float RuntimeContentsRevealDelay = 0.15f;

    [Header("Held Interaction")]
    public float ThrowForce = 5f;
    public float StockingInterval = 0.2f;

    [TextArea]
    public string OpenPrompt = "[E] Open Box";

    [TextArea]
    public string ClosePrompt = "[E] Close Box";

    [TextArea]
    public string ThrowPrompt = "[Right Click] Throw";

    [TextArea]
    public string StockShelfPrompt =
        "[Hold Left Click] Stock Shelf";

    [Header("Label")]
    public TMP_Text ProductNameLabel;
    public TMP_Text QuantityLabel;

    [HideInInspector]
    public bool ShowEditorPreview = true;

#if UNITY_EDITOR
    [HideInInspector]
    public bool ShowLayoutGizmos = true;

    [HideInInspector]
    public Transform EditorPreviewRoot;
#endif

    private readonly List<StockObject> runtimePreviewObjects =
        new List<StockObject>();

    private Transform runtimePreviewRoot;
    private bool isOpen;
    private bool isHeld;
    private float nextStockTime;

    private Coroutine flapRoutine;
    private Coroutine runtimePreviewRoutine;

    public bool IsOpen => isOpen;
    public bool IsHeld => isHeld;
    public bool CanBeHeld => !isHeld;

    public int MaximumQuantity =>
        Layout != null ? Layout.Capacity : 0;

    private void Awake()
    {
        CacheComponents();

        if (Layout == null && Product != null)
        {
            Layout = Product.DefaultBoxLayout;
        }

        ClampQuantity();
        SetFlapsImmediate(false);
        ClearRuntimeContents();
        UpdateLabels();
    }

    protected override int GetDefaultInteractionPriority()
    {
        return 30;
    }

    protected override bool SupportsInteraction(
        InteractionType interactionType)
    {
        return interactionType == InteractionType.Primary;
    }

    protected override bool CanInteractInternal(
        InteractionContext context)
    {
        return CanBeHeld &&
               !context.Player.IsHoldingAnything;
    }

    protected override void OnInteract(
        InteractionContext context)
    {
        context.Player.TryHold(this);
    }

    protected override string
        GetDefaultInteractionPrompt(
            InteractionType interactionType)
    {
        return interactionType ==
               InteractionType.Primary
            ? "[Left Click] Pick Up"
            : string.Empty;
    }

    public Transform GetHoldPoint(
        PlayerInteractionController player)
    {
        return player != null ? player.BoxHoldPoint : null;
    }

    public bool Pickup(
        PlayerInteractionController player,
        Transform holdPoint)
    {
        if (!CanBeHeld || holdPoint == null)
        {
            return false;
        }

        isHeld = true;
        nextStockTime = 0f;

        transform.SetParent(holdPoint,false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        SetPhysicsHeld(true);

        return true;
    }

    public void HandleHeldUpdate(
        PlayerInteractionController player,
        Ray interactionRay)
    {
        if (!isHeld || player == null)
        {
            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            ToggleOpen();
        }

        if (Mouse.current == null)
        {
            return;
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Throw(player);
            return;
        }

        if (!isOpen ||
            !Mouse.current.leftButton.isPressed ||
            Time.time < nextStockTime)
        {
            return;
        }

        if (!player.TryGetComponentInRay(
                interactionRay,
                out ShelfSpaceController shelf,
                out _))
        {
            return;
        }

        if (TryStockShelf(shelf))
        {
            nextStockTime =
                Time.time + StockingInterval;
        }
    }

    public string GetHeldPrompt(
        PlayerInteractionController player,
        Ray interactionRay)
    {
        string openClosePrompt =
            isOpen ? ClosePrompt : OpenPrompt;

        bool canStockShelf = false;

        if (isOpen &&
            player != null &&
            Quantity > 0 &&
            player.TryGetComponentInRay(
                interactionRay,
                out ShelfSpaceController shelf,
                out _))
        {
            canStockShelf =
                shelf.CanAcceptProduct(Product);
        }

        if (canStockShelf)
        {
            return openClosePrompt +
                   "\n" +
                   StockShelfPrompt +
                   "\n" +
                   ThrowPrompt;
        }

        return openClosePrompt +
               "\n" +
               ThrowPrompt;
    }

    private void Throw(PlayerInteractionController player)
    {
        Release();

        if (TheRB != null && player.TheCamera != null)
        {
            TheRB.AddForce(
                player.TheCamera.transform.forward * ThrowForce,
                ForceMode.Impulse);
        }

        player.ClearHeldItem(this);
    }

    public void ForceRelease(
        PlayerInteractionController player)
    {
        Release();

        if (player != null)
        {
            player.ClearHeldItem(this);
        }
    }

    public void Release()
    {
        isHeld = false;
        nextStockTime = 0f;

        SetOpen(false);
        transform.SetParent(null,true);
        SetPhysicsHeld(false);
    }

    public void ToggleOpen()
    {
        SetOpen(!isOpen);
    }

    public void SetOpen(bool open)
    {
        if (isOpen == open)
        {
            return;
        }

        isOpen = open;

        if (flapRoutine != null)
        {
            StopCoroutine(flapRoutine);
        }

        flapRoutine = StartCoroutine(AnimateFlaps());

        if (runtimePreviewRoutine != null)
        {
            StopCoroutine(runtimePreviewRoutine);
            runtimePreviewRoutine = null;
        }

        if (isOpen && ShowRuntimeContents)
        {
            runtimePreviewRoutine =
                StartCoroutine(RevealRuntimeContents());
        }
        else
        {
            ClearRuntimeContents();
        }
    }

    public bool TryStockShelf(
        ShelfSpaceController shelf)
    {
        if (!isOpen)
        {
            return false;
        }

        if (shelf == null ||
            Product == null ||
            Product.StockPrefab == null ||
            Layout == null ||
            Quantity <= 0)
        {
            return false;
        }

        Transform contentOrigin = GetContentOrigin();

        if (contentOrigin == null)
        {
            return false;
        }

        int sourceIndex = Mathf.Clamp(
            Quantity - 1,
            0,
            Layout.Capacity - 1);

        StockObject newStock =
            Instantiate(Product.StockPrefab,contentOrigin);

        newStock.Info = Product;
        newStock.transform.localPosition =
            Layout.GetLocalPosition(sourceIndex);

        newStock.transform.localRotation =
            Quaternion.Euler(Layout.LocalRotation);

        // Keep this world-space handoff. It preserves the
        // working box-to-shelf stocking animation.
        Vector3 startingWorldPosition =
            newStock.transform.position;

        Quaternion startingWorldRotation =
            newStock.transform.rotation;

        newStock.transform.SetParent(null,true);

        newStock.transform.SetPositionAndRotation(
            startingWorldPosition,
            startingWorldRotation);

        bool wasPlaced = shelf.PlaceStock(newStock);

        if (!wasPlaced)
        {
            Destroy(newStock.gameObject);
            return false;
        }

        Quantity--;

        if (ShowRuntimeContents)
        {
            RefreshRuntimeContents();
        }

        UpdateLabels();

        return true;
    }

    private IEnumerator AnimateFlaps()
    {
        Quaternion leftTarget = Quaternion.Euler(
            isOpen
                ? LeftFlapOpenRotation
                : LeftFlapClosedRotation);

        Quaternion rightTarget = Quaternion.Euler(
            isOpen
                ? RightFlapOpenRotation
                : RightFlapClosedRotation);

        while (true)
        {
            bool leftFinished = true;
            bool rightFinished = true;

            if (LeftFlapPivot != null)
            {
                LeftFlapPivot.localRotation =
                    Quaternion.Slerp(
                        LeftFlapPivot.localRotation,
                        leftTarget,
                        FlapAnimationSpeed *
                        Time.deltaTime);

                leftFinished =
                    Quaternion.Angle(
                        LeftFlapPivot.localRotation,
                        leftTarget) < 0.2f;
            }

            if (RightFlapPivot != null)
            {
                RightFlapPivot.localRotation =
                    Quaternion.Slerp(
                        RightFlapPivot.localRotation,
                        rightTarget,
                        FlapAnimationSpeed *
                        Time.deltaTime);

                rightFinished =
                    Quaternion.Angle(
                        RightFlapPivot.localRotation,
                        rightTarget) < 0.2f;
            }

            if (leftFinished && rightFinished)
            {
                break;
            }

            yield return null;
        }

        if (LeftFlapPivot != null)
        {
            LeftFlapPivot.localRotation = leftTarget;
        }

        if (RightFlapPivot != null)
        {
            RightFlapPivot.localRotation = rightTarget;
        }

        flapRoutine = null;
    }

    private IEnumerator RevealRuntimeContents()
    {
        yield return new WaitForSeconds(
            RuntimeContentsRevealDelay);

        if (isOpen && ShowRuntimeContents)
        {
            RefreshRuntimeContents();
        }

        runtimePreviewRoutine = null;
    }

    private void RefreshRuntimeContents()
    {
        ClearRuntimeContents();

        if (!ShowRuntimeContents || !isOpen)
        {
            return;
        }

        if (Layout == null ||
            Product == null ||
            Product.StockPrefab == null ||
            Quantity <= 0)
        {
            return;
        }

        Transform parent = GetContentOrigin();

        runtimePreviewRoot =
            new GameObject(
                "_RUNTIME_BOX_CONTENTS").transform;

        runtimePreviewRoot.SetParent(parent,false);

        int previewCount = Mathf.Min(
            Quantity,
            Layout.MaximumRuntimePreviewObjects,
            Layout.Capacity);

        for (int i = 0; i < previewCount; i++)
        {
            StockObject previewObject =
                Instantiate(
                    Product.StockPrefab,
                    runtimePreviewRoot);

            previewObject.Info = Product;

            previewObject.transform.localPosition =
                Layout.GetLocalPosition(i);

            previewObject.transform.localRotation =
                Quaternion.Euler(
                    Layout.LocalRotation);

            previewObject.SetAsBoxPreview();

            runtimePreviewObjects.Add(previewObject);
        }
    }

    private void ClearRuntimeContents()
    {
        for (int i = runtimePreviewObjects.Count - 1;
             i >= 0;
             i--)
        {
            if (runtimePreviewObjects[i] != null)
            {
                Destroy(
                    runtimePreviewObjects[i].gameObject);
            }
        }

        runtimePreviewObjects.Clear();

        if (runtimePreviewRoot != null)
        {
            Destroy(runtimePreviewRoot.gameObject);
            runtimePreviewRoot = null;
        }
    }

    private Transform GetContentOrigin()
    {
        return ContentOrigin != null
            ? ContentOrigin
            : transform;
    }

    private void UpdateLabels()
    {
        if (ProductNameLabel != null)
        {
            ProductNameLabel.text =
                Product != null
                    ? Product.ProductName
                    : "Empty";
        }

        if (QuantityLabel != null)
        {
            QuantityLabel.text =
                Quantity + " / " + MaximumQuantity;
        }
    }

    private void SetFlapsImmediate(bool open)
    {
        if (LeftFlapPivot != null)
        {
            LeftFlapPivot.localRotation =
                Quaternion.Euler(
                    open
                        ? LeftFlapOpenRotation
                        : LeftFlapClosedRotation);
        }

        if (RightFlapPivot != null)
        {
            RightFlapPivot.localRotation =
                Quaternion.Euler(
                    open
                        ? RightFlapOpenRotation
                        : RightFlapClosedRotation);
        }
    }

    private void ClampQuantity()
    {
        if (Layout == null)
        {
            Quantity = Mathf.Max(0,Quantity);
            return;
        }

        Quantity = Mathf.Clamp(
            Quantity,
            0,
            Layout.Capacity);
    }

    private void CacheComponents()
    {
        if (TheRB == null)
        {
            TheRB = GetComponent<Rigidbody>();
        }

        if (BoxCollider == null)
        {
            BoxCollider = GetComponent<Collider>();
        }
    }

    private void SetPhysicsHeld(bool held)
    {
        if (TheRB != null)
        {
            if (!TheRB.isKinematic)
            {
                TheRB.linearVelocity = Vector3.zero;
                TheRB.angularVelocity = Vector3.zero;
            }

            TheRB.isKinematic = held;

            if (!held)
            {
                TheRB.linearVelocity = Vector3.zero;
                TheRB.angularVelocity = Vector3.zero;
            }
        }

        if (BoxCollider != null)
        {
            BoxCollider.enabled = !held;
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        if (Layout == null && Product != null)
        {
            Layout = Product.DefaultBoxLayout;
        }

        FlapAnimationSpeed =
            Mathf.Max(0.01f,FlapAnimationSpeed);

        RuntimeContentsRevealDelay =
            Mathf.Max(0f,RuntimeContentsRevealDelay);

        ThrowForce = Mathf.Max(0f,ThrowForce);
        StockingInterval =
            Mathf.Max(0.01f,StockingInterval);

        ClampQuantity();
    }

    private void OnDestroy()
    {
        ClearRuntimeContents();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!ShowEditorPreview ||
            !ShowLayoutGizmos)
        {
            return;
        }

        BoxLayout activeLayout = Layout;

        if (activeLayout == null && Product != null)
        {
            activeLayout = Product.DefaultBoxLayout;
        }

        if (activeLayout == null)
        {
            return;
        }

        Transform origin =
            ContentOrigin != null
                ? ContentOrigin
                : transform;

        Gizmos.matrix = origin.localToWorldMatrix;

        for (int i = 0;
             i < activeLayout.Capacity;
             i++)
        {
            Gizmos.DrawWireCube(
                activeLayout.GetLocalPosition(i),
                new Vector3(0.06f,0.06f,0.06f));
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}
