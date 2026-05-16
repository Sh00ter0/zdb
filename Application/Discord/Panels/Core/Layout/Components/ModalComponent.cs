namespace Application.Discord.Panels.Core.Layout;

/// <summary>
/// Describes a Discord modal in the declarative layout model.
/// </summary>
/// <param name="Title">The modal title.</param>
/// <param name="Action">The submit action encoded into the modal custom id.</param>
/// <param name="Inputs">The text inputs displayed by the modal.</param>
public sealed record ModalComponent(
    string Title,
    PanelActionDescriptor Action,
    IReadOnlyList<ModalInputComponent> Inputs) : IUiComponent;

/// <summary>
/// Describes a text input displayed inside a Discord modal.
/// </summary>
/// <param name="CustomId">The Discord custom id for this input.</param>
/// <param name="Label">The visible input label.</param>
/// <param name="Placeholder">The optional placeholder text.</param>
/// <param name="MaxLength">The maximum accepted text length.</param>
/// <param name="IsParagraph">Whether the input should use paragraph style.</param>
/// <param name="Required">Whether Discord should require a value.</param>
public sealed record ModalInputComponent(
    string CustomId,
    string Label,
    string? Placeholder = null,
    int MaxLength = 100,
    bool IsParagraph = false,
    bool Required = true);
