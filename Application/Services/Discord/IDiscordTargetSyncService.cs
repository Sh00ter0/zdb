using Discord;
using Domain.Entities;

namespace Application.Services.Discord
{
    public interface IDiscordTargetSyncService
    {
        Task<KnownDeliveryTargets?> VerifyAndUpdateTargetAsync(KnownDeliveryTargets dbTarget, IChannel? resolvedChannel, IUser? resolvedUser);
    }
}
