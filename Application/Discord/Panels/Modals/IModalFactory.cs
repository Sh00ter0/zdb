using Application.Discord.Panels.Core;
using Discord;

namespace Application.Discord.Panels.Modals;

/// <summary>
/// Builds Discord modals for logical modal result types.
/// </summary>
public interface IModalFactory
{
    /// <summary>
    /// Determines whether this factory can build the requested modal type.
    /// </summary>
    /// <param name="modalType">The logical modal type returned by an action handler.</param>
    /// <returns><see langword="true"/> when this factory supports the modal type.</returns>
    bool CanCreate(string modalType);

    /// <summary>
    /// Creates a Discord modal for the supplied open-modal result.
    /// </summary>
    /// <param name="result">The result describing the modal to open.</param>
    /// <returns>The Discord modal to return to the user.</returns>
    Modal Create(OpenModalResult result);
}
