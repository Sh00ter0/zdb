namespace Application.Discord.Panels.Core;

/// <summary>
/// In-memory registry of configured panels keyed by panel identifier.
/// </summary>
public sealed class PanelRegistry(IEnumerable<IConfigPanel> panels) : IPanelRegistry
{
    private readonly Dictionary<string, IConfigPanel> _panels = panels.ToDictionary(x => x.Id);

    /// <inheritdoc />
    public IConfigPanel Get(string id)
    {
        if (!_panels.TryGetValue(id, out var panel))
            throw new InvalidOperationException($"Panel with ID '{id}' is not registered.");

        return panel;
    }
}
