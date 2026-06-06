using Content.Shared._DEN.Consent.Managers;
using Content.Shared._DEN.Consent.Prototypes;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._DEN.Consent;

[AnyCommand]
public sealed partial class SetConsentCommand : LocalizedCommands
{
    [Dependency] private IConsentManager _consentManager = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;

    public override string Command { get; } = "setconsent";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2 || shell.Player == null)
        {
            shell.WriteError(Loc.GetString("cmd-setconsent-error-args", ("usage", Help)));
            return;
        }

        if (!_protoManager.TryIndex<ConsentTogglePrototype>(args[0], out _))
        {
            shell.WriteError(Loc.GetString("cmd-setconsent-error-invalid-consent", ("consentId", args[0]), ("usage", Help)));
            return;
        }

        if (!bool.TryParse(args[1], out var newValue))
        {
            shell.WriteError(Loc.GetString("cmd-setconsent-error-bool", ("value", args[1]), ("usage", Help)));
            return;
        }

        _consentManager.SetConsentToggle(shell.Player.UserId, args[0], newValue);
        shell.WriteLine(Loc.GetString("cmd-setconsent-success", ("consentId", args[0]), ("value", args[1])));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = CompletionHelper.PrototypeIDs<ConsentTogglePrototype>();
            return CompletionResult.FromOptions(options);
        }

        if (args.Length == 2)
        {
            var options = CompletionHelper.Booleans;
            return CompletionResult.FromOptions(options);
        }

        return CompletionResult.Empty;
    }
}

[AnyCommand]
public sealed partial class ConsentCommand : LocalizedCommands
{
    [Dependency] private IConsentManager _consentManager = default!;

    public override string Command { get; } = "consents";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
            return;

        var consents = _consentManager.GetConsentToggles(shell.Player.UserId);
        var consentsMessage = string.Join("\n -", consents);

        if (consents.Count == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-consent-no-different"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-consent-differences", ("differentConsents", consentsMessage)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = CompletionHelper.PrototypeIDs<ConsentTogglePrototype>();
            return CompletionResult.FromOptions(options);
        }

        return CompletionResult.Empty;
    }
}
