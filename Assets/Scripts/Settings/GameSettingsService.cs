using System;
using UnityEngine;

[Serializable]
public sealed class GameSettingsData
{
    public float MasterVolume = 1f;
    public float LookSensitivity = 100f;
    public bool Fullscreen = true;
    public int QualityLevel = 2;
    public int TargetFrameRate = 60;
}

public sealed class GameSettingsService : MonoBehaviour
{
    private const string SettingsKey =
        "Clerk.Settings";

    public GameSettingsData Settings { get; private set; } =
        new GameSettingsData();

    public event Action SettingsChanged;

    private void Awake()
    {
        Load();
        Apply();
    }

    public void SetMasterVolume(float volume)
    {
        Settings.MasterVolume =
            Mathf.Clamp01(volume);

        ApplyAndSave();
    }

    public void SetLookSensitivity(float sensitivity)
    {
        Settings.LookSensitivity =
            Mathf.Clamp(sensitivity,1f,500f);

        PlayerController player =
            FindAnyObjectByType<PlayerController>();

        if (player != null)
        {
            player.LookSpeed =
                Settings.LookSensitivity;
        }

        Save();
        SettingsChanged?.Invoke();
    }

    public void SetFullscreen(bool fullscreen)
    {
        Settings.Fullscreen = fullscreen;
        ApplyAndSave();
    }

    public void SetQualityLevel(int qualityLevel)
    {
        Settings.QualityLevel =
            Mathf.Clamp(
                qualityLevel,
                0,
                Mathf.Max(
                    0,
                    QualitySettings.names.Length - 1));

        ApplyAndSave();
    }

    public void SetTargetFrameRate(int targetFrameRate)
    {
        Settings.TargetFrameRate =
            Mathf.Clamp(targetFrameRate,30,240);

        ApplyAndSave();
    }

    public void ResetToDefaults()
    {
        Settings = new GameSettingsData();
        ApplyAndSave();
    }

    public void Apply()
    {
        AudioListener.volume =
            Settings.MasterVolume;

        Screen.fullScreen =
            Settings.Fullscreen;

        if (QualitySettings.names.Length > 0)
        {
            QualitySettings.SetQualityLevel(
                Mathf.Clamp(
                    Settings.QualityLevel,
                    0,
                    QualitySettings.names.Length - 1),
                true);
        }

        Application.targetFrameRate =
            Settings.TargetFrameRate;

        PlayerController player =
            FindAnyObjectByType<PlayerController>();

        if (player != null)
        {
            player.LookSpeed =
                Settings.LookSensitivity;
        }

        SettingsChanged?.Invoke();
    }

    private void ApplyAndSave()
    {
        Apply();
        Save();
    }

    private void Save()
    {
        PlayerPrefs.SetString(
            SettingsKey,
            JsonUtility.ToJson(Settings));

        PlayerPrefs.Save();
    }

    private void Load()
    {
        if (!PlayerPrefs.HasKey(SettingsKey))
        {
            return;
        }

        string json =
            PlayerPrefs.GetString(
                SettingsKey,
                string.Empty);

        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        GameSettingsData loaded =
            JsonUtility.FromJson<GameSettingsData>(
                json);

        if (loaded != null)
        {
            Settings = loaded;
        }
    }
}
