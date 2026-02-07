using Content.Server._DEN.Language.EntitySystems;
using Content.Server.Administration;
using Content.Shared._DEN.Language;
using Content.Shared.Administration;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server._DEN.Language.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed partial class LanguageCommand : ToolshedCommand
{
    private LanguageSystem? _language;

    [CommandImplementation("add")]
    public EntityUid Add([PipedArgument] EntityUid target,
        ProtoId<LanguagePrototype> language,
        bool speaks,
        ProtoId<LanguageFluencyPrototype> fluency)
    {
        _language ??= GetSys<LanguageSystem>();
        _language.TryAddLanguage(target, language, speaks, fluency, out var _);

        return target;
    }

    [CommandImplementation("remove")]
    public EntityUid Remove([PipedArgument] EntityUid target,
        ProtoId<LanguagePrototype> language,
        bool all)
    {
        _language ??= GetSys<LanguageSystem>();

        if (all)
            _language.TryRemoveLanguages(target, language);
        else
            _language.TryRemoveLanguage(target, language);

        return target;
    }

    [CommandImplementation("get")]
    public List<(ProtoId<LanguagePrototype>, ProtoId<LanguageFluencyPrototype>, bool)> Get([PipedArgument] EntityUid target,
        ProtoId<LanguagePrototype> language)
    {
        _language ??= GetSys<LanguageSystem>();

        _language.TryGetLanguages(target, language, out var languages);

        return languages;
    }

    [CommandImplementation("getall")]
    public List<(ProtoId<LanguagePrototype>, ProtoId<LanguageFluencyPrototype>, bool)> GetAll(
        [PipedArgument] EntityUid target)
    {
        _language ??= GetSys<LanguageSystem>();

        _language.TryGetLanguages(target, out var languages);

        return languages;
    }

    [CommandImplementation("speaks")]
    public bool Speaks([PipedArgument] EntityUid target,
        ProtoId<LanguagePrototype> language,
        [CommandInverted] bool inverted)
    {
        _language ??= GetSys<LanguageSystem>();

        var speaks = _language.SpeaksLanguage(target, language);

        return inverted ? !speaks : speaks;
    }

    [CommandImplementation("understands")]
    public bool Understands([PipedArgument] EntityUid target,
        ProtoId<LanguagePrototype> language,
        ProtoId<LanguageFluencyPrototype> minimumFluency,
        [CommandInverted] bool inverted)
    {
        _language ??= GetSys<LanguageSystem>();

        var understands = _language.UnderstandsLanguage(target, language, minimumFluency);

        return inverted ? !understands : understands;
    }
}
