using Application.Discord.Panels.Core.Layout;
using Discord;
using LayoutSeparatorComponent = Application.Discord.Panels.Core.Layout.SeparatorComponent;

namespace Application.Discord.Panels.Rendering.LayoutMappers;

/// <summary>
/// Maps layout separators into Discord separator builders.
/// </summary>
public sealed class SeparatorMapper : ILayoutComponentMapper
{
    /// <inheritdoc />
    public bool CanMap(IUiComponent component) => component is LayoutSeparatorComponent;

    /// <inheritdoc />
    public object Map(IUiComponent component)
    {
        var separator = (LayoutSeparatorComponent)component;
        return new SeparatorBuilder(
            separator.IsDivider,
            separator.Size == SeparatorSize.Large ? SeparatorSpacingSize.Large : SeparatorSpacingSize.Small);
    }
}
