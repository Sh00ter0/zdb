namespace Application.Discord.Panels.Core;

/// <summary>
/// Resolves registered panels by their stable panel identifiers.
/// </summary>
public interface IPanelRegistry
{
    /// <summary>
    /// Gets a registered panel by identifier.
    /// </summary>
    /// <param name="id">The panel identifier encoded in the interaction.</param>
    /// <returns>The matching panel.</returns>
    IConfigPanel Get(string id);
}
