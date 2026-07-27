using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public InputActionReference MoveAction;
    public CharacterController CharController;
    public float MoveSpeed;

    private float YSpeed;

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
    public float InteractRange;
    public Transform HoldPoint;
    public float ThrowForce;

    private GameObject HeldPickup;

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
        // =========================
        // LOOK
        // =========================
        Vector2 lookInput = LookAction.action.ReadValue<Vector2>();

        HorizontalRotation += lookInput.x * LookSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Euler(
            0f,
            HorizontalRotation,
            0f
        );

        VerticalRotation -= lookInput.y * LookSpeed * Time.deltaTime;

        VerticalRotation = Mathf.Clamp(
            VerticalRotation,
            MinLookAngle,
            MaxLookAngle
        );

        TheCamera.transform.localRotation = Quaternion.Euler(
            VerticalRotation,
            0f,
            0f
        );

        // =========================
        // MOVEMENT
        // =========================
        Vector2 moveInput = MoveAction.action.ReadValue<Vector2>();

        Vector3 forwardMove = transform.forward * moveInput.y;
        Vector3 rightMove = transform.right * moveInput.x;

        Vector3 moveAmount = forwardMove + rightMove;
        moveAmount = moveAmount.normalized;
        moveAmount *= MoveSpeed;

        // =========================
        // JUMP AND GRAVITY
        // =========================
        if (CharController.isGrounded)
        {
            YSpeed = 0f;

            if (JumpAction.action.WasPressedThisFrame())
            {
                YSpeed = JumpForce;
            }
        }

        YSpeed += Physics.gravity.y * Time.deltaTime;
        moveAmount.y = YSpeed;

        CharController.Move(moveAmount * Time.deltaTime);

        // =========================
        // PICK UP AND THROW
        // =========================
        Ray ray = TheCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (HeldPickup == null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                RaycastHit hit;

                if (Physics.Raycast(
                    ray,
                    out hit,
                    InteractRange,
                    WhatIsStock
                ))
                {
                    Rigidbody pickupRigidbody =
                        hit.collider.GetComponent<Rigidbody>();

                    if (pickupRigidbody != null)
                    {
                        Debug.Log("Stock Detected");

                        HeldPickup = hit.collider.gameObject;

                        HeldPickup.transform.SetParent(HoldPoint);
                        HeldPickup.transform.localPosition = Vector3.zero;
                        HeldPickup.transform.localRotation =
                            Quaternion.identity;

                        pickupRigidbody.isKinematic = true;
                    }
                }
            }
        }
        else
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                Rigidbody pickupRigidbody =
                    HeldPickup.GetComponent<Rigidbody>();

                HeldPickup.transform.SetParent(null);

                if (pickupRigidbody != null)
                {
                    pickupRigidbody.isKinematic = false;

                    pickupRigidbody.AddForce(
                        TheCamera.transform.forward * ThrowForce,
                        ForceMode.Impulse
                    );
                }

                HeldPickup = null;
            }
        }
    }
}