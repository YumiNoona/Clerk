using System.Collections.Generic;
using UnityEngine;

public class CustomerAnimator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the Animator located on the Mesh child.")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private CustomerNavigation navigation;

    [Header("Movement Parameters")]
    [Tooltip("Optional Float parameter. Leave empty when using only IsWalking.")]
    [SerializeField]
    private string speedParameter = string.Empty;

    [Tooltip("Bool parameter controlling Idle and Walk.")]
    [SerializeField]
    private string walkingParameter = "IsWalking";

    [Min(0f)]
    [SerializeField]
    private float speedDampTime = 0.1f;

    [Range(0f,1f)]
    [SerializeField]
    private float walkingThreshold = 0.05f;

    [Header("Behaviour Parameters")]
    [Tooltip("Leave empty until browsing animations are added.")]
    [SerializeField]
    private string browsingParameter = string.Empty;

    [Tooltip("Leave empty until waiting animations are added.")]
    [SerializeField]
    private string waitingParameter = string.Empty;

    [Tooltip("Leave empty until checkout animations are added.")]
    [SerializeField]
    private string checkoutParameter = string.Empty;

    [Header("Trigger Parameters")]
    [Tooltip("Leave empty until a pickup animation is added.")]
    [SerializeField]
    private string pickupTrigger = string.Empty;

    [Tooltip("Leave empty until a checkout animation is added.")]
    [SerializeField]
    private string checkoutTrigger = string.Empty;

    [Tooltip("Leave empty until a greeting animation is added.")]
    [SerializeField]
    private string greetingTrigger = string.Empty;

    [Tooltip("Leave empty until a reaction animation is added.")]
    [SerializeField]
    private string reactionTrigger = string.Empty;

    [Header("Animator Settings")]
    [SerializeField]
    private bool disableRootMotion = true;

    [SerializeField]
    private bool validateAnimatorParameters = true;

    [Header("Debug")]
    [SerializeField]
    private bool showParameterWarnings = true;

    private int speedParameterHash;
    private int walkingParameterHash;
    private int browsingParameterHash;
    private int waitingParameterHash;
    private int checkoutParameterHash;

    private int pickupTriggerHash;
    private int checkoutTriggerHash;
    private int greetingTriggerHash;
    private int reactionTriggerHash;

    private bool hasSpeedParameter;
    private bool hasWalkingParameter;
    private bool hasBrowsingParameter;
    private bool hasWaitingParameter;
    private bool hasCheckoutParameter;

    private bool hasPickupTrigger;
    private bool hasCheckoutTrigger;
    private bool hasGreetingTrigger;
    private bool hasReactionTrigger;

    private RuntimeAnimatorController validatedController;

    public Animator Animator => animator;

    public CustomerNavigation Navigation => navigation;

    private void Reset()
    {
        CacheComponents();
        ConfigureAnimator();
        CacheParameterHashes();
    }

    private void Awake()
    {
        CacheComponents();
        ConfigureAnimator();
        CacheParameterHashes();
        ValidateParameters();
    }

    private void OnEnable()
    {
        ValidateParameters();
    }

    private void Update()
    {
        UpdateMovementAnimation();
    }

    public void ApplyOverrideController(
        AnimatorOverrideController overrideController)
    {
        if (animator == null || overrideController == null)
        {
            return;
        }

        animator.runtimeAnimatorController =
            overrideController;

        validatedController = null;

        CacheParameterHashes();
        ValidateParameters();
    }

    public void SetBrowsing(bool browsing)
    {
        SetBool(
            browsingParameterHash,
            hasBrowsingParameter,
            browsing);
    }

    public void SetWaiting(bool waiting)
    {
        SetBool(
            waitingParameterHash,
            hasWaitingParameter,
            waiting);
    }

    public void SetCheckingOut(bool checkingOut)
    {
        SetBool(
            checkoutParameterHash,
            hasCheckoutParameter,
            checkingOut);
    }

    public void TriggerPickup()
    {
        SetTrigger(
            pickupTriggerHash,
            hasPickupTrigger);
    }

    public void TriggerCheckout()
    {
        SetTrigger(
            checkoutTriggerHash,
            hasCheckoutTrigger);
    }

    public void TriggerGreeting()
    {
        SetTrigger(
            greetingTriggerHash,
            hasGreetingTrigger);
    }

    public void TriggerReaction()
    {
        SetTrigger(
            reactionTriggerHash,
            hasReactionTrigger);
    }

    public void ResetBehaviourStates()
    {
        SetBrowsing(false);
        SetWaiting(false);
        SetCheckingOut(false);
    }

    public void SetSpeedImmediately(float normalizedSpeed)
    {
        if (animator == null)
        {
            return;
        }

        normalizedSpeed =
            Mathf.Clamp01(normalizedSpeed);

        if (hasSpeedParameter)
        {
            animator.SetFloat(
                speedParameterHash,
                normalizedSpeed);
        }

        if (hasWalkingParameter)
        {
            animator.SetBool(
                walkingParameterHash,
                normalizedSpeed > walkingThreshold);
        }
    }

    private void UpdateMovementAnimation()
    {
        if (animator == null || navigation == null)
        {
            return;
        }

        float normalizedSpeed =
            navigation.NormalizedSpeed;

        if (hasSpeedParameter)
        {
            animator.SetFloat(
                speedParameterHash,
                normalizedSpeed,
                speedDampTime,
                Time.deltaTime);
        }

        if (hasWalkingParameter)
        {
            animator.SetBool(
                walkingParameterHash,
                normalizedSpeed > walkingThreshold);
        }
    }

    private void SetBool(
        int parameterHash,
        bool parameterExists,
        bool value)
    {
        if (animator == null || !parameterExists)
        {
            return;
        }

        animator.SetBool(parameterHash,value);
    }

    private void SetTrigger(
        int parameterHash,
        bool parameterExists)
    {
        if (animator == null || !parameterExists)
        {
            return;
        }

        animator.SetTrigger(parameterHash);
    }

    private void CacheComponents()
    {
        if (navigation == null)
        {
            navigation =
                GetComponent<CustomerNavigation>();
        }

        if (animator == null)
        {
            Animator[] animators =
                GetComponentsInChildren<Animator>(true);

            for (int i = 0; i < animators.Length; i++)
            {
                if (animators[i] == null)
                {
                    continue;
                }

                if (animators[i].gameObject == gameObject)
                {
                    continue;
                }

                animator = animators[i];
                break;
            }
        }
    }

    private void ConfigureAnimator()
    {
        if (animator == null)
        {
            return;
        }

        if (disableRootMotion)
        {
            animator.applyRootMotion = false;
        }
    }

    private void CacheParameterHashes()
    {
        speedParameterHash =
            GetHash(speedParameter);

        walkingParameterHash =
            GetHash(walkingParameter);

        browsingParameterHash =
            GetHash(browsingParameter);

        waitingParameterHash =
            GetHash(waitingParameter);

        checkoutParameterHash =
            GetHash(checkoutParameter);

        pickupTriggerHash =
            GetHash(pickupTrigger);

        checkoutTriggerHash =
            GetHash(checkoutTrigger);

        greetingTriggerHash =
            GetHash(greetingTrigger);

        reactionTriggerHash =
            GetHash(reactionTrigger);
    }

    private static int GetHash(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return 0;
        }

        return Animator.StringToHash(parameterName);
    }

    private void ValidateParameters()
    {
        if (animator == null)
        {
            if (showParameterWarnings)
            {
                Debug.LogWarning(
                    name +
                    " could not find an Animator on a child object.",
                    this);
            }

            return;
        }

        RuntimeAnimatorController controller =
            animator.runtimeAnimatorController;

        if (controller == null)
        {
            SetAllParametersUnavailable();

            if (showParameterWarnings)
            {
                Debug.LogWarning(
                    name +
                    " does not have an Animator Controller assigned.",
                    animator);
            }

            return;
        }

        if (!validateAnimatorParameters)
        {
            SetParametersAvailableFromNames();
            return;
        }

        if (validatedController == controller)
        {
            return;
        }

        validatedController = controller;

        Dictionary<int,AnimatorControllerParameterType>
            availableParameters =
                new Dictionary<
                    int,
                    AnimatorControllerParameterType>();

        AnimatorControllerParameter[] parameters =
            animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            availableParameters[
                parameters[i].nameHash] =
                    parameters[i].type;
        }

        hasSpeedParameter =
            ValidateParameter(
                availableParameters,
                speedParameter,
                speedParameterHash,
                AnimatorControllerParameterType.Float);

        hasWalkingParameter =
            ValidateParameter(
                availableParameters,
                walkingParameter,
                walkingParameterHash,
                AnimatorControllerParameterType.Bool);

        hasBrowsingParameter =
            ValidateParameter(
                availableParameters,
                browsingParameter,
                browsingParameterHash,
                AnimatorControllerParameterType.Bool);

        hasWaitingParameter =
            ValidateParameter(
                availableParameters,
                waitingParameter,
                waitingParameterHash,
                AnimatorControllerParameterType.Bool);

        hasCheckoutParameter =
            ValidateParameter(
                availableParameters,
                checkoutParameter,
                checkoutParameterHash,
                AnimatorControllerParameterType.Bool);

        hasPickupTrigger =
            ValidateParameter(
                availableParameters,
                pickupTrigger,
                pickupTriggerHash,
                AnimatorControllerParameterType.Trigger);

        hasCheckoutTrigger =
            ValidateParameter(
                availableParameters,
                checkoutTrigger,
                checkoutTriggerHash,
                AnimatorControllerParameterType.Trigger);

        hasGreetingTrigger =
            ValidateParameter(
                availableParameters,
                greetingTrigger,
                greetingTriggerHash,
                AnimatorControllerParameterType.Trigger);

        hasReactionTrigger =
            ValidateParameter(
                availableParameters,
                reactionTrigger,
                reactionTriggerHash,
                AnimatorControllerParameterType.Trigger);
    }

    private bool ValidateParameter(
        Dictionary<int,AnimatorControllerParameterType>
            availableParameters,
        string parameterName,
        int parameterHash,
        AnimatorControllerParameterType expectedType)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        if (!availableParameters.TryGetValue(
                parameterHash,
                out AnimatorControllerParameterType actualType))
        {
            if (showParameterWarnings)
            {
                Debug.LogWarning(
                    name +
                    " Animator is missing parameter '" +
                    parameterName +
                    "'.",
                    animator);
            }

            return false;
        }

        if (actualType != expectedType)
        {
            if (showParameterWarnings)
            {
                Debug.LogWarning(
                    name +
                    " Animator parameter '" +
                    parameterName +
                    "' is " +
                    actualType +
                    " but should be " +
                    expectedType +
                    ".",
                    animator);
            }

            return false;
        }

        return true;
    }

    private void SetParametersAvailableFromNames()
    {
        hasSpeedParameter =
            !string.IsNullOrWhiteSpace(speedParameter);

        hasWalkingParameter =
            !string.IsNullOrWhiteSpace(walkingParameter);

        hasBrowsingParameter =
            !string.IsNullOrWhiteSpace(browsingParameter);

        hasWaitingParameter =
            !string.IsNullOrWhiteSpace(waitingParameter);

        hasCheckoutParameter =
            !string.IsNullOrWhiteSpace(checkoutParameter);

        hasPickupTrigger =
            !string.IsNullOrWhiteSpace(pickupTrigger);

        hasCheckoutTrigger =
            !string.IsNullOrWhiteSpace(checkoutTrigger);

        hasGreetingTrigger =
            !string.IsNullOrWhiteSpace(greetingTrigger);

        hasReactionTrigger =
            !string.IsNullOrWhiteSpace(reactionTrigger);
    }

    private void SetAllParametersUnavailable()
    {
        hasSpeedParameter = false;
        hasWalkingParameter = false;
        hasBrowsingParameter = false;
        hasWaitingParameter = false;
        hasCheckoutParameter = false;

        hasPickupTrigger = false;
        hasCheckoutTrigger = false;
        hasGreetingTrigger = false;
        hasReactionTrigger = false;
    }

    private void OnValidate()
    {
        CacheComponents();

        speedDampTime =
            Mathf.Max(0f,speedDampTime);

        walkingThreshold =
            Mathf.Clamp01(walkingThreshold);

        ConfigureAnimator();
        CacheParameterHashes();

        validatedController = null;
    }
}