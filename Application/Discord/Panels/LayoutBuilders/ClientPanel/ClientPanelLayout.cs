using Application.Discord.Panels.ClientPanel.Actions;
using Application.Discord.Panels.Core.Layout;

namespace Application.Discord.Panels.LayoutBuilders.ClientPanel;

/// <summary>
/// Provides shared layout helpers used by the client management panel screens.
/// </summary>
internal static class ClientPanelLayout
{
    private const string PanelId = "client";

    /// <summary>
    /// Creates an action descriptor scoped to the client management panel.
    /// </summary>
    /// <param name="action">The action identifier handled by the client panel.</param>
    /// <param name="clientId">The client identifier carried by the interaction payload.</param>
    /// <returns>A descriptor that can be encoded into a Discord component custom id.</returns>
    public static PanelActionDescriptor Action(string action, long clientId) =>
        new(PanelId, action, clientId.ToString());

    /// <summary>
    /// Creates the standard client panel container with a header, body, optional controls, and footer.
    /// </summary>
    /// <param name="header">The container header displayed above the body.</param>
    /// <param name="body">The main Markdown body of the panel screen.</param>
    /// <param name="accentColor">The Discord container accent color.</param>
    /// <param name="controls">Optional interactive controls appended below the body.</param>
    /// <param name="footerSeparatorSize">The separator spacing used before the standard footer.</param>
    /// <returns>A reusable client panel container component.</returns>
    public static ContainerComponent StandardContainer(
        string header,
        string body,
        uint? accentColor,
        IReadOnlyList<IUiComponent>? controls = null,
        SeparatorSize footerSeparatorSize = SeparatorSize.Large)
    {
        var components = new List<IUiComponent>
        {
            new SeparatorComponent(SeparatorSize.Large),
            new TextComponent(body)
        };

        if (controls is { Count: > 0 })
        {
            components.Add(new SeparatorComponent(SeparatorSize.Small, IsDivider: false));
            components.AddRange(controls);
        }

        return new ContainerComponent(
            Header: header,
            Components: components,
            AccentColor: accentColor,
            FooterSeparatorSize: footerSeparatorSize);
    }

    /// <summary>
    /// Creates the standard return button that navigates back to the client overview screen.
    /// </summary>
    /// <param name="clientId">The client identifier that should be preserved during navigation.</param>
    /// <param name="label">The button label displayed to the user.</param>
    /// <returns>A secondary button wired to the client panel back action.</returns>
    public static ButtonComponent ReturnButton(long clientId, string label = "Return") =>
        new(
            Label: label,
            Action: Action(ClientPanelActionIds.Back, clientId),
            Style: ButtonStyleType.Secondary);
}
