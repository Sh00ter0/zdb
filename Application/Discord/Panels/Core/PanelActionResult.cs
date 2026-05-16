namespace Application.Discord.Panels.Core;

/// <summary>
/// Base type for all intents returned by panel action handlers.
/// </summary>
public abstract class PanelActionResult
{
}

/// <summary>
/// Requests that the current panel message be re-rendered with a new view state.
/// </summary>
public sealed class UpdatePanelResult : PanelActionResult
{
    /// <summary>
    /// Gets the view state that should be rendered into the panel response.
    /// </summary>
    public required IPanelViewState State { get; init; }

    /// <summary>
    /// Gets an optional ephemeral follow-up message shown after the panel update.
    /// </summary>
    public string? ToastMessage { get; init; }
}

/// <summary>
/// Requests that Discord opens a modal for the current interaction.
/// </summary>
public sealed class OpenModalResult : PanelActionResult
{
    /// <summary>
    /// Gets the logical modal type used to resolve an <see cref="Modals.IModalFactory"/>.
    /// </summary>
    public required string ModalType { get; init; }

    /// <summary>
    /// Gets the domain entity identifier that should be encoded into the modal submit action.
    /// </summary>
    public required string EntityId { get; init; }
}

/// <summary>
/// Requests that the current panel message be closed or cleared.
/// </summary>
public sealed class ClosePanelResult : PanelActionResult
{
    /// <summary>
    /// Gets an optional ephemeral follow-up message shown after the panel is closed.
    /// </summary>
    public string? ToastMessage { get; init; }
}
