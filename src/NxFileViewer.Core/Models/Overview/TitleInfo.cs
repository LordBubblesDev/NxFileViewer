using LibHac.Ns;

namespace Emignatik.NxFileViewer.Models.Overview;

public class TitleInfo(ref ApplicationControlProperty.ApplicationTitle applicationTitle, NacpLanguage language)
{
    private readonly ApplicationControlProperty.ApplicationTitle _applicationTitle = applicationTitle;

    public string AppName => _applicationTitle.NameString.ToString();

    public string Publisher => _applicationTitle.PublisherString.ToString();

    public NacpLanguage Language { get; } = language;

    public byte[]? Icon { get; set; }

    public override string ToString()
    {
        var appName = AppName;
        var publisher = Publisher;

        if (string.IsNullOrWhiteSpace(appName) && string.IsNullOrWhiteSpace(publisher)) { return ""; }

        var publisherStr = string.IsNullOrEmpty(publisher) ? "" : $" - {publisher}";
        return $"{appName}{publisherStr} ({Language})";
    }
}
