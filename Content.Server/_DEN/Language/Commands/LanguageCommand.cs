using System.Text;
using Content.Server._DEN.Language.EntitySystems;
using Content.Server.Administration;
using Content.Shared._DEN.Language;
using Content.Shared.Administration;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Server._DEN.Language.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed partial class LanguageCommand : ToolshedCommand
{
    private LanguageSystem? _language;

    [CommandImplementation("add")]
    public EntityUid Add([PipedArgument] EntityUid target,
        ProtoId<LanguagePrototype> language,
        bool speaks = true,
        [CommandArgument(typeof(FluencyProtoIdParser))] string fluency = "Fluent")
    {
        _language ??= GetSys<LanguageSystem>();
        _language.TryAddLanguage(target, language, fluency, speaks, out var _);

        return target;
    }

    [CommandImplementation("remove")]
    public EntityUid Remove([PipedArgument] EntityUid target,
        ProtoId<LanguagePrototype> language,
        bool all = false)
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
        [CommandInverted] bool inverted,
        [CommandArgument(typeof(FluencyProtoIdParser))] string minimumFluency = "Unfamiliar")
    {
        _language ??= GetSys<LanguageSystem>();

        var understands = _language.UnderstandsLanguage(target, language, minimumFluency);

        return inverted ? !understands : understands;
    }
}

// This is the only way I could figure out to make an optional argument that's a ProtoId
// C# won't convert from a default 'string' to 'ProtoId' in method parameters.
// This is gross and a copy paste of ProtoIdTypeParser because CommandArgument also won't
// accept a TypeParser, only a CustomTypeParser :(
public sealed class FluencyProtoIdParser : CustomTypeParser<string>
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override bool TryParse(ParserContext ctx, out string result)
    {
        result = "";
        string? proto;

        // Prototype ids can be specified without quotes, but for backwards compatibility, we also accept strings with
        // quotes, as previously it **had** to be a string
        if (ctx.PeekRune() == new Rune('"'))
        {
            if (!Toolshed.TryParse(ctx, out proto))
                return false;
        }
        else
        {
            proto = ctx.GetWord(ParserContext.IsToken);
        }

        if (proto is null || !_proto.HasIndex<LanguageFluencyPrototype>(proto))
        {
            _proto.TryGetKindFrom<LanguageFluencyPrototype>(out var kind);
            DebugTools.AssertNotNull(kind);

            ctx.Error = new NotAValidPrototype(proto ?? "[null]", kind!);
            result = "";
            return false;
        }

        result = new(proto);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {

        var hint = ToolshedCommand.GetArgHint(arg, typeof(ProtoId<LanguageFluencyPrototype>));
        var maxCount = _config.GetCVar(CVars.ToolshedPrototypesAutocompleteLimit);
        var options = CompletionHelper.PrototypeIdsLimited<LanguageFluencyPrototype>(ctx.Input[ctx.Index..], proto: _proto, maxCount: maxCount);
        return CompletionResult.FromHintOptions(options, hint);
    }
}
