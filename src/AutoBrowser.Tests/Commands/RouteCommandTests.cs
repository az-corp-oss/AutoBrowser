using AutoBrowser.Commands;

namespace AutoBrowser.Tests.Commands;

public class RouteCommandTests
{
    [Fact]
    public void Parse_UrlEndingWithAppName_KeepsUrl()
    {
        const string url = "https://github.com/az-corp-oss/AutoBrowser";

        var options = RouteCommand.Parse([url]);

        Assert.Equal(url, options.Url);
    }

    [Fact]
    public void Parse_UrlWithOptions_KeepsUrlAndFlags()
    {
        const string url = "https://example.com/fallback-probe";

        var options = RouteCommand.Parse([url, "--no-update-check", "--no-re-register-prompt"]);

        Assert.Equal(url, options.Url);
        Assert.True(options.SkipUpdate);
        Assert.True(options.SkipReRegister);
    }

    [Fact]
    public void Parse_MicrosoftEdgeUrl_KeepsUrl()
    {
        const string url = "microsoft-edge:https://github.com/az-corp-oss/AutoBrowser";

        var options = RouteCommand.Parse([url]);

        Assert.Equal(url, options.Url);
    }
}
