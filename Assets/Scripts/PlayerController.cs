using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public InputActionReference MoveAction;
    public CharacterController CharacterController;
    public float MoveSpeed;

    private float VerticalSpeed;

    [Header("Jump")]
    public InputActionReference JumpAction;
    public float JumpForce;

    [Header("Look")]
    public InputActionReference LookAction;
    public float LookSpeed;

    private float HorizontalRotation;
    private float VerticalRotation;

    public Camera TheCamera;
    public float MinLookAngle;
    public float MaxLookAngle;

    [Header("Interaction")]
    public LayerMask WhatIsStock;
    public float InteractionRange;

    private StockObject HeldPickup;
    public Transform HoldPoint;

    public float ThrowForce;

    public LayerMask WhatIsShelf;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        MoveAction.action.Enable();
        JumpAction.action.Enable();
        LookAction.action.Enable();
    }

    private void OnDisable()
    {
        MoveAction.action.Disable();
        JumpAction.action.Disable();
        LookAction.action.Disable();
    }

    private void Update()
    {
        // LOOK
        Vector2 LookInput = LookAction.action.ReadValue<Vector2>();

        HorizontalRotation +=
            LookInput.x * Time.deltaTime * LookSpeed;

        transform.rotation =
            Quaternion.Euler(0f, HorizontalRotation, 0f);

        VerticalRotation -=
            LookInput.y * Time.deltaTime * LookSpeed;

        VerticalRotation = Mathf.Clamp(
            VerticalRotation,
            MinLookAngle,
            MaxLookAngle
        );

        TheCamera.transform.localRotation =
            Quaternion.Euler(VerticalRotation, 0f, 0f);

        // MOVEMENT
        Vector2 MoveInput = MoveAction.action.ReadValue<Vector2>();

        Vector3 ForwardMovement =
            transform.forward * MoveInput.y;

        Vector3 RightMovement =
            transform.right * MoveInput.x;

        Vector3 MovementAmount =
            RightMovement + ForwardMovement;

        MovementAmount = MovementAmount.normalized;
        MovementAmount = MovementAmount * MoveSpeed;

        // JUMP AND GRAVITY
        if (CharacterController.isGrounded == true)
        {
            VerticalSpeed = 0f;

            if (JumpAction.action.WasPressedThisFrame())
            {
                VerticalSpeed = JumpForce;
            }
        }

        VerticalSpeed =
            VerticalSpeed +
            (Physics.gravity.y * Time.deltaTime);

        MovementAmount.y = VerticalSpeed;

        CharacterController.Move(
            MovementAmount * Time.deltaTime
        );

        // INTERACTION RAY
        Ray Ray = TheCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        RaycastHit Hit;

        // NOT HOLDING STOCK
        if (HeldPickup == null)
        {
            // Pick stock up from the floor
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (Physics.Raycast(
                    Ray,
                    out Hit,
                    InteractionRange,
                    WhatIsStock
                ))
                {
                    HeldPickup =
                        Hit.collider.GetComponent<StockObject>();

                    if (HeldPickup != null)
                    {
                        HeldPickup.transform.SetParent(HoldPoint);
                        HeldPickup.Pickup();
                    }
                }
            }

            // Get stock from a shelf
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (Physics.Raycast(
                    Ray,
                    out Hit,
                    InteractionRange,
                    WhatIsShelf
                ))
                {
                    ShelfSpaceController ShelfSpace =
                        Hit.collider.GetComponent<ShelfSpaceController>();

                    if (ShelfSpace != null)
                    {
                        HeldPickup = ShelfSpace.GetStock();

                        if (HeldPickup != null)
                        {
                            HeldPickup.transform.SetParent(HoldPoint);
                            HeldPickup.Pickup();
                        }
                    }
                }
            }
        }
        else
        {
            // Place stock on a shelf
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (Physics.Raycast(
                    Ray,
                    out Hit,
                    InteractionRange,
                    WhatIsShelf
                ))
                {
                    ShelfSpaceController ShelfSpace =
                        Hit.collider.GetComponent<ShelfSpaceController>();

                    if (ShelfSpace != null)
                    {
                        ShelfSpace.PlaceStock(HeldPickup);
                        HeldPickup = null;
                    }
                }
            }

            // Throw held stock
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                StockObject StockToThrow = HeldPickup;

                HeldPickup = null;

                StockToThrow.transform.SetParent(null);
                StockToThrow.Release();

                StockToThrow.TheRB.AddForce(
                    TheCamera.transform.forward * ThrowForce,
                    ForceMode.Impulse
                );
            }
        }
    }
}