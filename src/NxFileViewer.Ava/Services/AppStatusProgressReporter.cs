using Emignatik.NxFileViewer.Services.BackgroundTask;

namespace NxFileViewer.Ava.Services;

public sealed class AppStatusProgressReporter : IProgressReporter
{
    private string _text = string.Empty;
    private double _percentage;

    public void SetText(string text)
    {
        _text = text;
        Publish();
    }

    public void SetPercentage(double value)
    {
        _percentage = Math.Clamp(value, 0, 1);
        Publish();
    }

    private void Publish()
    {
        var suffix = _percentage > 0 ? $" {(_percentage * 100):0}%" : string.Empty;
        AppStatus.Set($"{_text}{suffix}", "fa-regular fa-hourglass", StatusType.Working);
    }
}
