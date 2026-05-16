namespace Application.Discord.Panels.Core;

/// <summary>
/// Encodes and decodes panel interactions for Discord component custom ids.
/// </summary>
public interface IInteractionCodec
{
    /// <summary>
    /// Encodes a panel interaction into a Discord custom id.
    /// </summary>
    /// <param name="interaction">The interaction to encode.</param>
    /// <returns>A compact custom id string.</returns>
    string Encode(PanelInteraction interaction);

    /// <summary>
    /// Decodes a Discord custom id into a panel interaction.
    /// </summary>
    /// <param name="customId">The custom id to decode.</param>
    /// <returns>The decoded panel interaction.</returns>
    PanelInteraction Decode(string customId);
}
