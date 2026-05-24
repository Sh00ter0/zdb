using System.Reflection;
using System.Text;
using Application.Services.Discord;
using Application.Views.Components;
using Application.Views.Layouts;
using Discord;
using Discord.WebSocket;
using Domain.Constants;

namespace Infrastructure.Views.Layouts;

public sealed class StandardLayout(IDiscordEmoteService emotes, DiscordSocketClient client) : ILayout
{
    private string _title = string.Empty;
    private List<IViewSection> Sections { get; } = [];
    private SeparatorSpacingSize Spacing { get; set; } = SeparatorSpacingSize.Large;
    private Color _accentColor = new Color(AppColors.Info);

    private readonly ContainerBuilder _container = new();

    public ILayout Create(string title)
    {
        _title = title;
        return this;
    }

    public MessageComponent Build()
    {
        _container.WithAccentColor(_accentColor);
        
        AddHeader();
        
        Sections.ForEach(section => section.Build(_container));
        
        AddFooter();
        
        return new ComponentBuilderV2().WithContainer(_container).Build();
    }

    public ILayout WithAccentColor(uint color)
    {
        _accentColor = color;
        return this;
    }

    public ILayout WithSpacing(SeparatorSpacingSize spacing)
    {
        Spacing = spacing;
        return this;
    }

    public ILayout AddSection(IViewSection section)
    {
        Sections.Add(section);
        return this;
    }

    public ILayout AddSections(IViewSection[] section)
    {
        Sections.AddRange(section);
        return this;
    }

    private void AddHeader()
    {
        _container.WithSection(
            [new TextDisplayBuilder($"‎‎‎\n### {_title}")],
                       new ThumbnailBuilder(client.CurrentUser.GetDisplayAvatarUrl())
        );
    }

    private void AddFooter()
    {
        _container.WithSeparator(Spacing);
        _container.WithTextDisplay(BuildFooterText());
    }

    private string BuildFooterText()
    {
        var githubIcon = emotes.GetEmote("UI_ICON_GITHUB_WHITE");
        var appAuthor = $"{githubIcon}**[Sh00ter0](https://github.com/Sh00ter0)**";
        
        Version? version = Assembly.GetEntryAssembly()?.GetName().Version;
        var major = version?.Major ?? 0;
        var minor = version?.Minor ?? 1;
        var build = version?.Build ?? 0;

        var sb = new StringBuilder();
        sb.Append($"""
                   -# [Zabbix-Discord Bridge](https://github.com/Sh00ter0/zdb)
                   -# Copyright (c) 2026 — {appAuthor}
                   -# `v{major}.{minor}.{build}`
                   """);
        return sb.ToString();
    }
}