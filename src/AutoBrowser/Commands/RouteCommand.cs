using System.CommandLine;
using AutoBrowser.Models;
using Serilog;

namespace AutoBrowser.Commands;

public record RouteOptions(
    string? Url,
    bool ForceUpdate,
    bool SkipUpdate,
    bool SkipReRegister,
    bool SkipSingleInstance);

public class RouteCommand
{
    public static RootCommand GetRootCommand()
    {
        var urlArg = new Argument<string?>("url", () => null, "URL to route (http/https/autobrowser)");
        var forceUpdate = new Option<bool>("--force-update-check", "Force update check on startup");
        var skipUpdate = new Option<bool>("--no-update-check", "Skip update check on startup");
        var skipReReg = new Option<bool>("--no-re-register-prompt", "Skip re-registration prompt on startup");
        var skipSingleInst = new Option<bool>("--no-single-instance", "Allow multiple instances");

        return new RootCommand("AutoBrowser — URL router") { urlArg, forceUpdate, skipUpdate, skipReReg, skipSingleInst };
    }

    public static RouteOptions Parse(string[] rawArgs)
    {
        var root = GetRootCommand();
        var urlArg = (Argument<string?>)root.Arguments[0];
        var forceUpdate = (Option<bool>)root.Options[0];
        var skipUpdate = (Option<bool>)root.Options[1];
        var skipReReg = (Option<bool>)root.Options[2];
        var skipSingleInst = (Option<bool>)root.Options[3];

        var result = root.Parse(rawArgs);

        return new RouteOptions(
            Url: result.GetValueForArgument(urlArg),
            ForceUpdate: result.GetValueForOption(forceUpdate),
            SkipUpdate: result.GetValueForOption(skipUpdate),
            SkipReRegister: result.GetValueForOption(skipReReg),
            SkipSingleInstance: result.GetValueForOption(skipSingleInst));
    }
}