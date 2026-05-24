using Discord;

namespace Application.Views.Components;

public class ActionSection(IInteractableComponentBuilder[] actions) : IViewSection
{
    public IViewSection Build(ContainerBuilder builder)
    {
        builder.WithSeparator(SeparatorSpacingSize.Small, false);
        foreach (var action in actions)
        {
            builder.WithActionRow(row => row.AddComponent(action));
        }
        return this;
    }
}