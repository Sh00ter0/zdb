namespace Application.Discord.Panels.Core.Layout;

/// <summary>
/// Describes a final panel action that can be encoded into a Discord component custom id.
/// </summary>
/// <param name="Panel">The target panel identifier.</param>
/// <param name="Action">The final action identifier handled by the target panel.</param>
/// <param name="EntityId">The optional primary domain entity identifier.</param>
/// <param name="SubEntityId">The optional secondary domain entity identifier.</param>
/// <param name="UserId">The optional Discord user identifier bound to the action.</param>
/// <param name="Metadata">Optional compact metadata to encode with the action.</param>
public sealed record PanelActionDescriptor(
    string Panel,
    string Action,
    string? EntityId = null,
    string? SubEntityId = null,
    ulong UserId = 0,
    IReadOnlyDictionary<string, string>? Metadata = null)
{
    /// <summary>
    /// Converts the descriptor into the transport-neutral interaction model.
    /// </summary>
    /// <returns>A panel interaction ready to be encoded.</returns>
    public PanelInteraction ToInteraction() => new()
    {
        Panel = Panel,
        Action = Action,
        EntityId = EntityId,
        SubEntityId = SubEntityId,
        UserId = UserId,
        Metadata = Metadata is null ? null : new Dictionary<string, string>(Metadata)
    };
}
