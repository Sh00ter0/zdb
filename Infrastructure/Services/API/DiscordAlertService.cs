using Application.Common.Zabbix;
using Application.Exceptions.API;
using Application.Repositories;
using Discord;
using Discord.WebSocket;
using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services.API
{
    public class DiscordAlertService(
        IKnownDeliveryTargetRepository targetRepository,
        DiscordSocketClient client,
        ZabbixAlertUiBuilder alertUiBuilder)
    {
        public async Task ProcessAlertAsync(long clientId, ulong targetId, ZabbixPayload payload)
        {
            var channel = await ResolveTargetChannelAsync(targetId);
            if (channel == null)
            {
                throw new ProblemException("Invalid Target", "Provided target id is not a valid channel.", StatusCodes.Status400BadRequest);
            }

            var component = alertUiBuilder.CreateAlertContainer(payload, false, clientId);
            var message = await SendAlertAsync(channel, component);

            var targetData = await targetRepository.GetByDiscordIdAsync(clientId, targetId);
            if (targetData != null && targetData.ChannelType == TextChannelType.GuildAnnouncementChannel && targetData.AutoCrosspost)
            {
                await HandleCrosspostAsync(message);
            }
        }

        private async Task<IMessageChannel?> ResolveTargetChannelAsync(ulong targetId)
        {
            var guildChannel = await client.Rest.GetChannelAsync(targetId);
            if (guildChannel is IMessageChannel) return (IMessageChannel)guildChannel;

            var user = await client.Rest.GetUserAsync(targetId);
            if (user is null) return null;

            var dm = await user.CreateDMChannelAsync();
            return dm;
        }

        private async Task HandleCrosspostAsync(IUserMessage message)
        {
            try
            {
                await message.CrosspostAsync();
            }
            catch (Exception)
            {
                throw new ProblemException("Crosspost error", $"Encountered error while trying to crosspost alert for target {message.Channel.Id}", StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<IUserMessage> SendAlertAsync(IMessageChannel channel, MessageComponent message)
        {
            try
            {
                return await channel.SendMessageAsync(components: message, flags: MessageFlags.ComponentsV2);
            }
            catch (Exception)
            {
                throw new ProblemException("Sending error", $"Encountered error while trying to send alert to {channel.Id}", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
