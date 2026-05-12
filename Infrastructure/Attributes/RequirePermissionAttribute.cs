using Discord;
using Discord.Interactions;
using Infrastructure.Discord.SlashCommands;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Attributes
{
    public class RequirePermissionAttribute : PreconditionAttribute
    {
        private readonly string _requiredPermission;

        public RequirePermissionAttribute(string requiredPermission)
        {
            _requiredPermission = requiredPermission;
        }

        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            // 1. Rzutujemy standardowy kontekst Discorda na nasz z rozszerzonymi danymi
            if (context is not AppInteractionContext appContext)
            {
                return PreconditionResult.FromError("Critical Error: Invalid interaction context type.");
            }

            var admin = appContext.Admin;

            if (admin == null)
            {
                return PreconditionResult.FromError("You have not been recognized as a system administrator.");
            }

            if (!admin.IsActive)
            {
                return PreconditionResult.FromError("Your administrator account is currently disabled.");
            }

            using var scope = services.CreateScope();
            var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApiSecurityDbContext>>();
            await using var db = await dbContextFactory.CreateDbContextAsync();

            var userPermissions = await db.SystemAdministrators
                .AsNoTracking()
                .Where(a => a.Id == admin.Id)
                .SelectMany(a => a.Role.RolePermissions)
                .Select(rp => rp.Permission.Key)
                .ToListAsync();

            // 5. Weryfikacja: Short-Circuit Logic (God Mode)
            if (userPermissions.Contains("root"))
            {
                return PreconditionResult.FromSuccess();
            }

            // 6. Weryfikacja docelowego uprawnienia
            if (userPermissions.Contains(_requiredPermission))
            {
                return PreconditionResult.FromSuccess();
            }

            return PreconditionResult.FromError($"You do not have the required permission (`{_requiredPermission}`) to perform this action.");
        }
    }
}
