using Application.Discord.Panels.Core.Layout;
using Discord;
using LayoutActionRowComponent = Application.Discord.Panels.Core.Layout.ActionRowComponent;

namespace Application.Discord.Panels.Rendering.LayoutMappers;

/// <summary>
/// Maps layout action rows into Discord action row builders.
/// </summary>
public sealed class ActionRowMapper(
    ButtonMapper buttonMapper,
    SelectMenuMapper selectMenuMapper) : ILayoutComponentMapper
{
    /// <inheritdoc />
    public bool CanMap(IUiComponent component) => component is LayoutActionRowComponent;

    /// <inheritdoc />
    public object Map(IUiComponent component)
    {
        var row = (LayoutActionRowComponent)component;
        var builder = new ActionRowBuilder();

        foreach (var child in row.Components)
        {
            var mapped = MapChild(child);
            builder.AddComponent(mapped);
        }

        return builder;
    }

    /// <summary>
    /// Maps a child component that is allowed inside a Discord action row.
    /// </summary>
    /// <param name="component">The action row child component.</param>
    /// <returns>The Discord message component builder for the child.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the component cannot be placed in an action row.</exception>
    private IMessageComponentBuilder MapChild(IUiComponent component)
    {
        if (buttonMapper.CanMap(component))
            return (IMessageComponentBuilder)buttonMapper.Map(component);

        if (selectMenuMapper.CanMap(component))
            return (IMessageComponentBuilder)selectMenuMapper.Map(component);

        throw new InvalidOperationException($"Unsupported action row component {component.GetType().Name}.");
    }
}
