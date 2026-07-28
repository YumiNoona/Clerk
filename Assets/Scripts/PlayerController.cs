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
    public float InteractionRange = 3f;
    public Transform HoldPoint;
    public float ThrowForce = 10f;

    private StockObject heldPickup;

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

        Ray interactionRay = TheCamera.ViewportPointToRay(new Vector3(0.5f,0.5f,0f));

        HandlePriceUpdate(interactionRay);

        if (heldPickup == null)
        {
            HandlePickupFromFloor(interactionRay);
            HandlePickupFromShelf(interactionRay);
        }
        else
        {
            HandlePlaceOnShelf(interactionRay);
            HandleThrow();
        }
    }

    private void HandlePriceUpdate(Ray interactionRay)
    {
        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
        {
            return;
        }

        if (!Physics.Raycast(interactionRay,out RaycastHit hit,InteractionRange,WhatIsShelf,QueryTriggerInteraction.Collide))
        {
            return;
        }

        ShelfSpaceController shelfSpace = hit.collider.GetComponentInParent<ShelfSpaceController>();

        if (shelfSpace == null)
        {
            return;
        }

        shelfSpace.StartPriceUpdate();
    }

    private void HandlePickupFromFloor(Ray interactionRay)
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (!Physics.Raycast(interactionRay,out RaycastHit hit,InteractionRange,WhatIsStock,QueryTriggerInteraction.Ignore))
        {
            return;
        }

        StockObject stockObject = hit.collider.GetComponentInParent<StockObject>();

        if (stockObject == null)
        {
            return;
        }

        PickupStock(stockObject);
    }

    private void HandlePickupFromShelf(Ray interactionRay)
    {
        if (!Mouse.current.rightButton.wasPressedThisFrame)
        {
            return;
        }

        if (!Physics.Raycast(interactionRay,out RaycastHit hit,InteractionRange,WhatIsShelf,QueryTriggerInteraction.Collide))
        {
            return;
        }

        ShelfSpaceController shelfSpace = hit.collider.GetComponentInParent<ShelfSpaceController>();

        if (shelfSpace == null)
        {
            return;
        }

        StockObject stockObject = shelfSpace.GetStock();

        if (stockObject == null)
        {
            return;
        }

        PickupStock(stockObject);
    }

    private void HandlePlaceOnShelf(Ray interactionRay)
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (!Physics.Raycast(interactionRay,out RaycastHit hit,InteractionRange,WhatIsShelf,QueryTriggerInteraction.Collide))
        {
            return;
        }

        ShelfSpaceController shelfSpace = hit.collider.GetComponentInParent<ShelfSpaceController>();

        if (shelfSpace == null)
        {
            return;
        }

        bool wasPlaced = shelfSpace.PlaceStock(heldPickup);

        if (wasPlaced)
        {
            heldPickup = null;
        }
    }

    private void HandleThrow()
    {
        if (!Mouse.current.rightButton.wasPressedThisFrame)
        {
            return;
        }

        if (heldPickup == null)
        {
            return;
        }

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