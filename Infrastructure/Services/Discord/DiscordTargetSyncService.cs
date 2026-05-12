using Application.Repositories;
using Application.Services.Discord;
using Discord;
using Discord.WebSocket;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Discord
{

    public class DiscordTargetSyncService : IDiscordTargetSyncService
    {
        private readonly IKnownDeliveryTargetRepository _targetRepository;
        private readonly ILogger<DiscordTargetSyncService> _logger;

        public DiscordTargetSyncService(
            IKnownDeliveryTargetRepository targetRepository,
            ILogger<DiscordTargetSyncService> logger)
        {
            _targetRepository = targetRepository;
            _logger = logger;
        }

        public async Task<KnownDeliveryTargets?> VerifyAndUpdateTargetAsync(KnownDeliveryTargets dbTarget, IChannel? resolvedChannel, IUser? resolvedUser)
        {
            bool requiresUpdate = false;
            bool requiresRemoval = false;
            TextChannelType actualType = TextChannelType.Unknown;
            ulong? actualGuildId = null;

            // 1. Analiza aktualnego stanu z Discorda
            if (resolvedUser != null)
            {
                actualType = TextChannelType.DirectMessage;
            }
            else if (resolvedChannel != null)
            {
                if (resolvedChannel is IGuildChannel guildChannel)
                {
                    actualGuildId = guildChannel.GuildId;
                }

                if (resolvedChannel is INewsChannel)
                {
                    actualType = TextChannelType.GuildAnnouncementChannel;
                }
                else if (resolvedChannel is SocketThreadChannel socketThread)
                {
                    if (socketThread.ParentChannel is IForumChannel) actualType = TextChannelType.GuildForumThreadChannel;
                    else if (socketThread.Type == ThreadType.PrivateThread) actualType = TextChannelType.GuildPrivateThreadChannel;
                    else actualType = TextChannelType.GuildPublicThreadChannel;
                }
                else if (resolvedChannel is IThreadChannel thread)
                {
                    if (dbTarget.ChannelType == TextChannelType.GuildForumThreadChannel ||
                        dbTarget.ChannelType == TextChannelType.GuildPrivateThreadChannel ||
                        dbTarget.ChannelType == TextChannelType.GuildPublicThreadChannel)
                    {
                        actualType = dbTarget.ChannelType;
                    }
                    else
                    {
                        actualType = TextChannelType.GuildPublicThreadChannel;
                    }
                }
                else if (resolvedChannel is IForumChannel)
                {
                    requiresRemoval = true;
                }
                else if (resolvedChannel is ITextChannel and not IVoiceChannel and not IStageChannel)
                {
                    actualType = TextChannelType.GuildTextChannel;
                }
                else if (resolvedChannel is IVoiceChannel and not IStageChannel)
                {
                    actualType = TextChannelType.GuildVoiceTextChannel;
                }
                else if (resolvedChannel is IStageChannel)
                {
                    actualType = TextChannelType.GuildStageVoiceTextChannel;
                }
            }

            if (requiresRemoval)
            {
                var success = await _targetRepository.DeleteByIdAsync(dbTarget.IntegrationClientId, dbTarget.Id);

                if (success)
                {
                    _logger.LogInformation("Target {TargetId} removed due to unsupported channel type (e.g. Forum).", dbTarget.TargetId);
                    return null;
                }

                _logger.LogError("Failed to remove target {TargetId} due to unsupported channel type.", dbTarget.TargetId);
                return dbTarget;
            }

            if (dbTarget.ChannelType != actualType)
            {
                _logger.LogInformation("Target type drift detected for ID {TargetId}. DB: {Db}, Discord: {Actual}", dbTarget.TargetId, dbTarget.ChannelType, actualType);
                dbTarget.ChannelType = actualType;
                requiresUpdate = true;
            }

            if (dbTarget.AssociatedGuildId != actualGuildId)
            {
                dbTarget.AssociatedGuildId = actualGuildId;
                requiresUpdate = true;
            }

            if (requiresUpdate)
            {
                var success = await _targetRepository.UpdateAsync(dbTarget);

                if (success)
                    _logger.LogInformation("Successfully synced target {TargetId} with current Discord state.", dbTarget.TargetId);
                else
                    _logger.LogError("Failed to update target {TargetId} during sync via repository.", dbTarget.TargetId);
            }

            return dbTarget;
        }
    }
}
