using Application.Discord.Panels.Modals;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Discord.Panels.Core.Orchestration;

/// <summary>
/// Resolves modal factories and builds Discord modals for open-modal results.
/// </summary>
public sealed class ModalCoordinator(IEnumerable<IModalFactory> factories)
{
    /// <summary>
    /// Builds a Discord modal for the requested modal type.
    /// </summary>
    /// <param name="result">The modal result returned by a panel action handler.</param>
    /// <returns>The Discord modal to return to the interaction.</returns>
    public Modal BuildModal(OpenModalResult result)
    {
        var factory = factories.FirstOrDefault(x => x.CanCreate(result.ModalType))
            ?? throw new InvalidOperationException($"No modal factory found for type: {result.ModalType}");

        return factory.Create(result);
    }
}
