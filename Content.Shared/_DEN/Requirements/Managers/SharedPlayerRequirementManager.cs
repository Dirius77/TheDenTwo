using System.Text;
using Content.Shared._DEN.Requirements.PlayerRequirements;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._DEN.Requirements.Managers;

/// <summary>
///     A manager used to check player stats against a list of requirements, getting the pass/fail status of these requirements.
///     This can be used to apply restrictions to character actions, like jobs or traits.
/// </summary>
public abstract partial class SharedPlayerRequirementManager : IPlayerRequirementManager
{
    /// <summary>
    ///     Check a context against enumerable requirements and gets the final pass/fail status of these requirements.
    /// </summary>
    /// <param name="context">The context containing fields to check against the requirements.</param>
    /// <param name="requirements">An enumerable collection of requirements.</param>
    /// <returns>Whether or not this context passes *all* requirements. If even one fails, then this is false.</returns>
    [PublicAPI]
    public static bool CheckRequirements(PlayerRequirementContext context, IEnumerable<IPlayerRequirement> requirements)
    {
        foreach (var requirement in requirements)
            if (!CheckRequirement(context, requirement))
                return false;

        return true;
    }

    /// <summary>
    ///     Whether or not the item associated with a set of requirements should be hidden
    ///     from the player, such as in UI.
    /// </summary>
    /// <param name="context">The context containing fields to check against the requirements.</param>
    /// <param name="requirements">An enumerable collection of requirements.</param>
    /// <returns>Whether or not the item should be hidden from the player.</returns>

    [PublicAPI]
    public static bool ShouldHide(PlayerRequirementContext context, IEnumerable<IPlayerRequirement> requirements)
    {
        foreach (var requirement in requirements)
            if (!CheckRequirement(context, requirement) && requirement.HideIfFailed)
                return true;

        return false;
    }

    /// <summary>
    ///     Check a single requirement for whether it passes/fails against a context.
    /// </summary>
    /// <param name="context">The context containing fields to check against the requirement.</param>
    /// <param name="requirements">The requirement to check.</param>
    /// <returns>Whether this context passes the requirement.</returns>
    [PublicAPI]
    public static bool CheckRequirement(PlayerRequirementContext context, IPlayerRequirement requirement)
    {
        // Pre-check the requirement. This ensures our context has all the fields needed for the requirement.
        // If the pre-check fails, whether or not this requirement passes depends on requirement.MustPassPreCheck.
        // If you don't need to pass the pre-check, then it's an auto-success.
        if (!requirement.PreCheck(context))
            return !requirement.MustPassPreCheck;

        // Check the actual requirement, now.
        if (!requirement.CheckRequirement(context))
            return requirement.Inverted;

        return !requirement.Inverted;
    }

    /// <summary>
    ///     Get a combined reason message for an enumerable collection of requirements.
    /// </summary>
    /// <param name="requirements">A collection of requirements.</param>
    /// <param name="context">A context to check against the requirements.</param>
    /// <returns>A formatted message containing all the requirement reasons.</returns>
    [PublicAPI]
    public static FormattedMessage GetCombinedReason(IEnumerable<IPlayerRequirement> requirements, PlayerRequirementContext? context = null)
    {
        var messageBuilder = new StringBuilder();
        foreach (var req in requirements)
            messageBuilder.AppendLine(req.GetReason(context));

        var messageString = messageBuilder.ToString().Trim();
        return FormattedMessage.FromMarkupPermissive(messageString);
    }

    /// <summary>
    ///     Get a combined reason message for an enumerable collection of requirements.
    ///     This function will only include requirements that fail, given a context.
    /// </summary>
    /// <param name="context">A context to check against the requirements.</param>
    /// <param name="requirements">A collection of requirements.</param>
    /// <returns>A formatted message containing all the requirement reasons.</returns>
    [PublicAPI]
    public static FormattedMessage GetFailedCombinedReason(PlayerRequirementContext context, IEnumerable<IPlayerRequirement> requirements)
    {
        var messageBuilder = new StringBuilder();
        foreach (var req in requirements)
        {
            if (!CheckRequirement(context, req))
                messageBuilder.AppendLine(req.GetReason(context));
        }

        var messageString = messageBuilder.ToString().Trim();
        return FormattedMessage.FromMarkupPermissive(messageString);
    }

    /// <inheritdoc />
    public abstract PlayerRequirementContext GetPlayerContext(ICommonSession session);
}
