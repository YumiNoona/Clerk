using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public InputActionReference MoveAction;
    public CharacterController CharacterController;
    public float MoveSpeed = 5f;

    [Header("Jump")]
    public InputActionReference JumpAction;
    public float JumpForce = 5f;

    [Header("Look")]
    public InputActionReference LookAction;
    public float LookSpeed = 100f;
    public Camera TheCamera;
    public float MinLookAngle = -80f;
    public float MaxLookAngle = 80f;

    private float verticalSpeed;
    private float horizontalRotation;
    private float verticalRotation;

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
        // Cursor ownership belongs to GameplayModeController. Setting it here
        // races the runtime main menu, whose Awake pauses and unlocks before
        // scene Start methods run.
        if (GameBootstrap.Instance == null)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

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
        GameplayModeController modes =
            GameBootstrap.Instance != null
                ? GameBootstrap.Instance.GameplayModes
                : null;

        if (modes != null &&
            !modes.AllowsMovement)
        {
            return;
        }

        if (modes == null ||
            modes.AllowsLooking)
        {
            HandleLook();
        }

        HandleMovement();
    }

    private void HandleLook()
    {
        if (TheCamera == null)
        {
            return;
        }

        Vector2 lookInput =
            GameBootstrap.Instance != null
                ? GameBootstrap.Instance.Input.ReadVector2(
                    GameplayAction.Look)
                : LookAction != null
                    ? LookAction.action.ReadValue<Vector2>()
                    : Vector2.zero;

        horizontalRotation += lookInput.x * LookSpeed * Time.deltaTime;
        verticalRotation -= lookInput.y * LookSpeed * Time.deltaTime;
        verticalRotation = Mathf.Clamp(verticalRotation,MinLookAngle,MaxLookAngle);

        transform.rotation = Quaternion.Euler(0f,horizontalRotation,0f);
        TheCamera.transform.localRotation = Quaternion.Euler(verticalRotation,0f,0f);
    }

    private void HandleMovement()
    {
        if (CharacterController == null)
        {
            return;
        }

        Vector2 moveInput =
            GameBootstrap.Instance != null
                ? GameBootstrap.Instance.Input.ReadVector2(
                    GameplayAction.Move)
                : MoveAction != null
                    ? MoveAction.action.ReadValue<Vector2>()
                    : Vector2.zero;
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

            bool jumpPressed =
                GameBootstrap.Instance != null
                    ? GameBootstrap.Instance.Input
                        .WasPressedThisFrame(
                            GameplayAction.Jump)
                    : JumpAction != null &&
                      JumpAction.action
                          .WasPressedThisFrame();

            if (jumpPressed)
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
