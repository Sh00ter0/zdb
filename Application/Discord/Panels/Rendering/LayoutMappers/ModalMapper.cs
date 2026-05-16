using Application.Discord.Panels.Core;
using Application.Discord.Panels.Core.Layout;
using Discord;
using LayoutModalComponent = Application.Discord.Panels.Core.Layout.ModalComponent;

namespace Application.Discord.Panels.Rendering.LayoutMappers;

/// <summary>
/// Maps declarative modal components into Discord modal builders.
/// </summary>
public sealed class ModalMapper(IInteractionCodec codec) : ILayoutComponentMapper
{
    /// <inheritdoc />
    public bool CanMap(IUiComponent component) => component is LayoutModalComponent;

    /// <inheritdoc />
    public object Map(IUiComponent component)
    {
        var modal = (LayoutModalComponent)component;
        var builder = new ModalBuilder()
            .WithTitle(modal.Title)
            .WithCustomId(codec.Encode(modal.Action.ToInteraction()));

        foreach (var input in modal.Inputs)
        {
            builder.AddTextInput(
                label: input.Label,
                customId: input.CustomId,
                style: input.IsParagraph ? TextInputStyle.Paragraph : TextInputStyle.Short,
                placeholder: input.Placeholder,
                required: input.Required,
                maxLength: input.MaxLength);
        }

        return builder.Build();
    }
}
