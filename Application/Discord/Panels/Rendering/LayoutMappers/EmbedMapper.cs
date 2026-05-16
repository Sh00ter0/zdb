using Application.Discord.Panels.Core.Layout;
using Discord;

namespace Application.Discord.Panels.Rendering.LayoutMappers;

/// <summary>
/// Maps legacy embed layout components into Discord embed models.
/// </summary>
public sealed class EmbedMapper : ILayoutComponentMapper
{
    /// <inheritdoc />
    public bool CanMap(IUiComponent component) => component is EmbedComponent;

    /// <inheritdoc />
    public object Map(IUiComponent component)
    {
        var embed = (EmbedComponent)component;
        var builder = new EmbedBuilder()
            .WithTitle(embed.Title)
            .WithDescription(embed.Description);

        if (embed.Color.HasValue)
            builder.WithColor(new Color(embed.Color.Value));

        return builder.Build();
    }
}
