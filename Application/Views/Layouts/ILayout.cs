using Application.Views.Components;
using Discord;

namespace Application.Views.Layouts;

public interface ILayout
{
    ILayout Create(string title);
    MessageComponent Build();
    ILayout AddSection(IViewSection section);
    ILayout AddSections(IViewSection[] section);
    ILayout WithAccentColor(uint color);
    ILayout WithSpacing(SeparatorSpacingSize spacing);
    ILayout WithFooter(string footer);
}