using System;
using UnityEngine;

public sealed class StoreDayController : MonoBehaviour
{
    [Header("Clock")]
    [Range(0,23)]
    [SerializeField]
    private int openingHour = 8;

    [Range(1,24)]
    [SerializeField]
    private int closingHour = 22;

    [Min(0.01f)]
    [SerializeField]
    private float gameMinutesPerRealSecond = 1f;

    [Header("Daily Costs")]
    [Min(0f)]
    [SerializeField]
    private float dailyRent = 50f;

    [Min(0f)]
    [SerializeField]
    private float dailyUtilities = 15f;

    public int CurrentDay { get; private set; } = 1;
    public float CurrentMinute { get; private set; }
    public bool IsDayRunning { get; private set; }
    public float DailyOperatingCost =>
        dailyRent + dailyUtilities;

    public int CurrentHour =>
        Mathf.FloorToInt(CurrentMinute / 60f) % 24;

    public int MinuteWithinHour =>
        Mathf.FloorToInt(CurrentMinute) % 60;

    public string FormattedTime =>
        CurrentHour.ToString("00") +
        ":" +
        MinuteWithinHour.ToString("00");

    public event Action<int> DayStarted;
    public event Action<int> DayEnded;
    public event Action<float> TimeChanged;

    private void Awake()
    {
        CurrentMinute = openingHour * 60f;
    }

    private void Update()
    {
        if (!IsDayRunning)
        {
            return;
        }

        CurrentMinute +=
            Time.deltaTime *
            gameMinutesPerRealSecond;

        TimeChanged?.Invoke(CurrentMinute);

        if (CurrentMinute >= closingHour * 60f)
        {
            EndDay();
        }
    }

    public void StartDay()
    {
        if (IsDayRunning)
        {
            return;
        }

        CurrentMinute = openingHour * 60f;
        IsDayRunning = true;

        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.Economy
                .SetCurrentDay(CurrentDay);
        }

        DayStarted?.Invoke(CurrentDay);
        TimeChanged?.Invoke(CurrentMinute);
    }

    public void EndDay()
    {
        if (!IsDayRunning)
        {
            return;
        }

        IsDayRunning = false;

        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.Economy
                .RecordOperatingCost(
                    Money.FromFloat(
                        DailyOperatingCost),
                    "Daily rent and utilities");
        }

        DayEnded?.Invoke(CurrentDay);
    }

    public void AdvanceToNextDay()
    {
        if (IsDayRunning)
        {
            EndDay();
        }

        CurrentDay++;
        StartDay();
    }

    public void Restore(
        int day,
        float currentMinute,
        bool running)
    {
        CurrentDay = Mathf.Max(1,day);

        CurrentMinute = Mathf.Clamp(
            currentMinute,
            0f,
            24f * 60f);

        IsDayRunning = running;

        if (GameBootstrap.Instance != null)
        {
            GameBootstrap.Instance.Economy
                .SetCurrentDay(CurrentDay);
        }

        TimeChanged?.Invoke(CurrentMinute);
    }

    private void OnValidate()
    {
        closingHour =
            Mathf.Clamp(
                closingHour,
                openingHour + 1,
                24);

        gameMinutesPerRealSecond =
            Mathf.Max(
                0.01f,
                gameMinutesPerRealSecond);

        dailyRent = Mathf.Max(0f,dailyRent);
        dailyUtilities =
            Mathf.Max(0f,dailyUtilities);
    }
}
