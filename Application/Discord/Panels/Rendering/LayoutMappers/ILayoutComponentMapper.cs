using Application.Discord.Panels.Core.Layout;

namespace Application.Discord.Panels.Rendering.LayoutMappers;

/// <summary>
/// Maps a declarative layout component into the matching Discord.NET component representation.
/// </summary>
public interface ILayoutComponentMapper
{
    /// <summary>
    /// Determines whether this mapper supports the supplied layout component.
    /// </summary>
    /// <param name="component">The layout component to inspect.</param>
    /// <returns><see langword="true" /> when this mapper can map the component; otherwise <see langword="false" />.</returns>
    bool CanMap(IUiComponent component);

    /// <summary>
    /// Maps a supported layout component into a Discord.NET builder or model.
    /// </summary>
    /// <param name="component">The supported layout component to map.</param>
    /// <returns>The Discord.NET object produced from the layout component.</returns>
    object Map(IUiComponent component);
}
