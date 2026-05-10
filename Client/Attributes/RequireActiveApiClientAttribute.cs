using Client.Data.Repositories;
using Client.Security;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Client.Attributes
{
    public class RequireActiveApiClientAttribute : ParameterPreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(
            IInteractionContext context,
            IParameterInfo parameterInfo,
            object value,
            IServiceProvider services)
        {
            if (value is not long apiId)
            {
                return PreconditionResult.FromError("Critical error: Invalid API ID parameter type.");
            }

            var apiClientRepository = services.GetRequiredService<IApiClientRepository>();

            var isActive = await apiClientRepository.IsActiveAsync(apiId);

            if (!isActive)
            {
                return PreconditionResult.FromError("The API client associated with this action no longer exists or has been disabled.");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}
