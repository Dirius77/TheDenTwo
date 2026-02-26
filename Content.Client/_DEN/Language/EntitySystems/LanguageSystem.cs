using System.Diagnostics.CodeAnalysis;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Client._DEN.Language.EntitySystems;

public sealed class LanguageSystem : SharedLanguageSystem
{
    private event Action? OnLanguageUpdate;
}
