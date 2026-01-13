using Income.Application.Services.Streams;

namespace Dashboard.Services;

/// <summary>
/// Application-wide state that controls the current viewing mode.
/// </summary>
public sealed class AppState
{
    private AppMode _currentMode = AppMode.Income;

    /// <summary>
    /// The current application mode (Income, Outcome, or Divergence).
    /// </summary>
    public AppMode CurrentMode
    {
        get => _currentMode;
        set
        {
            if (_currentMode != value)
            {
                _currentMode = value;
                OnModeChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Event raised when the mode changes.
    /// </summary>
    public event Action? OnModeChanged;

    /// <summary>
    /// Gets the StreamTypeDto for the current mode.
    /// Returns null for Divergence mode as it shows both.
    /// </summary>
    public StreamTypeDto? CurrentStreamType => CurrentMode switch
    {
        AppMode.Income => StreamTypeDto.Income,
        AppMode.Outcome => StreamTypeDto.Outcome,
        AppMode.Divergence => null,
        _ => StreamTypeDto.Income
    };

    /// <summary>
    /// Gets the appropriate language for the current mode.
    /// </summary>
    public StreamLanguage CurrentLanguage => CurrentMode switch
    {
        AppMode.Income => StreamLanguage.Income,
        AppMode.Outcome => StreamLanguage.Outcome,
        AppMode.Divergence => StreamLanguage.Income, // Divergence uses neutral language
        _ => StreamLanguage.Income
    };

    /// <summary>
    /// Gets the accent color for the current mode.
    /// </summary>
    public string AccentColor => CurrentMode switch
    {
        AppMode.Income => "#10b981",   // Green
        AppMode.Outcome => "#ef4444",  // Red
        AppMode.Divergence => "#8b5cf6", // Purple
        _ => "#10b981"
    };

    /// <summary>
    /// Gets the background tint for the current mode.
    /// </summary>
    public string BackgroundTint => CurrentMode switch
    {
        AppMode.Income => "rgba(16,185,129,0.02)",
        AppMode.Outcome => "rgba(239,68,68,0.02)",
        AppMode.Divergence => "rgba(139,92,246,0.02)",
        _ => "rgba(16,185,129,0.02)"
    };

    /// <summary>
    /// Gets the mode display name.
    /// </summary>
    public string ModeDisplayName => CurrentMode switch
    {
        AppMode.Income => "Income",
        AppMode.Outcome => "Outcome",
        AppMode.Divergence => "Divergence",
        _ => "Income"
    };
}

/// <summary>
/// The application viewing mode.
/// </summary>
public enum AppMode
{
    /// <summary>View income streams (money flowing in)</summary>
    Income,

    /// <summary>View outcome streams (money flowing out)</summary>
    Outcome,

    /// <summary>View net flow (income minus outcome)</summary>
    Divergence
}
