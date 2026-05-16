using Application.Discord.Panels.Core.Layout;
using Application.Discord.Panels.Rendering.LayoutMappers;
using Discord;

namespace Application.Discord.Panels.Rendering;

/// <summary>
/// Converts transport-neutral panel layouts into Discord.NET message components.
/// </summary>
public sealed class DiscordLayoutMapper(IEnumerable<ILayoutComponentMapper> mappers)
{
    private readonly IReadOnlyList<ILayoutComponentMapper> _mappers = mappers.ToArray();

    /// <summary>
    /// Maps a panel layout into a Discord message component tree.
    /// </summary>
    /// <param name="layout">The declarative layout produced by a panel layout builder.</param>
    /// <returns>A Discord.NET message component ready to send or update.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a layout component cannot be mapped to a Discord message component.
    /// </exception>
    public MessageComponent Map(PanelLayout layout)
    {
        var builder = new ComponentBuilderV2();

        foreach (var component in layout.Components)
        {
            var mapped = MapComponent(component);
            if (mapped is IMessageComponentBuilder messageComponent)
            {
                builder.AddComponent(messageComponent);
                continue;
            }

            throw new InvalidOperationException($"Layout component {component.GetType().Name} did not map to a Discord message component.");
        }

        return builder.Build();
    }

    /// <summary>
    /// Finds the registered mapper that supports the component and maps it.
    /// </summary>
    /// <param name="component">The layout component to map.</param>
    /// <returns>The Discord.NET builder or model produced by the component mapper.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no mapper supports the component type.</exception>
    private object MapComponent(IUiComponent component)
    {
        var mapper = _mappers.FirstOrDefault(x => x.CanMap(component))
            ?? throw new InvalidOperationException($"No layout mapper found for component {component.GetType().Name}.");

        return mapper.Map(component);
    }
}
