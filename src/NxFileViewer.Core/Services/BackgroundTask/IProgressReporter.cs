namespace Emignatik.NxFileViewer.Services.BackgroundTask;

public interface IProgressReporter
{
    void SetText(string text);

    void SetPercentage(double value);
}
