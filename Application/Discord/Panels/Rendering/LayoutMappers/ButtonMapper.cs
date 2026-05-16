using Application.Discord.Panels.Core;
using Application.Discord.Panels.Core.Layout;
using Application.Services.Discord;
using Discord;
using LayoutButtonComponent = Application.Discord.Panels.Core.Layout.ButtonComponent;

namespace Application.Discord.Panels.Rendering.LayoutMappers;

/// <summary>
/// Maps layout buttons into Discord button builders.
/// </summary>
public sealed class ButtonMapper(
    IInteractionCodec codec,
    IDiscordEmoteService emotes) : ILayoutComponentMapper
{
    /// <inheritdoc />
    public bool CanMap(IUiComponent component) => component is LayoutButtonComponent;

    /// <inheritdoc />
    public object Map(IUiComponent component)
    {
        var button = (LayoutButtonComponent)component;
        var builder = new ButtonBuilder()
            .WithLabel(button.Label)
            .WithStyle(MapStyle(button.Style))
            .WithDisabled(button.Disabled);

        if (button.Style == ButtonStyleType.Link && !string.IsNullOrWhiteSpace(button.Url))
            builder.WithUrl(button.Url);
        else
            builder.WithCustomId(codec.Encode(button.Action.ToInteraction()));

        var emote = emotes.GetEmote(button.EmoteName);
        if (emote != null)
            builder.WithEmote(emote);

        return builder;
    }

    /// <summary>
    /// Converts the panel button style abstraction to the Discord.NET button style.
    /// </summary>
    /// <param name="style">The panel-level button style.</param>
    /// <returns>The matching Discord.NET button style.</returns>
    private static ButtonStyle MapStyle(ButtonStyleType style) => style switch
    {
        ButtonStyleType.Primary => ButtonStyle.Primary,
        ButtonStyleType.Secondary => ButtonStyle.Secondary,
        ButtonStyleType.Success => ButtonStyle.Success,
        ButtonStyleType.Danger => ButtonStyle.Danger,
        ButtonStyleType.Link => ButtonStyle.Link,
        _ => ButtonStyle.Primary
    };
}
