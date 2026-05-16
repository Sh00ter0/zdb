using Application.Discord.Panels.Core;
using Application.Discord.Panels.Core.Layout;

namespace Application.Discord.Panels.LayoutBuilders;

/// <summary>
/// Builds a declarative panel layout for a specific panel view state.
/// </summary>
/// <typeparam name="TState">The state type supported by this layout builder.</typeparam>
public interface IPanelLayoutBuilder<in TState>
    where TState : IPanelViewState
{
    /// <summary>
    /// Converts the supplied view state into a transport-neutral panel layout.
    /// </summary>
    /// <param name="state">The state that contains all data required by the layout.</param>
    /// <returns>A panel layout that can be mapped to Discord components.</returns>
    PanelLayout Build(TState state);
}
