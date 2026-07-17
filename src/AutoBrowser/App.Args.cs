using System.CommandLine;
using System.CommandLine.Parsing;
using Serilog;

namespace AutoBrowser;

public partial class App
{
    internal sealed record ParsedArgs(
        string? Url,
        bool ForceUpdate,
        bool SkipUpdate,
        bool SkipReRegister,
        bool SkipSingleInstance);

    private static ParsedArgs ParseArgs(string[] rawArgs)
    {
        var urlArg         = new Argument<string?>("url", () => null, "URL to route (http/https/autobrowser)");
        var forceUpdate    = new Option<bool>("--force-update-check",  "Force update check on startup");
        var skipUpdate     = new Option<bool>("--no-update-check",     "Skip update check on startup");
        var skipReReg      = new Option<bool>("--no-re-register-prompt", "Skip re-registration prompt on startup");
        var skipSingleInst = new Option<bool>("--no-single-instance",  "Allow multiple instances");

        var root = new RootCommand("AutoBrowser — URL router") { urlArg, forceUpdate, skipUpdate, skipReReg, skipSingleInst };

        var result = root.Parse(rawArgs);

        if (result.Errors.Count > 0)
        {
            foreach (var err in result.Errors)
                Log.Warning("Arg parse error: {Error}", err.Message);
        }

        var rawUrl = result.GetValueForArgument(urlArg);
        var url    = IsUrl(rawUrl) ? rawUrl : null;

        return new ParsedArgs(
            Url:                url,
            ForceUpdate:        result.GetValueForOption(forceUpdate),
            SkipUpdate:         result.GetValueForOption(skipUpdate),
            SkipReRegister:     result.GetValueForOption(skipReReg),
            SkipSingleInstance: result.GetValueForOption(skipSingleInst));
    }
}
