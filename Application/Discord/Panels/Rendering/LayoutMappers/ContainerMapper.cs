using Application.Discord.Panels.Core.Layout;
using Application.Services.Discord;
using Discord;
using Discord.WebSocket;
using Domain.Constants;
using System.Reflection;
using System.Text;
using LayoutContainerComponent = Application.Discord.Panels.Core.Layout.ContainerComponent;
using LayoutSectionComponent = Application.Discord.Panels.Core.Layout.SectionComponent;
using LayoutSeparatorComponent = Application.Discord.Panels.Core.Layout.SeparatorComponent;

namespace Application.Discord.Panels.Rendering.LayoutMappers;

/// <summary>
/// Maps panel containers into Discord V2 container builders with shared header and footer support.
/// </summary>
public sealed class ContainerMapper(
    TextMapper textMapper,
    SectionMapper sectionMapper,
    SeparatorMapper separatorMapper,
    ActionRowMapper actionRowMapper,
    DiscordSocketClient client,
    IDiscordEmoteService emotes) : ILayoutComponentMapper
{
    /// <inheritdoc />
    public bool CanMap(IUiComponent component) => component is LayoutContainerComponent;

    /// <inheritdoc />
    public object Map(IUiComponent component)
    {
        var container = (LayoutContainerComponent)component;
        var builder = new ContainerBuilder()
            .WithAccentColor(new Color(container.AccentColor ?? AppColors.Info));

        AddHeader(builder, container);

        foreach (var child in container.Components)
        {
            builder.AddComponent(MapChild(child));
        }

        if (container.IncludeFooter)
        {
            builder.AddComponent((IMessageComponentBuilder)separatorMapper.Map(
                new LayoutSeparatorComponent(container.FooterSeparatorSize, container.FooterSeparatorDivider)));
            builder.AddComponent((IMessageComponentBuilder)textMapper.Map(new TextComponent(BuildFooterText())));
        }

        return builder;
    }

    /// <summary>
    /// Adds the configured header to a container, optionally using the bot avatar as a thumbnail.
    /// </summary>
    /// <param name="builder">The Discord container builder being populated.</param>
    /// <param name="container">The layout container whose header should be rendered.</param>
    private void AddHeader(ContainerBuilder builder, LayoutContainerComponent container)
    {
        if (string.IsNullOrWhiteSpace(container.Header))
            return;

        if (container.UseBotThumbnail && !string.IsNullOrWhiteSpace(GetBotAvatarUrl()))
        {
            builder.AddComponent((IMessageComponentBuilder)sectionMapper.Map(new LayoutSectionComponent(
                Texts: [new TextComponent($"\n### {container.Header}")],
                UseBotThumbnail: true)));
            return;
        }

        builder.AddComponent((IMessageComponentBuilder)textMapper.Map(new TextComponent($"## {container.Header}")));
    }

    /// <summary>
    /// Maps a child component that is allowed inside a Discord container.
    /// </summary>
    /// <param name="component">The container child component.</param>
    /// <returns>The Discord message component builder for the child.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the child component type is unsupported.</exception>
    private IMessageComponentBuilder MapChild(IUiComponent component)
    {
        if (sectionMapper.CanMap(component))
            return (IMessageComponentBuilder)sectionMapper.Map(component);

        if (textMapper.CanMap(component))
            return (IMessageComponentBuilder)textMapper.Map(component);

        if (separatorMapper.CanMap(component))
            return (IMessageComponentBuilder)separatorMapper.Map(component);

        if (actionRowMapper.CanMap(component))
            return (IMessageComponentBuilder)actionRowMapper.Map(component);

        throw new InvalidOperationException($"Unsupported container component {component.GetType().Name}.");
    }

    /// <summary>
    /// Resolves the current bot avatar URL used by headers with bot thumbnails.
    /// </summary>
    /// <returns>The display or default bot avatar URL, or <see langword="null" /> when unavailable.</returns>
    private string? GetBotAvatarUrl() =>
        client.CurrentUser?.GetDisplayAvatarUrl()
        ?? client.CurrentUser?.GetDefaultAvatarUrl();

    /// <summary>
    /// Builds the standard footer text appended to panel containers.
    /// </summary>
    /// <returns>Markdown footer content containing project, author, and version information.</returns>
    private string BuildFooterText()
    {
        var githubIcon = emotes.GetEmote("UI_ICON_GITHUB_WHITE");
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        var major = version?.Major ?? 0;
        var minor = version?.Minor ?? 1;
        var build = version?.Build ?? 0;

        var sb = new StringBuilder();
        sb.Append($"""
            -# [Zabbix-Discord Bridge](https://github.com/Sh00ter0/zdb)
            -# Copyright (c) 2026 - {githubIcon}**[Sh00ter0](https://github.com/Sh00ter0)**
            -# `v{major}.{minor}.{build}`
            """);
        return sb.ToString();
    }
}
