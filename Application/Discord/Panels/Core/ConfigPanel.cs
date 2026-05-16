using System.Threading.Tasks;

namespace Application.Discord.Panels.Core;

/// <summary>
/// Provides a strongly typed base class for panels that build and handle declarative view states.
/// </summary>
/// <typeparam name="TState">The default state type produced by the panel.</typeparam>
public abstract class ConfigPanel<TState> : IConfigPanel where TState : IPanelViewState
{
    /// <inheritdoc />
    public abstract string Id { get; }

    /// <summary>
    /// Builds the default state for this panel from the current interaction context.
    /// </summary>
    /// <param name="context">The current panel context.</param>
    /// <returns>The default state for this panel.</returns>
    public abstract Task<TState> BuildStateAsync(ConfigPanelContext context);

    /// <inheritdoc />
    async Task<IPanelViewState> IConfigPanel.BuildStateAsync(ConfigPanelContext context)
    {
        return await BuildStateAsync(context);
    }

    /// <inheritdoc />
    public abstract Task<PanelActionResult> ExecuteActionAsync(ConfigPanelContext context, PanelInteraction interaction);
}
