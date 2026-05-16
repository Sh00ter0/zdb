using Application.Discord.Panels.Core;
using Application.Discord.Panels.Core.Layout;
using Application.Services.Discord;
using Discord;
using LayoutSelectMenuComponent = Application.Discord.Panels.Core.Layout.SelectMenuComponent;

namespace Application.Discord.Panels.Rendering.LayoutMappers;

/// <summary>
/// Maps layout select menus into Discord select menu builders.
/// </summary>
public sealed class SelectMenuMapper(
    IInteractionCodec codec,
    IDiscordEmoteService emotes) : ILayoutComponentMapper
{
    /// <inheritdoc />
    public bool CanMap(IUiComponent component) => component is LayoutSelectMenuComponent;

    /// <inheritdoc />
    public object Map(IUiComponent component)
    {
        var menu = (LayoutSelectMenuComponent)component;
        var builder = new SelectMenuBuilder()
            .WithCustomId(codec.Encode(menu.Action.ToInteraction()))
            .WithPlaceholder(menu.Placeholder)
            .WithMinValues(menu.MinValues)
            .WithMaxValues(menu.MaxValues)
            .WithDisabled(menu.Disabled);

        foreach (var option in menu.Options)
        {
            builder.AddOption(
                label: option.Label,
                value: ResolveOptionValue(option),
                description: option.Description,
                emote: emotes.GetEmote(option.EmoteName),
                isDefault: option.IsDefault);
        }

        return builder;
    }

    /// <summary>
    /// Resolves the submitted value for a select menu option.
    /// </summary>
    /// <param name="option">The option whose value should be used by Discord.</param>
    /// <returns>An encoded panel action or a static option value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the option does not define an action or static value.</exception>
    private string ResolveOptionValue(SelectMenuOptionComponent option)
    {
        if (option.Action != null)
            return codec.Encode(option.Action.ToInteraction());

        return option.Value
            ?? throw new InvalidOperationException("Select menu option must define either Value or Action.");
    }
}
