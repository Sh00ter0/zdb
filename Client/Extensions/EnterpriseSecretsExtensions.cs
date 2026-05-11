using Client.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using VaultSharp.Extensions.Configuration;
using VaultSharp.V1.AuthMethods.AppRole;

namespace Client.Extensions;

public static class EnterpriseSecretsExtensions
{
    public static WebApplicationBuilder AddEnterpriseSecrets(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddEnvironmentVariables(prefix: "DZB_");

        var providerString = builder.Configuration.GetValue<string>("SECRET_PROVIDER", "Local");

        if (!Enum.TryParse<SecretProviderType>(providerString, true, out var providerType))
        {
            Log.Warning("Unknown secret provider '{Provider}'. Falling back to Local settings.", providerString);
            providerType = SecretProviderType.Local;
        }

        Log.Information("Secret Management Engine: Initializing '{ProviderType}' provider...", providerType);

        switch (providerType)
        {
            case SecretProviderType.Local:
                Log.Information("Using Local/Environment based secret provider.");
                break;

            case SecretProviderType.HashiCorpVault:
                ApplyVaultConfiguration(builder);
                break;

            default:
                throw new InvalidOperationException($"Unsupported secret provider: {providerType}");
        }

        return builder;
    }

    private static void ApplyVaultConfiguration(WebApplicationBuilder builder)
    {
        var config = builder.Configuration;

        var vaultAddr = config["VAULT_ADDR"];
        var roleId = config["VAULT_ROLE_ID"];
        var secretId = config["VAULT_SECRET_ID"];

        var mountPoint = config["VAULT_MOUNT_POINT"];
        var rootPath = config["VAULT_ROOT_PATH"];

        var bypassSslRaw = config["VAULT_BYPASS_SSL"] ?? "false";
        var bypassSsl = bypassSslRaw.Equals("true", StringComparison.OrdinalIgnoreCase);

        ValidateVaultConfiguration(vaultAddr, roleId, secretId, mountPoint, rootPath);

        try
        {
            if (bypassSsl)
            {
                Log.Warning("SECURITY WARNING: Vault SSL certificate validation is DISABLED (Bypass active).");
            }

            Log.Information(
                "Connecting to Vault at '{VaultAddress}' using mount '{MountPoint}' and root path '{RootPath}'.",
                vaultAddr, mountPoint, rootPath);

            var vaultOptions = new VaultOptions(
                vaultAddr!,
                new AppRoleAuthMethodInfo(roleId!, secretId!),
                insecureConnection: bypassSsl);

            builder.Configuration.AddVaultConfiguration(
                () => vaultOptions,
                rootPath!,
                mountPoint);

            Log.Information("HashiCorp Vault configuration provider successfully initialized.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Vault initialization failed: {ex.GetBaseException().Message}", ex);
        }
    }

    private static void ValidateVaultConfiguration(
        string? vaultAddress,
        string? roleId,
        string? secretId,
        string? mountPoint,
        string? rootPath)
    {
        var missingVariables = new List<string>();

        if (string.IsNullOrWhiteSpace(vaultAddress)) missingVariables.Add("DZB_VAULT_ADDR");
        if (string.IsNullOrWhiteSpace(roleId)) missingVariables.Add("DZB_VAULT_ROLE_ID");
        if (string.IsNullOrWhiteSpace(secretId)) missingVariables.Add("DZB_VAULT_SECRET_ID");
        if (string.IsNullOrWhiteSpace(mountPoint)) missingVariables.Add("DZB_VAULT_MOUNT_POINT");
        if (string.IsNullOrWhiteSpace(rootPath)) missingVariables.Add("DZB_VAULT_ROOT_PATH");

        if (missingVariables.Count > 0)
        {
            throw new InvalidOperationException(
                "Vault provider is enabled but required configuration values are missing: " +
                string.Join(", ", missingVariables));
        }
    }
}