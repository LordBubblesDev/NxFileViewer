using CommunityToolkit.Mvvm.ComponentModel;
using NxFileViewer.Ava.Localization;

namespace NxFileViewer.Ava;

public enum StatusType
{
    Static,
    Working
}

public sealed partial class AppStatus : ObservableObject
{
    private const string Default_Icon = "fa-regular fa-circle-check";
    private const string Dot = ".";
    
    public static AppStatus Shared { get; } = new();

    private AppStatus()
    {
        _timer = new Timer(UpdateLoadingStatus);
        _timer.Change(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0.3));
    }

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly Timer _timer;

    [ObservableProperty]
    public partial string Status { get; set; } = Locale[FvLocale.Status_Ready];

    [ObservableProperty]
    public partial string Suffix { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Icon { get; set; } = Default_Icon;
    [ObservableProperty]
    public partial StatusType Type { get; set; } = StatusType.Static;

    /// <summary>
    /// Reset the status.
    /// </summary>
    public static void Reset() => Shared.SetInternal(Locale[FvLocale.Status_Ready]);

    /// <summary>
    /// Set a temporary status for 1.5 seconds.
    /// </summary>
    /// <param name="status"></param>
    /// <param name="icon"></param>
    /// <param name="type"></param>
    public static void SetTemporaryShort(string status, string icon = Default_Icon, StatusType type = StatusType.Static)
        => Shared.SetTemporaryInternal(status, icon, type, duration: 1.5);

    /// <summary>
    /// Set a temporary status for 3 seconds.
    /// </summary>
    /// <param name="status"></param>
    /// <param name="icon"></param>
    /// <param name="type"></param>
    public static void SetTemporaryLong(string status, string icon = Default_Icon, StatusType type = StatusType.Static)
        => Shared.SetTemporaryInternal(status, icon, type, duration: 3);

    /// <summary>
    /// Set a static or working status.
    /// </summary>
    /// <param name="status"></param>
    /// <param name="icon"></param>
    /// <param name="type"></param>
    public static void Set(string status, string icon = Default_Icon, StatusType type = StatusType.Static)
        => Shared.SetInternal(status, icon, type);

    /// <summary>
    /// Set a temporary static or working status.
    /// </summary>
    /// <param name="status"></param>
    /// <param name="icon"></param>
    /// <param name="type"></param>
    /// <param name="duration">The duration of the status in seconds</param>
    public static void SetTemporary(string status, string icon = Default_Icon, StatusType type = StatusType.Static, double duration = 2.5)
        => Shared.SetTemporaryInternal(status, icon, type, duration);
    
    /// <inheritdoc cref="Set"/>
    private void SetInternal(string status, string icon = Default_Icon, StatusType type = StatusType.Static)
    {
        Status = status;
        Icon = icon;
        Type = type;
    }

    /// <inheritdoc cref="SetTemporary"/>
    private void SetTemporaryInternal(string status, string icon = Default_Icon, StatusType type = StatusType.Static, double duration = 2.5)
    {
        Set(status, icon, type);
        _ = Task.Run((Func<Task?>)(async () => {
            await Task.Delay(TimeSpan.FromSeconds(duration));

            if (Status == status) {
                Reset();
            }
        }));
    }

    private void UpdateLoadingStatus(object? _)
    {
        if (Type is StatusType.Static) {
            return;
        }

        Suffix = Suffix.Length switch {
            4 => Dot,
            _ => Suffix + Dot
        };
    }

    partial void OnTypeChanged(StatusType value)
    {
        Suffix = value switch {
            StatusType.Static => string.Empty,
            _ => Suffix
        };
    }
}