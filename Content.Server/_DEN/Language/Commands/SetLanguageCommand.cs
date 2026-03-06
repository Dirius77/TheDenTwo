using System.Linq;
using Content.Server._DEN.Language.EntitySystems;
using Content.Shared._DEN.Language;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Server._DEN.Language.Commands;

[AnyCommand]
public sealed class SetLanguageCommand : LocalizedEntityCommands
{
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override string Command => "setlang";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (player.Status != SessionStatus.InGame)
            return;

        if (player.AttachedEntity is not { } playerEntity)
        {
            shell.WriteError(Loc.GetString("shell-must-be-attached-to-entity"));
            return;
        }

        if (args.Length != 1)
            return;

        if (_proto.TryIndex<LanguagePrototype>(args[0], out var language))
            _language.TrySetLanguage(playerEntity, language);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (shell.Player is not { } player)
            return CompletionResult.Empty;

        if (player.Status != SessionStatus.InGame || player.AttachedEntity is not { } ent)
            return CompletionResult.Empty;

        if (args.Length == 1)
        {
            if (!_language.TryGetLanguageEntities(ent, out var languages))
                return CompletionResult.Empty;

            var spoken = languages.FindAll(lang => lang.Comp.Speaks);
            return CompletionResult.FromHintOptions(
                spoken.Select(lang => lang.Comp.Language.Id),
                Loc.GetString("setlang-completion-hint"));
        }
        return CompletionResult.Empty;
    }
}
