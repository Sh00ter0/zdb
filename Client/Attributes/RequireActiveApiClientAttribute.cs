using Application.Repositories;
using Discord;
using Discord.Interactions;

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

            var apiClientRepository = services.GetRequiredService<IIntegrationClientRepository>();

            var isActive = await apiClientRepository.IsActiveAsync(apiId);

            if (!isActive)
            {
                return PreconditionResult.FromError("The API client associated with this action no longer exists or has been disabled.");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}
