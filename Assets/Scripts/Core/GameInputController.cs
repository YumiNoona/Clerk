using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GameInputController : MonoBehaviour
{
    private const string BindingOverridesKey =
        "Clerk.Input.BindingOverrides";

    private readonly Dictionary<GameplayAction,InputAction>
        actions =
            new Dictionary<GameplayAction,InputAction>();

    private InputActionMap gameplayMap;
    private InputActionMap systemMap;

    public event Action BindingsChanged;

    private void Awake()
    {
        BuildActions();
        LoadBindingOverrides();
        gameplayMap.Enable();
        systemMap.Enable();
    }

    public InputAction GetAction(GameplayAction action)
    {
        return actions.TryGetValue(action,out InputAction value)
            ? value
            : null;
    }

    public bool WasPressedThisFrame(GameplayAction action)
    {
        InputAction inputAction = GetAction(action);
        return inputAction != null &&
               inputAction.WasPressedThisFrame();
    }

    public bool IsPressed(GameplayAction action)
    {
        InputAction inputAction = GetAction(action);
        return inputAction != null &&
               inputAction.IsPressed();
    }

    public Vector2 ReadVector2(GameplayAction action)
    {
        InputAction inputAction = GetAction(action);

        return inputAction != null
            ? inputAction.ReadValue<Vector2>()
            : Vector2.zero;
    }

    public float ReadFloat(GameplayAction action)
    {
        InputAction inputAction = GetAction(action);

        return inputAction != null
            ? inputAction.ReadValue<float>()
            : 0f;
    }

    public string GetBindingDisplay(
        GameplayAction action,
        int bindingIndex = -1)
    {
        InputAction inputAction = GetAction(action);

        if (inputAction == null)
        {
            return action.ToString();
        }

        if (bindingIndex >= 0 &&
            bindingIndex < inputAction.bindings.Count)
        {
            return inputAction.GetBindingDisplayString(
                bindingIndex);
        }

        for (int i = 0;
             i < inputAction.bindings.Count;
             i++)
        {
            InputBinding binding =
                inputAction.bindings[i];

            if (binding.isComposite ||
                binding.isPartOfComposite)
            {
                continue;
            }

            string display =
                inputAction.GetBindingDisplayString(i);

            if (!string.IsNullOrWhiteSpace(display))
            {
                return display;
            }
        }

        return action.ToString();
    }

    public string FormatPrompt(
        GameplayAction action,
        string description)
    {
        return "[" +
               GetBindingDisplay(action) +
               "] " +
               description;
    }

    public InputActionRebindingExtensions.RebindingOperation
        BeginInteractiveRebind(
            GameplayAction action,
            int bindingIndex,
            Action<bool> completed)
    {
        InputAction inputAction = GetAction(action);

        if (inputAction == null ||
            bindingIndex < 0 ||
            bindingIndex >= inputAction.bindings.Count)
        {
            completed?.Invoke(false);
            return null;
        }

        inputAction.Disable();

        InputActionRebindingExtensions.RebindingOperation operation =
            inputAction
                .PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough("<Keyboard>/escape");

        operation.OnCancel(value =>
        {
            value.Dispose();
            inputAction.Enable();
            completed?.Invoke(false);
        });

        operation.OnComplete(value =>
        {
            value.Dispose();
            inputAction.Enable();
            SaveBindingOverrides();
            BindingsChanged?.Invoke();
            completed?.Invoke(true);
        });

        operation.Start();
        return operation;
    }

    public void ResetBindingOverrides()
    {
        gameplayMap.RemoveAllBindingOverrides();
        systemMap.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(BindingOverridesKey);
        PlayerPrefs.Save();
        BindingsChanged?.Invoke();
    }

    public void SaveBindingOverrides()
    {
        string json =
            gameplayMap.asset.SaveBindingOverridesAsJson();

        PlayerPrefs.SetString(
            BindingOverridesKey,
            json);

        PlayerPrefs.Save();
    }

    private void LoadBindingOverrides()
    {
        if (!PlayerPrefs.HasKey(BindingOverridesKey))
        {
            return;
        }

        string json =
            PlayerPrefs.GetString(
                BindingOverridesKey,
                string.Empty);

        if (!string.IsNullOrWhiteSpace(json))
        {
            gameplayMap.asset
                .LoadBindingOverridesFromJson(json);
        }
    }

    private void BuildActions()
    {
        InputActionAsset asset =
            ScriptableObject.CreateInstance<InputActionAsset>();

        asset.hideFlags = HideFlags.HideAndDontSave;

        gameplayMap =
            new InputActionMap("Gameplay");

        systemMap =
            new InputActionMap("System");

        asset.AddActionMap(gameplayMap);
        asset.AddActionMap(systemMap);

        AddMoveAction();
        AddLookAction();

        AddButton(
            gameplayMap,
            GameplayAction.Jump,
            "<Keyboard>/space",
            "<Gamepad>/buttonSouth");

        AddButton(
            gameplayMap,
            GameplayAction.Primary,
            "<Mouse>/leftButton",
            "<Gamepad>/buttonWest");

        AddButton(
            gameplayMap,
            GameplayAction.Secondary,
            "<Mouse>/rightButton",
            "<Gamepad>/leftTrigger");

        AddButton(
            gameplayMap,
            GameplayAction.Use,
            "<Keyboard>/e",
            "<Gamepad>/buttonNorth");

        AddButton(
            gameplayMap,
            GameplayAction.MoveFurniture,
            "<Keyboard>/f",
            "<Gamepad>/dpad/up");

        AddButton(
            gameplayMap,
            GameplayAction.Rotate,
            "<Keyboard>/r",
            "<Gamepad>/rightShoulder");

        AddButton(
            gameplayMap,
            GameplayAction.Cancel,
            "<Keyboard>/escape",
            "<Gamepad>/buttonEast");

        InputAction scroll =
            gameplayMap.AddAction(
                GameplayAction.Scroll.ToString(),
                InputActionType.Value,
                expectedControlLayout: "Axis");

        scroll.AddBinding("<Mouse>/scroll/y");
        actions.Add(GameplayAction.Scroll,scroll);

        AddButton(
            systemMap,
            GameplayAction.Pause,
            "<Keyboard>/escape",
            "<Gamepad>/start");

    }

    private void AddMoveAction()
    {
        InputAction move =
            gameplayMap.AddAction(
                GameplayAction.Move.ToString(),
                InputActionType.Value,
                expectedControlLayout: "Vector2");

        move.AddCompositeBinding("2DVector")
            .With("Up","<Keyboard>/w")
            .With("Down","<Keyboard>/s")
            .With("Left","<Keyboard>/a")
            .With("Right","<Keyboard>/d");

        move.AddBinding("<Gamepad>/leftStick");
        actions.Add(GameplayAction.Move,move);
    }

    private void AddLookAction()
    {
        InputAction look =
            gameplayMap.AddAction(
                GameplayAction.Look.ToString(),
                InputActionType.Value,
                expectedControlLayout: "Vector2");

        look.AddBinding("<Pointer>/delta");
        look.AddBinding("<Gamepad>/rightStick");
        actions.Add(GameplayAction.Look,look);
    }

    private void AddButton(
        InputActionMap map,
        GameplayAction action,
        string keyboardBinding,
        string gamepadBinding)
    {
        InputAction inputAction =
            map.AddAction(
                action.ToString(),
                InputActionType.Button);

        inputAction.AddBinding(keyboardBinding);
        inputAction.AddBinding(gamepadBinding);
        actions.Add(action,inputAction);
    }

    private void OnDestroy()
    {
        if (gameplayMap != null)
        {
            gameplayMap.asset.Disable();
            Destroy(gameplayMap.asset);
        }

        actions.Clear();
    }
}
