using System.CommandLine;
using AutoBrowser.Helpers;
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
        var forceUpdate = new Option<bool>("--force-update-check", "Force update check on startup");
        var skipUpdate = new Option<bool>("--no-update-check", "Skip update check on startup");
        var skipReReg = new Option<bool>("--no-re-register-prompt", "Skip re-registration prompt on startup");
        var skipSingleInst = new Option<bool>("--no-single-instance", "Allow multiple instances");

        var root = new RootCommand("AutoBrowser — URL router") { forceUpdate, skipUpdate, skipReReg, skipSingleInst };
        root.Name = "AutoBrowser";
        return root;
    }

    public static RouteOptions Parse(string[] rawArgs)
    {
        var root = GetRootCommand();
        var forceUpdate = (Option<bool>)root.Options[0];
        var skipUpdate = (Option<bool>)root.Options[1];
        var skipReReg = (Option<bool>)root.Options[2];
        var skipSingleInst = (Option<bool>)root.Options[3];

        var url = rawArgs.FirstOrDefault(UrlHelper.IsUrl);
        var optionArgs = rawArgs.Where(arg => !string.Equals(arg, url, StringComparison.Ordinal)).ToArray();
        var result = root.Parse(optionArgs);

        Log.Information("Parsed command line: args=[{Args}] url={Url} options=[{Options}] errors=[{Errors}]",
            string.Join(" | ", rawArgs),
            url,
            string.Join(" | ", optionArgs),
            string.Join(" | ", result.Errors.Select(e => e.Message)));

        return new RouteOptions(
            Url: url,
            ForceUpdate: result.GetValueForOption(forceUpdate),
            SkipUpdate: result.GetValueForOption(skipUpdate),
            SkipReRegister: result.GetValueForOption(skipReReg),
            SkipSingleInstance: result.GetValueForOption(skipSingleInst));
    }
}
