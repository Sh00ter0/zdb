using Application.Views.Components;
using Discord;

namespace Infrastructure.Views.Components;

public class CbActionSection(Action<ContainerBuilder> action) : IViewSection
{
    public IViewSection Build(ContainerBuilder builder)
    {
        builder.WithSeparator(SeparatorSpacingSize.Small, false);
        action.Invoke(builder);
        return this;
    }
}