using System;
using UnityEngine;

public sealed class GameplayModeController : MonoBehaviour
{
    private GameplayMode currentMode = GameplayMode.Gameplay;
    private GameplayMode modeBeforePause = GameplayMode.Gameplay;

    public GameplayMode CurrentMode => currentMode;

    public bool IsPaused =>
        currentMode == GameplayMode.Paused;

    public bool AllowsMovement =>
        currentMode == GameplayMode.Gameplay ||
        currentMode == GameplayMode.FurniturePlacement;

    public bool AllowsLooking => AllowsMovement;

    public bool AllowsWorldInteraction =>
        currentMode == GameplayMode.Gameplay;

    public event Action<GameplayMode> ModeChanged;

    private void Awake()
    {
        ApplyModePresentation();
    }

    public bool TrySetMode(GameplayMode newMode)
    {
        if (newMode == currentMode)
        {
            return true;
        }

        if (currentMode == GameplayMode.Paused &&
            newMode != modeBeforePause)
        {
            return false;
        }

        currentMode = newMode;
        ApplyModePresentation();
        ModeChanged?.Invoke(currentMode);
        return true;
    }

    public void Pause()
    {
        if (IsPaused)
        {
            return;
        }

        modeBeforePause = currentMode;
        currentMode = GameplayMode.Paused;
        ApplyModePresentation();
        ModeChanged?.Invoke(currentMode);
    }

    public void Resume()
    {
        if (!IsPaused)
        {
            return;
        }

        currentMode = modeBeforePause;
        ApplyModePresentation();
        ModeChanged?.Invoke(currentMode);
    }

    public void TogglePause()
    {
        if (IsPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void RefreshPresentation()
    {
        ApplyModePresentation();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            ApplyModePresentation();
        }
    }

    private void ApplyModePresentation()
    {
        bool showCursor =
            currentMode == GameplayMode.PriceEditing ||
            currentMode == GameplayMode.CheckoutInteraction ||
            currentMode == GameplayMode.DeviceUI ||
            currentMode == GameplayMode.Paused;

        Cursor.lockState = showCursor
            ? CursorLockMode.None
            : CursorLockMode.Locked;

        Cursor.visible = showCursor;
        Time.timeScale = IsPaused ? 0f : 1f;
    }

    private void OnDestroy()
    {
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }
}
