using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public InputActionReference MoveAction;
    public CharacterController CharacterController;
    public float MoveSpeed = 5f;

    private float verticalSpeed;

    [Header("Jump")]
    public InputActionReference JumpAction;
    public float JumpForce = 5f;

    [Header("Look")]
    public InputActionReference LookAction;
    public float LookSpeed = 100f;
    public Camera TheCamera;
    public float MinLookAngle = -80f;
    public float MaxLookAngle = 80f;

    private float horizontalRotation;
    private float verticalRotation;

    [Header("Interaction")]
    public LayerMask WhatIsStock;
    public LayerMask WhatIsShelf;
    public LayerMask WhatIsStockBox;
    public LayerMask WhatIsFurniture;
    public float InteractionRange = 3f;

    [Header("Stock Holding")]
    public Transform HoldPoint;
    public float ThrowForce = 10f;

    [Header("Box Holding")]
    public Transform BoxHoldPoint;
    public float BoxThrowForce = 5f;
    public float StockingInterval = 0.2f;

    private StockObject heldPickup;
    private StockBoxController heldBox;
    private float nextStockTime;

    private void Awake()
    {
        if (CharacterController == null)
        {
            CharacterController = GetComponent<CharacterController>();
        }

        if (TheCamera == null)
        {
            TheCamera = GetComponentInChildren<Camera>();
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        horizontalRotation = transform.eulerAngles.y;
    }

    private void OnEnable()
    {
        EnableInputAction(MoveAction);
        EnableInputAction(JumpAction);
        EnableInputAction(LookAction);
    }

    private void OnDisable()
    {
        DisableInputAction(MoveAction);
        DisableInputAction(JumpAction);
        DisableInputAction(LookAction);
    }

    private void Update()
    {
        if (UIController.Instance != null && UIController.Instance.IsPricePanelOpen)
        {
            return;
        }

        HandleLook();
        HandleMovement();

        if (FurniturePlacementController.Instance != null && FurniturePlacementController.Instance.IsPlacing)
        {
            return;
        }

        HandleInteraction();
    }

    private void HandleLook()
    {
        if (LookAction == null || TheCamera == null)
        {
            return;
        }

        Vector2 lookInput = LookAction.action.ReadValue<Vector2>();

        horizontalRotation += lookInput.x * LookSpeed * Time.deltaTime;
        verticalRotation -= lookInput.y * LookSpeed * Time.deltaTime;
        verticalRotation = Mathf.Clamp(verticalRotation,MinLookAngle,MaxLookAngle);

        transform.rotation = Quaternion.Euler(0f,horizontalRotation,0f);
        TheCamera.transform.localRotation = Quaternion.Euler(verticalRotation,0f,0f);
    }

    private void HandleMovement()
    {
        if (CharacterController == null || MoveAction == null)
        {
            return;
        }

        Vector2 moveInput = MoveAction.action.ReadValue<Vector2>();
        Vector3 movement = transform.forward * moveInput.y + transform.right * moveInput.x;

        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        movement *= MoveSpeed;

        if (CharacterController.isGrounded)
        {
            if (verticalSpeed < 0f)
            {
                verticalSpeed = -2f;
            }

            if (JumpAction != null && JumpAction.action.WasPressedThisFrame())
            {
                verticalSpeed = JumpForce;
            }
        }
        else
        {
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
        }

        movement.y = verticalSpeed;
        CharacterController.Move(movement * Time.deltaTime);
    }

    private void HandleInteraction()
    {
        if (TheCamera == null || Mouse.current == null)
        {
            return;
        }

        Ray ray = TheCamera.ViewportPointToRay(new Vector3(0.5f,0.5f,0f));

        HandlePriceUpdate(ray);

        if (heldBox != null)
        {
            HandleHeldBox(ray);
            return;
        }

        if (heldPickup != null)
        {
            HandleHeldStock(ray);
            return;
        }

        HandleMoveFurniture(ray);
        HandlePickupStock(ray);
        HandlePickupStockFromShelf(ray);
        HandlePickupBox(ray);
    }

    private void HandleMoveFurniture(Ray ray)
    {
    if (Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame)
    {
        return;
    }

    if (FurniturePlacementController.Instance == null || FurniturePlacementController.Instance.IsPlacing)
    {
        return;
    }

    RaycastHit[] hits = Physics.RaycastAll(ray,InteractionRange,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);

    System.Array.Sort(hits,(firstHit,secondHit) => firstHit.distance.CompareTo(secondHit.distance));

    for (int i = 0; i < hits.Length; i++)
    {
        PlaceableFurniture furniture = hits[i].collider.GetComponentInParent<PlaceableFurniture>();

        if (furniture == null)
        {
            continue;
        }

        FurniturePlacementController.Instance.BeginMovePlacement(furniture);
        return;
    }
    }

    private void HandlePickupBox(Ray ray)
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (!Physics.Raycast(ray,out RaycastHit hit,InteractionRange,WhatIsStockBox,QueryTriggerInteraction.Ignore))
        {
            return;
        }

        StockBoxController box = hit.collider.GetComponentInParent<StockBoxController>();

        if (box == null || BoxHoldPoint == null)
        {
            return;
        }

        heldBox = box;
        heldBox.Pickup(BoxHoldPoint);
    }

    private void HandleHeldBox(Ray ray)
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            heldBox.ToggleOpen();
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            DropHeldBox();
            return;
        }

        if (!heldBox.IsOpen || !Mouse.current.leftButton.isPressed || Time.time < nextStockTime)
        {
            return;
        }

        if (!Physics.Raycast(ray,out RaycastHit hit,InteractionRange,WhatIsShelf,QueryTriggerInteraction.Collide))
        {
            return;
        }

        ShelfSpaceController shelf = hit.collider.GetComponentInParent<ShelfSpaceController>();

        if (shelf == null)
        {
            return;
        }

        if (heldBox.TryStockShelf(shelf))
        {
            nextStockTime = Time.time + StockingInterval;
        }
    }

    private void DropHeldBox()
    {
        StockBoxController boxToDrop = heldBox;
        heldBox = null;

        boxToDrop.Release();

        if (boxToDrop.TheRB != null && TheCamera != null)
        {
            boxToDrop.TheRB.AddForce(TheCamera.transform.forward * BoxThrowForce,ForceMode.Impulse);
        }
    }

    private void HandlePickupStock(Ray ray)
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (!Physics.Raycast(ray,out RaycastHit hit,InteractionRange,WhatIsStock,QueryTriggerInteraction.Ignore))
        {
            return;
        }

        StockObject stockObject = hit.collider.GetComponentInParent<StockObject>();

        if (stockObject == null || stockObject.IsBoxPreview)
        {
            return;
        }

        PickupStock(stockObject);
    }

    private void HandlePickupStockFromShelf(Ray ray)
    {
        if (!Mouse.current.rightButton.wasPressedThisFrame)
        {
            return;
        }

        if (!Physics.Raycast(ray,out RaycastHit hit,InteractionRange,WhatIsShelf,QueryTriggerInteraction.Collide))
        {
            return;
        }

        ShelfSpaceController shelf = hit.collider.GetComponentInParent<ShelfSpaceController>();

        if (shelf == null)
        {
            return;
        }

        StockObject stockObject = shelf.GetStock();

        if (stockObject == null)
        {
            return;
        }

        PickupStock(stockObject);
    }

    private void HandleHeldStock(Ray ray)
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Physics.Raycast(ray,out RaycastHit hit,InteractionRange,WhatIsShelf,QueryTriggerInteraction.Collide))
            {
                ShelfSpaceController shelf = hit.collider.GetComponentInParent<ShelfSpaceController>();

                if (shelf != null && shelf.PlaceStock(heldPickup))
                {
                    heldPickup = null;
                }
            }
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            ThrowHeldStock();
        }
    }

    private void ThrowHeldStock()
    {
        StockObject stockToThrow = heldPickup;
        heldPickup = null;

        stockToThrow.Release();

        if (stockToThrow.TheRB != null && TheCamera != null)
        {
            stockToThrow.TheRB.AddForce(TheCamera.transform.forward * ThrowForce,ForceMode.Impulse);
        }
    }

    private void PickupStock(StockObject stockObject)
    {
        if (stockObject == null || HoldPoint == null)
        {
            return;
        }

        heldPickup = stockObject;
        heldPickup.transform.SetParent(HoldPoint,true);
        heldPickup.Pickup();
    }

    private void HandlePriceUpdate(Ray ray)
    {
        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame || heldBox != null)
        {
            return;
        }

        if (!Physics.Raycast(ray,out RaycastHit hit,InteractionRange,WhatIsShelf,QueryTriggerInteraction.Collide))
        {
            return;
        }

        ShelfSpaceController shelf = hit.collider.GetComponentInParent<ShelfSpaceController>();

        if (shelf != null)
        {
            shelf.StartPriceUpdate();
        }
    }

    private static void EnableInputAction(InputActionReference actionReference)
    {
        if (actionReference != null)
        {
            actionReference.action.Enable();
        }
    }

    private static void DisableInputAction(InputActionReference actionReference)
    {
        if (actionReference != null)
        {
            actionReference.action.Disable();
        }
    }
}